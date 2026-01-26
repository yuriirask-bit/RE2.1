using System.Text.Json;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records compliance-related actions, verifications, or changes for audit trail.
/// T153: AuditEvent domain model per data-model.md entity 15.
/// Supports FR-027 (audit trail for all data changes) and FR-027c (conflict audit logging).
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of audit event.
    /// Required.
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// When the event occurred.
    /// Required.
    /// </summary>
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who performed the action.
    /// Required.
    /// </summary>
    public Guid PerformedBy { get; set; }

    /// <summary>
    /// Name of the user who performed the action (denormalized for display).
    /// </summary>
    public string? PerformedByName { get; set; }

    /// <summary>
    /// Type of entity affected by this event.
    /// Required.
    /// </summary>
    public AuditEntityType EntityType { get; set; }

    /// <summary>
    /// ID of the affected entity.
    /// Required.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Event details as JSON-serialized object.
    /// Contains before/after values, reason, etc.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Reference to supporting evidence (document URL, screenshot, etc.).
    /// </summary>
    public string? SupportingEvidenceUrl { get; set; }

    /// <summary>
    /// IP address of the client that performed the action.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// User agent string of the client.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Validates the audit event.
    /// </summary>
    /// <returns>Validation result.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (PerformedBy == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "PerformedBy is required"
            });
        }

        if (EntityId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EntityId is required"
            });
        }

        if (EventDate == default)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EventDate is required"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Sets the details from a typed object (serializes to JSON).
    /// </summary>
    /// <typeparam name="T">Type of the details object.</typeparam>
    /// <param name="details">The details object.</param>
    public void SetDetails<T>(T details) where T : class
    {
        Details = JsonSerializer.Serialize(details, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Gets the details as a typed object (deserializes from JSON).
    /// </summary>
    /// <typeparam name="T">Type of the details object.</typeparam>
    /// <returns>The deserialized details, or null if details is null/empty.</returns>
    public T? GetDetails<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(Details))
            return null;

        return JsonSerializer.Deserialize<T>(Details, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Creates an audit event for entity creation.
    /// </summary>
    public static AuditEvent ForCreate(AuditEntityType entityType, Guid entityId, Guid performedBy, object? details = null)
    {
        var eventType = entityType switch
        {
            AuditEntityType.Licence => AuditEventType.LicenceCreated,
            AuditEntityType.Customer => AuditEventType.CustomerCreated,
            AuditEntityType.Transaction => AuditEventType.TransactionValidated,
            AuditEntityType.GdpSite => AuditEventType.GdpSiteCreated,
            AuditEntityType.Inspection => AuditEventType.InspectionRecorded,
            _ => AuditEventType.EntityCreated
        };

        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            EntityType = entityType,
            EntityId = entityId
        };

        if (details != null)
        {
            auditEvent.SetDetails(details);
        }

        return auditEvent;
    }

    /// <summary>
    /// Creates an audit event for entity modification.
    /// </summary>
    public static AuditEvent ForModify(AuditEntityType entityType, Guid entityId, Guid performedBy, object? changes = null)
    {
        var eventType = entityType switch
        {
            AuditEntityType.Licence => AuditEventType.LicenceModified,
            AuditEntityType.Customer => AuditEventType.CustomerModified,
            AuditEntityType.GdpSite => AuditEventType.GdpSiteModified,
            _ => AuditEventType.EntityModified
        };

        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            EntityType = entityType,
            EntityId = entityId
        };

        if (changes != null)
        {
            auditEvent.SetDetails(changes);
        }

        return auditEvent;
    }

    /// <summary>
    /// Creates an audit event for override approval.
    /// </summary>
    public static AuditEvent ForOverrideApproval(Guid transactionId, Guid approvedBy, string justification)
    {
        return new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.OverrideApproved,
            EventDate = DateTime.UtcNow,
            PerformedBy = approvedBy,
            EntityType = AuditEntityType.Transaction,
            EntityId = transactionId,
            Details = JsonSerializer.Serialize(new { justification })
        };
    }

    /// <summary>
    /// Creates an audit event for conflict resolution (FR-027c).
    /// </summary>
    public static AuditEvent ForConflictResolution(AuditEntityType entityType, Guid entityId, Guid resolvedBy, ConflictResolutionDetails details)
    {
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.ConflictResolved,
            EventDate = DateTime.UtcNow,
            PerformedBy = resolvedBy,
            EntityType = entityType,
            EntityId = entityId
        };

        auditEvent.SetDetails(details);
        return auditEvent;
    }

    /// <summary>
    /// Creates an audit event for customer status change.
    /// </summary>
    public static AuditEvent ForCustomerStatusChange(Guid customerId, Guid performedBy, AuditEventType statusEventType, string? reason = null)
    {
        return new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = statusEventType,
            EventDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            EntityType = AuditEntityType.Customer,
            EntityId = customerId,
            Details = reason != null ? JsonSerializer.Serialize(new { reason }) : null
        };
    }
}

