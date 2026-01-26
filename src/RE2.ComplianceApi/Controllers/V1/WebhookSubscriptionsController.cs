using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Webhook subscription management API endpoints.
/// T149j: WebhookSubscriptionsController v1 for managing webhook subscriptions per FR-059.
/// Allows integration systems to subscribe to compliance events for async notifications.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class WebhookSubscriptionsController : ControllerBase
{
    private readonly IWebhookSubscriptionRepository _repository;
    private readonly IIntegrationSystemRepository _integrationSystemRepository;
    private readonly ILogger<WebhookSubscriptionsController> _logger;

    public WebhookSubscriptionsController(
        IWebhookSubscriptionRepository repository,
        IIntegrationSystemRepository integrationSystemRepository,
        ILogger<WebhookSubscriptionsController> logger)
    {
        _repository = repository;
        _integrationSystemRepository = integrationSystemRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all webhook subscriptions.
    /// Only SystemAdmin role can view all subscriptions.
    /// </summary>
    /// <param name="integrationSystemId">Optional filter by integration system ID.</param>
    /// <param name="activeOnly">If true, only returns active subscriptions. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of webhook subscriptions.</returns>
    [HttpGet]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<WebhookSubscriptionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookSubscriptions(
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebhookSubscription> subscriptions;

        if (integrationSystemId.HasValue)
        {
            subscriptions = await _repository.GetByIntegrationSystemIdAsync(integrationSystemId.Value, cancellationToken);
        }
        else if (activeOnly)
        {
            subscriptions = await _repository.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            subscriptions = await _repository.GetAllAsync(cancellationToken);
        }

        var response = subscriptions.Select(WebhookSubscriptionResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific webhook subscription by ID.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook subscription details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Webhook subscription with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(WebhookSubscriptionResponseDto.FromDomainModel(subscription));
    }

    /// <summary>
    /// Creates a new webhook subscription.
    /// T149k: Only SystemAdmin role can create webhook subscriptions.
    /// </summary>
    /// <param name="request">Subscription creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created webhook subscription details.</returns>
    [HttpPost]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWebhookSubscription(
        [FromBody] CreateWebhookSubscriptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate integration system exists
        var integrationSystem = await _integrationSystemRepository.GetByIdAsync(request.IntegrationSystemId, cancellationToken);
        if (integrationSystem == null)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Integration system with ID '{request.IntegrationSystemId}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var subscription = request.ToDomainModel();

        // Validate the subscription
        var validationResult = subscription.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Webhook subscription validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate callback URL for this integration system
        if (await _repository.ExistsByCallbackUrlAsync(subscription.IntegrationSystemId, subscription.CallbackUrl, null, cancellationToken))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"A subscription with callback URL '{subscription.CallbackUrl}' already exists for this integration system",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var created = await _repository.CreateAsync(subscription, cancellationToken);
        _logger.LogInformation(
            "Created webhook subscription {Id} for integration system {SystemId} with callback URL {CallbackUrl}",
            created.SubscriptionId,
            created.IntegrationSystemId,
            created.CallbackUrl);

        return CreatedAtAction(
            nameof(GetWebhookSubscription),
            new { id = created.SubscriptionId },
            WebhookSubscriptionResponseDto.FromDomainModel(created));
    }

    /// <summary>
    /// Updates an existing webhook subscription.
    /// Only SystemAdmin role can modify webhook subscriptions.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="request">Updated subscription data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated webhook subscription details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWebhookSubscription(
        Guid id,
        [FromBody] UpdateWebhookSubscriptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Webhook subscription with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var subscription = request.ToDomainModel(id, existing.IntegrationSystemId);
        subscription.CreatedDate = existing.CreatedDate;
        subscription.FailedAttempts = existing.FailedAttempts;
        subscription.LastSuccessfulDelivery = existing.LastSuccessfulDelivery;
        subscription.LastFailedDelivery = existing.LastFailedDelivery;

        // Validate the subscription
        var validationResult = subscription.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Webhook subscription validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate callback URL (if changed)
        if (existing.CallbackUrl != subscription.CallbackUrl &&
            await _repository.ExistsByCallbackUrlAsync(subscription.IntegrationSystemId, subscription.CallbackUrl, id, cancellationToken))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"A subscription with callback URL '{subscription.CallbackUrl}' already exists for this integration system",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var updated = await _repository.UpdateAsync(subscription, cancellationToken);
        _logger.LogInformation("Updated webhook subscription {Id}", id);

        return Ok(WebhookSubscriptionResponseDto.FromDomainModel(updated));
    }

    /// <summary>
    /// Deletes a webhook subscription.
    /// Only SystemAdmin role can delete webhook subscriptions.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhookSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Webhook subscription with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted webhook subscription {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Reactivates a disabled webhook subscription.
    /// Resets the failure count and reactivates the subscription.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated webhook subscription details.</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateWebhookSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);
        if (subscription == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Webhook subscription with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        subscription.Reactivate();
        var updated = await _repository.UpdateAsync(subscription, cancellationToken);
        _logger.LogInformation("Reactivated webhook subscription {Id}", id);

        return Ok(WebhookSubscriptionResponseDto.FromDomainModel(updated));
    }

    /// <summary>
    /// Deactivates a webhook subscription.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated webhook subscription details.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateWebhookSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);
        if (subscription == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Webhook subscription with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        subscription.Deactivate();
        var updated = await _repository.UpdateAsync(subscription, cancellationToken);
        _logger.LogInformation("Deactivated webhook subscription {Id}", id);

        return Ok(WebhookSubscriptionResponseDto.FromDomainModel(updated));
    }

    /// <summary>
    /// Gets the available event types for webhook subscriptions.
    /// </summary>
    /// <returns>List of available event types.</returns>
    [HttpGet("event-types")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<WebhookEventTypeDto>), StatusCodes.Status200OK)]
    public IActionResult GetEventTypes()
    {
        var eventTypes = new List<WebhookEventTypeDto>
        {
            new() { Name = nameof(WebhookEventType.ComplianceStatusChanged), Value = (int)WebhookEventType.ComplianceStatusChanged, Description = "Fired when a customer's compliance status changes" },
            new() { Name = nameof(WebhookEventType.OrderApproved), Value = (int)WebhookEventType.OrderApproved, Description = "Fired when an order passes compliance validation or override is approved" },
            new() { Name = nameof(WebhookEventType.OrderRejected), Value = (int)WebhookEventType.OrderRejected, Description = "Fired when an order fails compliance validation" },
            new() { Name = nameof(WebhookEventType.LicenceExpiring), Value = (int)WebhookEventType.LicenceExpiring, Description = "Fired when a licence is within the expiry warning period" },
            new() { Name = nameof(WebhookEventType.OverrideApproved), Value = (int)WebhookEventType.OverrideApproved, Description = "Fired when a compliance override is approved" }
        };

        return Ok(eventTypes);
    }
}

