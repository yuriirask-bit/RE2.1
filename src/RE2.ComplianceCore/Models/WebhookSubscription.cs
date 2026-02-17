using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a webhook subscription for receiving async notifications when compliance events occur.
/// T149c: WebhookSubscription domain model per FR-059 (webhook/callback mechanism for async notifications).
/// Integration systems can subscribe to receive callbacks when compliance status changes, orders are approved/rejected, etc.
/// </summary>
public class WebhookSubscription
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Reference to the integration system that owns this subscription.
    /// Required.
    /// </summary>
    public Guid IntegrationSystemId { get; set; }

    /// <summary>
    /// Event types this subscription listens for.
    /// Required.
    /// </summary>
    public WebhookEventType EventTypes { get; set; }

    /// <summary>
    /// The URL to send webhook notifications to.
    /// Required.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Secret key used for HMAC-SHA256 signature generation.
    /// Required. Used to sign payloads so receivers can verify authenticity.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether this subscription is active.
    /// Required, default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description of the subscription purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of consecutive failed delivery attempts.
    /// Used for circuit breaker logic.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Timestamp of last successful delivery.
    /// </summary>
    public DateTime? LastSuccessfulDelivery { get; set; }

    /// <summary>
    /// Timestamp of last failed delivery attempt.
    /// </summary>
    public DateTime? LastFailedDelivery { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the integration system.
    /// </summary>
    public IntegrationSystem? IntegrationSystem { get; set; }

    /// <summary>
    /// Validates the webhook subscription according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (IntegrationSystemId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "IntegrationSystemId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(CallbackUrl))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CallbackUrl is required"
            });
        }
        else if (!Uri.TryCreate(CallbackUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CallbackUrl must be a valid HTTP or HTTPS URL"
            });
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SecretKey is required for webhook signature verification"
            });
        }
        else if (SecretKey.Length < 32)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SecretKey must be at least 32 characters for security"
            });
        }

        if (EventTypes == WebhookEventType.None)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "At least one EventType must be specified"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if this subscription should receive notifications for the given event type.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>True if the subscription includes this event type and is active.</returns>
    public bool ShouldReceive(WebhookEventType eventType)
    {
        return IsActive && EventTypes.HasFlag(eventType);
    }

    /// <summary>
    /// Records a successful delivery.
    /// </summary>
    public void RecordSuccess()
    {
        LastSuccessfulDelivery = DateTime.UtcNow;
        FailedAttempts = 0;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed delivery attempt.
    /// </summary>
    public void RecordFailure()
    {
        LastFailedDelivery = DateTime.UtcNow;
        FailedAttempts++;
        ModifiedDate = DateTime.UtcNow;

        // Auto-disable after 10 consecutive failures
        if (FailedAttempts >= 10)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Reactivates a subscription and resets failure count.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        FailedAttempts = 0;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the subscription.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list of event types as an array.
    /// </summary>
    /// <returns>Array of event types this subscription listens for.</returns>
    public WebhookEventType[] GetEventTypesArray()
    {
        var types = new List<WebhookEventType>();

        if (EventTypes.HasFlag(WebhookEventType.ComplianceStatusChanged))
        {
            types.Add(WebhookEventType.ComplianceStatusChanged);
        }

        if (EventTypes.HasFlag(WebhookEventType.OrderApproved))
        {
            types.Add(WebhookEventType.OrderApproved);
        }

        if (EventTypes.HasFlag(WebhookEventType.OrderRejected))
        {
            types.Add(WebhookEventType.OrderRejected);
        }

        if (EventTypes.HasFlag(WebhookEventType.LicenceExpiring))
        {
            types.Add(WebhookEventType.LicenceExpiring);
        }

        if (EventTypes.HasFlag(WebhookEventType.OverrideApproved))
        {
            types.Add(WebhookEventType.OverrideApproved);
        }

        return types.ToArray();
    }
}

/// <summary>
/// Types of events that can trigger webhook notifications.
/// Per FR-059: webhook/callback mechanism for asynchronous notifications.
/// </summary>
[Flags]
public enum WebhookEventType
{
    /// <summary>
    /// No events selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Customer compliance status changed (e.g., became non-compliant due to licence expiry).
    /// </summary>
    ComplianceStatusChanged = 1,

    /// <summary>
    /// Order was approved (passed compliance validation or override approved).
    /// </summary>
    OrderApproved = 2,

    /// <summary>
    /// Order was rejected (failed compliance validation).
    /// </summary>
    OrderRejected = 4,

    /// <summary>
    /// Licence is expiring soon (within configured warning period).
    /// </summary>
    LicenceExpiring = 8,

    /// <summary>
    /// Compliance override was approved by authorized user.
    /// </summary>
    OverrideApproved = 16,

    /// <summary>
    /// All event types.
    /// </summary>
    All = ComplianceStatusChanged | OrderApproved | OrderRejected | LicenceExpiring | OverrideApproved
}
