using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a specific compliance violation found during transaction validation.
/// T125: TransactionViolation domain model for detailed violation tracking.
/// </summary>
public class TransactionViolation
{
    /// <summary>
    /// Unique identifier for this violation record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Transaction ID this violation belongs to.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Type of violation.
    /// </summary>
    public ViolationType ViolationType { get; set; }

    /// <summary>
    /// Error code (from ErrorCodes constants).
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable violation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level (Info, Warning, Critical).
    /// </summary>
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Critical;

    #region Context

    /// <summary>
    /// Line number where violation occurred (null for transaction-level).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Substance code related to this violation (business key).
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Substance name (denormalized).
    /// </summary>
    public string? SubstanceName { get; set; }

    /// <summary>
    /// Customer ID related to this violation.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Customer name (denormalized).
    /// </summary>
    public string? CustomerName { get; set; }

    #endregion

    #region Licence Context

    /// <summary>
    /// Required licence type that is missing or invalid.
    /// </summary>
    public string? RequiredLicenceType { get; set; }

    /// <summary>
    /// Licence ID related to this violation (e.g., expired licence).
    /// </summary>
    public Guid? LicenceId { get; set; }

    /// <summary>
    /// Licence number (denormalized).
    /// </summary>
    public string? LicenceNumber { get; set; }

    /// <summary>
    /// Licence expiry date (if relevant).
    /// </summary>
    public DateOnly? LicenceExpiryDate { get; set; }

    #endregion

    #region Threshold Context

    /// <summary>
    /// Threshold ID that was exceeded (if applicable).
    /// </summary>
    public Guid? ThresholdId { get; set; }

    /// <summary>
    /// Threshold type that was exceeded.
    /// </summary>
    public ThresholdType? ThresholdType { get; set; }

    /// <summary>
    /// Threshold limit value.
    /// </summary>
    public decimal? ThresholdLimit { get; set; }

    /// <summary>
    /// Actual value that exceeded the threshold.
    /// </summary>
    public decimal? ActualValue { get; set; }

    /// <summary>
    /// Period for the threshold.
    /// </summary>
    public ThresholdPeriod? ThresholdPeriod { get; set; }

    #endregion

    #region Override

    /// <summary>
    /// Whether this specific violation can be overridden.
    /// Per FR-019a: Some violations allow manual override.
    /// </summary>
    public bool CanOverride { get; set; }

    /// <summary>
    /// Whether this violation has been overridden.
    /// </summary>
    public bool IsOverridden { get; set; }

    /// <summary>
    /// User who overrode this violation.
    /// </summary>
    public string? OverriddenBy { get; set; }

    /// <summary>
    /// Date/time when override was applied.
    /// </summary>
    public DateTime? OverriddenAt { get; set; }

    /// <summary>
    /// Reason for override.
    /// </summary>
    public string? OverrideReason { get; set; }

    #endregion

    #region Audit

    /// <summary>
    /// Timestamp when violation was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a licence-related violation.
    /// </summary>
    public static TransactionViolation LicenceViolation(
        Guid transactionId,
        ViolationType type,
        string errorCode,
        string message,
        Guid? licenceId = null,
        string? licenceNumber = null,
        DateOnly? expiryDate = null,
        int? lineNumber = null,
        string? substanceCode = null,
        bool canOverride = false)
    {
        return new TransactionViolation
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ViolationType = type,
            ErrorCode = errorCode,
            Message = message,
            Severity = ViolationSeverity.Critical,
            LicenceId = licenceId,
            LicenceNumber = licenceNumber,
            LicenceExpiryDate = expiryDate,
            LineNumber = lineNumber,
            SubstanceCode = substanceCode,
            CanOverride = canOverride,
            DetectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a threshold violation.
    /// </summary>
    public static TransactionViolation ThresholdViolation(
        Guid transactionId,
        string errorCode,
        string message,
        ThresholdType thresholdType,
        decimal limit,
        decimal actualValue,
        ThresholdPeriod period,
        Guid? thresholdId = null,
        int? lineNumber = null,
        string? substanceCode = null,
        bool canOverride = true)
    {
        return new TransactionViolation
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ViolationType = ViolationType.ThresholdExceeded,
            ErrorCode = errorCode,
            Message = message,
            Severity = ViolationSeverity.Critical,
            ThresholdId = thresholdId,
            ThresholdType = thresholdType,
            ThresholdLimit = limit,
            ActualValue = actualValue,
            ThresholdPeriod = period,
            LineNumber = lineNumber,
            SubstanceCode = substanceCode,
            CanOverride = canOverride,
            DetectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a customer qualification violation.
    /// </summary>
    public static TransactionViolation CustomerViolation(
        Guid transactionId,
        ViolationType type,
        string errorCode,
        string message,
        Guid customerId,
        string? customerName = null,
        bool canOverride = false)
    {
        return new TransactionViolation
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ViolationType = type,
            ErrorCode = errorCode,
            Message = message,
            Severity = ViolationSeverity.Critical,
            CustomerId = customerId,
            CustomerName = customerName,
            CanOverride = canOverride,
            DetectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a cross-border permit violation.
    /// </summary>
    public static TransactionViolation PermitViolation(
        Guid transactionId,
        string errorCode,
        string message,
        string requiredPermitType,
        bool canOverride = false)
    {
        return new TransactionViolation
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ViolationType = ViolationType.CrossBorderNoPermit,
            ErrorCode = errorCode,
            Message = message,
            Severity = ViolationSeverity.Critical,
            RequiredLicenceType = requiredPermitType,
            CanOverride = canOverride,
            DetectedAt = DateTime.UtcNow
        };
    }

    #endregion
}
