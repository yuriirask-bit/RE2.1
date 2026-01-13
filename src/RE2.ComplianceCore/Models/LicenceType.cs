using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a category of legal authorization with defined rules and requirements.
/// Per data-model.md entity 2: LicenceType
/// T060: Domain model implementation.
/// </summary>
public class LicenceType
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid LicenceTypeId { get; set; }

    /// <summary>
    /// Type name (e.g., "Wholesale Licence (WDA)", "Opium Act Exemption").
    /// Required, must be unique.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Typical authority for this type (e.g., "IGJ", "Farmatec").
    /// Required.
    /// </summary>
    public required string IssuingAuthority { get; set; }

    /// <summary>
    /// Standard validity period in months (nullable for permanent licences).
    /// </summary>
    public int? TypicalValidityMonths { get; set; }

    /// <summary>
    /// Activities this type authorizes (flags enum).
    /// Required, must have at least one activity.
    /// </summary>
    public LicenceTypes.PermittedActivity PermittedActivities { get; set; }

    /// <summary>
    /// Whether this type is still in use.
    /// Default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the licence type according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Name is required"
            });
        }

        if (string.IsNullOrWhiteSpace(IssuingAuthority))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "IssuingAuthority is required"
            });
        }

        if (PermittedActivities == LicenceTypes.PermittedActivity.None)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "PermittedActivities must include at least one activity"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Deactivates this licence type.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates this licence type.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
