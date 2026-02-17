namespace RE2.Shared.Models;

/// <summary>
/// Standardized error response DTO per transaction-validation-api.yaml ErrorResponse schema.
/// T047: Standardized error response DTOs per FR-064.
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Standardized error code (e.g., "LICENCE_EXPIRED", "VALIDATION_ERROR").
    /// </summary>
    public required string ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message describing what went wrong.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Request trace identifier for debugging and support purposes.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601 UTC format).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional error details (e.g., stack trace in development, field validation errors).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// List of field-specific validation errors (for 400 Bad Request responses).
    /// </summary>
    public List<FieldError>? FieldErrors { get; set; }
}

/// <summary>
/// Field-level validation error.
/// </summary>
public class FieldError
{
    /// <summary>
    /// Name of the field that failed validation.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Error code for this specific field (e.g., "REQUIRED", "INVALID_FORMAT").
    /// </summary>
    public required string ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message for this field.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// The invalid value that was provided (if applicable).
    /// </summary>
    public object? AttemptedValue { get; set; }
}

/// <summary>
/// Compliance violation response DTO per transaction-validation-api.yaml ComplianceViolation schema.
/// Used in transaction validation responses.
/// </summary>
public class ComplianceViolationDto
{
    /// <summary>
    /// Type of violation (standardized error code per FR-064).
    /// </summary>
    public required string ViolationType { get; set; }

    /// <summary>
    /// Severity level: "Info", "Warning", or "Critical".
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Transaction line number where violation occurred (null for transaction-level violations).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Internal substance code related to this violation.
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Detailed violation message explaining what went wrong.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Required licence type that is missing or invalid.
    /// </summary>
    public string? RequiredLicenceType { get; set; }

    /// <summary>
    /// Licence number that is expired, suspended, or missing.
    /// </summary>
    public string? MissingLicenceNumber { get; set; }

    /// <summary>
    /// Type of threshold exceeded (MonthlyQuantity or AnnualFrequency).
    /// </summary>
    public string? ThresholdType { get; set; }

    /// <summary>
    /// Threshold limit value.
    /// </summary>
    public decimal? ThresholdLimit { get; set; }

    /// <summary>
    /// Current period cumulative quantity.
    /// </summary>
    public decimal? CurrentPeriodTotal { get; set; }

    /// <summary>
    /// Quantity in this order.
    /// </summary>
    public decimal? ThisOrderQuantity { get; set; }
}

/// <summary>
/// Validation result response DTO for transaction validation API.
/// Per transaction-validation-api.yaml ValidationResult schema.
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Request ID for tracking.
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Timestamp of validation (ISO 8601 UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Overall validation status: "Passed", "Failed", or "Warning".
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// List of compliance violations found (empty if status is "Passed").
    /// </summary>
    public List<ComplianceViolationDto> Violations { get; set; } = new();

    /// <summary>
    /// Licences that authorized this transaction.
    /// </summary>
    public List<LicenceUsageDto>? LicencesUsed { get; set; }
}

/// <summary>
/// Licence usage information in transaction validation response.
/// Per transaction-validation-api.yaml LicenceUsage schema.
/// </summary>
public class LicenceUsageDto
{
    /// <summary>
    /// Licence ID.
    /// </summary>
    public required string LicenceId { get; set; }

    /// <summary>
    /// Official licence number.
    /// </summary>
    public required string LicenceNumber { get; set; }

    /// <summary>
    /// Licence type name.
    /// </summary>
    public required string LicenceType { get; set; }

    /// <summary>
    /// Holder type: "Customer" or "Company".
    /// </summary>
    public required string Holder { get; set; }
}
