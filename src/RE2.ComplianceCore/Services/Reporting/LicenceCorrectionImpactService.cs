using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Services.Reporting;

/// <summary>
/// Service for analyzing the impact of licence corrections on historical transactions.
/// T163a-T163b: Implements SC-038 historical validation report.
/// When a licence's effective dates are corrected, this service identifies transactions
/// that may have had different compliance outcomes under the corrected licence data.
/// </summary>
public class LicenceCorrectionImpactService : ILicenceCorrectionImpactService
{
    private readonly ILicenceRepository _licenceRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<LicenceCorrectionImpactService> _logger;

    public LicenceCorrectionImpactService(
        ILicenceRepository licenceRepository,
        ITransactionRepository transactionRepository,
        ICustomerRepository customerRepository,
        ILogger<LicenceCorrectionImpactService> logger)
    {
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes the impact of a licence correction on historical transactions.
    /// T163b: Query transactions where licence effective dates overlap transaction dates,
    /// re-validate each transaction under corrected licence data.
    /// </summary>
    /// <param name="criteria">Impact analysis criteria including licence ID and correction details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report showing affected transactions with original vs. corrected compliance status.</returns>
    public async Task<LicenceCorrectionImpactReport?> AnalyzeImpactAsync(
        LicenceCorrectionImpactCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing licence correction impact for licence {LicenceId}, correction date: {CorrectionDate}",
            criteria.LicenceId, criteria.CorrectionDate);

        // Get the licence
        var licence = await _licenceRepository.GetByIdAsync(criteria.LicenceId, cancellationToken);
        if (licence == null)
        {
            _logger.LogWarning("Licence {LicenceId} not found", criteria.LicenceId);
            return null;
        }

        // Determine the analysis window
        var analysisFromDate = criteria.OriginalEffectiveDate ?? licence.IssueDate.ToDateTime(TimeOnly.MinValue);
        var analysisToDate = criteria.CorrectionDate;

        // If we're correcting to make the licence valid earlier, we need to look at transactions
        // from before the original effective date
        if (criteria.CorrectedEffectiveDate.HasValue &&
            criteria.OriginalEffectiveDate.HasValue &&
            criteria.CorrectedEffectiveDate < criteria.OriginalEffectiveDate)
        {
            analysisFromDate = criteria.CorrectedEffectiveDate.Value;
        }

        // Get the licence holder (customer) to find relevant transactions
        IEnumerable<Transaction> transactions;
        if (licence.HolderType == "Customer")
        {
            // Look up the customer by ComplianceExtensionId (the licence's HolderId)
            // to get the CustomerAccount/DataAreaId composite key
            var allCustomers = await _customerRepository.GetAllAsync(cancellationToken);
            var holderCustomer = allCustomers.FirstOrDefault(c => c.ComplianceExtensionId == licence.HolderId);

            if (holderCustomer != null)
            {
                transactions = await _transactionRepository.GetCustomerTransactionsInPeriodAsync(
                    holderCustomer.CustomerAccount, holderCustomer.DataAreaId,
                    analysisFromDate, analysisToDate, cancellationToken);
            }
            else
            {
                // Customer not found by ComplianceExtensionId; fall back to all transactions in period
                _logger.LogWarning("Customer with ComplianceExtensionId {HolderId} not found, analyzing all transactions in period", licence.HolderId);
                transactions = await _transactionRepository.GetByDateRangeAsync(
                    analysisFromDate, analysisToDate, cancellationToken);
            }
        }
        else
        {
            // For company-level licences, get all transactions in the period
            transactions = await _transactionRepository.GetByDateRangeAsync(
                analysisFromDate, analysisToDate, cancellationToken);
        }

        var transactionList = transactions.ToList();
        _logger.LogInformation("Found {Count} transactions to analyze", transactionList.Count);

        // Analyze each transaction
        var impactItems = new List<LicenceCorrectionImpactItem>();

        foreach (var transaction in transactionList)
        {
            var impactItem = await AnalyzeTransactionImpactAsync(
                transaction, licence, criteria, cancellationToken);

            if (impactItem != null)
            {
                impactItems.Add(impactItem);
            }
        }

        // Build the report
        var report = new LicenceCorrectionImpactReport
        {
            GeneratedDate = DateTime.UtcNow,
            LicenceId = criteria.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            CorrectionDate = criteria.CorrectionDate,
            OriginalEffectiveDate = criteria.OriginalEffectiveDate,
            CorrectedEffectiveDate = criteria.CorrectedEffectiveDate,
            OriginalExpiryDate = criteria.OriginalExpiryDate,
            CorrectedExpiryDate = criteria.CorrectedExpiryDate,
            AnalysisFromDate = analysisFromDate,
            AnalysisToDate = analysisToDate,
            AffectedTransactions = impactItems,
            TotalTransactionsAnalyzed = transactionList.Count,
            TotalAffectedTransactions = impactItems.Count,
            CriticalImpactCount = impactItems.Count(i => i.ImpactSeverity == ImpactSeverity.Critical),
            MajorImpactCount = impactItems.Count(i => i.ImpactSeverity == ImpactSeverity.Major),
            MinorImpactCount = impactItems.Count(i => i.ImpactSeverity == ImpactSeverity.Minor)
        };

        _logger.LogInformation(
            "Licence correction impact analysis complete: {Affected} of {Total} transactions affected " +
            "(Critical: {Critical}, Major: {Major}, Minor: {Minor})",
            report.TotalAffectedTransactions, report.TotalTransactionsAnalyzed,
            report.CriticalImpactCount, report.MajorImpactCount, report.MinorImpactCount);

        return report;
    }

