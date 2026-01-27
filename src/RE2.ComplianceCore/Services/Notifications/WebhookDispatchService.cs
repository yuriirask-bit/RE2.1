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
/// T149h2: Retry logic with exponential backoff (10s, 60s, 300s) per FR-059.
/// Sends async notifications when compliance events occur (status changes, order approvals, etc.).
/// </summary>
public class WebhookDispatchService : IWebhookDispatchService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAlertRepository? _alertRepository;
    private readonly ILogger<WebhookDispatchService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// T149h2: Retry delays in seconds for exponential backoff per FR-059.
    /// 3 retries: 10s, 60s, 300s (5 minutes).
    /// </summary>
    private static readonly int[] RetryDelaysSeconds = { 10, 60, 300 };

    /// <summary>
    /// T149h2: Maximum consecutive failures before marking subscription as unhealthy per FR-059.
    /// After 3 consecutive failures (initial + retries exhausted), subscription is marked unhealthy.
    /// </summary>
    public const int MaxConsecutiveFailuresBeforeUnhealthy = 3;

    public WebhookDispatchService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatchService> logger,
        IAlertRepository? alertRepository = null)
    {
        _subscriptionRepository = subscriptionRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _alertRepository = alertRepository;
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

    /// <summary>
    /// T149h2: Delivers a webhook to a subscriber with retry logic.
    /// Retries up to 3 times with exponential backoff (10s, 60s, 300s) per FR-059.
    /// After all retries exhausted, records failure and checks if subscription should be marked unhealthy.
    /// </summary>
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

        var webhookPayload = new WebhookPayload
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Data = payload
        };

        var jsonPayload = JsonSerializer.Serialize(webhookPayload, JsonOptions);
        var signature = ComputeSignature(jsonPayload, subscription.SecretKey);

        // Attempt delivery with retries (initial attempt + RetryDelaysSeconds.Length retries)
        var totalAttempts = 1 + RetryDelaysSeconds.Length;
        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            var (success, statusCode, errorMessage) = await AttemptDeliveryAsync(
                subscription, jsonPayload, signature, eventType, webhookPayload.EventId, webhookPayload.Timestamp, cancellationToken);

            if (success)
            {
                result.Success = true;
                result.StatusCode = statusCode;
                await _subscriptionRepository.RecordSuccessAsync(subscription.SubscriptionId, cancellationToken);

                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Webhook delivered to {Url} on retry attempt {Attempt} for subscription {Id}",
                        subscription.CallbackUrl, attempt, subscription.SubscriptionId);
                }
                else
                {
                    _logger.LogDebug("Successfully delivered webhook to {Url} for subscription {Id}",
                        subscription.CallbackUrl, subscription.SubscriptionId);
                }

                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            result.StatusCode = statusCode;
            result.ErrorMessage = errorMessage;

            // If more retries remain, wait before next attempt
            if (attempt < totalAttempts)
            {
                var delaySeconds = RetryDelaysSeconds[attempt - 1];
                _logger.LogWarning(
                    "Webhook delivery to {Url} failed (attempt {Attempt}/{Total}). Retrying in {Delay}s. Error: {Error}",
                    subscription.CallbackUrl, attempt, totalAttempts, delaySeconds, errorMessage);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Webhook retry cancelled for subscription {Id}", subscription.SubscriptionId);
                    break;
                }
            }
        }

        // All attempts exhausted — record failure
        result.Success = false;
        result.CompletedAt = DateTime.UtcNow;

        await _subscriptionRepository.RecordFailureAsync(subscription.SubscriptionId, cancellationToken);

        _logger.LogError(
            "Webhook delivery to {Url} failed after {Attempts} attempts for subscription {Id}. Last error: {Error}",
            subscription.CallbackUrl, totalAttempts, subscription.SubscriptionId, result.ErrorMessage);

        // T149h2: Check if subscription should be marked unhealthy after consecutive failures
        await CheckAndMarkUnhealthyAsync(subscription, cancellationToken);

        return result;
    }

    /// <summary>
    /// Executes a single HTTP delivery attempt.
    /// </summary>
    private async Task<(bool success, int? statusCode, string? errorMessage)> AttemptDeliveryAsync(
        WebhookSubscription subscription,
        string jsonPayload,
        string signature,
        WebhookEventType eventType,
        Guid eventId,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("WebhookClient");
            client.Timeout = TimeSpan.FromSeconds(30);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add signature headers per industry standards
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", eventType.ToString());
            request.Headers.Add("X-Webhook-Id", eventId.ToString());
            request.Headers.Add("X-Webhook-Timestamp", timestamp.ToString("O"));

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, (int)response.StatusCode, null);
            }

            return (false, (int)response.StatusCode, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, null, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"Connection error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// T149h2: Checks if a subscription has exceeded the consecutive failure threshold
    /// and marks it as unhealthy, generating a SystemAdmin alert per FR-059.
    /// </summary>
    private async Task CheckAndMarkUnhealthyAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken)
    {
        // Re-read current state to get accurate FailedAttempts count
        var current = await _subscriptionRepository.GetByIdAsync(subscription.SubscriptionId, cancellationToken);
        if (current == null || !current.IsActive) return;

        if (current.FailedAttempts >= MaxConsecutiveFailuresBeforeUnhealthy)
        {
            _logger.LogError(
                "Webhook subscription {Id} for {Url} marked as unhealthy after {Failures} consecutive failures. " +
                "Integration system: {SystemId}",
                current.SubscriptionId, current.CallbackUrl, current.FailedAttempts, current.IntegrationSystemId);

            current.Deactivate();
            await _subscriptionRepository.UpdateAsync(current, cancellationToken);

            // Generate SystemAdmin alert per FR-059
            await GenerateWebhookFailureAlertAsync(current, cancellationToken);
        }
    }

    /// <summary>
    /// T149h2: Generates a SystemAdmin alert when a webhook subscription is marked unhealthy per FR-059.
    /// </summary>
    private async Task GenerateWebhookFailureAlertAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (_alertRepository == null)
        {
            _logger.LogWarning(
                "Cannot generate webhook failure alert for subscription {Id}: IAlertRepository not available",
                subscription.SubscriptionId);
            return;
        }

        try
        {
            var alert = new Alert
            {
                AlertId = Guid.NewGuid(),
                AlertType = AlertType.WebhookDeliveryFailure,
                Severity = AlertSeverity.Critical,
                TargetEntityType = TargetEntityType.WebhookSubscription,
                TargetEntityId = subscription.SubscriptionId,
                Message = $"Webhook subscription deactivated after {subscription.FailedAttempts} consecutive delivery failures",
                Details = $"Subscription ID: {subscription.SubscriptionId}, " +
                          $"Callback URL: {subscription.CallbackUrl}, " +
                          $"Integration System: {subscription.IntegrationSystemId}, " +
                          $"Last failed: {subscription.LastFailedDelivery:O}. " +
                          $"Subscription has been automatically deactivated. Manual reactivation required.",
                GeneratedDate = DateTime.UtcNow
            };

            await _alertRepository.CreateAsync(alert, cancellationToken);

            _logger.LogInformation(
                "Generated webhook failure alert {AlertId} for subscription {SubscriptionId}",
                alert.AlertId, subscription.SubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate webhook failure alert for subscription {Id}",
                subscription.SubscriptionId);
        }
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
