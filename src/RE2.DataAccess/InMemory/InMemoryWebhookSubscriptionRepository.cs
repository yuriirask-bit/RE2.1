using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IWebhookSubscriptionRepository for local development and testing.
/// T149e-T149f: Repository implementation for local development without Dataverse.
/// </summary>
public class InMemoryWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly Dictionary<Guid, WebhookSubscription> _subscriptions = new();
    private readonly object _lock = new();

    public Task<WebhookSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _subscriptions.TryGetValue(subscriptionId, out var subscription);
            return Task.FromResult(subscription);
        }
    }

    public Task<IReadOnlyList<WebhookSubscription>> GetByIntegrationSystemIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _subscriptions.Values
                .Where(s => s.IntegrationSystemId == integrationSystemId)
                .ToList();
            return Task.FromResult<IReadOnlyList<WebhookSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(WebhookEventType eventType, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _subscriptions.Values
                .Where(s => s.IsActive && s.EventTypes.HasFlag(eventType))
                .ToList();
            return Task.FromResult<IReadOnlyList<WebhookSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _subscriptions.Values.ToList();
            return Task.FromResult<IReadOnlyList<WebhookSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<WebhookSubscription>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _subscriptions.Values.Where(s => s.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<WebhookSubscription>>(result);
        }
    }

    public Task<WebhookSubscription> CreateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            subscription.SubscriptionId = Guid.NewGuid();
            subscription.CreatedDate = DateTime.UtcNow;
            subscription.ModifiedDate = DateTime.UtcNow;
            _subscriptions[subscription.SubscriptionId] = subscription;
            return Task.FromResult(subscription);
        }
    }

    public Task<WebhookSubscription> UpdateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            subscription.ModifiedDate = DateTime.UtcNow;
            _subscriptions[subscription.SubscriptionId] = subscription;
            return Task.FromResult(subscription);
        }
    }

    public Task<bool> DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_subscriptions.Remove(subscriptionId));
        }
    }

    public Task RecordSuccessAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
            {
                subscription.RecordSuccess();
            }
            return Task.CompletedTask;
        }
    }

    public Task RecordFailureAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
            {
                subscription.RecordFailure();
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsByCallbackUrlAsync(Guid integrationSystemId, string callbackUrl, Guid? excludeSubscriptionId = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var exists = _subscriptions.Values.Any(s =>
                s.IntegrationSystemId == integrationSystemId &&
                s.CallbackUrl.Equals(callbackUrl, StringComparison.OrdinalIgnoreCase) &&
                (!excludeSubscriptionId.HasValue || s.SubscriptionId != excludeSubscriptionId.Value));
            return Task.FromResult(exists);
        }
    }
}
