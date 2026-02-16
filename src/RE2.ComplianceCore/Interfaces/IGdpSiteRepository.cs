using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP site operations.
/// T188: Combined D365FO warehouse queries and Dataverse GDP extension CRUD.
/// </summary>
public interface IGdpSiteRepository
{
    #region D365FO Warehouse Queries

    /// <summary>
    /// Gets all warehouses from D365FO.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific warehouse from D365FO by warehouse ID and data area.
    /// </summary>
    Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Extension Operations (Dataverse)

    /// <summary>
    /// Gets the GDP extension for a specific warehouse.
    /// </summary>
    Task<GdpSite?> GetGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all warehouses that have GDP configuration.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllGdpConfiguredSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new GDP extension for a warehouse.
    /// </summary>
    Task<Guid> SaveGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP extension.
    /// </summary>
    Task UpdateGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the GDP extension for a warehouse.
    /// </summary>
    Task DeleteGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region WDA Coverage Operations

    /// <summary>
    /// Gets WDA coverage records for a warehouse.
    /// </summary>
    Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a WDA coverage record.
    /// </summary>
    Task<Guid> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a WDA coverage record.
    /// </summary>
    Task DeleteWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default);

    #endregion
}
