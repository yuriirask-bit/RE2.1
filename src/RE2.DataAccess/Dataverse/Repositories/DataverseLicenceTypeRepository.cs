using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ILicenceTypeRepository.
/// T071: Repository implementation for LicenceType.
/// </summary>
public class DataverseLicenceTypeRepository : ILicenceTypeRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseLicenceTypeRepository> _logger;
    private const string EntityName = "phr_licencetype";

    public DataverseLicenceTypeRepository(IDataverseClient client, ILogger<DataverseLicenceTypeRepository> _logger)
    {
        _client = client;
        this._logger = _logger;
    }

    public async Task<LicenceType?> GetByIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, licenceTypeId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licence type {Id}", licenceTypeId);
            return null;
        }
    }

    public async Task<LicenceType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_name", ConditionOperator.Equal, name)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
    }

    public async Task<IEnumerable<LicenceType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<LicenceType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<Guid> CreateAsync(LicenceType licenceType, CancellationToken cancellationToken = default)
    {
        var dto = LicenceTypeDto.FromDomainModel(licenceType);
        var entity = MapToEntity(dto);
        return await _client.CreateAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(LicenceType licenceType, CancellationToken cancellationToken = default)
    {
        var dto = LicenceTypeDto.FromDomainModel(licenceType);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid licenceTypeId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, licenceTypeId, cancellationToken);
    }

    private LicenceTypeDto MapToDto(Entity entity)
    {
        return new LicenceTypeDto
        {
            phr_licencetypeid = entity.Id,
            phr_name = entity.GetAttributeValue<string>("phr_name"),
            phr_issuingauthority = entity.GetAttributeValue<string>("phr_issuingauthority"),
            phr_typicalvaliditymonths = entity.GetAttributeValue<int?>("phr_typicalvaliditymonths"),
            phr_permittedactivities = entity.GetAttributeValue<int>("phr_permittedactivities"),
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive")
        };
    }

    private Entity MapToEntity(LicenceTypeDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_licencetypeid };
        entity["phr_name"] = dto.phr_name;
        entity["phr_issuingauthority"] = dto.phr_issuingauthority;
        entity["phr_typicalvaliditymonths"] = dto.phr_typicalvaliditymonths;
        entity["phr_permittedactivities"] = dto.phr_permittedactivities;
        entity["phr_isactive"] = dto.phr_isactive;
        return entity;
    }
}
