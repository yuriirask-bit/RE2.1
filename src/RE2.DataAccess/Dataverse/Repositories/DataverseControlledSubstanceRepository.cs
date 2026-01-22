using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IControlledSubstanceRepository.
/// T073: Repository implementation for ControlledSubstance CRUD operations.
/// </summary>
public class DataverseControlledSubstanceRepository : IControlledSubstanceRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseControlledSubstanceRepository> _logger;
    private const string EntityName = "phr_controlledsubstance";

    public DataverseControlledSubstanceRepository(IDataverseClient client, ILogger<DataverseControlledSubstanceRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ControlledSubstance?> GetByIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, substanceId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controlled substance {Id}", substanceId);
            return null;
        }
    }

    public async Task<ControlledSubstance?> GetByInternalCodeAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_internalcode", ConditionOperator.Equal, internalCode)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controlled substance by internal code {InternalCode}", internalCode);
            return null;
        }
    }

    public async Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_isactive", ConditionOperator.Equal, true) }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active controlled substances");
            return Enumerable.Empty<ControlledSubstance>();
        }
    }

    public async Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all controlled substances");
            return Enumerable.Empty<ControlledSubstance>();
        }
    }

    public async Task<Guid> CreateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        var dto = ControlledSubstanceDto.FromDomainModel(substance);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created controlled substance {Id} with name {Name}", id, substance.SubstanceName);
        return id;
    }

    public async Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        var dto = ControlledSubstanceDto.FromDomainModel(substance);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated controlled substance {Id}", substance.SubstanceId);
    }

    public async Task DeleteAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, substanceId, cancellationToken);
        _logger.LogInformation("Deleted controlled substance {Id}", substanceId);
    }

    private ControlledSubstanceDto MapToDto(Entity entity)
    {
        return new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = entity.Id,
            phr_substancename = entity.GetAttributeValue<string>("phr_substancename"),
            phr_opiumactlist = entity.GetAttributeValue<int?>("phr_opiumactlist"),
            phr_precursorcategory = entity.GetAttributeValue<int?>("phr_precursorcategory"),
            phr_internalcode = entity.GetAttributeValue<string>("phr_internalcode"),
            phr_regulatoryrestrictions = entity.GetAttributeValue<string>("phr_regulatoryrestrictions"),
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive")
        };
    }

    private Entity MapToEntity(ControlledSubstanceDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_controlledsubstanceid };
        entity["phr_substancename"] = dto.phr_substancename;
        entity["phr_opiumactlist"] = dto.phr_opiumactlist;
        entity["phr_precursorcategory"] = dto.phr_precursorcategory;
        entity["phr_internalcode"] = dto.phr_internalcode;
        entity["phr_regulatoryrestrictions"] = dto.phr_regulatoryrestrictions;
        entity["phr_isactive"] = dto.phr_isactive;
        return entity;
    }
}
