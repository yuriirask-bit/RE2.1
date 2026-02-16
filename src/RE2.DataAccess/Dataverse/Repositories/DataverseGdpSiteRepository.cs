using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Combined D365FO + Dataverse implementation of IGdpSiteRepository.
/// T189: Warehouse data from D365FO via ID365FoClient, GDP extensions from Dataverse via IDataverseClient.
/// </summary>
public class DataverseGdpSiteRepository : IGdpSiteRepository
{
    private readonly ID365FoClient _d365FoClient;
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpSiteRepository> _logger;

    private const string GdpExtensionEntityName = "phr_gdpwarehouseextension";
    private const string WdaCoverageEntityName = "phr_gdpsitewdacoverage";
    private const string WarehouseEntitySet = "Warehouses";
    private const string SiteEntitySet = "OperationalSitesV2";

    public DataverseGdpSiteRepository(
        ID365FoClient d365FoClient,
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpSiteRepository> logger)
    {
        _d365FoClient = d365FoClient;
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    #region D365FO Warehouse Queries

    public async Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _d365FoClient.GetAsync<WarehouseODataResponse>(
                WarehouseEntitySet,
                "$select=WarehouseId,WarehouseName,OperationalSiteId,dataAreaId,WarehouseType," +
                "FormattedPrimaryAddress,PrimaryAddressStreet,PrimaryAddressStreetNumber," +
                "PrimaryAddressCity,PrimaryAddressZipCode,PrimaryAddressCountryRegionId," +
                "PrimaryAddressStateId,PrimaryAddressLatitude,PrimaryAddressLongitude",
                cancellationToken);

            if (response?.Value == null)
                return Enumerable.Empty<GdpSite>();

            // Load site names for enrichment
            var siteNames = await GetSiteNameLookupAsync(cancellationToken);

            return response.Value.Select(w =>
            {
                siteNames.TryGetValue(w.OperationalSiteId, out var siteName);
                return w.ToDomainModel(siteName);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouses from D365 F&O");
            return Enumerable.Empty<GdpSite>();
        }
    }

    public async Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"WarehouseId='{warehouseId}',dataAreaId='{dataAreaId}'";
            var warehouseDto = await _d365FoClient.GetByKeyAsync<WarehouseDto>(WarehouseEntitySet, key, cancellationToken);

            if (warehouseDto == null)
                return null;

            // Look up site name
            string? siteName = null;
            if (!string.IsNullOrEmpty(warehouseDto.OperationalSiteId))
            {
                var siteNames = await GetSiteNameLookupAsync(cancellationToken);
                siteNames.TryGetValue(warehouseDto.OperationalSiteId, out siteName);
            }

            return warehouseDto.ToDomainModel(siteName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse {WarehouseId} from D365 F&O", warehouseId);
            return null;
        }
    }

    private async Task<Dictionary<string, string>> GetSiteNameLookupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _d365FoClient.GetAsync<OperationalSiteODataResponse>(
                SiteEntitySet,
                "$select=SiteId,SiteName",
                cancellationToken);

