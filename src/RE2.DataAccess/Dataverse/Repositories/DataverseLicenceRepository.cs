using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ILicenceRepository.
/// T069: Repository implementation for Licence CRUD operations.
/// </summary>
public class DataverseLicenceRepository : ILicenceRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseLicenceRepository> _logger;
    private const string EntityName = "phr_licence";

    public DataverseLicenceRepository(IDataverseClient client, ILogger<DataverseLicenceRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, licenceId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licence {Id}", licenceId);
            return null;
        }
    }

    public async Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_licencenumber", ConditionOperator.Equal, licenceNumber)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licence by number {LicenceNumber}", licenceNumber);
            return null;
        }
    }

    public async Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_holderid", ConditionOperator.Equal, holderId),
                        new ConditionExpression("phr_holdertype", ConditionOperator.Equal, holderType)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licences for holder {HolderId} of type {HolderType}", holderId, holderType);
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        try
        {
            var futureDate = DateTime.UtcNow.AddDays(daysAhead);
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_expirydate", ConditionOperator.NotNull),
                        new ConditionExpression("phr_expirydate", ConditionOperator.LessEqual, futureDate),
                        new ConditionExpression("phr_expirydate", ConditionOperator.GreaterEqual, DateTime.UtcNow),
                        new ConditionExpression("phr_status", ConditionOperator.Equal, "Valid")
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring licences within {DaysAhead} days", daysAhead);
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all licences");
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var dto = LicenceDto.FromDomainModel(licence);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created licence {Id} with number {LicenceNumber}", id, licence.LicenceNumber);
        return id;
    }

    public async Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var dto = LicenceDto.FromDomainModel(licence);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated licence {Id}", licence.LicenceId);
    }

    public async Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, licenceId, cancellationToken);
        _logger.LogInformation("Deleted licence {Id}", licenceId);
    }

    public async Task<IEnumerable<Licence>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query licences through LicenceSubstanceMapping join
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Distinct = true,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_status", ConditionOperator.Equal, "Valid")
                    }
                }
            };

            // Add link entity to join with phr_licencesubstancemapping
            var linkEntity = new LinkEntity(
                EntityName,
                "phr_licencesubstancemapping",
                "phr_licenceid",
                "phr_licenceid",
                JoinOperator.Inner);

            linkEntity.LinkCriteria.AddCondition(
                "phr_substanceid",
                ConditionOperator.Equal,
                substanceId);

            query.LinkEntities.Add(linkEntity);

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licences for substance {SubstanceId}", substanceId);
            return Enumerable.Empty<Licence>();
        }
    }

    private LicenceDto MapToDto(Entity entity)
    {
        return new LicenceDto
        {
            phr_licenceid = entity.Id,
            phr_licencenumber = entity.GetAttributeValue<string>("phr_licencenumber"),
            phr_licencetypeid = entity.GetAttributeValue<Guid>("phr_licencetypeid"),
            phr_holdertype = entity.GetAttributeValue<string>("phr_holdertype"),
            phr_holderid = entity.GetAttributeValue<Guid>("phr_holderid"),
            phr_issuingauthority = entity.GetAttributeValue<string>("phr_issuingauthority"),
            phr_issuedate = entity.GetAttributeValue<DateTime>("phr_issuedate"),
            phr_expirydate = entity.GetAttributeValue<DateTime?>("phr_expirydate"),
            phr_status = entity.GetAttributeValue<string>("phr_status"),
            phr_scope = entity.GetAttributeValue<string>("phr_scope"),
            phr_permittedactivities = entity.GetAttributeValue<int>("phr_permittedactivities"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate")
        };
    }

    private Entity MapToEntity(LicenceDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_licenceid };
        entity["phr_licencenumber"] = dto.phr_licencenumber;
        entity["phr_licencetypeid"] = dto.phr_licencetypeid;
        entity["phr_holdertype"] = dto.phr_holdertype;
        entity["phr_holderid"] = dto.phr_holderid;
        entity["phr_issuingauthority"] = dto.phr_issuingauthority;
        entity["phr_issuedate"] = dto.phr_issuedate;
        entity["phr_expirydate"] = dto.phr_expirydate;
        entity["phr_status"] = dto.phr_status;
        entity["phr_scope"] = dto.phr_scope;
        entity["phr_permittedactivities"] = dto.phr_permittedactivities;
        entity["phr_modifieddate"] = DateTime.UtcNow;
        return entity;
    }
}
