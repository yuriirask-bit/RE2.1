using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.Reporting;

/// <summary>
/// Service for generating compliance reports.
/// T160-T163: Implements report generation per FR-026, FR-029.
/// Supports transaction audit reports, licence usage reports, and customer compliance history.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        IAuditRepository auditRepository,
        ITransactionRepository transactionRepository,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        ILogger<ReportingService> logger)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Transaction Audit Report (FR-026)

    /// <summary>
    /// Generates a transaction audit report per FR-026.
    /// T161: Supports filtering by substance, customer, and country.
    /// </summary>
    public async Task<TransactionAuditReport> GenerateTransactionAuditReportAsync(
        TransactionAuditReportCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating transaction audit report for {FromDate} to {ToDate}",
            criteria.FromDate, criteria.ToDate);

        IEnumerable<Transaction> transactions;

        // Filter by specific criteria
        if (!string.IsNullOrEmpty(criteria.SubstanceCode))
        {
            transactions = await _transactionRepository.GetBySubstanceAsync(
                criteria.SubstanceCode, criteria.FromDate, criteria.ToDate, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(criteria.CustomerAccount))
        {
            // Filter by customer account: get all transactions in range and filter by composite key
            var allTransactions = await _transactionRepository.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, cancellationToken);
            transactions = allTransactions.Where(t =>
                t.CustomerAccount.Equals(criteria.CustomerAccount, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(criteria.CustomerDataAreaId) ||
                 t.CustomerDataAreaId.Equals(criteria.CustomerDataAreaId, StringComparison.OrdinalIgnoreCase)));
        }
        else if (!string.IsNullOrEmpty(criteria.CountryCode))
        {
            transactions = await _transactionRepository.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, cancellationToken);
            // Filter by country would require additional customer lookup
            // For now, return all and filter in memory
            var countryFiltered = new List<Transaction>();
            foreach (var transaction in transactions)
            {
                var customer = await _customerRepository.GetByAccountAsync(transaction.CustomerAccount, transaction.CustomerDataAreaId, cancellationToken);
                if (customer?.Country?.Equals(criteria.CountryCode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    countryFiltered.Add(transaction);
                }
            }
            transactions = countryFiltered;
        }
        else
        {
            transactions = await _transactionRepository.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, cancellationToken);
        }

        var transactionList = transactions.ToList();

        // Build report items
        var reportItems = new List<TransactionAuditReportItem>();
        foreach (var transaction in transactionList)
        {
            var item = new TransactionAuditReportItem
            {
                TransactionId = transaction.Id,
                ExternalTransactionId = transaction.ExternalId,
                TransactionDate = transaction.TransactionDate,
                CustomerAccount = transaction.CustomerAccount,
                CustomerDataAreaId = transaction.CustomerDataAreaId,
                TransactionType = transaction.TransactionType.ToString(),
                ValidationStatus = transaction.ValidationStatus.ToString(),
                TotalQuantity = transaction.Lines?.Sum(l => l.Quantity) ?? 0
            };

            // Include licence details if requested
            if (criteria.IncludeLicenceDetails && transaction.LicenceUsages?.Any() == true)
            {
                var licenceDetails = new List<LicenceReportDetail>();
                foreach (var usage in transaction.LicenceUsages)
                {
                    var licence = await _licenceRepository.GetByIdAsync(usage.LicenceId, cancellationToken);
                    if (licence != null)
                    {
                        licenceDetails.Add(new LicenceReportDetail
                        {
                            LicenceId = licence.LicenceId,
                            LicenceNumber = licence.LicenceNumber,
                            Status = licence.Status,
                            IssuingAuthority = licence.IssuingAuthority
                        });
                    }
                }
                item.LicencesUsed = licenceDetails;
            }

            reportItems.Add(item);
        }

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = criteria.FromDate,
            ToDate = criteria.ToDate,
            Transactions = reportItems,
            TotalCount = reportItems.Count,
            FilteredBySubstanceCode = criteria.SubstanceCode,
            FilteredByCustomerAccount = criteria.CustomerAccount,
            FilteredByCustomerDataAreaId = criteria.CustomerDataAreaId,
            FilteredByCountry = criteria.CountryCode
        };

        _logger.LogInformation("Generated transaction audit report with {Count} transactions", report.TotalCount);

        return report;
    }

    #endregion

    #region Licence Usage Report (FR-026)

    /// <summary>
    /// Generates a licence usage report per FR-026.
    /// T162: Shows which licences were used in transactions.
    /// </summary>
    public async Task<LicenceUsageReport> GenerateLicenceUsageReportAsync(
        LicenceUsageReportCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating licence usage report for {FromDate} to {ToDate}",
            criteria.FromDate, criteria.ToDate);

        IEnumerable<Licence> licences;

        if (criteria.LicenceId.HasValue)
        {
            var licence = await _licenceRepository.GetByIdAsync(criteria.LicenceId.Value, cancellationToken);
            licences = licence != null ? new[] { licence } : Array.Empty<Licence>();
        }
        else if (criteria.HolderId.HasValue)
        {
            licences = await _licenceRepository.GetByHolderAsync(
                criteria.HolderId.Value, criteria.HolderType ?? "Customer", cancellationToken);
        }
        else
        {
            licences = await _licenceRepository.GetAllAsync(cancellationToken);
        }

        var licenceList = licences.ToList();

        // Get all transactions in the date range
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            criteria.FromDate, criteria.ToDate, cancellationToken);
        var transactionList = transactions.ToList();

        // Calculate usage for each licence
        var usageItems = new List<LicenceUsageReportItem>();
        foreach (var licence in licenceList)
        {
            var transactionsUsingLicence = transactionList
                .Where(t => t.LicenceUsages?.Any(u => u.LicenceId == licence.LicenceId) == true)
                .ToList();

            var item = new LicenceUsageReportItem
            {
                LicenceId = licence.LicenceId,
                LicenceNumber = licence.LicenceNumber,
                Status = licence.Status,
                IssuingAuthority = licence.IssuingAuthority,
                ExpiryDate = licence.ExpiryDate,
                TransactionCount = transactionsUsingLicence.Count,
                TotalQuantityProcessed = transactionsUsingLicence
                    .SelectMany(t => t.Lines ?? Enumerable.Empty<TransactionLine>())
                    .Sum(l => l.Quantity),
                LastUsedDate = transactionsUsingLicence.MaxBy(t => t.TransactionDate)?.TransactionDate
            };

            usageItems.Add(item);
        }

        var report = new LicenceUsageReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = criteria.FromDate,
            ToDate = criteria.ToDate,
            LicenceUsages = usageItems,
            TotalLicences = usageItems.Count,
            TotalTransactions = usageItems.Sum(u => u.TransactionCount)
        };

        _logger.LogInformation("Generated licence usage report for {Count} licences", report.TotalLicences);

        return report;
    }

    #endregion

    #region Customer Compliance History Report (FR-029)

    /// <summary>
    /// Generates a customer compliance history report per FR-029.
    /// T163: Shows all compliance events for a customer.
    /// </summary>
    public async Task<CustomerComplianceHistoryReport?> GenerateCustomerComplianceHistoryAsync(
        CustomerComplianceHistoryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating compliance history for customer {CustomerAccount}/{DataAreaId}",
            criteria.CustomerAccount, criteria.DataAreaId);

        var customer = await _customerRepository.GetByAccountAsync(criteria.CustomerAccount, criteria.DataAreaId, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerAccount}/{DataAreaId} not found",
                criteria.CustomerAccount, criteria.DataAreaId);
            return null;
        }

        // Get audit events using ComplianceExtensionId (Guid key for audit repository)
        var auditEvents = await _auditRepository.GetCustomerComplianceHistoryAsync(
            customer.ComplianceExtensionId,
            criteria.FromDate,
            criteria.ToDate,
            cancellationToken);

        var report = new CustomerComplianceHistoryReport
        {
            GeneratedDate = DateTime.UtcNow,
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            CustomerName = customer.BusinessName,
            BusinessCategory = customer.BusinessCategory.ToString(),
            ApprovalStatus = customer.ApprovalStatus.ToString(),
            Events = auditEvents.Select(e => new ComplianceEventItem
            {
                EventId = e.EventId,
                EventType = e.EventType.ToString(),
                EventDate = e.EventDate,
                PerformedBy = e.PerformedByName ?? e.PerformedBy.ToString(),
                Details = e.Details
            }).ToList()
        };

        // Include licence status if requested
        if (criteria.IncludeLicenceStatus)
        {
            var licences = await _licenceRepository.GetByHolderAsync(
                customer.ComplianceExtensionId, "Customer", cancellationToken);

            report.CurrentLicences = licences.Select(l => new LicenceStatusItem
            {
                LicenceId = l.LicenceId,
                LicenceNumber = l.LicenceNumber,
                Status = l.Status,
                ExpiryDate = l.ExpiryDate,
                IssuingAuthority = l.IssuingAuthority
            }).ToList();
        }

        _logger.LogInformation("Generated compliance history with {Count} events for customer {CustomerName}",
            report.Events.Count, customer.BusinessName);

        return report;
    }

    #endregion
}