    /// <summary>
    /// Analyzes the impact of a licence correction on a single transaction.
    /// </summary>
    private async Task<LicenceCorrectionImpactItem?> AnalyzeTransactionImpactAsync(
        Transaction transaction,
        Licence licence,
        LicenceCorrectionImpactCriteria criteria,
        CancellationToken cancellationToken)
    {
        // Check if this transaction used the licence being corrected
        var usedLicence = transaction.LicenceUsages?.Any(u => u.LicenceId == criteria.LicenceId) ?? false;

        // Determine original compliance status
        var originalStatus = DetermineOriginalComplianceStatus(transaction, licence, criteria);

        // Determine corrected compliance status
        var correctedStatus = DetermineCorrectedComplianceStatus(transaction, licence, criteria);

        // If there's no change, don't include in the report
        if (originalStatus == correctedStatus)
        {
            return null;
        }

        // Get customer name
        string? customerName = null;
        var customer = await _customerRepository.GetByAccountAsync(transaction.CustomerAccount, transaction.CustomerDataAreaId, cancellationToken);
        if (customer != null)
        {
            customerName = customer.BusinessName;
        }

        // Determine impact severity
        var impactSeverity = DetermineImpactSeverity(originalStatus, correctedStatus);

        // Build impact details
        var details = BuildImpactDetails(transaction, licence, criteria, originalStatus, correctedStatus, usedLicence);

        return new LicenceCorrectionImpactItem
        {
            TransactionId = transaction.Id,
            ExternalTransactionId = transaction.ExternalId,
            TransactionDate = transaction.TransactionDate,
            CustomerAccount = transaction.CustomerAccount,
            CustomerDataAreaId = transaction.CustomerDataAreaId,
            CustomerName = customerName,
            OriginalStatus = originalStatus.ToString(),
            CorrectedStatus = correctedStatus.ToString(),
            ImpactSeverity = impactSeverity,
            ImpactDetails = details,
            RequiresReview = impactSeverity == ImpactSeverity.Critical || impactSeverity == ImpactSeverity.Major
        };
    }

    /// <summary>
    /// Determines the original compliance status based on transaction date and original licence dates.
    /// </summary>
    private ValidationStatus DetermineOriginalComplianceStatus(
        Transaction transaction,
        Licence licence,
        LicenceCorrectionImpactCriteria criteria)
    {
        // Use the actual recorded validation status for the original
        // This represents what the system determined at the time
        return transaction.ValidationStatus;
    }

