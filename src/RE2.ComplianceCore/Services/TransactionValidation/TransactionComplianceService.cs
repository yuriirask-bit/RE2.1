using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.Notifications;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Services.TransactionValidation;

/// <summary>
/// Service for transaction compliance validation.
/// T133-T141: Implements validation rules per FR-018 through FR-024.
/// </summary>
public class TransactionComplianceService : ITransactionComplianceService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IThresholdRepository _thresholdRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWebhookDispatchService? _webhookDispatchService;
    private readonly ILogger<TransactionComplianceService> _logger;

    public TransactionComplianceService(
        ITransactionRepository transactionRepository,
        IThresholdRepository thresholdRepository,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        IControlledSubstanceRepository substanceRepository,
        IProductRepository productRepository,
        ILogger<TransactionComplianceService> logger,
        IWebhookDispatchService? webhookDispatchService = null)
    {
        _transactionRepository = transactionRepository;
        _thresholdRepository = thresholdRepository;
        _licenceRepository = licenceRepository;
        _customerRepository = customerRepository;
        _substanceRepository = substanceRepository;
        _productRepository = productRepository;
        _webhookDispatchService = webhookDispatchService;
        _logger = logger;
    }

    #region Validation

    /// <inheritdoc />
    public async Task<TransactionValidationResult> ValidateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var violations = new List<TransactionViolation>();
        var licenceUsages = new List<TransactionLicenceUsage>();

        _logger.LogInformation(
            "Validating transaction {ExternalId} for customer {CustomerAccount} in {DataAreaId}",
            transaction.ExternalId,
            transaction.CustomerAccount,
            transaction.CustomerDataAreaId);

        try
        {
            // 1. Validate customer qualification (FR-019)
            var customerViolations = await ValidateCustomerQualificationAsync(transaction, cancellationToken);
            violations.AddRange(customerViolations);

            // 2. Validate licence coverage for each line (FR-018)
            var (licenceCoverageViolations, usages) = await ValidateLicenceCoverageAsync(transaction, cancellationToken);
            violations.AddRange(licenceCoverageViolations);
            licenceUsages.AddRange(usages);

            // 3. Validate quantity thresholds (FR-020)
            var quantityViolations = await CheckQuantityThresholdsInternalAsync(transaction, cancellationToken);
            violations.AddRange(quantityViolations);

            // 4. Validate cross-border requirements (FR-021)
            var crossBorderViolations = await ValidateCrossBorderRequirementsAsync(transaction, cancellationToken);
            violations.AddRange(crossBorderViolations);

            // 5. Validate frequency thresholds (FR-022)
            var frequencyViolations = await CheckFrequencyThresholdsInternalAsync(
                transaction.CustomerAccount,
                transaction.CustomerDataAreaId,
                transaction.TransactionDate,
                cancellationToken);
            violations.AddRange(frequencyViolations);

            // Set validation result on transaction
            stopwatch.Stop();
            var validationTime = stopwatch.ElapsedMilliseconds;

            if (violations.Count == 0)
            {
                // Validation passed
                transaction.SetValidationResult(ValidationResult.Success(), DateTime.UtcNow);
                transaction.LicenceUsages = licenceUsages;
                transaction.LicencesUsed = licenceUsages.Select(u => u.LicenceId).ToList();

                // Save transaction
                if (transaction.Id == Guid.Empty)
                {
                    await _transactionRepository.CreateAsync(transaction, cancellationToken);
                }
                else
                {
                    await _transactionRepository.UpdateAsync(transaction, cancellationToken);
                }

                // Record licence usages
                foreach (var usage in licenceUsages)
                {
                    await _transactionRepository.AddLicenceUsageAsync(usage, cancellationToken);
                }

                _logger.LogInformation(
                    "Transaction {ExternalId} passed validation in {ElapsedMs}ms",
                    transaction.ExternalId,
                    validationTime);

                return TransactionValidationResult.Success(transaction, licenceUsages, validationTime);
            }
            else
            {
                // Validation failed
                var validationViolations = violations.Select(v => new ValidationViolation
                {
                    ErrorCode = v.ErrorCode,
                    Message = v.Message,
                    Severity = v.Severity,
                    CanOverride = v.CanOverride,
                    LineNumber = v.LineNumber,
                    SubstanceCode = v.SubstanceCode
                }).ToList();

                var result = ValidationResult.Failure(validationViolations);
                transaction.SetValidationResult(result, DateTime.UtcNow);
                transaction.Violations = violations;
                transaction.LicenceUsages = licenceUsages;
                transaction.LicencesUsed = licenceUsages.Select(u => u.LicenceId).ToList();

                // Save transaction
                if (transaction.Id == Guid.Empty)
                {
                    await _transactionRepository.CreateAsync(transaction, cancellationToken);
                }
                else
                {
                    await _transactionRepository.UpdateAsync(transaction, cancellationToken);
                }

                // Record violations
                await _transactionRepository.AddViolationsAsync(transaction.Id, violations, cancellationToken);

                // Record licence usages (for partial coverage)
                foreach (var usage in licenceUsages)
                {
                    await _transactionRepository.AddLicenceUsageAsync(usage, cancellationToken);
                }

                _logger.LogWarning(
                    "Transaction {ExternalId} failed validation with {ViolationCount} violations in {ElapsedMs}ms",
                    transaction.ExternalId,
                    violations.Count,
                    validationTime);

                return TransactionValidationResult.Failure(transaction, validationViolations, licenceUsages, validationTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction {ExternalId}", transaction.ExternalId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionValidationResult> RevalidateTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        // Clear existing validation data
        await _transactionRepository.ClearViolationsAsync(transactionId, cancellationToken);
        transaction.Violations.Clear();
        transaction.ValidationStatus = ValidationStatus.Pending;

        return await ValidateTransactionAsync(transaction, cancellationToken);
    }

    #endregion

    #region Validation Rules

    /// <summary>
    /// Validates customer qualification for the transaction (FR-019).
    /// </summary>
    private async Task<List<TransactionViolation>> ValidateCustomerQualificationAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var violations = new List<TransactionViolation>();

        var customer = await _customerRepository.GetByAccountAsync(transaction.CustomerAccount, transaction.CustomerDataAreaId, cancellationToken);
        if (customer == null)
        {
            violations.Add(TransactionViolation.CustomerViolation(
                transaction.Id,
                ViolationType.CustomerNotQualified,
                ErrorCodes.CUSTOMER_NOT_FOUND,
                $"Customer '{transaction.CustomerAccount}' not found in data area '{transaction.CustomerDataAreaId}'",
                Guid.Empty,
                canOverride: false));
            return violations;
        }

        // Update customer name on transaction
        transaction.CustomerName = customer.BusinessName;

        // Check if customer is suspended
        if (customer.IsSuspended)
        {
            violations.Add(TransactionViolation.CustomerViolation(
                transaction.Id,
                ViolationType.CustomerSuspended,
                ErrorCodes.CUSTOMER_SUSPENDED,
                $"Customer '{customer.BusinessName}' is suspended: {customer.SuspensionReason}",
                customer.ComplianceExtensionId,
                customer.BusinessName,
                canOverride: false)); // Cannot override suspended customer
        }

        // Check if customer is approved
        if (customer.ApprovalStatus != ApprovalStatus.Approved)
        {
            violations.Add(TransactionViolation.CustomerViolation(
                transaction.Id,
                ViolationType.CustomerNotQualified,
                ErrorCodes.CUSTOMER_NOT_APPROVED,
                $"Customer '{customer.BusinessName}' is not approved (status: {customer.ApprovalStatus})",
                customer.ComplianceExtensionId,
                customer.BusinessName,
                canOverride: true)); // Can override pending approval in some cases
        }

        // Check GDP qualification for applicable categories
        if (RequiresGdpQualification(customer.BusinessCategory))
        {
            if (customer.GdpQualificationStatus != GdpQualificationStatus.Approved &&
                customer.GdpQualificationStatus != GdpQualificationStatus.ConditionallyApproved)
            {
                violations.Add(TransactionViolation.CustomerViolation(
                    transaction.Id,
                    ViolationType.CustomerNotQualified,
                    ErrorCodes.GDP_QUALIFICATION_INVALID,
                    $"Customer '{customer.BusinessName}' is not GDP qualified (status: {customer.GdpQualificationStatus})",
                    customer.ComplianceExtensionId,
                    customer.BusinessName,
                    canOverride: true));
            }
        }

        return violations;
    }

    /// <summary>
    /// Validates licence coverage for each transaction line (FR-018).
    /// </summary>
    private async Task<(List<TransactionViolation> violations, List<TransactionLicenceUsage> usages)> ValidateLicenceCoverageAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var violations = new List<TransactionViolation>();
        var usages = new List<TransactionLicenceUsage>();
        var coveredLinesByLicence = new Dictionary<Guid, List<TransactionLine>>();

        // Get customer's licences (look up customer to get ComplianceExtensionId for licence holder)
        var licenceCustomer = await _customerRepository.GetByAccountAsync(
            transaction.CustomerAccount, transaction.CustomerDataAreaId, cancellationToken);
        var customerLicences = licenceCustomer != null
            ? await _licenceRepository.GetByHolderAsync(licenceCustomer.ComplianceExtensionId, "Customer", cancellationToken)
            : Enumerable.Empty<Licence>();

        // Get company's licences (wholesaler licences)
        var companyLicences = await _licenceRepository.GetByHolderAsync(
            WellKnownIds.CompanyHolderId,
            "Company",
            cancellationToken);

        var allLicences = customerLicences.Concat(companyLicences).ToList();

        foreach (var line in transaction.Lines)
        {
            // Resolve SubstanceCode from product master via ItemNumber + DataAreaId
            var resolvedSubstanceCode = line.SubstanceCode
                ?? await _productRepository.ResolveSubstanceCodeAsync(line.ItemNumber, line.DataAreaId);

            if (string.IsNullOrEmpty(resolvedSubstanceCode))
            {
                // Not a controlled product - skip line
                continue;
            }

            // Set resolved substance code on line
            line.SubstanceCode = resolvedSubstanceCode;

            // Lookup substance to get full details
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(resolvedSubstanceCode, cancellationToken);
            if (substance == null)
            {
                line.SetValidationError(
                    ErrorCodes.SUBSTANCE_NOT_FOUND,
                    $"Substance with code '{resolvedSubstanceCode}' not found");

                violations.Add(TransactionViolation.LicenceViolation(
                    transaction.Id,
                    ViolationType.SubstanceNotFound,
                    ErrorCodes.SUBSTANCE_NOT_FOUND,
                    $"Substance '{resolvedSubstanceCode}' not found in system",
                    lineNumber: line.LineNumber,
                    substanceCode: line.SubstanceCode,
                    canOverride: false));
                continue;
            }

            // Update substance details on line
            line.SubstanceName = substance.SubstanceName;
            line.Substance = substance;

            // Find valid licence for this substance
            var coveringLicence = FindCoveringLicence(substance, allLicences, transaction);

            if (coveringLicence == null)
            {
                // No licence found
                line.SetValidationError(
                    ErrorCodes.LICENCE_MISSING,
                    $"No valid licence found for substance '{substance.SubstanceName}'");

                violations.Add(TransactionViolation.LicenceViolation(
                    transaction.Id,
                    ViolationType.NoLicence,
                    ErrorCodes.LICENCE_MISSING,
                    $"No valid licence found for substance '{substance.SubstanceName}' (Line {line.LineNumber})",
                    lineNumber: line.LineNumber,
                    substanceCode: line.SubstanceCode,
                    canOverride: true)); // Can override if licence pending
            }
            else
            {
                // Check licence status
                if (coveringLicence.Status == "Expired" || coveringLicence.IsExpired())
                {
                    line.SetValidationError(
                        ErrorCodes.LICENCE_EXPIRED,
                        $"Licence '{coveringLicence.LicenceNumber}' has expired");

                    violations.Add(TransactionViolation.LicenceViolation(
                        transaction.Id,
                        ViolationType.ExpiredLicence,
                        ErrorCodes.LICENCE_EXPIRED,
                        $"Licence '{coveringLicence.LicenceNumber}' expired on {coveringLicence.ExpiryDate} (Line {line.LineNumber})",
                        licenceId: coveringLicence.LicenceId,
                        licenceNumber: coveringLicence.LicenceNumber,
                        expiryDate: coveringLicence.ExpiryDate,
                        lineNumber: line.LineNumber,
                        substanceCode: line.SubstanceCode,
                        canOverride: true)); // Can override for renewal in progress
                }
                else if (coveringLicence.Status == "Suspended")
                {
                    line.SetValidationError(
                        ErrorCodes.LICENCE_SUSPENDED,
                        $"Licence '{coveringLicence.LicenceNumber}' is suspended");

                    violations.Add(TransactionViolation.LicenceViolation(
                        transaction.Id,
                        ViolationType.LicenceSuspended,
                        ErrorCodes.LICENCE_SUSPENDED,
                        $"Licence '{coveringLicence.LicenceNumber}' is suspended (Line {line.LineNumber})",
                        licenceId: coveringLicence.LicenceId,
                        licenceNumber: coveringLicence.LicenceNumber,
                        lineNumber: line.LineNumber,
                        substanceCode: line.SubstanceCode,
                        canOverride: false)); // Cannot override suspended
                }
                else
                {
                    // Valid coverage
                    line.SetLicenceCoverage(coveringLicence);

                    // Track for usage recording
                    if (!coveredLinesByLicence.ContainsKey(coveringLicence.LicenceId))
                    {
                        coveredLinesByLicence[coveringLicence.LicenceId] = new List<TransactionLine>();
                    }
                    coveredLinesByLicence[coveringLicence.LicenceId].Add(line);
                }
            }
        }

        // Create licence usage records
        foreach (var (licenceId, coveredLines) in coveredLinesByLicence)
        {
            var licence = allLicences.First(l => l.LicenceId == licenceId);
            var usage = TransactionLicenceUsage.FromLicence(transaction.Id, licence, coveredLines);
            usages.Add(usage);
        }

        return (violations, usages);
    }

    /// <summary>
    /// Validates cross-border requirements (FR-021).
    /// </summary>
    private async Task<List<TransactionViolation>> ValidateCrossBorderRequirementsAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var violations = new List<TransactionViolation>();

        if (!transaction.IsCrossBorder())
        {
            return violations;
        }

        // Get company's permits
        var companyLicences = await _licenceRepository.GetByHolderAsync(
            WellKnownIds.CompanyHolderId,
            "Company",
            cancellationToken);

        // Check for import permit
        if (transaction.RequiresImportPermit())
        {
            var importPermit = companyLicences.FirstOrDefault(l =>
                l.LicenceTypeId == WellKnownIds.ImportPermitTypeId &&
                l.Status == "Valid" &&
                !l.IsExpired());

            if (importPermit == null)
            {
                violations.Add(TransactionViolation.PermitViolation(
                    transaction.Id,
                    ErrorCodes.IMPORT_PERMIT_REQUIRED,
                    $"Import from {transaction.OriginCountry} requires valid import permit",
                    "Import Permit (Opium Act)",
                    canOverride: true));
            }
        }

        // Check for export permit
        if (transaction.RequiresExportPermit())
        {
            var exportPermit = companyLicences.FirstOrDefault(l =>
                l.LicenceTypeId == WellKnownIds.ExportPermitTypeId &&
                l.Status == "Valid" &&
                !l.IsExpired());

            if (exportPermit == null)
            {
                violations.Add(TransactionViolation.PermitViolation(
                    transaction.Id,
                    ErrorCodes.EXPORT_PERMIT_REQUIRED,
                    $"Export to {transaction.DestinationCountry} requires valid export permit",
                    "Export Permit (Opium Act)",
                    canOverride: true));
            }
        }

        return violations;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ValidationViolation>> CheckQuantityThresholdsAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        var violations = await CheckQuantityThresholdsInternalAsync(transaction, cancellationToken);
        return ConvertToValidationViolations(violations);
    }

    /// <summary>
    /// Internal implementation of quantity threshold checking that returns TransactionViolation.
    /// </summary>
    private async Task<List<TransactionViolation>> CheckQuantityThresholdsInternalAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var violations = new List<TransactionViolation>();

        // Get customer for category-based thresholds
        var customer = await _customerRepository.GetByAccountAsync(transaction.CustomerAccount, transaction.CustomerDataAreaId, cancellationToken);
        if (customer == null)
        {
            return violations; // Customer validation will catch this
        }

        // Get all substances in the transaction
        var substanceCodes = transaction.Lines
            .Where(l => !string.IsNullOrEmpty(l.SubstanceCode))
            .Select(l => l.SubstanceCode!)
            .Distinct()
            .ToList();

        // Get applicable thresholds
        var thresholds = await _thresholdRepository.GetApplicableThresholdsAsync(
            substanceCodes,
            customer.ComplianceExtensionId,
            customer.BusinessCategory,
            cancellationToken);

        foreach (var threshold in thresholds.Where(t => t.ThresholdType == ThresholdType.Quantity ||
                                                         t.ThresholdType == ThresholdType.CumulativeQuantity))
        {
            // Calculate quantity for this threshold
            decimal transactionQuantity;
            if (!string.IsNullOrEmpty(threshold.SubstanceCode))
            {
                // Substance-specific threshold
                transactionQuantity = transaction.Lines
                    .Where(l => l.SubstanceCode == threshold.SubstanceCode)
                    .Sum(l => l.BaseUnitQuantity);
            }
            else
            {
                // Global threshold - all substances
                transactionQuantity = transaction.Lines.Sum(l => l.BaseUnitQuantity);
            }

            // For cumulative thresholds, add historical usage
            if (threshold.ThresholdType == ThresholdType.CumulativeQuantity)
            {
                var (fromDate, toDate) = GetPeriodDates(threshold.Period, transaction.TransactionDate);

                decimal historicalUsage;
                if (!string.IsNullOrEmpty(threshold.SubstanceCode))
                {
                    historicalUsage = await GetCumulativeUsageAsync(
                        customer.CustomerAccount,
                        customer.DataAreaId,
                        threshold.SubstanceCode,
                        fromDate,
                        toDate,
                        cancellationToken);
                }
                else
                {
                    // Sum all substances
                    historicalUsage = 0;
                    foreach (var substanceCode in substanceCodes)
                    {
                        historicalUsage += await GetCumulativeUsageAsync(
                            customer.CustomerAccount,
                            customer.DataAreaId,
                            substanceCode,
                            fromDate,
                            toDate,
                            cancellationToken);
                    }
                }

                transactionQuantity += historicalUsage;
            }

            // Check threshold
            if (threshold.IsExceeded(transactionQuantity))
            {
                var substanceName = threshold.SubstanceName ?? "All substances";
                violations.Add(TransactionViolation.ThresholdViolation(
                    transaction.Id,
                    ErrorCodes.QUANTITY_THRESHOLD_EXCEEDED,
                    $"{threshold.Name}: {transactionQuantity:F2} {threshold.LimitUnit} exceeds limit of {threshold.LimitValue:F2} {threshold.LimitUnit} for {substanceName}",
                    threshold.ThresholdType,
                    threshold.LimitValue,
                    transactionQuantity,
                    threshold.Period,
                    threshold.Id,
                    substanceCode: threshold.SubstanceCode,
                    canOverride: threshold.AllowOverride && !threshold.ExceedsMaxOverride(transactionQuantity)));
            }
            else if (threshold.IsWarning(transactionQuantity))
            {
                // Add warning (non-blocking)
                var warningViolation = TransactionViolation.ThresholdViolation(
                    transaction.Id,
                    ErrorCodes.VALIDATION_WARNING,
                    $"Warning: Approaching {threshold.Name} limit ({threshold.GetUsagePercent(transactionQuantity):F0}% used)",
                    threshold.ThresholdType,
                    threshold.LimitValue,
                    transactionQuantity,
                    threshold.Period,
                    threshold.Id,
                    substanceCode: threshold.SubstanceCode,
                    canOverride: true);
                warningViolation.Severity = ViolationSeverity.Warning;
                violations.Add(warningViolation);
            }
        }

        return violations;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ValidationViolation>> CheckFrequencyThresholdsAsync(
        string customerAccount,
        string customerDataAreaId,
        DateTime transactionDate,
        CancellationToken cancellationToken = default)
    {
        var violations = await CheckFrequencyThresholdsInternalAsync(customerAccount, customerDataAreaId, transactionDate, cancellationToken);
        return ConvertToValidationViolations(violations);
    }

    /// <summary>
    /// Internal implementation of frequency threshold checking that returns TransactionViolation.
    /// </summary>
    private async Task<List<TransactionViolation>> CheckFrequencyThresholdsInternalAsync(
        string customerAccount,
        string customerDataAreaId,
        DateTime transactionDate,
        CancellationToken cancellationToken)
    {
        var violations = new List<TransactionViolation>();

        // Get customer for category
        var customer = await _customerRepository.GetByAccountAsync(customerAccount, customerDataAreaId, cancellationToken);
        if (customer == null)
        {
            return violations;
        }

        // Get frequency thresholds
        var thresholds = await _thresholdRepository.GetByTypeAsync(ThresholdType.Frequency, cancellationToken);
        var applicableThresholds = thresholds.Where(t =>
            t.AppliesToCustomer(customer.ComplianceExtensionId, customer.BusinessCategory));

        foreach (var threshold in applicableThresholds)
        {
            var (fromDate, toDate) = GetPeriodDates(threshold.Period, transactionDate);

            // Count transactions in period
            var transactions = await _transactionRepository.GetCustomerTransactionsInPeriodAsync(
                customer.CustomerAccount,
                customer.DataAreaId,
                fromDate,
                toDate,
                cancellationToken);

            var transactionCount = transactions.Count() + 1; // +1 for current transaction

            if (threshold.IsExceeded(transactionCount))
            {
                violations.Add(TransactionViolation.ThresholdViolation(
                    Guid.Empty, // Will be set by caller
                    ErrorCodes.FREQUENCY_THRESHOLD_EXCEEDED,
                    $"{threshold.Name}: {transactionCount} transactions exceeds limit of {threshold.LimitValue:F0} per {threshold.Period}",
                    ThresholdType.Frequency,
                    threshold.LimitValue,
                    transactionCount,
                    threshold.Period,
                    threshold.Id,
                    canOverride: threshold.AllowOverride));
            }
        }

        return violations;
    }

    #endregion

    #region Override Management

    /// <inheritdoc />
    public async Task<ValidationResult> ApproveOverrideAsync(
        Guid transactionId,
        string approverUserId,
        string justification,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction == null)
        {
            return ValidationResult.Failure(ErrorCodes.TRANSACTION_NOT_FOUND, $"Transaction {transactionId} not found");
        }

        if (!transaction.RequiresOverride)
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "Transaction does not require override");
        }

        if (transaction.OverrideStatus != OverrideStatus.Pending)
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR,
                $"Transaction override is not pending (current status: {transaction.OverrideStatus})");
        }

        if (string.IsNullOrWhiteSpace(justification))
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "Override justification is required");
        }

        transaction.ApproveOverride(approverUserId, justification, DateTime.UtcNow);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Override approved for transaction {TransactionId} by {ApproverUserId}: {Justification}",
            transactionId,
            approverUserId,
            justification);

        // T149i: Dispatch webhook notifications for override approval
        await DispatchOverrideApprovedWebhookAsync(transaction, approverUserId, justification, cancellationToken);
        await DispatchOrderApprovedWebhookAsync(transaction, cancellationToken);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> RejectOverrideAsync(
        Guid transactionId,
        string rejecterUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction == null)
        {
            return ValidationResult.Failure(ErrorCodes.TRANSACTION_NOT_FOUND, $"Transaction {transactionId} not found");
        }

        if (!transaction.RequiresOverride)
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "Transaction does not require override");
        }

        if (transaction.OverrideStatus != OverrideStatus.Pending)
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR,
                $"Transaction override is not pending (current status: {transaction.OverrideStatus})");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "Rejection reason is required");
        }

        transaction.RejectOverride(rejecterUserId, reason, DateTime.UtcNow);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Override rejected for transaction {TransactionId} by {RejecterUserId}: {Reason}",
            transactionId,
            rejecterUserId,
            reason);

        // T149i: Dispatch webhook notification for order rejection
        await DispatchOrderRejectedWebhookAsync(transaction, rejecterUserId, reason, cancellationToken);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Transaction>> GetPendingOverridesAsync(CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetPendingOverrideAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetPendingOverrideCountAsync(CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetPendingOverrideCountAsync(cancellationToken);
    }

    #endregion

    #region Transaction Retrieval

    /// <inheritdoc />
    public async Task<Transaction?> GetTransactionByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetTransactionByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByExternalIdAsync(externalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(
        ValidationStatus? status = null,
        string? customerAccount = null,
        string? customerDataAreaId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Transaction> transactions;

        if (status.HasValue)
        {
            transactions = await _transactionRepository.GetByStatusAsync(status.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(customerAccount) && !string.IsNullOrEmpty(customerDataAreaId))
        {
            transactions = await _transactionRepository.GetByCustomerAccountAsync(customerAccount, customerDataAreaId, cancellationToken);
        }
        else if (fromDate.HasValue && toDate.HasValue)
        {
            transactions = await _transactionRepository.GetByDateRangeAsync(fromDate.Value, toDate.Value, cancellationToken);
        }
        else
        {
            transactions = await _transactionRepository.GetAllAsync(cancellationToken);
        }

        return transactions;
    }

    #endregion

    #region Threshold Checking

    /// <inheritdoc />
    public async Task<decimal> GetCumulativeUsageAsync(
        string customerAccount,
        string customerDataAreaId,
        string substanceCode,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var lines = await _transactionRepository.GetCustomerSubstanceLinesInPeriodAsync(
            customerAccount,
            customerDataAreaId,
            substanceCode,
            fromDate,
            toDate,
            cancellationToken);

        return lines.Sum(l => l.BaseUnitQuantity);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a valid licence that covers the given substance.
    /// </summary>
    private Licence? FindCoveringLicence(ControlledSubstance substance, IEnumerable<Licence> licences, Transaction transaction)
    {
        // Determine required activity based on transaction type
        var requiredActivity = transaction.TransactionType switch
        {
            TransactionType.Order => LicenceTypes.PermittedActivity.Distribute,
            TransactionType.Shipment => LicenceTypes.PermittedActivity.Distribute,
            TransactionType.Return => LicenceTypes.PermittedActivity.Possess,
            TransactionType.Transfer => LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Store,
            _ => LicenceTypes.PermittedActivity.Possess
        };

        // Add import/export if cross-border
        if (transaction.RequiresImportPermit())
        {
            requiredActivity |= LicenceTypes.PermittedActivity.Import;
        }
        if (transaction.RequiresExportPermit())
        {
            requiredActivity |= LicenceTypes.PermittedActivity.Export;
        }

        // Find licence that covers this substance and activity
        // First, try to find a valid licence
        var coveringLicence = FindCoveringLicenceWithFilter(substance, licences, requiredActivity, l => l.Status == "Valid" && !l.IsExpired());
        if (coveringLicence != null)
        {
            return coveringLicence;
        }

        // If no valid licence, look for expired/suspended licences so we can report specific errors
        return FindCoveringLicenceWithFilter(substance, licences, requiredActivity, _ => true);
    }

    /// <summary>
    /// Finds a licence that covers the given substance with the specified filter.
    /// </summary>
    private Licence? FindCoveringLicenceWithFilter(
        ControlledSubstance substance,
        IEnumerable<Licence> licences,
        LicenceTypes.PermittedActivity requiredActivity,
        Func<Licence, bool> filter)
    {
        foreach (var licence in licences.Where(filter))
        {
            // Check if licence permits required activities
            if ((licence.PermittedActivities & requiredActivity) != requiredActivity)
            {
                continue;
            }

            // Check substance coverage based on licence type
            // For Opium Act exemptions, check if it covers the opium list
            if (licence.LicenceTypeId == WellKnownIds.OpiumExemptionTypeId)
            {
                if (substance.IsOpiumActControlled())
                {
                    return licence;
                }
            }
            // For Precursor registration, check if substance is precursor
            else if (licence.LicenceTypeId == WellKnownIds.PrecursorRegistrationTypeId)
            {
                if (substance.IsPrecursor())
                {
                    return licence;
                }
            }
            // For Pharmacy/Wholesale licences, they generally cover all substances
            else if (licence.LicenceTypeId == WellKnownIds.PharmacyLicenceTypeId ||
                     licence.LicenceTypeId == WellKnownIds.WholesaleLicenceTypeId)
            {
                return licence;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if the business category requires GDP qualification.
    /// </summary>
    private static bool RequiresGdpQualification(BusinessCategory category)
    {
        return category switch
        {
            BusinessCategory.WholesalerEU => true,
            BusinessCategory.WholesalerNonEU => true,
            BusinessCategory.HospitalPharmacy => true,
            BusinessCategory.CommunityPharmacy => true,
            BusinessCategory.Veterinarian => false, // GDP not typically required
            _ => false
        };
    }

    /// <summary>
    /// Gets the date range for a threshold period.
    /// </summary>
    private static (DateTime fromDate, DateTime toDate) GetPeriodDates(ThresholdPeriod period, DateTime referenceDate)
    {
        return period switch
        {
            ThresholdPeriod.PerTransaction => (referenceDate, referenceDate),
            ThresholdPeriod.Daily => (referenceDate.Date, referenceDate.Date.AddDays(1).AddTicks(-1)),
            ThresholdPeriod.Weekly => (referenceDate.Date.AddDays(-(int)referenceDate.DayOfWeek),
                                       referenceDate.Date.AddDays(7 - (int)referenceDate.DayOfWeek).AddTicks(-1)),
            ThresholdPeriod.Monthly => (new DateTime(referenceDate.Year, referenceDate.Month, 1),
                                        new DateTime(referenceDate.Year, referenceDate.Month, 1).AddMonths(1).AddTicks(-1)),
            ThresholdPeriod.Yearly => (new DateTime(referenceDate.Year, 1, 1),
                                       new DateTime(referenceDate.Year + 1, 1, 1).AddTicks(-1)),
            _ => (referenceDate.Date, referenceDate.Date.AddDays(1).AddTicks(-1))
        };
    }

    /// <summary>
    /// Converts a list of TransactionViolation to ValidationViolation for public interface.
    /// </summary>
    private static IEnumerable<ValidationViolation> ConvertToValidationViolations(List<TransactionViolation> violations)
    {
        return violations.Select(v => new ValidationViolation
        {
            ErrorCode = v.ErrorCode,
            Message = v.Message,
            Severity = v.Severity,
            CanOverride = v.CanOverride,
            LineNumber = v.LineNumber,
            SubstanceCode = v.SubstanceCode
        });
    }

    #endregion

    #region Webhook Dispatch (T149i)

    /// <summary>
    /// Dispatches webhook notification when an override is approved.
    /// T149i: Integration of WebhookDispatchService per FR-059.
    /// </summary>
    private async Task DispatchOverrideApprovedWebhookAsync(
        Transaction transaction,
        string approverUserId,
        string justification,
        CancellationToken cancellationToken)
    {
        if (_webhookDispatchService == null)
        {
            return;
        }

        try
        {
            var payload = new OverrideApprovedEventPayload
            {
                TransactionId = transaction.Id,
                ExternalId = transaction.ExternalId,
                CustomerAccount = transaction.CustomerAccount,
                CustomerName = transaction.CustomerName,
                ApproverUserId = approverUserId,
                Justification = justification,
                ApprovedAt = transaction.OverrideDecisionDate ?? DateTime.UtcNow,
                ViolationCount = transaction.Violations?.Count ?? 0
            };

            await _webhookDispatchService.DispatchAsync(WebhookEventType.OverrideApproved, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail the operation - webhook dispatch is non-critical
            _logger.LogWarning(ex, "Failed to dispatch OverrideApproved webhook for transaction {TransactionId}", transaction.Id);
        }
    }

    /// <summary>
    /// Dispatches webhook notification when an order is approved (validation passed or override approved).
    /// T149i: Integration of WebhookDispatchService per FR-059.
    /// </summary>
    private async Task DispatchOrderApprovedWebhookAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        if (_webhookDispatchService == null)
        {
            return;
        }

        try
        {
            var payload = new OrderStatusChangedEventPayload
            {
                TransactionId = transaction.Id,
                ExternalId = transaction.ExternalId,
                CustomerAccount = transaction.CustomerAccount,
                CustomerName = transaction.CustomerName,
                Status = "Approved",
                ValidationStatus = transaction.ValidationStatus.ToString(),
                OverrideStatus = transaction.OverrideStatus.ToString(),
                ApprovedAt = DateTime.UtcNow,
                LicencesUsed = transaction.LicencesUsed?.ToList() ?? new List<Guid>()
            };

            await _webhookDispatchService.DispatchAsync(WebhookEventType.OrderApproved, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch OrderApproved webhook for transaction {TransactionId}", transaction.Id);
        }
    }

    /// <summary>
    /// Dispatches webhook notification when an order is rejected.
    /// T149i: Integration of WebhookDispatchService per FR-059.
    /// </summary>
    private async Task DispatchOrderRejectedWebhookAsync(
        Transaction transaction,
        string? rejectedBy,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (_webhookDispatchService == null)
        {
            return;
        }

        try
        {
            var payload = new OrderStatusChangedEventPayload
            {
                TransactionId = transaction.Id,
                ExternalId = transaction.ExternalId,
                CustomerAccount = transaction.CustomerAccount,
                CustomerName = transaction.CustomerName,
                Status = "Rejected",
                ValidationStatus = transaction.ValidationStatus.ToString(),
                OverrideStatus = transaction.OverrideStatus.ToString(),
                RejectedAt = DateTime.UtcNow,
                RejectedBy = rejectedBy,
                RejectionReason = reason,
                Violations = transaction.Violations?.Select(v => new ViolationSummary
                {
                    ErrorCode = v.ErrorCode,
                    Message = v.Message,
                    Severity = v.Severity.ToString(),
                    CanOverride = v.CanOverride
                }).ToList() ?? new List<ViolationSummary>()
            };

            await _webhookDispatchService.DispatchAsync(WebhookEventType.OrderRejected, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch OrderRejected webhook for transaction {TransactionId}", transaction.Id);
        }
    }

    #endregion
}

/// <summary>
/// Payload for OverrideApproved webhook event.
/// </summary>
public class OverrideApprovedEventPayload
{
    public Guid TransactionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string CustomerAccount { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string ApproverUserId { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
    public int ViolationCount { get; set; }
}

/// <summary>
/// Payload for OrderApproved and OrderRejected webhook events.
/// </summary>
public class OrderStatusChangedEventPayload
{
    public Guid TransactionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string CustomerAccount { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ValidationStatus { get; set; }
    public string? OverrideStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public List<Guid> LicencesUsed { get; set; } = new();
    public List<ViolationSummary> Violations { get; set; } = new();
}

/// <summary>
/// Summary of a validation violation for webhook payloads.
/// </summary>
public class ViolationSummary
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool CanOverride { get; set; }
}
