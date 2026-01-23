using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// Data transfer object for D365 F&O PharmaComplianceAlertEntity virtual data entity.
/// T109: DTO for Alert mapping to data-model.md entity 11.
/// Stored in D365 F&O via virtual data entity.
/// </summary>
public class AlertDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid AlertId { get; set; }

    /// <summary>
    /// Alert type as integer per AlertType enum.
    /// </summary>
    public int AlertType { get; set; }

    /// <summary>
    /// Severity level as integer per AlertSeverity enum.
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Target entity type as integer per TargetEntityType enum.
    /// </summary>
    public int TargetEntityType { get; set; }

    /// <summary>
    /// Reference to the target entity.
    /// </summary>
    public Guid TargetEntityId { get; set; }

    /// <summary>
    /// When the alert was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// When the alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedDate { get; set; }

    /// <summary>
    /// Who acknowledged the alert.
    /// </summary>
    public Guid? AcknowledgedBy { get; set; }

    /// <summary>
    /// Name of acknowledger (for display).
    /// </summary>
    public string? AcknowledgerName { get; set; }

    /// <summary>
    /// Alert message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional details (JSON serialized).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Related entity reference.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Due date for action.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>Alert domain model.</returns>
    public Alert ToDomainModel()
    {
        return new Alert
        {
            AlertId = AlertId,
            AlertType = (RE2.ComplianceCore.Models.AlertType)AlertType,
            Severity = (AlertSeverity)Severity,
            TargetEntityType = (RE2.ComplianceCore.Models.TargetEntityType)TargetEntityType,
            TargetEntityId = TargetEntityId,
            GeneratedDate = GeneratedDate,
            AcknowledgedDate = AcknowledgedDate,
            AcknowledgedBy = AcknowledgedBy,
            AcknowledgerName = AcknowledgerName,
            Message = Message ?? string.Empty,
            Details = Details,
            RelatedEntityId = RelatedEntityId,
            DueDate = DueDate.HasValue ? DateOnly.FromDateTime(DueDate.Value) : null
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>AlertDto for D365 F&O persistence.</returns>
    public static AlertDto FromDomainModel(Alert model)
    {
        return new AlertDto
        {
            AlertId = model.AlertId,
            AlertType = (int)model.AlertType,
            Severity = (int)model.Severity,
            TargetEntityType = (int)model.TargetEntityType,
            TargetEntityId = model.TargetEntityId,
            GeneratedDate = model.GeneratedDate,
            AcknowledgedDate = model.AcknowledgedDate,
            AcknowledgedBy = model.AcknowledgedBy,
            AcknowledgerName = model.AcknowledgerName,
            Message = model.Message,
            Details = model.Details,
            RelatedEntityId = model.RelatedEntityId,
            DueDate = model.DueDate?.ToDateTime(TimeOnly.MinValue)
        };
    }
}

/// <summary>
/// OData response wrapper for Alert queries.
/// </summary>
public class AlertODataResponse
{
    /// <summary>
    /// Collection of alert DTOs.
    /// </summary>
    public List<AlertDto> value { get; set; } = new();
}
