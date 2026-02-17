using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.Auditing;

/// <summary>
/// Service for logging audit events for data modifications.
/// T156: Interface for audit logging interceptor per FR-027.
/// All data modification operations should log through this service.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Gets or sets the current correlation ID for tracing related events.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the current user context.
    /// </summary>
    UserContext? CurrentUser { get; set; }

    /// <summary>
    /// Logs a create operation.
    /// T156: Log entity creation per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity created.</param>
    /// <param name="entityId">ID of the created entity.</param>
    /// <param name="performedBy">User who performed the action.</param>
    /// <param name="details">Optional details about the creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCreateAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        object? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a modify operation with before/after values.
    /// T156: Log entity modification with change tracking per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity modified.</param>
    /// <param name="entityId">ID of the modified entity.</param>
    /// <param name="performedBy">User who performed the action.</param>
    /// <param name="changes">Details of changes made.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogModifyAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        EntityModificationDetails? changes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a delete operation.
    /// T156: Log entity deletion per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity deleted.</param>
    /// <param name="entityId">ID of the deleted entity.</param>
    /// <param name="performedBy">User who performed the action.</param>
    /// <param name="reason">Optional reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogDeleteAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid performedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a status change operation.
    /// T156: Log entity status changes per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="eventType">Specific event type for the status change.</param>
    /// <param name="performedBy">User who performed the action.</param>
    /// <param name="reason">Optional reason for status change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogStatusChangeAsync(
        AuditEntityType entityType,
        Guid entityId,
        AuditEventType eventType,
        Guid performedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an override approval.
    /// T156: Log override approvals per FR-027.
    /// </summary>
    /// <param name="transactionId">Transaction being overridden.</param>
    /// <param name="approvedBy">User who approved.</param>
    /// <param name="justification">Justification for the override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogOverrideApprovalAsync(
        Guid transactionId,
        Guid approvedBy,
        string justification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a conflict resolution.
    /// T156: Log conflict resolutions per FR-027c.
    /// </summary>
    /// <param name="entityType">Type of entity with conflict.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="resolvedBy">User who resolved.</param>
    /// <param name="details">Conflict resolution details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogConflictResolutionAsync(
        AuditEntityType entityType,
        Guid entityId,
        Guid resolvedBy,
        ConflictResolutionDetails details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a custom audit event.
    /// T156: Support for custom event types.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs multiple audit events in a batch.
    /// T156: Efficient batch logging.
    /// </summary>
    /// <param name="auditEvents">The audit events to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for the current user performing operations.
/// </summary>
public class UserContext
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User display name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }
}
