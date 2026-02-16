using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpSiteRepository for local development and testing.
/// T189: Stores both mock warehouse data and GDP extensions in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpSiteRepository : IGdpSiteRepository
{
    private readonly ConcurrentDictionary<string, GdpSite> _warehouses = new();
    private readonly ConcurrentDictionary<string, GdpSite> _gdpExtensions = new();
    private readonly ConcurrentDictionary<Guid, GdpSiteWdaCoverage> _wdaCoverages = new();

    private static string GetKey(string warehouseId, string dataAreaId) => $"{warehouseId}|{dataAreaId}";

    #region D365FO Warehouse Queries

    public Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = _warehouses.Values.ToList();

        // Merge GDP extension data where available
        foreach (var warehouse in warehouses)
        {
            var key = GetKey(warehouse.WarehouseId, warehouse.DataAreaId);
            if (_gdpExtensions.TryGetValue(key, out var extension))
            {
                MergeGdpExtension(warehouse, extension);
            }
        }

        return Task.FromResult<IEnumerable<GdpSite>>(warehouses);
    }

    public Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(warehouseId, dataAreaId);
        _warehouses.TryGetValue(key, out var warehouse);

        if (warehouse != null)
        {
            // Clone to prevent mutation
            var result = CloneWarehouse(warehouse);
            if (_gdpExtensions.TryGetValue(key, out var extension))
            {
                MergeGdpExtension(result, extension);
            }
            return Task.FromResult<GdpSite?>(result);
        }

        return Task.FromResult<GdpSite?>(null);
    }

    #endregion

    #region GDP Extension Operations

    public Task<GdpSite?> GetGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(warehouseId, dataAreaId);
        if (!_gdpExtensions.TryGetValue(key, out var extension))
            return Task.FromResult<GdpSite?>(null);

        // Merge with warehouse data
        var result = CloneGdpExtension(extension);
        if (_warehouses.TryGetValue(key, out var warehouse))
        {
            MergeWarehouseData(result, warehouse);
        }

        return Task.FromResult<GdpSite?>(result);
    }

    public Task<IEnumerable<GdpSite>> GetAllGdpConfiguredSitesAsync(CancellationToken cancellationToken = default)
    {
        var sites = _gdpExtensions.Values.Select(ext =>
        {
            var result = CloneGdpExtension(ext);
            var key = GetKey(ext.WarehouseId, ext.DataAreaId);
            if (_warehouses.TryGetValue(key, out var warehouse))
            {
                MergeWarehouseData(result, warehouse);
            }
            return result;
        }).ToList();

        return Task.FromResult<IEnumerable<GdpSite>>(sites);
    }

    public Task<Guid> SaveGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        if (site.GdpExtensionId == Guid.Empty)
            site.GdpExtensionId = Guid.NewGuid();

        site.CreatedDate = DateTime.UtcNow;
        site.ModifiedDate = DateTime.UtcNow;

        var key = GetKey(site.WarehouseId, site.DataAreaId);
        _gdpExtensions[key] = CloneGdpExtension(site);

        return Task.FromResult(site.GdpExtensionId);
    }

    public Task UpdateGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        site.ModifiedDate = DateTime.UtcNow;
        var key = GetKey(site.WarehouseId, site.DataAreaId);
        _gdpExtensions[key] = CloneGdpExtension(site);

        return Task.CompletedTask;
    }

    public Task DeleteGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(warehouseId, dataAreaId);
        _gdpExtensions.TryRemove(key, out _);

        // Also remove WDA coverages for this warehouse
        var coveragesToRemove = _wdaCoverages.Values
            .Where(c => c.WarehouseId == warehouseId && c.DataAreaId == dataAreaId)
            .Select(c => c.CoverageId)
            .ToList();

        foreach (var id in coveragesToRemove)
        {
            _wdaCoverages.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region WDA Coverage Operations

    public Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var coverages = _wdaCoverages.Values
            .Where(c => c.WarehouseId == warehouseId && c.DataAreaId == dataAreaId)
            .ToList();

        return Task.FromResult<IEnumerable<GdpSiteWdaCoverage>>(coverages);
    }

    public Task<Guid> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default)
    {
        if (coverage.CoverageId == Guid.Empty)
            coverage.CoverageId = Guid.NewGuid();

        _wdaCoverages[coverage.CoverageId] = coverage;
        return Task.FromResult(coverage.CoverageId);
    }

    public Task DeleteWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default)
    {
        _wdaCoverages.TryRemove(coverageId, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region Seed Methods

    /// <summary>
    /// Seeds mock warehouse data for local development.
    /// </summary>
    internal void SeedWarehouses(IEnumerable<GdpSite> warehouses)
    {
        foreach (var warehouse in warehouses)
        {
            var key = GetKey(warehouse.WarehouseId, warehouse.DataAreaId);
            _warehouses.TryAdd(key, warehouse);
        }
    }

    /// <summary>
    /// Seeds GDP extension data for local development.
    /// </summary>
    internal void SeedGdpExtensions(IEnumerable<GdpSite> extensions)
    {
        foreach (var extension in extensions)
        {
            var key = GetKey(extension.WarehouseId, extension.DataAreaId);
            _gdpExtensions.TryAdd(key, extension);
        }
    }

    /// <summary>
    /// Seeds WDA coverage data for local development.
    /// </summary>
    internal void SeedWdaCoverages(IEnumerable<GdpSiteWdaCoverage> coverages)
    {
        foreach (var coverage in coverages)
        {
            _wdaCoverages.TryAdd(coverage.CoverageId, coverage);
        }
    }

    #endregion

    #region Private Helpers

    private static GdpSite CloneWarehouse(GdpSite source) => new()
    {
        WarehouseId = source.WarehouseId,
        WarehouseName = source.WarehouseName,
        OperationalSiteId = source.OperationalSiteId,
        OperationalSiteName = source.OperationalSiteName,
        DataAreaId = source.DataAreaId,
        WarehouseType = source.WarehouseType,
        Street = source.Street,
        StreetNumber = source.StreetNumber,
        City = source.City,
        ZipCode = source.ZipCode,
        CountryRegionId = source.CountryRegionId,
        StateId = source.StateId,
        FormattedAddress = source.FormattedAddress,
        Latitude = source.Latitude,
        Longitude = source.Longitude
    };

    private static GdpSite CloneGdpExtension(GdpSite source) => new()
    {
        WarehouseId = source.WarehouseId,
        DataAreaId = source.DataAreaId,
        GdpExtensionId = source.GdpExtensionId,
        GdpSiteType = source.GdpSiteType,
        PermittedActivities = source.PermittedActivities,
        IsGdpActive = source.IsGdpActive,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate,
        RowVersion = source.RowVersion
    };

    private static void MergeGdpExtension(GdpSite target, GdpSite extension)
    {
        target.GdpExtensionId = extension.GdpExtensionId;
        target.GdpSiteType = extension.GdpSiteType;
        target.PermittedActivities = extension.PermittedActivities;
        target.IsGdpActive = extension.IsGdpActive;
        target.CreatedDate = extension.CreatedDate;
        target.ModifiedDate = extension.ModifiedDate;
        target.RowVersion = extension.RowVersion;
    }

    private static void MergeWarehouseData(GdpSite target, GdpSite warehouseSource)
    {
        target.WarehouseName = warehouseSource.WarehouseName;
        target.OperationalSiteId = warehouseSource.OperationalSiteId;
        target.OperationalSiteName = warehouseSource.OperationalSiteName;
        target.WarehouseType = warehouseSource.WarehouseType;
        target.Street = warehouseSource.Street;
        target.StreetNumber = warehouseSource.StreetNumber;
        target.City = warehouseSource.City;
        target.ZipCode = warehouseSource.ZipCode;
        target.CountryRegionId = warehouseSource.CountryRegionId;
        target.StateId = warehouseSource.StateId;
        target.FormattedAddress = warehouseSource.FormattedAddress;
        target.Latitude = warehouseSource.Latitude;
        target.Longitude = warehouseSource.Longitude;
    }

    #endregion
}