#region DTOs

/// <summary>
/// Webhook subscription response DTO for API responses.
/// </summary>
public class WebhookSubscriptionResponseDto
{
    public Guid SubscriptionId { get; set; }
    public Guid IntegrationSystemId { get; set; }
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    public int EventTypesFlags { get; set; }
    public required string CallbackUrl { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LastSuccessfulDelivery { get; set; }
    public DateTime? LastFailedDelivery { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static WebhookSubscriptionResponseDto FromDomainModel(WebhookSubscription subscription)
    {
        return new WebhookSubscriptionResponseDto
        {
            SubscriptionId = subscription.SubscriptionId,
            IntegrationSystemId = subscription.IntegrationSystemId,
            EventTypes = subscription.GetEventTypesArray().Select(e => e.ToString()).ToArray(),
            EventTypesFlags = (int)subscription.EventTypes,
            CallbackUrl = subscription.CallbackUrl,
            IsActive = subscription.IsActive,
            Description = subscription.Description,
            FailedAttempts = subscription.FailedAttempts,
            LastSuccessfulDelivery = subscription.LastSuccessfulDelivery,
            LastFailedDelivery = subscription.LastFailedDelivery,
            CreatedDate = subscription.CreatedDate,
            ModifiedDate = subscription.ModifiedDate
        };
    }
}

/// <summary>
/// Request DTO for creating a new webhook subscription.
/// </summary>
public class CreateWebhookSubscriptionRequestDto
{
    /// <summary>
    /// ID of the integration system that will receive webhooks.
    /// </summary>
    public Guid IntegrationSystemId { get; set; }

    /// <summary>
    /// Event types to subscribe to. Can be individual event names or a combined flags value.
    /// </summary>
    public string[]? EventTypes { get; set; }

    /// <summary>
    /// Combined flags value for event types. Alternative to EventTypes array.
    /// </summary>
    public int? EventTypesFlags { get; set; }

    /// <summary>
    /// The HTTPS URL to receive webhook notifications.
    /// </summary>
    public required string CallbackUrl { get; set; }

    /// <summary>
    /// Secret key for HMAC-SHA256 signature. Must be at least 32 characters.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Optional description of the subscription purpose.
    /// </summary>
    public string? Description { get; set; }

    public WebhookSubscription ToDomainModel()
    {
        var eventTypes = WebhookEventType.None;

        if (EventTypesFlags.HasValue)
        {
            eventTypes = (WebhookEventType)EventTypesFlags.Value;
        }
        else if (EventTypes != null)
        {
            foreach (var eventName in EventTypes)
            {
                if (Enum.TryParse<WebhookEventType>(eventName, true, out var parsed))
                {
                    eventTypes |= parsed;
                }
            }
        }

        return new WebhookSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            IntegrationSystemId = IntegrationSystemId,
            EventTypes = eventTypes,
            CallbackUrl = CallbackUrl,
            SecretKey = SecretKey,
            Description = Description,
            IsActive = true
        };
    }
}

/// <summary>
/// Request DTO for updating a webhook subscription.
/// </summary>
public class UpdateWebhookSubscriptionRequestDto
{
    /// <summary>
    /// Event types to subscribe to. Can be individual event names or a combined flags value.
    /// </summary>
    public string[]? EventTypes { get; set; }

