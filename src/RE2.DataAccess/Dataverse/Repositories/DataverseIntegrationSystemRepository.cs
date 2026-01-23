using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IIntegrationSystemRepository.
/// T047d: Repository implementation for IntegrationSystem per data-model.md entity 27.
/// Manages API client registrations for external system access to compliance APIs.
/// </summary>
public class DataverseIntegrationSystemRepository : IIntegrationSystemRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseIntegrationSystemRepository> _logger;
    private const string EntityName = "phr_integrationsystem";

    public DataverseIntegrationSystemRepository(
        IDataverseClient client,
        ILogger<DataverseIntegrationSystemRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IntegrationSystem?> GetByIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, integrationSystemId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integration system {Id}", integrationSystemId);
            return null;
        }
    }

    public async Task<IntegrationSystem?> GetBySystemNameAsync(string systemName, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_systemname", ConditionOperator.Equal, systemName)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
    }

    public async Task<IntegrationSystem?> GetByOAuthClientIdAsync(string oauthClientId, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_oauthclientid", ConditionOperator.Equal, oauthClientId)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
    }

    public async Task<IEnumerable<IntegrationSystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<IntegrationSystem>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_isactive", ConditionOperator.Equal, true)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<IntegrationSystem>> GetBySystemTypeAsync(IntegrationSystemType systemType, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_systemtype", ConditionOperator.Equal, (int)systemType)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<Guid> CreateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default)
    {
        integrationSystem.CreatedDate = DateTime.UtcNow;
        integrationSystem.ModifiedDate = DateTime.UtcNow;

        var dto = IntegrationSystemDto.FromDomainModel(integrationSystem);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);

        _logger.LogInformation("Created integration system {Name} with ID {Id}", integrationSystem.SystemName, id);
        return id;
    }

    public async Task UpdateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default)
    {
        integrationSystem.ModifiedDate = DateTime.UtcNow;

        var dto = IntegrationSystemDto.FromDomainModel(integrationSystem);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);

        _logger.LogInformation("Updated integration system {Id}", integrationSystem.IntegrationSystemId);
    }

    public async Task DeleteAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, integrationSystemId, cancellationToken);
        _logger.LogInformation("Deleted integration system {Id}", integrationSystemId);
    }

    public async Task<bool> ExistsAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, integrationSystemId, new ColumnSet("phr_integrationsystemid"), cancellationToken);
            return entity != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SystemNameExistsAsync(string systemName, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet("phr_integrationsystemid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_systemname", ConditionOperator.Equal, systemName)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);

        if (!result.Entities.Any())
        {
            return false;
        }

        // If excludeId is provided, check if the found system is the same one being updated
        if (excludeId.HasValue)
        {
            return result.Entities.Any(e => e.Id != excludeId.Value);
        }

        return true;
    }

    public async Task<bool> IsAuthorizedAsync(Guid integrationSystemId, string endpoint, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var system = await GetByIdAsync(integrationSystemId, cancellationToken);
        if (system == null)
        {
            _logger.LogWarning("Integration system {Id} not found for authorization check", integrationSystemId);
            return false;
        }

        if (!system.IsActive)
        {
            _logger.LogWarning("Integration system {Name} is inactive", system.SystemName);
            return false;
        }

        // Check endpoint authorization
        if (!system.IsEndpointAuthorized(endpoint))
        {
            _logger.LogWarning("Endpoint {Endpoint} not authorized for system {Name}", endpoint, system.SystemName);
            return false;
        }

        // Check IP whitelist if IP address is provided
        if (!string.IsNullOrEmpty(ipAddress) && !system.IsIpAllowed(ipAddress))
        {
            _logger.LogWarning("IP {IpAddress} not allowed for system {Name}", ipAddress, system.SystemName);
            return false;
        }

        return true;
    }

    private IntegrationSystemDto MapToDto(Entity entity)
    {
        return new IntegrationSystemDto
        {
            phr_integrationsystemid = entity.Id,
            phr_systemname = entity.GetAttributeValue<string>("phr_systemname"),
            phr_systemtype = entity.GetAttributeValue<int>("phr_systemtype"),
            phr_apikeyhash = entity.GetAttributeValue<string>("phr_apikeyhash"),
            phr_oauthclientid = entity.GetAttributeValue<string>("phr_oauthclientid"),
            phr_authorizedendpoints = entity.GetAttributeValue<string>("phr_authorizedendpoints"),
            phr_ipwhitelist = entity.GetAttributeValue<string>("phr_ipwhitelist"),
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive"),
            phr_contactperson = entity.GetAttributeValue<string>("phr_contactperson"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate")
        };
    }

    private Entity MapToEntity(IntegrationSystemDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_integrationsystemid };
        entity["phr_systemname"] = dto.phr_systemname;
        entity["phr_systemtype"] = dto.phr_systemtype;
        entity["phr_apikeyhash"] = dto.phr_apikeyhash;
        entity["phr_oauthclientid"] = dto.phr_oauthclientid;
        entity["phr_authorizedendpoints"] = dto.phr_authorizedendpoints;
        entity["phr_ipwhitelist"] = dto.phr_ipwhitelist;
        entity["phr_isactive"] = dto.phr_isactive;
        entity["phr_contactperson"] = dto.phr_contactperson;
        entity["phr_createddate"] = dto.phr_createddate;
        entity["phr_modifieddate"] = dto.phr_modifieddate;
        return entity;
    }
}
