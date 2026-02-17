using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP training record operations.
/// T279: CRUD for TrainingRecord per US12 (FR-050).
/// </summary>
public interface ITrainingRepository
{
    /// <summary>
    /// Gets all training records.
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific training record by ID.
    /// </summary>
    Task<TrainingRecord?> GetByIdAsync(Guid trainingRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training records for a specific staff member.
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetByStaffAsync(Guid staffMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training records for a specific site.
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training records for a specific SOP.
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetBySopAsync(Guid sopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired training records.
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new training record.
    /// </summary>
    Task<Guid> CreateAsync(TrainingRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing training record.
    /// </summary>
    Task UpdateAsync(TrainingRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a training record.
    /// </summary>
    Task DeleteAsync(Guid trainingRecordId, CancellationToken cancellationToken = default);
}
