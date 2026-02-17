using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpSopRepository.
/// T278: CRUD for GDP SOPs and site-SOP links via IDataverseClient.
/// </summary>
public class DataverseGdpSopRepository : IGdpSopRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpSopRepository> _logger;

    private const string SopEntityName = "phr_gdpsop";
    private const string SiteSopEntityName = "phr_gdpsitesop";

    public DataverseGdpSopRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpSopRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    #region GdpSop Operations

    public async Task<IEnumerable<GdpSop>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(SopEntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToSopDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP SOPs");
            return Enumerable.Empty<GdpSop>();
        }
    }

    public async Task<GdpSop?> GetByIdAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(SopEntityName, sopId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToSopDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP SOP {SopId}", sopId);
            return null;
        }
    }

    public async Task<IEnumerable<GdpSop>> GetByCategoryAsync(GdpSopCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(SopEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_category", ConditionOperator.Equal, (int)category) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToSopDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP SOPs of category {Category}", category);
            return Enumerable.Empty<GdpSop>();
        }
    }

    public async Task<Guid> CreateAsync(GdpSop sop, CancellationToken cancellationToken = default)
    {
        try
        {
            if (sop.SopId == Guid.Empty)
            {
                sop.SopId = Guid.NewGuid();
            }

            var entity = MapSopToEntity(sop);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP SOP {SopId} ({SopNumber})", sop.SopId, sop.SopNumber);
            return sop.SopId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP SOP {SopNumber}", sop.SopNumber);
            throw;
        }
    }

    public async Task UpdateAsync(GdpSop sop, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = MapSopToEntity(sop);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP SOP {SopId}", sop.SopId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP SOP {SopId}", sop.SopId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(SopEntityName, sopId, cancellationToken);
            _logger.LogInformation("Deleted GDP SOP {SopId}", sopId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP SOP {SopId}", sopId);
            throw;
        }
    }

    #endregion

    #region GdpSiteSop Operations

    public async Task<IEnumerable<GdpSop>> GetSiteSopsAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            // First get the site-SOP links
            var linkQuery = new QueryExpression(SiteSopEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_siteid", ConditionOperator.Equal, siteId) }
                }
            };
            var linkResult = await _dataverseClient.RetrieveMultipleAsync(linkQuery, cancellationToken);
            var sopIds = linkResult.Entities.Select(e => e.GetAttributeValue<Guid>("phr_sopid")).ToList();

            if (!sopIds.Any())
            {
                return Enumerable.Empty<GdpSop>();
            }

            // Then get the SOPs
            var sops = new List<GdpSop>();
            foreach (var sopId in sopIds)
            {
                var sop = await GetByIdAsync(sopId, cancellationToken);
                if (sop != null)
                {
                    sops.Add(sop);
                }
            }
            return sops;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SOPs for site {SiteId}", siteId);
            return Enumerable.Empty<GdpSop>();
        }
    }

    public async Task<Guid> LinkSopToSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default)
    {
        try
        {
            var linkId = Guid.NewGuid();
            var entity = new Entity(SiteSopEntityName, linkId);
            entity["phr_siteid"] = siteId;
            entity["phr_sopid"] = sopId;
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Linked SOP {SopId} to site {SiteId}", sopId, siteId);
            return linkId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking SOP {SopId} to site {SiteId}", sopId, siteId);
            throw;
        }
    }

    public async Task UnlinkSopFromSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the link record
            var query = new QueryExpression(SiteSopEntityName)
            {
                ColumnSet = new ColumnSet("phr_gdpsitesopid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_siteid", ConditionOperator.Equal, siteId),
                        new ConditionExpression("phr_sopid", ConditionOperator.Equal, sopId)
                    }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            foreach (var link in result.Entities)
            {
                await _dataverseClient.DeleteAsync(SiteSopEntityName, link.Id, cancellationToken);
            }

            _logger.LogInformation("Unlinked SOP {SopId} from site {SiteId}", sopId, siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking SOP {SopId} from site {SiteId}", sopId, siteId);
            throw;
        }
    }

    #endregion

    #region Mapping Helpers

    private static GdpSopDto MapToSopDto(Entity entity)
    {
        return new GdpSopDto
        {
            phr_gdpsopid = entity.Id,
            phr_sopnumber = entity.GetAttributeValue<string>("phr_sopnumber"),
            phr_title = entity.GetAttributeValue<string>("phr_title"),
            phr_category = entity.GetAttributeValue<int>("phr_category"),
            phr_version = entity.GetAttributeValue<string>("phr_version"),
            phr_effectivedate = entity.Contains("phr_effectivedate") ? entity.GetAttributeValue<DateTime?>("phr_effectivedate") : null,
            phr_documenturl = entity.GetAttributeValue<string>("phr_documenturl"),
            phr_isactive = entity.Contains("phr_isactive") && entity.GetAttributeValue<bool>("phr_isactive")
        };
    }

    private static Entity MapSopToEntity(GdpSop sop)
    {
        var entity = new Entity(SopEntityName, sop.SopId);
        entity["phr_sopnumber"] = sop.SopNumber;
        entity["phr_title"] = sop.Title;
        entity["phr_category"] = (int)sop.Category;
        entity["phr_version"] = sop.Version;
        entity["phr_effectivedate"] = sop.EffectiveDate.ToDateTime(TimeOnly.MinValue);
        entity["phr_documenturl"] = sop.DocumentUrl;
        entity["phr_isactive"] = sop.IsActive;
        return entity;
    }

    #endregion
}
