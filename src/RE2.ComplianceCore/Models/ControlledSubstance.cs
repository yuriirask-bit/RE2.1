using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a regulated drug or precursor subject to compliance checks.
/// Per data-model.md entity 3: ControlledSubstance
/// T061: Domain model implementation.
/// </summary>
public class ControlledSubstance
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid SubstanceId { get; set; }

    /// <summary>
    /// Common name of the substance.
    /// Required.
    /// </summary>
    public required string SubstanceName { get; set; }

    /// <summary>
    /// Dutch Opium Act classification (None, ListI, ListII).
    /// Per data-model.md: At least one of OpiumActList or PrecursorCategory must be specified.
    /// </summary>
    public SubstanceCategories.OpiumActList OpiumActList { get; set; } = SubstanceCategories.OpiumActList.None;

    /// <summary>
    /// EU precursor regulation category (None, Category1, Category2, Category3).
    /// </summary>
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; } = SubstanceCategories.PrecursorCategory.None;

    /// <summary>
    /// Company's internal product/substance code.
    /// Required, must be unique.
    /// </summary>
    public required string InternalCode { get; set; }

    /// <summary>
    /// Additional restrictions or notes.
    /// </summary>
    public string? RegulatoryRestrictions { get; set; }

    /// <summary>
    /// Whether substance is still in use.
    /// Default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the controlled substance according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(SubstanceName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SubstanceName is required"
            });
        }

        if (string.IsNullOrWhiteSpace(InternalCode))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "InternalCode is required"
            });
        }

        // Per data-model.md: At least one of OpiumActList or PrecursorCategory must be specified
        if (OpiumActList == SubstanceCategories.OpiumActList.None &&
            PrecursorCategory == SubstanceCategories.PrecursorCategory.None)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "At least one of OpiumActList or PrecursorCategory must be specified (not both None)"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if this substance is controlled under the Opium Act.
    /// </summary>
    /// <returns>True if classified as List I or List II.</returns>
    public bool IsOpiumActControlled()
    {
        return OpiumActList != SubstanceCategories.OpiumActList.None;
    }

    /// <summary>
    /// Checks if this substance is a precursor.
    /// </summary>
    /// <returns>True if classified in any precursor category.</returns>
    public bool IsPrecursor()
    {
        return PrecursorCategory != SubstanceCategories.PrecursorCategory.None;
    }
}
