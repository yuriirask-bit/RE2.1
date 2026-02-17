using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Tracks qualification/validation status of GDP equipment and processes.
/// T255: GdpEquipmentQualification domain model per US11 (FR-048).
/// Examples: temperature-controlled vehicles, monitoring systems, storage equipment.
/// Stored in Dataverse phr_gdpequipmentqualification table.
/// </summary>
public class GdpEquipmentQualification
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid EquipmentQualificationId { get; set; }

    /// <summary>
    /// Name/identifier of the equipment or process.
    /// Required. E.g., "Temperature Vehicle TRK-001", "Cold Storage Monitor CSM-003".
    /// </summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>
    /// Type of equipment.
    /// Required.
    /// </summary>
    public GdpEquipmentType EquipmentType { get; set; }

    /// <summary>
    /// Optional FK to the provider that owns this equipment.
    /// </summary>
    public Guid? ProviderId { get; set; }

    /// <summary>
    /// Optional FK to the site where this equipment is located.
    /// </summary>
    public Guid? SiteId { get; set; }

    /// <summary>
    /// When the equipment was last qualified/validated.
    /// Required.
    /// </summary>
    public DateOnly QualificationDate { get; set; }

    /// <summary>
    /// When re-qualification is due. Null if no periodic re-qualification required.
    /// </summary>
    public DateOnly? RequalificationDueDate { get; set; }

    /// <summary>
    /// Current qualification status.
    /// </summary>
    public GdpQualificationStatusType QualificationStatus { get; set; } = GdpQualificationStatusType.Qualified;

    /// <summary>
    /// Who performed the qualification.
    /// Required.
    /// </summary>
    public string QualifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes about the qualification.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    #region Business Logic

    /// <summary>
    /// Validates the equipment qualification record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(EquipmentName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EquipmentName is required"
            });
        }

        if (string.IsNullOrWhiteSpace(QualifiedBy))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "QualifiedBy is required"
            });
        }

        if (QualificationDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "QualificationDate cannot be in the future"
            });
        }

        if (RequalificationDueDate.HasValue && RequalificationDueDate.Value <= QualificationDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "RequalificationDueDate must be after QualificationDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if qualification has expired (re-qualification due date has passed).
    /// </summary>
    public bool IsExpired()
    {
        if (!RequalificationDueDate.HasValue)
        {
            return false;
        }

        return RequalificationDueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Checks if re-qualification is due within the next 30 days.
    /// </summary>
    public bool IsDueForRequalification(int daysAhead = 30)
    {
        if (!RequalificationDueDate.HasValue)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return RequalificationDueDate.Value >= today &&
               RequalificationDueDate.Value <= today.AddDays(daysAhead);
    }

    #endregion
}

/// <summary>
/// Types of GDP equipment/processes that require qualification.
/// Per US11 FR-048.
/// </summary>
public enum GdpEquipmentType
{
    /// <summary>
    /// Temperature-controlled vehicle for medicine transport.
    /// </summary>
    TemperatureControlledVehicle,

    /// <summary>
    /// Temperature/humidity monitoring system.
    /// </summary>
    MonitoringSystem,

    /// <summary>
    /// Storage equipment (cold rooms, fridges, etc.).
    /// </summary>
    StorageEquipment,

    /// <summary>
    /// Other equipment or process.
    /// </summary>
    Other
}

/// <summary>
/// Qualification status of GDP equipment.
/// Per US11 FR-048.
/// </summary>
public enum GdpQualificationStatusType
{
    /// <summary>
    /// Equipment is currently qualified.
    /// </summary>
    Qualified,

    /// <summary>
    /// Re-qualification is due soon.
    /// </summary>
    DueForRequalification,

    /// <summary>
    /// Qualification has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Equipment is not qualified.
    /// </summary>
    NotQualified
}
