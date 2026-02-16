using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for transaction compliance validation.
/// T133-T141: Transaction validation service per FR-018 through FR-024.
/// </summary>
public interface ITransactionComplianceService
{
    #region Validation

    /// <summary>
    /// Validates a transaction against all compliance rules.
    /// Per FR-018: Real-time transaction compliance validation.
    /// Returns validation result with all violations found.
    /// </summary>
    /// <param name="transaction">The transaction to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction validation result with violations and licence coverage.</returns>
    Task<TransactionValidationResult> ValidateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-validates a previously validated transaction.
    /// Clears existing violations and performs fresh validation.
    /// </summary>
    Task<TransactionValidationResult> RevalidateTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Override Management

    /// <summary>
    /// Approves an override for a failed transaction.
    /// Per FR-019a: ComplianceManager override approval.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="approverUserId">User approving the override.</param>
    /// <param name="justification">Reason for approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or failure.</returns>
    Task<ValidationResult> ApproveOverrideAsync(
        Guid transactionId,
        string approverUserId,
        string justification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an override request for a failed transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="rejecterUserId">User rejecting the override.</param>
    /// <param name="reason">Reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or failure.</returns>
    Task<ValidationResult> RejectOverrideAsync(
        Guid transactionId,
        string rejecterUserId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions pending override approval.
    /// </summary>
    Task<IEnumerable<Transaction>> GetPendingOverridesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of pending override transactions for dashboard.
    /// </summary>
    Task<int> GetPendingOverrideCountAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Retrieval

    /// <summary>
    /// Gets a transaction by ID with full details.
    /// </summary>
    Task<Transaction?> GetTransactionByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by external ID (ERP order number).
    /// </summary>
    Task<Transaction?> GetTransactionByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions with optional filtering.
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionsAsync(
        Shared.Constants.TransactionTypes.ValidationStatus? status = null,
        string? customerAccount = null,
        string? customerDataAreaId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Threshold Checking

    /// <summary>
    /// Checks if a transaction would exceed any quantity thresholds.
    /// Per FR-020: Quantity threshold validation.
    /// </summary>
    Task<IEnumerable<ValidationViolation>> CheckQuantityThresholdsAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer has exceeded transaction frequency limits.
    /// Per FR-022: Frequency threshold validation.
    /// </summary>
    Task<IEnumerable<ValidationViolation>> CheckFrequencyThresholdsAsync(
        string customerAccount,
        string customerDataAreaId,
        DateTime transactionDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cumulative usage for a customer-substance combination in a period.
    /// Used for threshold tracking and reporting.
    /// </summary>
    Task<decimal> GetCumulativeUsageAsync(
        string customerAccount,
        string customerDataAreaId,
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of a transaction validation operation.
/// Includes validation result, updated transaction, and licence coverage details.
/// </summary>
public class TransactionValidationResult
{
    /// <summary>
    /// The underlying validation result.
    /// </summary>
    public required ValidationResult ValidationResult { get; init; }

    /// <summary>
    /// The validated transaction with updated status.
    /// </summary>
    public required Transaction Transaction { get; init; }

    /// <summary>
    /// Licences used to cover the transaction.
    /// </summary>
    public IReadOnlyList<TransactionLicenceUsage> LicenceUsages { get; init; } = Array.Empty<TransactionLicenceUsage>();

    /// <summary>
    /// Time taken for validation in milliseconds.
    /// Per SC-005: Response time must be <3 seconds.
    /// </summary>
    public long ValidationTimeMs { get; init; }

    /// <summary>
    /// Whether the transaction can proceed (passed or approved with override).
    /// </summary>
    public bool CanProceed => ValidationResult.IsValid || Transaction.ValidationStatus == Shared.Constants.TransactionTypes.ValidationStatus.ApprovedWithOverride;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static TransactionValidationResult Success(Transaction transaction, IEnumerable<TransactionLicenceUsage> usages, long validationTimeMs)
    {
        return new TransactionValidationResult
        {
            ValidationResult = ValidationResult.Success(),
            Transaction = transaction,
            LicenceUsages = usages.ToList().AsReadOnly(),
            ValidationTimeMs = validationTimeMs
        };
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static TransactionValidationResult Failure(Transaction transaction, IEnumerable<ValidationViolation> violations, IEnumerable<TransactionLicenceUsage> usages, long validationTimeMs)
    {
        return new TransactionValidationResult
        {
            ValidationResult = ValidationResult.Failure(violations),
            Transaction = transaction,
            LicenceUsages = usages.ToList().AsReadOnly(),
            ValidationTimeMs = validationTimeMs
        };
    }
}
