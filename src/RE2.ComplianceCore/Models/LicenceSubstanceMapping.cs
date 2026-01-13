using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Maps which substances a specific licence authorizes.
/// Per data-model.md entity 4: LicenceSubstanceMapping
/// T062: Domain model implementation.
/// </summary>
public class LicenceSubstanceMapping
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid MappingId { get; set; }

    /// <summary>
    /// Reference to Licence.
    /// Required.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Reference to ControlledSubstance.
    /// Required.
    /// </summary>
    public Guid SubstanceId { get; set; }

    /// <summary>
    /// Maximum quantity per single transaction (nullable for unlimited).
    /// </summary>
    public decimal? MaxQuantityPerTransaction { get; set; }

    /// <summary>
    /// Maximum cumulative quantity per period (nullable for unlimited).
    /// </summary>
    public decimal? MaxQuantityPerPeriod { get; set; }

    /// <summary>
    /// Period type (e.g., "Monthly", "Annual", "Quarterly").
    /// </summary>
    public string? PeriodType { get; set; }

    /// <summary>
    /// Additional restrictions for this specific mapping.
    /// </summary>
    public string? Restrictions { get; set; }

    /// <summary>
    /// Navigation property to Licence.
    /// </summary>
    public Licence? Licence { get; set; }

    /// <summary>
    /// Navigation property to ControlledSubstance.
    /// </summary>
    public ControlledSubstance? Substance { get; set; }

    /// <summary>
    /// Validates the mapping according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (LicenceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceId is required"
            });
        }

        if (SubstanceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SubstanceId is required"
            });
        }

        if (MaxQuantityPerTransaction.HasValue && MaxQuantityPerTransaction.Value < 0)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "MaxQuantityPerTransaction cannot be negative"
            });
        }

        if (MaxQuantityPerPeriod.HasValue && MaxQuantityPerPeriod.Value < 0)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "MaxQuantityPerPeriod cannot be negative"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}
