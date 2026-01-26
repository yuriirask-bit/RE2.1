using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IWebhookSubscriptionRepository.
/// T149f: Repository implementation for WebhookSubscription per FR-059.
/// Manages webhook subscriptions for async notifications to integration systems.
/// </summary>
public class DataverseWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseWebhookSubscriptionRepository> _logger;
    private const string EntityName = "phr_webhooksubscription";

    public DataverseWebhookSubscriptionRepository(
        IDataverseClient client,
        ILogger<DataverseWebhookSubscriptionRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<WebhookSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, subscriptionId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook subscription {Id}", subscriptionId);
            return null;
        }
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetByIntegrationSystemIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_integrationsystemid", ConditionOperator.Equal, integrationSystemId)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(WebhookEventType eventType, CancellationToken cancellationToken = default)
    {
        // Get all active subscriptions first, then filter in memory
        // This is because Dataverse doesn't support bitwise operations in queries
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
        var subscriptions = result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();

        // Filter by event type flag
        return subscriptions.Where(s => s.EventTypes.HasFlag(eventType)).ToList();
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetAllActiveAsync(CancellationToken cancellationToken = default)
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

    public async Task<WebhookSubscription> CreateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        subscription.CreatedDate = DateTime.UtcNow;
        subscription.ModifiedDate = DateTime.UtcNow;

        var dto = WebhookSubscriptionDto.FromDomainModel(subscription);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);

        subscription.SubscriptionId = id;
        _logger.LogInformation("Created webhook subscription {Id} for integration system {SystemId}", id, subscription.IntegrationSystemId);

        return subscription;
    }

    public async Task<WebhookSubscription> UpdateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        subscription.ModifiedDate = DateTime.UtcNow;

        var dto = WebhookSubscriptionDto.FromDomainModel(subscription);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);

        _logger.LogInformation("Updated webhook subscription {Id}", subscription.SubscriptionId);
        return subscription;
    }

    public async Task<bool> DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteAsync(EntityName, subscriptionId, cancellationToken);
            _logger.LogInformation("Deleted webhook subscription {Id}", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook subscription {Id}", subscriptionId);
            return false;
        }
    }

    public async Task RecordSuccessAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription != null)
        {
            subscription.RecordSuccess();
            await UpdateAsync(subscription, cancellationToken);
            _logger.LogDebug("Recorded successful delivery for subscription {Id}", subscriptionId);
        }
    }

    public async Task RecordFailureAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription != null)
        {
            subscription.RecordFailure();
            await UpdateAsync(subscription, cancellationToken);

            if (!subscription.IsActive)
            {
                _logger.LogWarning("Webhook subscription {Id} auto-disabled after {Attempts} consecutive failures",
                    subscriptionId, subscription.FailedAttempts);
            }
            else
            {
                _logger.LogDebug("Recorded failed delivery for subscription {Id}, attempt {Attempt}",
                    subscriptionId, subscription.FailedAttempts);
            }
        }
    }

    public async Task<bool> ExistsByCallbackUrlAsync(Guid integrationSystemId, string callbackUrl, Guid? excludeSubscriptionId = null, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet("phr_webhooksubscriptionid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_integrationsystemid", ConditionOperator.Equal, integrationSystemId),
                    new ConditionExpression("phr_callbackurl", ConditionOperator.Equal, callbackUrl)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);

        if (!result.Entities.Any())
        {
            return false;
        }

        // If excludeSubscriptionId is provided, check if the found subscription is the same one being updated
        if (excludeSubscriptionId.HasValue)
        {
            return result.Entities.Any(e => e.Id != excludeSubscriptionId.Value);
        }

        return true;
    }

    private WebhookSubscriptionDto MapToDto(Entity entity)
    {
        return new WebhookSubscriptionDto
        {
            phr_webhooksubscriptionid = entity.Id,
            phr_integrationsystemid = entity.GetAttributeValue<Guid>("phr_integrationsystemid"),
            phr_eventtypes = entity.GetAttributeValue<int>("phr_eventtypes"),
            phr_callbackurl = entity.GetAttributeValue<string>("phr_callbackurl"),
            phr_secretkey = entity.GetAttributeValue<string>("phr_secretkey"),
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive"),
            phr_description = entity.GetAttributeValue<string>("phr_description"),
            phr_failedattempts = entity.GetAttributeValue<int>("phr_failedattempts"),
            phr_lastsuccessfuldelivery = entity.GetAttributeValue<DateTime?>("phr_lastsuccessfuldelivery"),
            phr_lastfaileddelivery = entity.GetAttributeValue<DateTime?>("phr_lastfaileddelivery"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate")
        };
    }

    private Entity MapToEntity(WebhookSubscriptionDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_webhooksubscriptionid };
        entity["phr_integrationsystemid"] = dto.phr_integrationsystemid;
        entity["phr_eventtypes"] = dto.phr_eventtypes;
        entity["phr_callbackurl"] = dto.phr_callbackurl;
        entity["phr_secretkey"] = dto.phr_secretkey;
        entity["phr_isactive"] = dto.phr_isactive;
        entity["phr_description"] = dto.phr_description;
        entity["phr_failedattempts"] = dto.phr_failedattempts;
        entity["phr_lastsuccessfuldelivery"] = dto.phr_lastsuccessfuldelivery;
        entity["phr_lastfaileddelivery"] = dto.phr_lastfaileddelivery;
        entity["phr_createddate"] = dto.phr_createddate;
        entity["phr_modifieddate"] = dto.phr_modifieddate;
        return entity;
    }
}
