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
    /// When the current classification became effective.
    /// T080c: Tracks classification changes per FR-066.
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

    /// <summary>
    /// Navigation property to reclassification history.
    /// T080c: Per FR-066, historical transactions remain valid under classification at time of transaction.
    /// </summary>
    public List<SubstanceReclassification>? ReclassificationHistory { get; set; }

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

    /// <summary>
    /// Gets the classification that was effective at a specific date.
    /// T080m: Per FR-066, historical transactions remain valid under classification at time of transaction.
    /// </summary>
    /// <param name="asOfDate">The date to check classification for.</param>
    /// <returns>Tuple of (OpiumActList, PrecursorCategory) effective at that date.</returns>
    public (SubstanceCategories.OpiumActList OpiumActList, SubstanceCategories.PrecursorCategory PrecursorCategory) GetClassificationAsOf(DateOnly asOfDate)
    {
        if (ReclassificationHistory == null || !ReclassificationHistory.Any())
        {
            // No reclassification history, current classification applies
            return (OpiumActList, PrecursorCategory);
        }

        // Find the most recent reclassification before or on the given date
        var effectiveReclassifications = ReclassificationHistory
            .Where(r => r.EffectiveDate <= asOfDate && r.Status == ReclassificationStatus.Completed)
            .OrderByDescending(r => r.EffectiveDate)
            .ToList();

        if (!effectiveReclassifications.Any())
        {
            // No reclassifications before this date, need to find the first one
            // and use its "previous" values
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

        // Return the "new" classification from the most recent effective reclassification
        var latestReclassification = effectiveReclassifications.First();
        return (latestReclassification.NewOpiumActList, latestReclassification.NewPrecursorCategory);
    }

    /// <summary>
    /// Checks if this substance has been reclassified since a given date.
    /// </summary>
    /// <param name="sinceDate">The date to check from.</param>
    /// <returns>True if reclassified since the given date.</returns>
    public bool HasBeenReclassifiedSince(DateOnly sinceDate)
    {
        if (ReclassificationHistory == null)
            return false;

        return ReclassificationHistory.Any(r =>
            r.EffectiveDate > sinceDate &&
            r.Status == ReclassificationStatus.Completed);
    }
}
