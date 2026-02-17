using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for CAPA (Corrective and Preventive Action) operations.
/// T222: CRUD for Capa.
/// Per User Story 9 (FR-041, FR-042).
/// </summary>
public interface ICapaRepository
{
    /// <summary>
    /// Gets all CAPAs.
    /// </summary>
    Task<IEnumerable<Capa>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific CAPA by ID.
    /// </summary>
    Task<Capa?> GetByIdAsync(Guid capaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets CAPAs by status.
    /// </summary>
    Task<IEnumerable<Capa>> GetByStatusAsync(CapaStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets CAPAs for a specific finding.
    /// </summary>
    Task<IEnumerable<Capa>> GetByFindingAsync(Guid findingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue CAPAs (past due date and not completed).
    /// </summary>
    Task<IEnumerable<Capa>> GetOverdueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets CAPAs by owner name.
    /// </summary>
    Task<IEnumerable<Capa>> GetByOwnerAsync(string ownerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new CAPA.
    /// </summary>
    Task<Guid> CreateAsync(Capa capa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing CAPA.
    /// </summary>
    Task UpdateAsync(Capa capa, CancellationToken cancellationToken = default);
}
