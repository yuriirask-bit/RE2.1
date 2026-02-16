using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for substance reclassification operations.
/// T080d: Per FR-066 requirements for reclassification tracking.
/// </summary>
public interface ISubstanceReclassificationRepository
{
    /// <summary>
    /// Gets a reclassification record by ID.
    /// </summary>
    Task<SubstanceReclassification?> GetByIdAsync(Guid reclassificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reclassifications for a specific substance.
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reclassifications with a specific status.
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetByStatusAsync(ReclassificationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reclassifications effective within a date range.
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetByEffectiveDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending reclassifications that have become effective (need processing).
    /// </summary>
    Task<IEnumerable<SubstanceReclassification>> GetPendingEffectiveReclassificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent reclassification for a substance effective as of a given date.
    /// T080m: Used for historical transaction validation.
    /// </summary>
    Task<SubstanceReclassification?> GetEffectiveReclassificationAsync(string substanceCode, DateOnly asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new reclassification record.
    /// </summary>
    Task<Guid> CreateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing reclassification record.
    /// </summary>
    Task UpdateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customer impacts for a reclassification.
    /// </summary>
    Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomerImpactsAsync(Guid reclassificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer impact record for a specific customer and reclassification.
    /// </summary>
    Task<ReclassificationCustomerImpact?> GetCustomerImpactAsync(Guid reclassificationId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers requiring re-qualification (across all reclassifications).
    /// T080j: Per FR-066 customer flagging.
    /// </summary>
    Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomersRequiringReQualificationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a customer impact record.
    /// </summary>
    Task<Guid> CreateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer impact record.
    /// </summary>
    Task UpdateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk creates customer impact records.
    /// </summary>
    Task CreateCustomerImpactsBatchAsync(IEnumerable<ReclassificationCustomerImpact> impacts, CancellationToken cancellationToken = default);
}
