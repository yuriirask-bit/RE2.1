using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records a controlled substance reclassification event.
/// Per FR-066: Tracks when substances move between regulatory categories
/// (e.g., Opium Act List II to List I, or added to precursor category).
/// T080a: Domain model implementation.
/// </summary>
public class SubstanceReclassification
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ReclassificationId { get; set; }

    /// <summary>
    /// Reference to the substance being reclassified (business key).
    /// Required.
    /// </summary>
    public string SubstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// Previous Opium Act classification before reclassification.
    /// </summary>
    public SubstanceCategories.OpiumActList PreviousOpiumActList { get; set; }

    /// <summary>
    /// New Opium Act classification after reclassification.
    /// </summary>
    public SubstanceCategories.OpiumActList NewOpiumActList { get; set; }

    /// <summary>
    /// Previous precursor category before reclassification.
    /// </summary>
    public SubstanceCategories.PrecursorCategory PreviousPrecursorCategory { get; set; }

    /// <summary>
    /// New precursor category after reclassification.
    /// </summary>
    public SubstanceCategories.PrecursorCategory NewPrecursorCategory { get; set; }

    /// <summary>
    /// Date when the reclassification becomes effective.
    /// Required per FR-066.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// Reference to regulatory authority document (e.g., gazette publication number).
    /// Required for audit trail per FR-066.
    /// </summary>
    public required string RegulatoryReference { get; set; }

    /// <summary>
    /// Name of the regulatory authority that issued the reclassification.
    /// </summary>
    public required string RegulatoryAuthority { get; set; }

    /// <summary>
    /// Reason or justification for the reclassification.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Status of the reclassification processing.
    /// </summary>
    public ReclassificationStatus Status { get; set; } = ReclassificationStatus.Pending;

    /// <summary>
    /// Number of customers affected by this reclassification.
    /// Populated after impact analysis per FR-066.
    /// </summary>
    public int AffectedCustomerCount { get; set; }

    /// <summary>
    /// Number of customers flagged for re-qualification.
    /// Per FR-066: Customers whose existing licences are insufficient.
    /// </summary>
    public int FlaggedCustomerCount { get; set; }

    /// <summary>
    /// User who initiated the reclassification.
    /// </summary>
    public Guid? InitiatedByUserId { get; set; }

    /// <summary>
    /// When the reclassification was recorded in the system.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When impact analysis was completed.
    /// </summary>
    public DateTime? ProcessedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the affected substance.
    /// </summary>
    public ControlledSubstance? Substance { get; set; }

    /// <summary>
    /// Navigation property to affected customer records.
    /// </summary>
    public List<ReclassificationCustomerImpact>? AffectedCustomers { get; set; }

    /// <summary>
    /// Validates the reclassification record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(SubstanceCode))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SubstanceCode is required"
            });
        }

        if (string.IsNullOrWhiteSpace(RegulatoryReference))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "RegulatoryReference is required for audit trail"
            });
        }

        if (string.IsNullOrWhiteSpace(RegulatoryAuthority))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "RegulatoryAuthority is required"
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

        // Verify that at least one classification actually changed
        if (PreviousOpiumActList == NewOpiumActList && PreviousPrecursorCategory == NewPrecursorCategory)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Reclassification must change at least one classification (OpiumActList or PrecursorCategory)"
            });
        }

        // Verify new classification is valid (at least one of OpiumActList or PrecursorCategory must be non-None)
        if (NewOpiumActList == SubstanceCategories.OpiumActList.None &&
            NewPrecursorCategory == SubstanceCategories.PrecursorCategory.None)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "New classification must have at least one of OpiumActList or PrecursorCategory specified"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Determines if this reclassification increases regulatory requirements.
    /// Per FR-066: Used to identify customers needing re-qualification.
    /// </summary>
    public bool IsUpgrade()
    {
        // Opium Act regulatory severity: None (0) < ListII (2) < ListI (1) in strictness
        // ListI is the most restrictive despite having lower numeric value
        var opiumUpgrade = GetOpiumActSeverity(NewOpiumActList) > GetOpiumActSeverity(PreviousOpiumActList);

        // Precursor regulatory severity: None (0) < Category3 (3) < Category2 (2) < Category1 (1)
        // Category1 is most restrictive despite having lowest numeric value after None
        var precursorUpgrade = GetPrecursorSeverity(NewPrecursorCategory) > GetPrecursorSeverity(PreviousPrecursorCategory);

        return opiumUpgrade || precursorUpgrade;
    }

    /// <summary>
    /// Gets the regulatory severity level for Opium Act classification.
    /// Higher value = more restrictive.
    /// </summary>
    public static int GetOpiumActSeverity(SubstanceCategories.OpiumActList list) => list switch
    {
        SubstanceCategories.OpiumActList.None => 0,
        SubstanceCategories.OpiumActList.ListII => 1,  // Less restrictive (therapeutic use)
        SubstanceCategories.OpiumActList.ListI => 2,   // Most restrictive (hard drugs)
        _ => 0
    };

    /// <summary>
    /// Gets the regulatory severity level for Precursor category.
    /// Higher value = more restrictive.
    /// </summary>
    public static int GetPrecursorSeverity(SubstanceCategories.PrecursorCategory category) => category switch
    {
        SubstanceCategories.PrecursorCategory.None => 0,
        SubstanceCategories.PrecursorCategory.Category3 => 1,  // Least restrictive
        SubstanceCategories.PrecursorCategory.Category2 => 2,
        SubstanceCategories.PrecursorCategory.Category1 => 3,  // Most restrictive
        _ => 0
    };

    /// <summary>
    /// Determines if this reclassification is currently in effect.
    /// </summary>
    public bool IsEffective()
    {
        return EffectiveDate <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

/// <summary>
/// Status of a substance reclassification.
/// </summary>
public enum ReclassificationStatus
{
    /// <summary>
    /// Reclassification recorded but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Impact analysis in progress.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Impact analysis completed, customers flagged.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Reclassification cancelled (e.g., regulatory change reversed).
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Records the impact of a reclassification on a specific customer.
/// Per FR-066: Tracks which customers are affected and their re-qualification status.
/// </summary>
public class ReclassificationCustomerImpact
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ImpactId { get; set; }

    /// <summary>
    /// Reference to the reclassification event.
    /// </summary>
    public Guid ReclassificationId { get; set; }

    /// <summary>
    /// Reference to the affected customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Customer's name (denormalized for reporting).
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Whether customer's existing licences cover the new classification.
    /// Per FR-066: False means customer needs re-qualification.
    /// </summary>
    public bool HasSufficientLicence { get; set; }

    /// <summary>
    /// Whether customer has been flagged for re-qualification.
    /// Per FR-066: Set when HasSufficientLicence is false.
    /// </summary>
    public bool RequiresReQualification { get; set; }

    /// <summary>
    /// IDs of licences that authorize this substance for the customer.
    /// </summary>
    public List<Guid>? RelevantLicenceIds { get; set; }

    /// <summary>
    /// Summary of what licences are missing or insufficient.
    /// </summary>
    public string? LicenceGapSummary { get; set; }

    /// <summary>
    /// Whether customer has been notified of the reclassification.
    /// </summary>
    public bool NotificationSent { get; set; }

    /// <summary>
    /// When the customer was notified.
    /// </summary>
    public DateTime? NotificationDate { get; set; }

    /// <summary>
    /// When re-qualification was completed (licence updated).
    /// </summary>
    public DateTime? ReQualificationDate { get; set; }

    /// <summary>
    /// When this impact record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the reclassification.
    /// </summary>
    public SubstanceReclassification? Reclassification { get; set; }
}
