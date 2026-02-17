using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpEquipmentRepository.
/// T258: CRUD for GDP equipment qualifications via IDataverseClient.
/// </summary>
public class DataverseGdpEquipmentRepository : IGdpEquipmentRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpEquipmentRepository> _logger;

    private const string EntityName = "phr_gdpequipmentqualification";

    public DataverseGdpEquipmentRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpEquipmentRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<GdpEquipmentQualification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP equipment qualifications");
            return Enumerable.Empty<GdpEquipmentQualification>();
        }
    }

    public async Task<GdpEquipmentQualification?> GetByIdAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(EntityName, equipmentQualificationId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP equipment qualification {Id}", equipmentQualificationId);
            return null;
        }
    }

    public async Task<IEnumerable<GdpEquipmentQualification>> GetByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_providerid", ConditionOperator.Equal, providerId) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving equipment for provider {ProviderId}", providerId);
            return Enumerable.Empty<GdpEquipmentQualification>();
        }
    }

    public async Task<IEnumerable<GdpEquipmentQualification>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_siteid", ConditionOperator.Equal, siteId) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving equipment for site {SiteId}", siteId);
            return Enumerable.Empty<GdpEquipmentQualification>();
        }
    }

    public async Task<IEnumerable<GdpEquipmentQualification>> GetDueForRequalificationAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var dueDate = DateTime.UtcNow.AddDays(daysAhead);
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_requalificationduedate", ConditionOperator.LessEqual, dueDate),
                        new ConditionExpression("phr_requalificationduedate", ConditionOperator.GreaterEqual, DateTime.UtcNow)
                    }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving equipment due for requalification");
            return Enumerable.Empty<GdpEquipmentQualification>();
        }
    }

    public async Task<Guid> CreateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        try
        {
            if (equipment.EquipmentQualificationId == Guid.Empty)
            {
                equipment.EquipmentQualificationId = Guid.NewGuid();
            }

            equipment.CreatedDate = DateTime.UtcNow;
            equipment.ModifiedDate = DateTime.UtcNow;

            var entity = MapToEntity(equipment);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP equipment qualification {Id} for {Name}", equipment.EquipmentQualificationId, equipment.EquipmentName);
            return equipment.EquipmentQualificationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP equipment qualification {Name}", equipment.EquipmentName);
            throw;
        }
    }

    public async Task UpdateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        try
        {
            equipment.ModifiedDate = DateTime.UtcNow;
            var entity = MapToEntity(equipment);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP equipment qualification {Id}", equipment.EquipmentQualificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP equipment qualification {Id}", equipment.EquipmentQualificationId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(EntityName, equipmentQualificationId, cancellationToken);
            _logger.LogInformation("Deleted GDP equipment qualification {Id}", equipmentQualificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP equipment qualification {Id}", equipmentQualificationId);
            throw;
        }
    }

    #region Mapping Helpers

    private static GdpEquipmentQualificationDto MapToDto(Entity entity)
    {
        return new GdpEquipmentQualificationDto
        {
            phr_gdpequipmentqualificationid = entity.Id,
            phr_equipmentname = entity.GetAttributeValue<string>("phr_equipmentname"),
            phr_equipmenttype = entity.GetAttributeValue<int>("phr_equipmenttype"),
            phr_providerid = entity.Contains("phr_providerid") ? entity.GetAttributeValue<Guid?>("phr_providerid") : null,
            phr_siteid = entity.Contains("phr_siteid") ? entity.GetAttributeValue<Guid?>("phr_siteid") : null,
            phr_qualificationdate = entity.Contains("phr_qualificationdate") ? entity.GetAttributeValue<DateTime?>("phr_qualificationdate") : null,
            phr_requalificationduedate = entity.Contains("phr_requalificationduedate") ? entity.GetAttributeValue<DateTime?>("phr_requalificationduedate") : null,
            phr_qualificationstatus = entity.GetAttributeValue<int>("phr_qualificationstatus"),
            phr_qualifiedby = entity.GetAttributeValue<string>("phr_qualifiedby"),
            phr_notes = entity.GetAttributeValue<string>("phr_notes"),
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon")
        };
    }

    private static Entity MapToEntity(GdpEquipmentQualification equipment)
    {
        var entity = new Entity(EntityName, equipment.EquipmentQualificationId);
        entity["phr_equipmentname"] = equipment.EquipmentName;
        entity["phr_equipmenttype"] = (int)equipment.EquipmentType;
        if (equipment.ProviderId.HasValue)
        {
            entity["phr_providerid"] = equipment.ProviderId.Value;
        }

        if (equipment.SiteId.HasValue)
        {
            entity["phr_siteid"] = equipment.SiteId.Value;
        }

        entity["phr_qualificationdate"] = equipment.QualificationDate.ToDateTime(TimeOnly.MinValue);
        if (equipment.RequalificationDueDate.HasValue)
        {
            entity["phr_requalificationduedate"] = equipment.RequalificationDueDate.Value.ToDateTime(TimeOnly.MinValue);
        }

        entity["phr_qualificationstatus"] = (int)equipment.QualificationStatus;
        entity["phr_qualifiedby"] = equipment.QualifiedBy;
        entity["phr_notes"] = equipment.Notes;
        return entity;
    }

    #endregion
}
