using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.Auditing;

/// <summary>
/// Service for logging audit events for data modifications.
/// T156: Implementation of audit logging interceptor per FR-027.
/// Supports async-scoped correlation IDs and user context.
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<AuditLoggingService> _logger;

    // Use AsyncLocal for request-scoped context
    private static readonly AsyncLocal<string?> _correlationId = new();
    private static readonly AsyncLocal<UserContext?> _userContext = new();

    public AuditLoggingService(
        IAuditRepository auditRepository,
        ILogger<AuditLoggingService> logger)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    public UserContext? CurrentUser
    {
        get => _userContext.Value;
        set => _userContext.Value = value;
    }

    public async Task LogCreateAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = AuditEvent.ForCreate(entityType, entityId, performedBy, details);
            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogDebug("Logged create event for {EntityType} {EntityId} by {PerformedBy}",
                entityType, entityId, performedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log create event for {EntityType} {EntityId}",
                entityType, entityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogModifyAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        EntityModificationDetails? changes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = AuditEvent.ForModify(entityType, entityId, performedBy, changes);
            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogDebug("Logged modify event for {EntityType} {EntityId} by {PerformedBy}",
                entityType, entityId, performedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log modify event for {EntityType} {EntityId}",
                entityType, entityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogDeleteAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventType = GetDeleteEventType(entityType);

            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                EventDate = DateTime.UtcNow,
                PerformedBy = performedBy,
                EntityType = entityType,
                EntityId = entityId
            };

            if (!string.IsNullOrEmpty(reason))
            {
                auditEvent.SetDetails(new { reason });
            }

            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogDebug("Logged delete event for {EntityType} {EntityId} by {PerformedBy}",
                entityType, entityId, performedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log delete event for {EntityType} {EntityId}",
                entityType, entityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogStatusChangeAsync(
        AuditEntityType entityType,
        Guid entityId,
        AuditEventType eventType,
        Guid performedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                EventDate = DateTime.UtcNow,
                PerformedBy = performedBy,
                EntityType = entityType,
                EntityId = entityId
            };

            if (!string.IsNullOrEmpty(reason))
            {
                auditEvent.SetDetails(new { reason });
            }

            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogDebug("Logged status change event {EventType} for {EntityType} {EntityId}",
                eventType, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log status change for {EntityType} {EntityId}",
                entityType, entityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogOverrideApprovalAsync(
        Guid transactionId,
        Guid approvedBy,
        string justification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = AuditEvent.ForOverrideApproval(transactionId, approvedBy, justification);
            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogInformation("Logged override approval for transaction {TransactionId} by {ApprovedBy}",
                transactionId, approvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log override approval for transaction {TransactionId}",
                transactionId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogConflictResolutionAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid resolvedBy,
        ConflictResolutionDetails details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = AuditEvent.ForConflictResolution(entityType, entityId, resolvedBy, details);
            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogInformation("Logged conflict resolution for {EntityType} {EntityId} by {ResolvedBy}",
                entityType, entityId, resolvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log conflict resolution for {EntityType} {EntityId}",
                entityType, entityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (auditEvent.EventId == Guid.Empty)
            {
                auditEvent.EventId = Guid.NewGuid();
            }

            EnrichEvent(auditEvent);

            await _auditRepository.CreateAsync(auditEvent, cancellationToken);

            _logger.LogDebug("Logged audit event {EventType} for {EntityType} {EntityId}",
                auditEvent.EventType, auditEvent.EntityType, auditEvent.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event for {EntityType} {EntityId}",
                auditEvent.EntityType, auditEvent.EntityId);
            // Don't throw - audit logging should not break main operations
        }
    }

    public async Task LogEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = auditEvents.ToList();

            foreach (var auditEvent in events)
            {
                if (auditEvent.EventId == Guid.Empty)
                {
                    auditEvent.EventId = Guid.NewGuid();
                }

                EnrichEvent(auditEvent);
            }

            await _auditRepository.CreateBatchAsync(events, cancellationToken);

            _logger.LogDebug("Logged {Count} audit events in batch", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit events batch");
            // Don't throw - audit logging should not break main operations
        }
    }

    /// <summary>
    /// Enriches an audit event with context information.
    /// </summary>
    private void EnrichEvent(AuditEvent auditEvent)
    {
        auditEvent.CorrelationId = CorrelationId;

        if (CurrentUser != null)
        {
            auditEvent.PerformedByName = CurrentUser.UserName;
            auditEvent.ClientIpAddress = CurrentUser.ClientIpAddress;
            auditEvent.UserAgent = CurrentUser.UserAgent;
        }
    }

    /// <summary>
    /// Gets the delete event type for an entity type.
    /// </summary>
    private static AuditEventType GetDeleteEventType(AuditEntityType entityType)
    {
        // Currently no specific delete events defined, using generic modified
        // This could be extended in the future
        return entityType switch
        {
            AuditEntityType.Licence => AuditEventType.LicenceModified,
            AuditEntityType.Customer => AuditEventType.CustomerModified,
            AuditEntityType.GdpSite => AuditEventType.GdpSiteModified,
            _ => AuditEventType.EntityModified
        };
    }
}