    /// <summary>
    /// Determines what the compliance status would have been with corrected licence dates.
    /// </summary>
    private ValidationStatus DetermineCorrectedComplianceStatus(
        Transaction transaction,
        Licence licence,
        LicenceCorrectionImpactCriteria criteria)
    {
        var transactionDate = transaction.TransactionDate;
        var transactionDateOnly = DateOnly.FromDateTime(transactionDate);

        // Check if the licence would have been valid under the corrected dates
        var correctedEffective = criteria.CorrectedEffectiveDate ?? licence.IssueDate.ToDateTime(TimeOnly.MinValue);
        var correctedExpiry = criteria.CorrectedExpiryDate ?? licence.ExpiryDate;

        bool licenceWouldBeValid = true;

        // Check effective date
        var effectiveDateOnly = DateOnly.FromDateTime(correctedEffective);
        if (transactionDateOnly < effectiveDateOnly)
        {
            licenceWouldBeValid = false;
        }

        // Check expiry date
        if (correctedExpiry.HasValue && transactionDateOnly > correctedExpiry.Value)
        {
            licenceWouldBeValid = false;
        }

        // If the licence would now be valid, and the original status was failed/blocked,
        // the corrected status would be passed
        if (licenceWouldBeValid && transaction.ValidationStatus == ValidationStatus.Failed)
        {
            return ValidationStatus.Passed;
        }

        // If the licence would now be invalid, and the original status was passed,
        // the corrected status would be failed
        if (!licenceWouldBeValid && transaction.ValidationStatus == ValidationStatus.Passed)
        {
            return ValidationStatus.Failed;
        }

        // No change
        return transaction.ValidationStatus;
    }

    /// <summary>
    /// Determines the impact severity based on the change in compliance status.
    /// </summary>
    private ImpactSeverity DetermineImpactSeverity(
        ValidationStatus originalStatus,
        ValidationStatus correctedStatus)
    {
        // Transaction was approved but should have been blocked
        if ((originalStatus == ValidationStatus.Passed || originalStatus == ValidationStatus.ApprovedWithOverride) &&
            correctedStatus == ValidationStatus.Failed)
        {
            return ImpactSeverity.Critical; // Compliance breach
        }

        // Transaction was blocked but should have been approved
        if (originalStatus == ValidationStatus.Failed &&
            (correctedStatus == ValidationStatus.Passed || correctedStatus == ValidationStatus.ApprovedWithOverride))
        {
            return ImpactSeverity.Major; // Customer impact, but no compliance breach
        }

        // Override status changed
        if (originalStatus == ValidationStatus.ApprovedWithOverride &&
            correctedStatus == ValidationStatus.Passed)
        {
            return ImpactSeverity.Minor; // Override was unnecessary
        }

        return ImpactSeverity.Minor;
    }

    /// <summary>
    /// Builds a human-readable description of the impact.
    /// </summary>
    private string BuildImpactDetails(
        Transaction transaction,
        Licence licence,
        LicenceCorrectionImpactCriteria criteria,
        ValidationStatus originalStatus,
        ValidationStatus correctedStatus,
        bool usedLicence)
    {
        var details = new List<string>();

        details.Add($"Transaction date: {transaction.TransactionDate:yyyy-MM-dd}");
        details.Add($"Licence: {licence.LicenceNumber}");

        if (criteria.OriginalEffectiveDate.HasValue && criteria.CorrectedEffectiveDate.HasValue)
        {
            details.Add($"Effective date changed: {criteria.OriginalEffectiveDate:yyyy-MM-dd} → {criteria.CorrectedEffectiveDate:yyyy-MM-dd}");
        }

        if (criteria.OriginalExpiryDate.HasValue && criteria.CorrectedExpiryDate.HasValue)
        {
            details.Add($"Expiry date changed: {criteria.OriginalExpiryDate:yyyy-MM-dd} → {criteria.CorrectedExpiryDate:yyyy-MM-dd}");
        }

        details.Add($"Status change: {originalStatus} → {correctedStatus}");

        if (usedLicence)
        {
            details.Add("This licence was used to authorize the transaction");
        }

        if (originalStatus == ValidationStatus.Passed && correctedStatus == ValidationStatus.Failed)
        {
            details.Add("CRITICAL: Transaction was approved but licence was not valid at time of transaction");
        }
        else if (originalStatus == ValidationStatus.Failed && correctedStatus == ValidationStatus.Passed)
        {
            details.Add("Transaction was blocked but licence was actually valid at time of transaction");
        }

        return string.Join("; ", details);
    }
}

#region Impact Analysis Models

/// <summary>
/// Criteria for licence correction impact analysis.
/// </summary>
public class LicenceCorrectionImpactCriteria
{
    /// <summary>
    /// ID of the licence being corrected.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Date the correction was made.
    /// </summary>
    public DateTime CorrectionDate { get; set; }

    /// <summary>
    /// Original effective date before correction (if applicable).
    /// </summary>
    public DateTime? OriginalEffectiveDate { get; set; }

    /// <summary>
    /// Corrected effective date.
    /// </summary>
    public DateTime? CorrectedEffectiveDate { get; set; }

