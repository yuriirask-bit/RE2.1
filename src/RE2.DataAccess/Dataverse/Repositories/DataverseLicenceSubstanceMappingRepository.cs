using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ILicenceSubstanceMappingRepository.
/// T079b: Repository implementation for LicenceSubstanceMapping CRUD operations.
/// </summary>
public class DataverseLicenceSubstanceMappingRepository : ILicenceSubstanceMappingRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseLicenceSubstanceMappingRepository> _logger;
    private const string EntityName = "phr_licencesubstancemapping";

    public DataverseLicenceSubstanceMappingRepository(
        IDataverseClient client,
        ILogger<DataverseLicenceSubstanceMappingRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<LicenceSubstanceMapping?> GetByIdAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, mappingId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mapping {Id}", mappingId);
            return null;
        }
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mappings for licence {LicenceId}", licenceId);
            return Enumerable.Empty<LicenceSubstanceMapping>();
        }
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
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
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mappings for substance {SubstanceCode}", substanceCode);
            return Enumerable.Empty<LicenceSubstanceMapping>();
        }
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetActiveMappingsByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId),
                        new ConditionExpression("phr_effectivedate", ConditionOperator.LessEqual, today)
                    },
                    Filters =
                    {
                        new FilterExpression
                        {
                            FilterOperator = LogicalOperator.Or,
                            Conditions =
                            {
                                new ConditionExpression("phr_expirydate", ConditionOperator.Null),
                                new ConditionExpression("phr_expirydate", ConditionOperator.GreaterEqual, today)
                            }
                        }
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active mappings for licence {LicenceId}", licenceId);
            return Enumerable.Empty<LicenceSubstanceMapping>();
        }
    }

    public async Task<LicenceSubstanceMapping?> GetByLicenceSubstanceEffectiveDateAsync(
        Guid licenceId,
        string substanceCode,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId),
                        new ConditionExpression("phr_substancecode", ConditionOperator.Equal, substanceCode),
                        new ConditionExpression("phr_effectivedate", ConditionOperator.Equal, effectiveDate.ToDateTime(TimeOnly.MinValue))
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
            _logger.LogError(ex, "Error checking for duplicate mapping {LicenceId}/{SubstanceCode}/{EffectiveDate}",
                licenceId, substanceCode, effectiveDate);
            return null;
        }
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true)
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all mappings");
            return Enumerable.Empty<LicenceSubstanceMapping>();
        }
    }

    public async Task<Guid> CreateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        var dto = LicenceSubstanceMappingDto.FromDomainModel(mapping);
        var entity = MapToEntity(dto);

        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created mapping {Id} for licence {LicenceId} and substance {SubstanceCode}",
            id, mapping.LicenceId, mapping.SubstanceCode);

        return id;
    }

    public async Task UpdateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        var dto = LicenceSubstanceMappingDto.FromDomainModel(mapping);
        var entity = MapToEntity(dto);
        entity.Id = mapping.MappingId;

        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated mapping {Id}", mapping.MappingId);
    }

    public async Task DeleteAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, mappingId, cancellationToken);
        _logger.LogInformation("Deleted mapping {Id}", mappingId);
    }

    private static LicenceSubstanceMappingDto MapToDto(Entity entity)
    {
        return new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = entity.Id,
            phr_licenceid = entity.GetAttributeValue<EntityReference>("phr_licenceid")?.Id ?? Guid.Empty,
            phr_substancecode = entity.GetAttributeValue<string>("phr_substancecode") ?? string.Empty,
            phr_maxquantitypertransaction = entity.GetAttributeValue<decimal?>("phr_maxquantitypertransaction"),
            phr_maxquantityperperiod = entity.GetAttributeValue<decimal?>("phr_maxquantityperperiod"),
            phr_periodtype = entity.GetAttributeValue<string>("phr_periodtype"),
            phr_restrictions = entity.GetAttributeValue<string>("phr_restrictions"),
            phr_effectivedate = entity.GetAttributeValue<DateTime>("phr_effectivedate"),
            phr_expirydate = entity.GetAttributeValue<DateTime?>("phr_expirydate")
        };
    }

    private static Entity MapToEntity(LicenceSubstanceMappingDto dto)
    {
        var entity = new Entity(EntityName);

        if (dto.phr_licencesubstancemappingid != Guid.Empty)
        {
            entity.Id = dto.phr_licencesubstancemappingid;
        }

        entity["phr_licenceid"] = new EntityReference("phr_licence", dto.phr_licenceid);
        entity["phr_substancecode"] = dto.phr_substancecode;
        entity["phr_effectivedate"] = dto.phr_effectivedate;

        if (dto.phr_maxquantitypertransaction.HasValue)
            entity["phr_maxquantitypertransaction"] = dto.phr_maxquantitypertransaction.Value;
        if (dto.phr_maxquantityperperiod.HasValue)
            entity["phr_maxquantityperperiod"] = dto.phr_maxquantityperperiod.Value;
        if (!string.IsNullOrEmpty(dto.phr_periodtype))
            entity["phr_periodtype"] = dto.phr_periodtype;
        if (!string.IsNullOrEmpty(dto.phr_restrictions))
            entity["phr_restrictions"] = dto.phr_restrictions;
        if (dto.phr_expirydate.HasValue)
            entity["phr_expirydate"] = dto.phr_expirydate.Value;

        return entity;
    }
}