#region Report Criteria

/// <summary>
/// Criteria for generating transaction audit reports.
/// </summary>
public class TransactionAuditReportCriteria
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? SubstanceCode { get; set; }
    public string? CustomerAccount { get; set; }
    public string? CustomerDataAreaId { get; set; }
    public string? CountryCode { get; set; }
    public bool IncludeLicenceDetails { get; set; }
}

/// <summary>
/// Criteria for generating licence usage reports.
/// </summary>
public class LicenceUsageReportCriteria
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? LicenceId { get; set; }
    public Guid? HolderId { get; set; }
    public string? HolderType { get; set; }
}

/// <summary>
/// Criteria for generating customer compliance history.
/// Uses composite key (CustomerAccount + DataAreaId) per D365FO pattern.
/// </summary>
public class CustomerComplianceHistoryCriteria
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeLicenceStatus { get; set; }
}

#endregion

#region Report Models

/// <summary>
/// Transaction audit report per FR-026.
/// </summary>
public class TransactionAuditReport
{
    public DateTime GeneratedDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<TransactionAuditReportItem> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public string? FilteredBySubstanceCode { get; set; }
    public string? FilteredByCustomerAccount { get; set; }
    public string? FilteredByCustomerDataAreaId { get; set; }
    public string? FilteredByCountry { get; set; }
}

