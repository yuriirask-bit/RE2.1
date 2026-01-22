using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for substance reclassification operations.
/// T080f: Per FR-066 requirements for reclassification workflow.
/// </summary>
public interface ISubstanceReclassificationService
{
    /// <summary>
    /// Gets a reclassification by ID.
    /// </summary>
    Task<SubstanceReclassification?> GetByIdAsync(Guid reclassificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reclassifications for a substance.
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending reclassifications that need processing.
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetPendingReclassificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new reclassification record.
    /// Per FR-066: Records the new classification with effective date.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateReclassificationAsync(
        SubstanceReclassification reclassification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs customer impact analysis for a reclassification.
    /// Per FR-066: Identifies all customers whose licence scope includes the substance,
    /// validates whether existing licences authorize the new classification,
    /// and flags affected customers for re-qualification.
    /// </summary>
    Task<ReclassificationImpactAnalysis> AnalyzeCustomerImpactAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a reclassification: runs impact analysis, flags customers, generates notifications.
    /// Per FR-066: Full reclassification workflow.
    /// </summary>
    Task<ValidationResult> ProcessReclassificationAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a customer as re-qualified after licence update.
    /// Per FR-066: Clears the "Requires Re-Qualification" flag.
    /// </summary>
    Task<ValidationResult> MarkCustomerReQualifiedAsync(
        Guid reclassificationId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer is blocked from transactions due to reclassification.
    /// Per FR-066: Returns true if customer requires re-qualification for any substance.
    /// </summary>
    Task<(bool IsBlocked, IEnumerable<ReclassificationCustomerImpact> BlockingImpacts)> CheckCustomerBlockedAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective classification for a substance at a specific date.
    /// Per FR-066 (T080m): Historical transactions remain valid under classification at time of transaction.
    /// </summary>
    Task<SubstanceClassification> GetEffectiveClassificationAsync(
        Guid substanceId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates compliance team notification data for affected customers.
    /// Per FR-066 (T080k): Lists affected customers and required actions.
    /// </summary>
    Task<ComplianceNotification> GenerateComplianceNotificationAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of customer impact analysis for a reclassification.
/// </summary>
public class ReclassificationImpactAnalysis
{
    /// <summary>
    /// The reclassification being analyzed.
    /// </summary>
    public required SubstanceReclassification Reclassification { get; set; }

    /// <summary>
    /// Total number of customers with licences covering this substance.
    /// </summary>
    public int TotalAffectedCustomers { get; set; }

    /// <summary>
    /// Number of customers whose licences are sufficient for new classification.
    /// </summary>
    public int CustomersWithSufficientLicences { get; set; }

    /// <summary>
    /// Number of customers flagged for re-qualification.
    /// </summary>
    public int CustomersFlaggedForReQualification { get; set; }

    /// <summary>
    /// Detailed impact records for each customer.
    /// </summary>
    public List<ReclassificationCustomerImpact> CustomerImpacts { get; set; } = new();
}

/// <summary>
/// Classification state at a point in time.
/// </summary>
public class SubstanceClassification
{
    public Guid SubstanceId { get; set; }
    public DateOnly AsOfDate { get; set; }
    public Shared.Constants.SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public Shared.Constants.SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }
    public Guid? SourceReclassificationId { get; set; }
}

/// <summary>
/// Notification data for compliance team.
/// T080k: Per FR-066 compliance team notification generation.
/// </summary>
public class ComplianceNotification
{
    public Guid ReclassificationId { get; set; }
    public required string SubstanceName { get; set; }
    public required string RegulatoryReference { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public int TotalAffectedCustomers { get; set; }
    public int CustomersRequiringAction { get; set; }
    public List<CustomerActionRequired> RequiredActions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Action required for a specific customer.
/// </summary>
public class CustomerActionRequired
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public required string ActionRequired { get; set; }
    public string? LicenceGapSummary { get; set; }
    public List<Guid> RelevantLicenceIds { get; set; } = new();
}
