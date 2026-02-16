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
    /// Reference to ControlledSubstance by SubstanceCode (business key).
    /// Required.
    /// </summary>
    public string SubstanceCode { get; set; } = string.Empty;

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
    /// When this mapping became effective.
    /// Required per data-model.md.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// When this mapping expires (null = no separate expiry, follows licence expiry).
    /// Per data-model.md: must not exceed licence's ExpiryDate.
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

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

        if (string.IsNullOrWhiteSpace(SubstanceCode))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SubstanceCode is required"
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

        if (EffectiveDate == default)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EffectiveDate is required"
            });
        }

        // If mapping has expiry, it must be after effective date
        if (ExpiryDate.HasValue && ExpiryDate.Value <= EffectiveDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ExpiryDate must be after EffectiveDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Validates the mapping against its parent licence per data-model.md.
    /// T079: ExpiryDate must not exceed licence's ExpiryDate.
    /// </summary>
    /// <param name="licence">The parent licence.</param>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult ValidateAgainstLicence(Licence licence)
    {
        var violations = new List<ValidationViolation>();

        // Per data-model.md: ExpiryDate must not exceed licence's ExpiryDate
        if (ExpiryDate.HasValue && licence.ExpiryDate.HasValue && ExpiryDate.Value > licence.ExpiryDate.Value)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Mapping ExpiryDate ({ExpiryDate.Value:yyyy-MM-dd}) cannot exceed licence ExpiryDate ({licence.ExpiryDate.Value:yyyy-MM-dd})"
            });
        }

        // Effective date should not be before licence issue date
        if (EffectiveDate < licence.IssueDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Mapping EffectiveDate ({EffectiveDate:yyyy-MM-dd}) cannot be before licence IssueDate ({licence.IssueDate:yyyy-MM-dd})"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}
