using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.Notifications;

/// <summary>
/// Service for dispatching webhook notifications to subscribed integration systems.
/// T149g, T149h: WebhookDispatchService with HMAC-SHA256 signature per FR-059.
/// Sends async notifications when compliance events occur (status changes, order approvals, etc.).
/// </summary>
public class WebhookDispatchService : IWebhookDispatchService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatchService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public WebhookDispatchService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatchService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WebhookDispatchResult> DispatchAsync(
        WebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var subscribers = await _subscriptionRepository.GetActiveByEventTypeAsync(eventType, cancellationToken);

        if (!subscribers.Any())
        {
            _logger.LogDebug("No active subscribers for event type {EventType}", eventType);
            return new WebhookDispatchResult
            {
                EventType = eventType,
                SubscribersNotified = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Results = new List<WebhookDeliveryResult>()
            };
        }

        _logger.LogInformation("Dispatching {EventType} event to {Count} subscribers", eventType, subscribers.Count);

        var results = new List<WebhookDeliveryResult>();
        var tasks = subscribers.Select(async subscriber =>
        {
            var result = await DeliverToSubscriberAsync(subscriber, eventType, payload, cancellationToken);
            return result;
        });

        var deliveryResults = await Task.WhenAll(tasks);
        results.AddRange(deliveryResults);

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        _logger.LogInformation("Webhook dispatch complete for {EventType}: {Success} succeeded, {Failed} failed",
            eventType, successCount, failureCount);

        return new WebhookDispatchResult
        {
            EventType = eventType,
            SubscribersNotified = subscribers.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookSubscription>> GetSubscribersForEventAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default)
    {
        return await _subscriptionRepository.GetActiveByEventTypeAsync(eventType, cancellationToken);
    }

    /// <inheritdoc />
    public string ComputeSignature(string payload, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <inheritdoc />
    public bool VerifySignature(string payload, string signature, string secretKey)
    {
        var expectedSignature = ComputeSignature(payload, secretKey);
        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<WebhookDeliveryResult> DeliverToSubscriberAsync(
        WebhookSubscription subscription,
        WebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        var result = new WebhookDeliveryResult
        {
            SubscriptionId = subscription.SubscriptionId,
            IntegrationSystemId = subscription.IntegrationSystemId,
            CallbackUrl = subscription.CallbackUrl,
            EventType = eventType,
            AttemptedAt = DateTime.UtcNow
        };

        try
        {
            var webhookPayload = new WebhookPayload
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Data = payload
            };

            var jsonPayload = JsonSerializer.Serialize(webhookPayload, JsonOptions);
            var signature = ComputeSignature(jsonPayload, subscription.SecretKey);

            using var client = _httpClientFactory.CreateClient("WebhookClient");
            client.Timeout = TimeSpan.FromSeconds(30);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add signature header per industry standards
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", eventType.ToString());
            request.Headers.Add("X-Webhook-Id", webhookPayload.EventId.ToString());
            request.Headers.Add("X-Webhook-Timestamp", webhookPayload.Timestamp.ToString("O"));

            var response = await client.SendAsync(request, cancellationToken);

            result.StatusCode = (int)response.StatusCode;
            result.Success = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                await _subscriptionRepository.RecordSuccessAsync(subscription.SubscriptionId, cancellationToken);
                _logger.LogDebug("Successfully delivered webhook to {Url} for subscription {Id}",
                    subscription.CallbackUrl, subscription.SubscriptionId);
            }
            else
            {
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                await _subscriptionRepository.RecordFailureAsync(subscription.SubscriptionId, cancellationToken);
                _logger.LogWarning("Webhook delivery failed to {Url} with status {Status}",
                    subscription.CallbackUrl, response.StatusCode);
            }
        }
        catch (TaskCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Request timed out";
            await _subscriptionRepository.RecordFailureAsync(subscription.SubscriptionId, cancellationToken);
            _logger.LogWarning("Webhook delivery to {Url} timed out", subscription.CallbackUrl);
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Connection error: {ex.Message}";
            await _subscriptionRepository.RecordFailureAsync(subscription.SubscriptionId, cancellationToken);
            _logger.LogWarning(ex, "Webhook delivery to {Url} failed with connection error", subscription.CallbackUrl);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
            await _subscriptionRepository.RecordFailureAsync(subscription.SubscriptionId, cancellationToken);
            _logger.LogError(ex, "Unexpected error delivering webhook to {Url}", subscription.CallbackUrl);
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }
}

/// <summary>
/// Interface for the webhook dispatch service.
/// </summary>
public interface IWebhookDispatchService
{
    /// <summary>
    /// Dispatches a webhook notification to all active subscribers for the given event type.
    /// </summary>
    /// <param name="eventType">The type of event that occurred.</param>
    /// <param name="payload">The event payload data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results of the dispatch operation.</returns>
    Task<WebhookDispatchResult> DispatchAsync(WebhookEventType eventType, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscribers for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active subscriptions for the event type.</returns>
    Task<IReadOnlyList<WebhookSubscription>> GetSubscribersForEventAsync(WebhookEventType eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes an HMAC-SHA256 signature for the given payload.
    /// T149h: Signature generation per industry standards.
    /// </summary>
    /// <param name="payload">The JSON payload to sign.</param>
    /// <param name="secretKey">The secret key for signing.</param>
    /// <returns>The signature in format "sha256={hex-encoded-hash}".</returns>
    string ComputeSignature(string payload, string secretKey);

    /// <summary>
    /// Verifies an HMAC-SHA256 signature for the given payload.
    /// Useful for webhook receivers to validate incoming webhooks.
    /// </summary>
    /// <param name="payload">The JSON payload that was signed.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="secretKey">The secret key used for signing.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    bool VerifySignature(string payload, string signature, string secretKey);
}

/// <summary>
/// Standard webhook payload structure.
/// </summary>
public class WebhookPayload
{
    /// <summary>
    /// Unique identifier for this webhook event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of event that triggered the webhook.
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Event-specific payload data.
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Result of dispatching webhooks to all subscribers.
/// </summary>
public class WebhookDispatchResult
{
    /// <summary>
    /// The event type that was dispatched.
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Total number of subscribers that were notified.
    /// </summary>
    public int SubscribersNotified { get; set; }

    /// <summary>
    /// Number of successful deliveries.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed deliveries.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Individual delivery results for each subscriber.
    /// </summary>
    public IReadOnlyList<WebhookDeliveryResult> Results { get; set; } = new List<WebhookDeliveryResult>();
}

/// <summary>
/// Result of delivering a webhook to a single subscriber.
/// </summary>
public class WebhookDeliveryResult
{
    /// <summary>
    /// The subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// The integration system ID.
    /// </summary>
    public Guid IntegrationSystemId { get; set; }

    /// <summary>
    /// The callback URL that was called.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// The event type that was delivered.
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Whether the delivery was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code returned by the callback URL.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the delivery was attempted.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// When the delivery completed (success or failure).
    /// </summary>
    public DateTime CompletedAt { get; set; }
}
