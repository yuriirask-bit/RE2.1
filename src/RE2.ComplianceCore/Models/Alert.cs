using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a compliance warning or reminder.
/// T107: Alert domain model per data-model.md entity 11.
/// Used for licence expiry warnings, missing documentation, threshold alerts, etc.
/// </summary>
public class Alert
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid AlertId { get; set; }

    /// <summary>
    /// Type of alert.
    /// Required.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Urgency level.
    /// Required.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// What entity this alert is about.
    /// Required.
    /// </summary>
    public TargetEntityType TargetEntityType { get; set; }

    /// <summary>
    /// Reference to the entity.
    /// Required.
    /// </summary>
    public Guid TargetEntityId { get; set; }

    /// <summary>
    /// When alert was created.
    /// Required.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// When alert was acknowledged.
    /// Nullable.
    /// </summary>
    public DateTime? AcknowledgedDate { get; set; }

    /// <summary>
    /// Who acknowledged the alert.
    /// Nullable.
    /// </summary>
    public Guid? AcknowledgedBy { get; set; }

    /// <summary>
    /// Name of the person who acknowledged.
    /// For display purposes.
    /// </summary>
    public string? AcknowledgerName { get; set; }

    /// <summary>
    /// Alert message.
    /// Required.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional context or details (JSON serialized).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Reference to any related entity (e.g., the document that triggered the alert).
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Due date for action (if applicable).
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Validates the alert according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (TargetEntityId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "TargetEntityId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Message is required"
            });
        }

        // AcknowledgedBy required if AcknowledgedDate is set
        if (AcknowledgedDate.HasValue && !AcknowledgedBy.HasValue)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "AcknowledgedBy is required when AcknowledgedDate is set"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if the alert has been acknowledged.
    /// </summary>
    public bool IsAcknowledged()
    {
        return AcknowledgedDate.HasValue;
    }

    /// <summary>
    /// Acknowledges the alert.
    /// </summary>
    /// <param name="userId">User ID acknowledging the alert.</param>
    /// <param name="userName">Name of the user (optional).</param>
    public void Acknowledge(Guid userId, string? userName = null)
    {
        AcknowledgedDate = DateTime.UtcNow;
        AcknowledgedBy = userId;
        AcknowledgerName = userName;
    }

    /// <summary>
    /// Checks if this is a critical alert.
    /// </summary>
    public bool IsCritical()
    {
        return Severity == AlertSeverity.Critical;
    }

    /// <summary>
    /// Checks if this alert is past its due date.
    /// </summary>
    public bool IsOverdue()
    {
        if (!DueDate.HasValue)
        {
            return false;
        }

        return DueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Gets the age of this alert in days.
    /// </summary>
    public int GetAgeDays()
    {
        return (int)(DateTime.UtcNow - GeneratedDate).TotalDays;
    }

    /// <summary>
    /// Creates a licence expiry alert.
    /// </summary>
    public static Alert CreateLicenceExpiryAlert(Guid licenceId, string licenceNumber, int daysUntilExpiry, DateOnly expiryDate)
    {
        var severity = daysUntilExpiry switch
        {
            <= 30 => AlertSeverity.Critical,
            <= 60 => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };

        return new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = daysUntilExpiry <= 0 ? AlertType.LicenceExpired : AlertType.LicenceExpiring,
            Severity = severity,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = licenceId,
            GeneratedDate = DateTime.UtcNow,
            Message = daysUntilExpiry <= 0
                ? $"Licence {licenceNumber} has expired on {expiryDate:yyyy-MM-dd}"
                : $"Licence {licenceNumber} expires in {daysUntilExpiry} days ({expiryDate:yyyy-MM-dd})",
            DueDate = expiryDate
        };
    }

    /// <summary>
    /// Creates a re-verification due alert.
    /// </summary>
    public static Alert CreateReVerificationAlert(Guid customerId, string customerName, DateOnly dueDate)
    {
        var daysUntilDue = dueDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        var severity = daysUntilDue switch
        {
            <= 30 => AlertSeverity.Critical,
            <= 60 => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };

        return new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.ReVerificationDue,
            Severity = severity,
            TargetEntityType = TargetEntityType.Customer,
            TargetEntityId = customerId,
            GeneratedDate = DateTime.UtcNow,
            Message = $"Customer {customerName} requires re-verification by {dueDate:yyyy-MM-dd}",
            DueDate = dueDate
        };
    }
}

/// <summary>
/// Type of alert.
/// Per data-model.md entity 11 AlertType enum.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Licence is expiring soon.
    /// </summary>
    LicenceExpiring = 1,

    /// <summary>
    /// Licence has expired.
    /// </summary>
    LicenceExpired = 2,

    /// <summary>
    /// Required documentation is missing.
    /// </summary>
    MissingDocumentation = 3,

    /// <summary>
    /// Transaction threshold has been exceeded.
    /// </summary>
    ThresholdExceeded = 4,

    /// <summary>
    /// Customer re-verification is due.
    /// </summary>
    ReVerificationDue = 5,

    /// <summary>
    /// GDP certificate is expiring.
    /// </summary>
    GdpCertificateExpiring = 6,

    /// <summary>
    /// GDP certificate has expired.
    /// </summary>
    GdpCertificateExpired = 7,

    /// <summary>
    /// Verification is overdue.
    /// </summary>
    VerificationOverdue = 8,

    /// <summary>
    /// Substance reclassification affecting customer.
    /// </summary>
    ReclassificationImpact = 9
}

/// <summary>
/// Severity level of an alert.
/// Per data-model.md entity 11 Severity enum.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning - action recommended.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Critical - immediate action required.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Type of entity that an alert targets.
/// Per data-model.md entity 11 TargetEntityType enum.
/// </summary>
public enum TargetEntityType
{
    /// <summary>
    /// Alert is about a customer.
    /// </summary>
    Customer = 1,

    /// <summary>
    /// Alert is about a licence.
    /// </summary>
    Licence = 2,

    /// <summary>
    /// Alert is about a threshold.
    /// </summary>
    Threshold = 3,

    /// <summary>
    /// Alert is about a GDP site.
    /// </summary>
    GdpSite = 4,

    /// <summary>
    /// Alert is about a GDP credential.
    /// </summary>
    GdpCredential = 5,

    /// <summary>
    /// Alert is about a transaction.
    /// </summary>
    Transaction = 6
}
