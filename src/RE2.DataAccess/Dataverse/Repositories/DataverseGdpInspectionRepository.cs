using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpInspectionRepository.
/// T221: CRUD for GDP inspections and findings via IDataverseClient.
/// </summary>
public class DataverseGdpInspectionRepository : IGdpInspectionRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpInspectionRepository> _logger;

    private const string InspectionEntityName = "phr_gdpinspection";
    private const string FindingEntityName = "phr_gdpinspectionfinding";

    public DataverseGdpInspectionRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpInspectionRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    #region GdpInspection Operations

    public async Task<IEnumerable<GdpInspection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(InspectionEntityName)
            {
                ColumnSet = new ColumnSet(true)
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToInspectionDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP inspections");
            return Enumerable.Empty<GdpInspection>();
        }
    }

    public async Task<GdpInspection?> GetByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(InspectionEntityName, inspectionId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToInspectionDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP inspection {InspectionId}", inspectionId);
            return null;
        }
    }

    public async Task<IEnumerable<GdpInspection>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(InspectionEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_siteid", ConditionOperator.Equal, siteId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToInspectionDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP inspections for site {SiteId}", siteId);
            return Enumerable.Empty<GdpInspection>();
        }
    }

    public async Task<IEnumerable<GdpInspection>> GetByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(InspectionEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_inspectiondate", ConditionOperator.GreaterEqual, fromDate.ToDateTime(TimeOnly.MinValue)),
                        new ConditionExpression("phr_inspectiondate", ConditionOperator.LessEqual, toDate.ToDateTime(TimeOnly.MinValue))
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToInspectionDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP inspections for date range {From} to {To}", fromDate, toDate);
            return Enumerable.Empty<GdpInspection>();
        }
    }

    public async Task<IEnumerable<GdpInspection>> GetByTypeAsync(GdpInspectionType inspectionType, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(InspectionEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_inspectiontype", ConditionOperator.Equal, (int)inspectionType)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToInspectionDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP inspections of type {Type}", inspectionType);
            return Enumerable.Empty<GdpInspection>();
        }
    }

    public async Task<Guid> CreateAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        try
        {
            if (inspection.InspectionId == Guid.Empty)
                inspection.InspectionId = Guid.NewGuid();

            inspection.CreatedDate = DateTime.UtcNow;
            inspection.ModifiedDate = DateTime.UtcNow;

            var entity = MapInspectionToEntity(inspection);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP inspection {InspectionId} for site {SiteId}", inspection.InspectionId, inspection.SiteId);
            return inspection.InspectionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP inspection for site {SiteId}", inspection.SiteId);
            throw;
        }
    }

    public async Task UpdateAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        try
        {
            inspection.ModifiedDate = DateTime.UtcNow;
            var entity = MapInspectionToEntity(inspection);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP inspection {InspectionId}", inspection.InspectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP inspection {InspectionId}", inspection.InspectionId);
            throw;
        }
    }

    #endregion

    #region GdpInspectionFinding Operations

    public async Task<IEnumerable<GdpInspectionFinding>> GetFindingsAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(FindingEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_inspectionid", ConditionOperator.Equal, inspectionId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToFindingDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving findings for inspection {InspectionId}", inspectionId);
            return Enumerable.Empty<GdpInspectionFinding>();
        }
    }

    public async Task<GdpInspectionFinding?> GetFindingByIdAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(FindingEntityName, findingId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToFindingDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving finding {FindingId}", findingId);
            return null;
        }
    }

    public async Task<Guid> CreateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        try
        {
            if (finding.FindingId == Guid.Empty)
                finding.FindingId = Guid.NewGuid();

            var entity = MapFindingToEntity(finding);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created finding {FindingId} for inspection {InspectionId}", finding.FindingId, finding.InspectionId);
            return finding.FindingId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating finding for inspection {InspectionId}", finding.InspectionId);
            throw;
        }
    }

    public async Task UpdateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = MapFindingToEntity(finding);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated finding {FindingId}", finding.FindingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating finding {FindingId}", finding.FindingId);
            throw;
        }
    }

    public async Task DeleteFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(FindingEntityName, findingId, cancellationToken);
            _logger.LogInformation("Deleted finding {FindingId}", findingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting finding {FindingId}", findingId);
            throw;
        }
    }

    #endregion

    #region Mapping Helpers

    private static GdpInspectionDto MapToInspectionDto(Entity entity)
    {
        return new GdpInspectionDto
        {
            phr_gdpinspectionid = entity.Id,
            phr_inspectiondate = entity.Contains("phr_inspectiondate") ? entity.GetAttributeValue<DateTime?>("phr_inspectiondate") : null,
            phr_inspectorname = entity.GetAttributeValue<string>("phr_inspectorname"),
            phr_inspectiontype = entity.GetAttributeValue<int>("phr_inspectiontype"),
            phr_siteid = entity.GetAttributeValue<Guid>("phr_siteid"),
            phr_wdalicenceid = entity.Contains("phr_wdalicenceid") ? entity.GetAttributeValue<Guid?>("phr_wdalicenceid") : null,
            phr_findingssummary = entity.GetAttributeValue<string>("phr_findingssummary"),
            phr_reportreferenceurl = entity.GetAttributeValue<string>("phr_reportreferenceurl"),
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon")
        };
    }

    private static Entity MapInspectionToEntity(GdpInspection inspection)
    {
        var entity = new Entity(InspectionEntityName, inspection.InspectionId);
        entity["phr_inspectiondate"] = inspection.InspectionDate.ToDateTime(TimeOnly.MinValue);
        entity["phr_inspectorname"] = inspection.InspectorName;
        entity["phr_inspectiontype"] = (int)inspection.InspectionType;
        entity["phr_siteid"] = inspection.SiteId;
        if (inspection.WdaLicenceId.HasValue)
            entity["phr_wdalicenceid"] = inspection.WdaLicenceId.Value;
        entity["phr_findingssummary"] = inspection.FindingsSummary;
        entity["phr_reportreferenceurl"] = inspection.ReportReferenceUrl;
        return entity;
    }

    private static GdpInspectionFindingDto MapToFindingDto(Entity entity)
    {
        return new GdpInspectionFindingDto
        {
            phr_gdpinspectionfindingid = entity.Id,
            phr_inspectionid = entity.GetAttributeValue<Guid>("phr_inspectionid"),
            phr_findingdescription = entity.GetAttributeValue<string>("phr_findingdescription"),
            phr_classification = entity.GetAttributeValue<int>("phr_classification"),
            phr_findingnumber = entity.GetAttributeValue<string>("phr_findingnumber")
        };
    }

    private static Entity MapFindingToEntity(GdpInspectionFinding finding)
    {
        var entity = new Entity(FindingEntityName, finding.FindingId);
        entity["phr_inspectionid"] = finding.InspectionId;
        entity["phr_findingdescription"] = finding.FindingDescription;
        entity["phr_classification"] = (int)finding.Classification;
        entity["phr_findingnumber"] = finding.FindingNumber;
        return entity;
    }

    #endregion
}