/// <summary>
/// Item in the transaction audit report.
/// </summary>
public class TransactionAuditReportItem
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public List<LicenceReportDetail>? LicencesUsed { get; set; }
}

/// <summary>
/// Licence details in reports.
/// </summary>
public class LicenceReportDetail
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
}

/// <summary>
/// Licence usage report per FR-026.
/// </summary>
public class LicenceUsageReport
{
    public DateTime GeneratedDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<LicenceUsageReportItem> LicenceUsages { get; set; } = new();
    public int TotalLicences { get; set; }
    public int TotalTransactions { get; set; }
}

/// <summary>
/// Item in the licence usage report.
/// </summary>
public class LicenceUsageReportItem
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalQuantityProcessed { get; set; }
    public DateTime? LastUsedDate { get; set; }
}

/// <summary>
/// Customer compliance history report per FR-029.
/// </summary>
public class CustomerComplianceHistoryReport
{
    public DateTime GeneratedDate { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public List<ComplianceEventItem> Events { get; set; } = new();
    public List<LicenceStatusItem>? CurrentLicences { get; set; }
}

/// <summary>
/// Compliance event in history report.
/// </summary>
public class ComplianceEventItem
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
}

/// <summary>
/// Licence status in customer compliance report.
/// </summary>
public class LicenceStatusItem
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
}

#endregion
