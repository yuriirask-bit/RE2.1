using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for WebhookSubscription entity operations.
/// T149e: IWebhookSubscriptionRepository interface per FR-059.
/// Provides CRUD operations and query methods for webhook subscriptions.
/// </summary>
public interface IWebhookSubscriptionRepository
{
    /// <summary>
    /// Gets a webhook subscription by its unique identifier.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook subscription if found, null otherwise.</returns>
    Task<WebhookSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all webhook subscriptions for a specific integration system.
    /// </summary>
    /// <param name="integrationSystemId">The integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of webhook subscriptions for the integration system.</returns>
    Task<IReadOnlyList<WebhookSubscription>> GetByIntegrationSystemIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active webhook subscriptions that listen for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active subscriptions that include the specified event type.</returns>
    Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(WebhookEventType eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all webhook subscriptions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all webhook subscriptions.</returns>
    Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active webhook subscriptions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all active webhook subscriptions.</returns>
    Task<IReadOnlyList<WebhookSubscription>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new webhook subscription.
    /// </summary>
    /// <param name="subscription">The subscription to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created subscription with generated ID.</returns>
    Task<WebhookSubscription> CreateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing webhook subscription.
    /// </summary>
    /// <param name="subscription">The subscription to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated subscription.</returns>
    Task<WebhookSubscription> UpdateAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion was successful, false if subscription was not found.</returns>
    Task<bool> DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful webhook delivery for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordSuccessAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed webhook delivery attempt for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordFailureAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a subscription with the same callback URL already exists for an integration system.
    /// </summary>
    /// <param name="integrationSystemId">The integration system ID.</param>
    /// <param name="callbackUrl">The callback URL to check.</param>
    /// <param name="excludeSubscriptionId">Optional subscription ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a duplicate exists, false otherwise.</returns>
    Task<bool> ExistsByCallbackUrlAsync(Guid integrationSystemId, string callbackUrl, Guid? excludeSubscriptionId = null, CancellationToken cancellationToken = default);
}
