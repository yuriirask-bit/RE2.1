using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IControlledSubstanceRepository.
/// Compliance extension data stored in phr_substancecomplianceextension table.
/// Classification data (OpiumActList, PrecursorCategory) sourced from D365 F&amp;O product attributes.
/// </summary>
public class DataverseControlledSubstanceRepository : IControlledSubstanceRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseControlledSubstanceRepository> _logger;
    private const string EntityName = "phr_substancecomplianceextension";

    public DataverseControlledSubstanceRepository(IDataverseClient client, ILogger<DataverseControlledSubstanceRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ControlledSubstance?> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_substancecode", ConditionOperator.Equal, substanceCode)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null
                ? MapToDto(result.Entities.First()).ToDomainModel()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controlled substance by code {SubstanceCode}", substanceCode);
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

    public Task<IEnumerable<ControlledSubstance>> GetAllD365SubstancesAsync(CancellationToken cancellationToken = default)
    {
        // D365 F&O product attribute discovery is handled by the D365 integration layer.
        // This Dataverse repository cannot directly query D365 product attributes.
        // Returns empty; the service layer orchestrates D365 + Dataverse merging.
        _logger.LogWarning("GetAllD365SubstancesAsync called on Dataverse repository; D365 discovery requires D365 integration layer");
        return Task.FromResult<IEnumerable<ControlledSubstance>>(Enumerable.Empty<ControlledSubstance>());
    }

    public async Task SaveComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(substance);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        substance.ComplianceExtensionId = id;
        _logger.LogInformation("Saved compliance extension {Id} for substance {SubstanceCode}", id, substance.SubstanceCode);
    }

    public async Task UpdateComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(substance);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated compliance extension for substance {SubstanceCode}", substance.SubstanceCode);
    }

    public async Task DeleteComplianceExtensionAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet("phr_complianceextensionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_substancecode", ConditionOperator.Equal, substanceCode)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entity = result.Entities.FirstOrDefault();
            if (entity != null)
            {
                await _client.DeleteAsync(EntityName, entity.Id, cancellationToken);
                _logger.LogInformation("Deleted compliance extension for substance {SubstanceCode}", substanceCode);
            }
            else
            {
                _logger.LogWarning("No compliance extension found for substance {SubstanceCode} to delete", substanceCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting compliance extension for substance {SubstanceCode}", substanceCode);
            throw;
        }
    }

    public async Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        // For Dataverse, UpdateAsync delegates to UpdateComplianceExtensionAsync
        await UpdateComplianceExtensionAsync(substance, cancellationToken);
    }

    private SubstanceComplianceExtensionDto MapToDto(Entity entity)
    {
        return new SubstanceComplianceExtensionDto
        {
            phr_complianceextensionid = entity.Id,
            phr_substancecode = entity.GetAttributeValue<string>("phr_substancecode"),
            phr_substancename = entity.GetAttributeValue<string>("phr_substancename"),
            phr_regulatoryrestrictions = entity.GetAttributeValue<string>("phr_regulatoryrestrictions"),
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive"),
            phr_classificationeffectivedate = entity.GetAttributeValue<DateTime?>("phr_classificationeffectivedate"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate"),
            phr_rowversion = entity.GetAttributeValue<string>("phr_rowversion")
        };
    }

    private Entity MapToEntity(SubstanceComplianceExtensionDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_complianceextensionid };
        entity["phr_substancecode"] = dto.phr_substancecode;
        entity["phr_substancename"] = dto.phr_substancename;
        entity["phr_regulatoryrestrictions"] = dto.phr_regulatoryrestrictions;
        entity["phr_isactive"] = dto.phr_isactive;
        if (dto.phr_classificationeffectivedate.HasValue)
        {
            entity["phr_classificationeffectivedate"] = dto.phr_classificationeffectivedate.Value;
        }

        entity["phr_modifieddate"] = DateTime.UtcNow;
        return entity;
    }
}