    /// <summary>
    /// Combined flags value for event types. Alternative to EventTypes array.
    /// </summary>
    public int? EventTypesFlags { get; set; }

    /// <summary>
    /// The HTTPS URL to receive webhook notifications.
    /// </summary>
    public required string CallbackUrl { get; set; }

    /// <summary>
    /// Secret key for HMAC-SHA256 signature. Must be at least 32 characters.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Optional description of the subscription purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public WebhookSubscription ToDomainModel(Guid subscriptionId, Guid integrationSystemId)
    {
        var eventTypes = WebhookEventType.None;

        if (EventTypesFlags.HasValue)
        {
            eventTypes = (WebhookEventType)EventTypesFlags.Value;
        }
        else if (EventTypes != null)
        {
            foreach (var eventName in EventTypes)
            {
                if (Enum.TryParse<WebhookEventType>(eventName, true, out var parsed))
                {
                    eventTypes |= parsed;
                }
            }
        }

        return new WebhookSubscription
        {
            SubscriptionId = subscriptionId,
            IntegrationSystemId = integrationSystemId,
            EventTypes = eventTypes,
            CallbackUrl = CallbackUrl,
            SecretKey = SecretKey,
            Description = Description,
            IsActive = IsActive
        };
    }
}

/// <summary>
/// Webhook event type description DTO.
/// </summary>
public class WebhookEventTypeDto
{
    public required string Name { get; set; }
    public int Value { get; set; }
    public required string Description { get; set; }
}

#endregion
