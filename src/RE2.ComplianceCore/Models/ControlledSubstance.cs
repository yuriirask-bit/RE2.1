using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Composite model for a regulated drug or precursor subject to compliance checks.
/// D365 F&O product attributes provide classification (SubstanceCode, OpiumActList, PrecursorCategory).
/// Dataverse phr_substancecomplianceextension stores compliance metadata.
/// Business key: SubstanceCode (string) — the value of the ControlledSubstance product attribute.
/// </summary>
public class ControlledSubstance
{
    #region Business Key

    /// <summary>
    /// Business key — the substance code from D365 product attribute (e.g., "Morphine", "Fentanyl").
    /// </summary>
    public required string SubstanceCode { get; set; }

    #endregion

    #region D365 Read-Only (from product attributes, denormalized)

    /// <summary>
    /// Common name of the substance.
    /// Required.
    /// </summary>
    public required string SubstanceName { get; set; }

    /// <summary>
    /// Dutch Opium Act classification (None, ListI, ListII).
    /// Sourced from D365 product attribute.
    /// </summary>
    public SubstanceCategories.OpiumActList OpiumActList { get; set; } = SubstanceCategories.OpiumActList.None;

    /// <summary>
    /// EU precursor regulation category (None, Category1, Category2, Category3).
    /// Sourced from D365 product attribute.
    /// </summary>
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; } = SubstanceCategories.PrecursorCategory.None;

    #endregion

    #region Dataverse Compliance Extension

    /// <summary>
    /// PK for Dataverse phr_substancecomplianceextension record.
    /// Guid.Empty if no compliance extension is configured yet.
    /// </summary>
    public Guid ComplianceExtensionId { get; set; }

    /// <summary>
    /// Additional restrictions or notes (Dataverse compliance extension).
    /// </summary>
    public string? RegulatoryRestrictions { get; set; }

    /// <summary>
    /// Whether substance is still in use (Dataverse compliance extension).
    /// Default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the current classification became effective (Dataverse compliance extension).
    /// </summary>
    public DateOnly? ClassificationEffectiveDate { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether a Dataverse compliance extension has been configured for this substance.
    /// </summary>
    public bool IsComplianceConfigured => ComplianceExtensionId != Guid.Empty;

    /// <summary>
    /// Backward compatibility alias for SubstanceCode.
    /// </summary>
    public string InternalCode => SubstanceCode;

    #endregion

    /// <summary>
    /// Navigation property to reclassification history.
    /// </summary>
    public List<SubstanceReclassification>? ReclassificationHistory { get; set; }

    /// <summary>
    /// Validates the controlled substance according to business rules.
    /// </summary>
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

        if (string.IsNullOrWhiteSpace(SubstanceCode))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SubstanceCode is required"
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
    public bool IsOpiumActControlled()
    {
        return OpiumActList != SubstanceCategories.OpiumActList.None;
    }

    /// <summary>
    /// Checks if this substance is a precursor.
    /// </summary>
    public bool IsPrecursor()
    {
        return PrecursorCategory != SubstanceCategories.PrecursorCategory.None;
    }

    /// <summary>
    /// Gets the classification that was effective at a specific date.
    /// </summary>
    public (SubstanceCategories.OpiumActList OpiumActList, SubstanceCategories.PrecursorCategory PrecursorCategory) GetClassificationAsOf(DateOnly asOfDate)
    {
        if (ReclassificationHistory == null || !ReclassificationHistory.Any())
        {
            return (OpiumActList, PrecursorCategory);
        }

        var effectiveReclassifications = ReclassificationHistory
            .Where(r => r.EffectiveDate <= asOfDate && r.Status == ReclassificationStatus.Completed)
            .OrderByDescending(r => r.EffectiveDate)
            .ToList();

        if (!effectiveReclassifications.Any())
        {
            var firstReclassification = ReclassificationHistory
                .Where(r => r.Status == ReclassificationStatus.Completed)
                .OrderBy(r => r.EffectiveDate)
                .FirstOrDefault();

            if (firstReclassification != null && firstReclassification.EffectiveDate > asOfDate)
            {
                return (firstReclassification.PreviousOpiumActList, firstReclassification.PreviousPrecursorCategory);
            }

            return (OpiumActList, PrecursorCategory);
        }

        var latestReclassification = effectiveReclassifications.First();
        return (latestReclassification.NewOpiumActList, latestReclassification.NewPrecursorCategory);
    }

    /// <summary>
    /// Checks if this substance has been reclassified since a given date.
    /// </summary>
    public bool HasBeenReclassifiedSince(DateOnly sinceDate)
    {
        if (ReclassificationHistory == null)
            return false;

        return ReclassificationHistory.Any(r =>
            r.EffectiveDate > sinceDate &&
            r.Status == ReclassificationStatus.Completed);
    }
}
