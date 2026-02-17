using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP equipment qualification operations.
/// T256: CRUD for GdpEquipmentQualification per US11 (FR-048).
/// </summary>
public interface IGdpEquipmentRepository
{
    /// <summary>
    /// Gets all equipment qualifications.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific equipment qualification by ID.
    /// </summary>
    Task<GdpEquipmentQualification?> GetByIdAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets equipment qualifications for a specific provider.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets equipment qualifications for a specific site.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets equipment qualifications due for re-qualification within the specified days.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetDueForRequalificationAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new equipment qualification.
    /// </summary>
    Task<Guid> CreateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing equipment qualification.
    /// </summary>
    Task UpdateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an equipment qualification.
    /// </summary>
    Task DeleteAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default);
}
