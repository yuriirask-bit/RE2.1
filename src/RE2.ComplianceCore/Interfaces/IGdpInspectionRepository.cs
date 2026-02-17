using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP inspection and finding operations.
/// T220: CRUD for GdpInspection and GdpInspectionFinding.
/// Per User Story 9 (FR-040).
/// </summary>
public interface IGdpInspectionRepository
{
    #region GdpInspection Operations

    /// <summary>
    /// Gets all GDP inspections.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP inspection by ID.
    /// </summary>
    Task<GdpInspection?> GetByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections for a specific GDP site.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections within a date range.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections by type.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetByTypeAsync(GdpInspectionType inspectionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP inspection.
    /// </summary>
    Task<Guid> CreateAsync(GdpInspection inspection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP inspection.
    /// </summary>
    Task UpdateAsync(GdpInspection inspection, CancellationToken cancellationToken = default);

    #endregion

    #region GdpInspectionFinding Operations

    /// <summary>
    /// Gets all findings for an inspection.
    /// </summary>
    Task<IEnumerable<GdpInspectionFinding>> GetFindingsAsync(Guid inspectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific finding by ID.
    /// </summary>
    Task<GdpInspectionFinding?> GetFindingByIdAsync(Guid findingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a finding to an inspection.
    /// </summary>
    Task<Guid> CreateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing finding.
    /// </summary>
    Task UpdateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a finding.
    /// </summary>
    Task DeleteFindingAsync(Guid findingId, CancellationToken cancellationToken = default);

    #endregion
}