            return response?.Value?.ToDictionary(s => s.SiteId, s => s.SiteName) ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading site names from D365 F&O, using empty lookup");
            return new Dictionary<string, string>();
        }
    }

    #endregion

    #region GDP Extension Operations (Dataverse)

    public async Task<GdpSite?> GetGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(GdpExtensionEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_warehouseid", ConditionOperator.Equal, warehouseId),
                        new ConditionExpression("phr_dataareaid", ConditionOperator.Equal, dataAreaId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var entity = result.Entities.FirstOrDefault();
            if (entity == null)
                return null;

            var dto = MapToGdpExtensionDto(entity);
            var site = dto.ToDomainModel();

            // Enrich with warehouse data from D365FO
            var warehouse = await GetWarehouseAsync(warehouseId, dataAreaId, cancellationToken);
            if (warehouse != null)
            {
                MergeWarehouseData(site, warehouse);
            }

            return site;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP extension for warehouse {WarehouseId}", warehouseId);
            return null;
        }
    }

    public async Task<IEnumerable<GdpSite>> GetAllGdpConfiguredSitesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(GdpExtensionEntityName)
            {
                ColumnSet = new ColumnSet(true)
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var sites = result.Entities.Select(e => MapToGdpExtensionDto(e).ToDomainModel()).ToList();

            // Enrich with warehouse data
            var warehouses = (await GetAllWarehousesAsync(cancellationToken)).ToDictionary(
                w => $"{w.WarehouseId}|{w.DataAreaId}");

            foreach (var site in sites)
            {
                var key = $"{site.WarehouseId}|{site.DataAreaId}";
                if (warehouses.TryGetValue(key, out var warehouse))
                {
                    MergeWarehouseData(site, warehouse);
                }
            }

            return sites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP-configured sites");
            return Enumerable.Empty<GdpSite>();
        }
    }

    public async Task<Guid> SaveGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        try
        {
            if (site.GdpExtensionId == Guid.Empty)
                site.GdpExtensionId = Guid.NewGuid();

            site.CreatedDate = DateTime.UtcNow;
            site.ModifiedDate = DateTime.UtcNow;

            var dto = GdpWarehouseExtensionDto.FromDomainModel(site);
            var entity = MapToEntity(dto);

            await _dataverseClient.CreateAsync(entity, cancellationToken);
            _logger.LogInformation("Created GDP extension {Id} for warehouse {WarehouseId}", site.GdpExtensionId, site.WarehouseId);

            return site.GdpExtensionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving GDP extension for warehouse {WarehouseId}", site.WarehouseId);
            throw;
        }
    }

    public async Task UpdateGdpExtensionAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        try
        {
            site.ModifiedDate = DateTime.UtcNow;
            var dto = GdpWarehouseExtensionDto.FromDomainModel(site);
            var entity = MapToEntity(dto);

            await _dataverseClient.UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("Updated GDP extension for warehouse {WarehouseId}", site.WarehouseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP extension for warehouse {WarehouseId}", site.WarehouseId);
            throw;
        }
    }

    public async Task DeleteGdpExtensionAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await GetGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
            if (existing != null)
            {
                await _dataverseClient.DeleteAsync(GdpExtensionEntityName, existing.GdpExtensionId, cancellationToken);
                _logger.LogInformation("Deleted GDP extension for warehouse {WarehouseId}", warehouseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP extension for warehouse {WarehouseId}", warehouseId);
            throw;
        }
    }

    #endregion

    #region WDA Coverage Operations

    public async Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(WdaCoverageEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_warehouseid", ConditionOperator.Equal, warehouseId),
                        new ConditionExpression("phr_dataareaid", ConditionOperator.Equal, dataAreaId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToWdaCoverageDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving WDA coverage for warehouse {WarehouseId}", warehouseId);
            return Enumerable.Empty<GdpSiteWdaCoverage>();
        }
    }

    public async Task<Guid> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (coverage.CoverageId == Guid.Empty)
                coverage.CoverageId = Guid.NewGuid();

            var dto = GdpSiteWdaCoverageDto.FromDomainModel(coverage);
            var entity = MapToCoverageEntity(dto);

            await _dataverseClient.CreateAsync(entity, cancellationToken);
            _logger.LogInformation("Created WDA coverage {CoverageId} for warehouse {WarehouseId}", coverage.CoverageId, coverage.WarehouseId);

            return coverage.CoverageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding WDA coverage for warehouse {WarehouseId}", coverage.WarehouseId);
            throw;
        }
    }

    public async Task DeleteWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(WdaCoverageEntityName, coverageId, cancellationToken);
            _logger.LogInformation("Deleted WDA coverage {CoverageId}", coverageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting WDA coverage {CoverageId}", coverageId);
            throw;
        }
    }

    #endregion

    #region Mapping Helpers

    private static GdpWarehouseExtensionDto MapToGdpExtensionDto(Entity entity)
    {
        return new GdpWarehouseExtensionDto
        {
            phr_gdpextensionid = entity.Id,
            phr_warehouseid = entity.GetAttributeValue<string>("phr_warehouseid"),
            phr_dataareaid = entity.GetAttributeValue<string>("phr_dataareaid"),
            phr_gdpsitetype = entity.GetAttributeValue<int>("phr_gdpsitetype"),
            phr_permittedactivities = entity.GetAttributeValue<int>("phr_permittedactivities"),
            phr_isgdpactive = entity.GetAttributeValue<bool>("phr_isgdpactive"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate")
        };
    }

    private static Entity MapToEntity(GdpWarehouseExtensionDto dto)
    {
        var entity = new Entity(GdpExtensionEntityName, dto.phr_gdpextensionid);
        entity["phr_warehouseid"] = dto.phr_warehouseid;
        entity["phr_dataareaid"] = dto.phr_dataareaid;
        entity["phr_gdpsitetype"] = dto.phr_gdpsitetype;
        entity["phr_permittedactivities"] = dto.phr_permittedactivities;
        entity["phr_isgdpactive"] = dto.phr_isgdpactive;
        entity["phr_createddate"] = dto.phr_createddate;
        entity["phr_modifieddate"] = dto.phr_modifieddate;
        return entity;
    }

    private static GdpSiteWdaCoverageDto MapToWdaCoverageDto(Entity entity)
    {
        return new GdpSiteWdaCoverageDto
        {
            phr_coverageid = entity.Id,
            phr_warehouseid = entity.GetAttributeValue<string>("phr_warehouseid"),
            phr_dataareaid = entity.GetAttributeValue<string>("phr_dataareaid"),
            phr_licenceid = entity.GetAttributeValue<Guid>("phr_licenceid"),
            phr_effectivedate = entity.GetAttributeValue<DateTime>("phr_effectivedate"),
            phr_expirydate = entity.Contains("phr_expirydate") ? entity.GetAttributeValue<DateTime?>("phr_expirydate") : null
        };
    }

    private static Entity MapToCoverageEntity(GdpSiteWdaCoverageDto dto)
    {
        var entity = new Entity(WdaCoverageEntityName, dto.phr_coverageid);
        entity["phr_warehouseid"] = dto.phr_warehouseid;
        entity["phr_dataareaid"] = dto.phr_dataareaid;
        entity["phr_licenceid"] = dto.phr_licenceid;
        entity["phr_effectivedate"] = dto.phr_effectivedate;
        if (dto.phr_expirydate.HasValue)
            entity["phr_expirydate"] = dto.phr_expirydate.Value;
        return entity;
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
