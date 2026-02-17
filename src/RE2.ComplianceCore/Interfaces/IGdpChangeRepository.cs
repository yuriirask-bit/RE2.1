using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP change record operations.
/// T282: CRUD for GdpChangeRecord per US12 (FR-051).
/// </summary>
public interface IGdpChangeRepository
{
    /// <summary>
    /// Gets all change records.
    /// </summary>
    Task<IEnumerable<GdpChangeRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific change record by ID.
    /// </summary>
    Task<GdpChangeRecord?> GetByIdAsync(Guid changeRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending change records.
    /// </summary>
    Task<IEnumerable<GdpChangeRecord>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new change record.
    /// </summary>
    Task<Guid> CreateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing change record.
    /// </summary>
    Task UpdateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a change record.
    /// </summary>
    Task ApproveAsync(Guid changeRecordId, Guid approvedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a change record.
    /// </summary>
    Task RejectAsync(Guid changeRecordId, Guid rejectedBy, CancellationToken cancellationToken = default);
}