/// <summary>
/// Types of audit events.
/// Per data-model.md entity 15 EventType enum.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// Generic entity created event.
    /// </summary>
    EntityCreated = 0,

    /// <summary>
    /// Generic entity modified event.
    /// </summary>
    EntityModified = 1,

    /// <summary>
    /// Licence was created.
    /// </summary>
    LicenceCreated = 10,

    /// <summary>
    /// Licence was modified.
    /// </summary>
    LicenceModified = 11,

    /// <summary>
    /// Licence was suspended.
    /// </summary>
    LicenceSuspended = 12,

    /// <summary>
    /// Licence was revoked.
    /// </summary>
    LicenceRevoked = 13,

    /// <summary>
    /// Licence verification recorded.
    /// </summary>
    LicenceVerified = 14,

    /// <summary>
    /// Customer was created.
    /// </summary>
    CustomerCreated = 20,

    /// <summary>
    /// Customer was modified.
    /// </summary>
    CustomerModified = 21,

    /// <summary>
    /// Customer was approved.
    /// </summary>
    CustomerApproved = 22,

    /// <summary>
    /// Customer was suspended.
    /// </summary>
    CustomerSuspended = 23,

    /// <summary>
    /// Customer suspension was lifted.
    /// </summary>
    CustomerReinstated = 24,

    /// <summary>
    /// Transaction was validated.
    /// </summary>
    TransactionValidated = 30,

    /// <summary>
    /// Override was approved for a transaction.
    /// </summary>
    OverrideApproved = 31,

    /// <summary>
    /// Override was rejected for a transaction.
    /// </summary>
    OverrideRejected = 32,

    /// <summary>
    /// GDP site was created.
    /// </summary>
    GdpSiteCreated = 40,

    /// <summary>
    /// GDP site was modified.
    /// </summary>
    GdpSiteModified = 41,

    /// <summary>
    /// Inspection was recorded.
    /// </summary>
    InspectionRecorded = 50,

    /// <summary>
    /// CAPA was created.
    /// </summary>
    CapaCreated = 51,

    /// <summary>
    /// CAPA status changed.
    /// </summary>
    CapaStatusChanged = 52,

    /// <summary>
    /// Conflict was resolved (FR-027c).
    /// </summary>
    ConflictResolved = 60,

    /// <summary>
    /// Report was generated.
    /// </summary>
    ReportGenerated = 70,

    /// <summary>
    /// User logged in.
    /// </summary>
    UserLogin = 80,

    /// <summary>
    /// User logged out.
    /// </summary>
    UserLogout = 81
}

/// <summary>
/// Types of entities that can be audited.
/// Per data-model.md entity 15 EntityType enum.
/// </summary>
public enum AuditEntityType
{
    /// <summary>
    /// Customer entity.
    /// </summary>
    Customer = 1,

    /// <summary>
    /// Licence entity.
    /// </summary>
    Licence = 2,

    /// <summary>
    /// Transaction entity.
    /// </summary>
    Transaction = 3,

    /// <summary>
    /// GDP site entity.
    /// </summary>
    GdpSite = 4,

    /// <summary>
    /// Inspection entity.
    /// </summary>
    Inspection = 5,

    /// <summary>
    /// Controlled substance entity.
    /// </summary>
    ControlledSubstance = 6,

    /// <summary>
    /// Threshold configuration entity.
    /// </summary>
    Threshold = 7,

    /// <summary>
    /// CAPA entity.
    /// </summary>
    Capa = 8,

    /// <summary>
    /// GDP credential entity.
    /// </summary>
    GdpCredential = 9,

    /// <summary>
    /// Report entity.
    /// </summary>
    Report = 10
}

/// <summary>
/// Details for conflict resolution audit events (FR-027c).
/// </summary>
public class ConflictResolutionDetails
{
    /// <summary>
    /// The local version that was overwritten.
    /// </summary>
    public string? LocalVersion { get; set; }

    /// <summary>
    /// The remote version that was detected.
    /// </summary>
    public string? RemoteVersion { get; set; }

    /// <summary>
    /// How the conflict was resolved.
    /// </summary>
    public string? ResolutionMethod { get; set; }

    /// <summary>
    /// Fields that were in conflict.
    /// </summary>
    public List<string> ConflictingFields { get; set; } = new();

    /// <summary>
    /// User's notes about the resolution.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Details for entity modification audit events.
/// </summary>
public class EntityModificationDetails
{
    /// <summary>
    /// Values before the modification.
    /// </summary>
    public Dictionary<string, object?> Before { get; set; } = new();

    /// <summary>
    /// Values after the modification.
    /// </summary>
    public Dictionary<string, object?> After { get; set; } = new();

    /// <summary>
    /// Fields that were changed.
    /// </summary>
    public List<string> ChangedFields { get; set; } = new();

    /// <summary>
    /// Reason for the modification.
    /// </summary>
    public string? Reason { get; set; }
}
