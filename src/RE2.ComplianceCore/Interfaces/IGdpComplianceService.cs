using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for GDP compliance operations.
/// T190: Business logic for GDP site management per User Story 7.
/// </summary>
public interface IGdpComplianceService
{
    #region D365FO Warehouse Browsing

    /// <summary>
    /// Gets all D365FO warehouses for browsing.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific D365FO warehouse.
    /// </summary>
    Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP-Configured Sites

    /// <summary>
    /// Gets all warehouses that have been configured for GDP.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllGdpSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP-configured site.
    /// </summary>
    Task<GdpSite?> GetGdpSiteAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Configuration

    /// <summary>
    /// Configures GDP compliance for a D365FO warehouse.
    /// Validates GdpSiteType + PermittedActivities, verifies warehouse exists in D365FO.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> ConfigureGdpAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the GDP configuration for a warehouse.
    /// </summary>
    Task<ValidationResult> UpdateGdpConfigAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the GDP configuration from a warehouse.
    /// </summary>
    Task<ValidationResult> RemoveGdpConfigAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region WDA Coverage

    /// <summary>
    /// Gets WDA coverage records for a warehouse.
    /// </summary>
    Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds WDA coverage for a warehouse.
    /// FR-033: Validates that LicenceId references a WDA-type licence.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a WDA coverage record.
    /// </summary>
    Task<ValidationResult> RemoveWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default);

    #endregion
}
