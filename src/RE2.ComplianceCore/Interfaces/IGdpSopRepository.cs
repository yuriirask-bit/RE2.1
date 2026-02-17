using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP SOP and site-SOP link operations.
/// T276: CRUD for GdpSop and GdpSiteSop per US12 (FR-049).
/// </summary>
public interface IGdpSopRepository
{
    #region GdpSop Operations

    /// <summary>
    /// Gets all SOPs.
    /// </summary>
    Task<IEnumerable<GdpSop>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific SOP by ID.
    /// </summary>
    Task<GdpSop?> GetByIdAsync(Guid sopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SOPs by category.
    /// </summary>
    Task<IEnumerable<GdpSop>> GetByCategoryAsync(GdpSopCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new SOP.
    /// </summary>
    Task<Guid> CreateAsync(GdpSop sop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing SOP.
    /// </summary>
    Task UpdateAsync(GdpSop sop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a SOP.
    /// </summary>
    Task DeleteAsync(Guid sopId, CancellationToken cancellationToken = default);

    #endregion

    #region GdpSiteSop Operations

    /// <summary>
    /// Gets SOPs linked to a specific site.
    /// </summary>
    Task<IEnumerable<GdpSop>> GetSiteSopsAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a SOP to a site.
    /// </summary>
    Task<Guid> LinkSopToSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks a SOP from a site.
    /// </summary>
    Task UnlinkSopFromSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default);

    #endregion
}
