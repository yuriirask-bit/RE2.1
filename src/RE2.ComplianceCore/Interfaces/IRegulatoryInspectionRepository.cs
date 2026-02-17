using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for regulatory inspection records.
/// T167: Inspection recording for FR-028 audit trail.
/// </summary>
public interface IRegulatoryInspectionRepository
{
    /// <summary>
    /// Gets all inspections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All inspection records.</returns>
    Task<IEnumerable<RegulatoryInspection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an inspection by ID.
    /// </summary>
    /// <param name="inspectionId">The inspection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inspection if found; otherwise, null.</returns>
    Task<RegulatoryInspection?> GetByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections within a date range.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inspections within the date range.</returns>
    Task<IEnumerable<RegulatoryInspection>> GetByDateRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections by authority.
    /// </summary>
    /// <param name="authority">The inspecting authority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inspections by the specified authority.</returns>
    Task<IEnumerable<RegulatoryInspection>> GetByAuthorityAsync(
        InspectingAuthority authority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections with overdue corrective actions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inspections with overdue corrective actions.</returns>
    Task<IEnumerable<RegulatoryInspection>> GetWithOverdueCorrectiveActionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inspection record.
    /// </summary>
    /// <param name="inspection">The inspection to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created inspection with ID assigned.</returns>
    Task<RegulatoryInspection> CreateAsync(
        RegulatoryInspection inspection,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inspection record.
    /// </summary>
    /// <param name="inspection">The inspection to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated inspection.</returns>
    Task<RegulatoryInspection> UpdateAsync(
        RegulatoryInspection inspection,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an inspection record.
    /// </summary>
    /// <param name="inspectionId">The inspection ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid inspectionId, CancellationToken cancellationToken = default);
}