    /// <summary>
    /// Original expiry date before correction (if applicable).
    /// </summary>
    public DateOnly? OriginalExpiryDate { get; set; }

    /// <summary>
    /// Corrected expiry date.
    /// </summary>
    public DateOnly? CorrectedExpiryDate { get; set; }
}

/// <summary>
/// Report showing the impact of a licence correction on historical transactions.
/// </summary>
public class LicenceCorrectionImpactReport
{
    /// <summary>
    /// Date the report was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// ID of the corrected licence.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Licence number.
    /// </summary>
    public string LicenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date the correction was made.
    /// </summary>
    public DateTime CorrectionDate { get; set; }

    /// <summary>
    /// Original effective date (if changed).
    /// </summary>
    public DateTime? OriginalEffectiveDate { get; set; }

    /// <summary>
    /// Corrected effective date.
    /// </summary>
    public DateTime? CorrectedEffectiveDate { get; set; }

    /// <summary>
    /// Original expiry date (if changed).
    /// </summary>
    public DateOnly? OriginalExpiryDate { get; set; }

    /// <summary>
    /// Corrected expiry date.
    /// </summary>
    public DateOnly? CorrectedExpiryDate { get; set; }

    /// <summary>
    /// Start of analysis window.
    /// </summary>
    public DateTime AnalysisFromDate { get; set; }

    /// <summary>
    /// End of analysis window.
    /// </summary>
    public DateTime AnalysisToDate { get; set; }

    /// <summary>
    /// Transactions affected by the correction.
    /// </summary>
    public List<LicenceCorrectionImpactItem> AffectedTransactions { get; set; } = new();

    /// <summary>
    /// Total transactions analyzed.
    /// </summary>
    public int TotalTransactionsAnalyzed { get; set; }

    /// <summary>
    /// Total affected transactions.
    /// </summary>
    public int TotalAffectedTransactions { get; set; }

    /// <summary>
    /// Count of transactions with critical impact.
    /// </summary>
    public int CriticalImpactCount { get; set; }

    /// <summary>
    /// Count of transactions with major impact.
    /// </summary>
    public int MajorImpactCount { get; set; }

    /// <summary>
    /// Count of transactions with minor impact.
    /// </summary>
    public int MinorImpactCount { get; set; }
}

/// <summary>
/// Individual transaction impact item in the report.
/// </summary>
public class LicenceCorrectionImpactItem
{
    /// <summary>
    /// Internal transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// External transaction ID.
    /// </summary>
    public string ExternalTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Customer account number.
    /// </summary>
    public string CustomerAccount { get; set; } = string.Empty;

    /// <summary>
    /// Customer data area ID.
    /// </summary>
    public string CustomerDataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Customer name.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Original compliance status at time of transaction.
    /// </summary>
    public string OriginalStatus { get; set; } = string.Empty;

    /// <summary>
    /// What the compliance status would have been with corrected licence data.
    /// </summary>
    public string CorrectedStatus { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the impact.
    /// </summary>
    public ImpactSeverity ImpactSeverity { get; set; }

    /// <summary>
    /// Detailed description of the impact.
    /// </summary>
    public string ImpactDetails { get; set; } = string.Empty;

    /// <summary>
    /// Whether this transaction requires compliance team review.
    /// </summary>
    public bool RequiresReview { get; set; }
}

/// <summary>
/// Impact severity levels.
/// </summary>
public enum ImpactSeverity
{
    /// <summary>
    /// Minor impact - documentation update only.
    /// </summary>
    Minor = 0,

    /// <summary>
    /// Major impact - customer was incorrectly blocked.
    /// </summary>
    Major = 1,

    /// <summary>
    /// Critical impact - compliance breach (transaction should have been blocked).
    /// </summary>
    Critical = 2
}

#endregion

#region Interface

/// <summary>
/// Service interface for licence correction impact analysis.
/// T163a: Service for SC-038 historical validation report.
/// </summary>
public interface ILicenceCorrectionImpactService
{
    /// <summary>
    /// Analyzes the impact of a licence correction on historical transactions.
    /// </summary>
    /// <param name="criteria">Impact analysis criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impact report, or null if licence not found.</returns>
    Task<LicenceCorrectionImpactReport?> AnalyzeImpactAsync(
        LicenceCorrectionImpactCriteria criteria,
        CancellationToken cancellationToken = default);
}

#endregion
