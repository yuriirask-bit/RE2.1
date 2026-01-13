using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Value object representing the result of a compliance validation operation.
/// Immutable, supports fluent API for combining multiple validation results.
/// Used throughout compliance services for transaction validation, licence checks, GDP validation, etc.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed (no violations).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation violations (empty if valid).
    /// </summary>
    public IReadOnlyList<ValidationViolation> Violations { get; init; }

    /// <summary>
    /// Gets whether the validation result allows override approval (some violations can be manually approved).
    /// Per FR-019a: Some blocked transactions can be approved by ComplianceManager role.
    /// </summary>
    public bool CanOverride { get; init; }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private ValidationResult(bool isValid, IEnumerable<ValidationViolation> violations, bool canOverride = false)
    {
        IsValid = isValid;
        Violations = violations.ToList().AsReadOnly();
        CanOverride = canOverride && !isValid; // Can only override if there are violations
    }

    #region Factory Methods

    /// <summary>
    /// Creates a successful validation result (no violations).
    /// </summary>
    /// <returns>ValidationResult indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult(true, Array.Empty<ValidationViolation>(), canOverride: false);
    }

    /// <summary>
    /// Creates a failed validation result with a single violation.
    /// </summary>
    /// <param name="violation">The validation violation.</param>
    /// <returns>ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(ValidationViolation violation)
    {
        return new ValidationResult(false, new[] { violation }, canOverride: violation.CanOverride);
    }

    /// <summary>
    /// Creates a failed validation result with multiple violations.
    /// </summary>
    /// <param name="violations">The collection of validation violations.</param>
    /// <returns>ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationViolation> violations)
    {
        var violationList = violations.ToList();
        var canOverride = violationList.Any() && violationList.All(v => v.CanOverride);
        return new ValidationResult(false, violationList, canOverride);
    }

    /// <summary>
    /// Creates a failed validation result with a simple error message.
    /// </summary>
    /// <param name="errorCode">The error code (from ErrorCodes constants).</param>
    /// <param name="message">The error message.</param>
    /// <param name="severity">The violation severity (default: Critical).</param>
    /// <param name="canOverride">Whether this violation can be overridden (default: false).</param>
    /// <returns>ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(string errorCode, string message, ViolationSeverity severity = ViolationSeverity.Critical, bool canOverride = false)
    {
        var violation = new ValidationViolation
        {
            ErrorCode = errorCode,
            Message = message,
            Severity = severity,
            CanOverride = canOverride
        };
        return Failure(violation);
    }

    #endregion

    #region Fluent Combination Methods

    /// <summary>
    /// Combines this validation result with another.
    /// Result is valid only if both are valid.
    /// </summary>
    /// <param name="other">Another validation result to combine.</param>
    /// <returns>Combined validation result.</returns>
    public ValidationResult Combine(ValidationResult other)
    {
        if (this.IsValid && other.IsValid)
            return Success();

        var allViolations = this.Violations.Concat(other.Violations);
        return Failure(allViolations);
    }

    /// <summary>
    /// Combines multiple validation results.
    /// Result is valid only if all are valid.
    /// </summary>
    /// <param name="results">Validation results to combine.</param>
    /// <returns>Combined validation result.</returns>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        return Combine(results.AsEnumerable());
    }

    /// <summary>
    /// Combines multiple validation results.
    /// Result is valid only if all are valid.
    /// </summary>
    /// <param name="results">Validation results to combine.</param>
    /// <returns>Combined validation result.</returns>
    public static ValidationResult Combine(IEnumerable<ValidationResult> results)
    {
        var resultList = results.ToList();

        if (resultList.All(r => r.IsValid))
            return Success();

        var allViolations = resultList.SelectMany(r => r.Violations);
        return Failure(allViolations);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets whether the validation has any critical violations.
    /// </summary>
    public bool HasCriticalViolations => Violations.Any(v => v.Severity == ViolationSeverity.Critical);

    /// <summary>
    /// Gets whether the validation has any warnings (non-critical violations).
    /// </summary>
    public bool HasWarnings => Violations.Any(v => v.Severity == ViolationSeverity.Warning);

    /// <summary>
    /// Gets violations of a specific severity.
    /// </summary>
    /// <param name="severity">The severity to filter by.</param>
    /// <returns>Filtered list of violations.</returns>
    public IEnumerable<ValidationViolation> GetViolationsBySeverity(ViolationSeverity severity)
    {
        return Violations.Where(v => v.Severity == severity);
    }

    /// <summary>
    /// Gets violations by error code.
    /// </summary>
    /// <param name="errorCode">The error code to filter by.</param>
    /// <returns>Filtered list of violations.</returns>
    public IEnumerable<ValidationViolation> GetViolationsByErrorCode(string errorCode)
    {
        return Violations.Where(v => v.ErrorCode == errorCode);
    }

    #endregion
}

/// <summary>
/// Represents a single validation violation.
/// </summary>
public sealed class ValidationViolation
{
    /// <summary>
    /// Gets or sets the standardized error code (per FR-064).
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the violation severity level.
    /// </summary>
    public ViolationSeverity Severity { get; init; } = ViolationSeverity.Critical;

    /// <summary>
    /// Gets or sets whether this violation can be overridden by ComplianceManager (per FR-019a).
    /// </summary>
    public bool CanOverride { get; init; } = false;

    /// <summary>
    /// Gets or sets the transaction line number where violation occurred (null for transaction-level violations).
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets or sets the substance code related to this violation.
    /// </summary>
    public string? SubstanceCode { get; init; }

    /// <summary>
    /// Gets or sets the required licence type that is missing or invalid.
    /// </summary>
    public string? RequiredLicenceType { get; init; }

    /// <summary>
    /// Gets or sets the licence number that is expired, suspended, or missing.
    /// </summary>
    public string? LicenceNumber { get; init; }

    /// <summary>
    /// Gets or sets additional context data for the violation.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Severity levels for validation violations.
/// Corresponds to ComplianceViolation.severity enum in transaction-validation-api.yaml
/// </summary>
public enum ViolationSeverity
{
    /// <summary>
    /// Informational message (no blocking).
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message (should be reviewed but not blocking).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Critical violation (blocks transaction processing).
    /// </summary>
    Critical = 2
}
