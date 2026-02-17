using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// Data transfer object for D365 F&O PharmaComplianceAuditEventEntity virtual data entity.
/// T155: DTO for AuditEvent mapping to data-model.md entity 15.
/// Stored in D365 F&O via virtual data entity for audit trail per FR-027.
/// </summary>
public class AuditEventDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event type as integer per AuditEventType enum.
    /// </summary>
    public int EventType { get; set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// User ID who performed the action.
    /// </summary>
    public Guid PerformedBy { get; set; }

    /// <summary>
    /// Name of the user who performed the action (denormalized for display).
    /// </summary>
    public string? PerformedByName { get; set; }

    /// <summary>
    /// Entity type as integer per AuditEntityType enum.
    /// </summary>
    public int EntityType { get; set; }

    /// <summary>
    /// Reference to the affected entity.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Event details (JSON serialized).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// URL to supporting evidence (document, screenshot, etc.).
    /// </summary>
    public string? SupportingEvidenceUrl { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>AuditEvent domain model.</returns>
    public AuditEvent ToDomainModel()
    {
        return new AuditEvent
        {
            EventId = EventId,
            EventType = (AuditEventType)EventType,
            EventDate = EventDate,
            PerformedBy = PerformedBy,
            PerformedByName = PerformedByName,
            EntityType = (AuditEntityType)EntityType,
            EntityId = EntityId,
            Details = Details,
            SupportingEvidenceUrl = SupportingEvidenceUrl,
            ClientIpAddress = ClientIpAddress,
            UserAgent = UserAgent,
            CorrelationId = CorrelationId
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>AuditEventDto for D365 F&O persistence.</returns>
    public static AuditEventDto FromDomainModel(AuditEvent model)
    {
        return new AuditEventDto
        {
            EventId = model.EventId,
            EventType = (int)model.EventType,
            EventDate = model.EventDate,
            PerformedBy = model.PerformedBy,
            PerformedByName = model.PerformedByName,
            EntityType = (int)model.EntityType,
            EntityId = model.EntityId,
            Details = model.Details,
            SupportingEvidenceUrl = model.SupportingEvidenceUrl,
            ClientIpAddress = model.ClientIpAddress,
            UserAgent = model.UserAgent,
            CorrelationId = model.CorrelationId
        };
    }
}

/// <summary>
/// OData response wrapper for AuditEvent queries.
/// </summary>
public class AuditEventODataResponse
{
    /// <summary>
    /// Collection of audit event DTOs.
    /// </summary>
    public List<AuditEventDto> value { get; set; } = new();

    /// <summary>
    /// Total count of matching records (when $count=true).
    /// </summary>
    public int? odataCount { get; set; }
}
