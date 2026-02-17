namespace RE2.ComplianceCore.Configuration;

/// <summary>
/// Configuration settings for override approval workflow.
/// T149/T149a: Configurable role-based override approval per FR-019a.
/// </summary>
public class OverrideApprovalSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "OverrideApproval";

    /// <summary>
    /// Roles authorized to approve transaction overrides.
    /// Default: ["ComplianceManager"]
    /// </summary>
    public List<string> AuthorizedRoles { get; set; } = new() { "ComplianceManager" };

    /// <summary>
    /// Whether override requires justification.
    /// Default: true
    /// </summary>
    public bool RequireJustification { get; set; } = true;

    /// <summary>
    /// Minimum justification length for override approval.
    /// Default: 20 characters
    /// </summary>
    public int MinJustificationLength { get; set; } = 20;

    /// <summary>
    /// Maximum age in hours for transactions to be eligible for override.
    /// Default: 168 (7 days)
    /// </summary>
    public int MaxOverrideAgeHours { get; set; } = 168;

    /// <summary>
    /// Whether to send notification on override approval.
    /// Default: true
    /// </summary>
    public bool NotifyOnApproval { get; set; } = true;

    /// <summary>
    /// Whether to send notification on override rejection.
    /// Default: true
    /// </summary>
    public bool NotifyOnRejection { get; set; } = true;

    /// <summary>
    /// Email addresses to notify on override decisions.
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();

    /// <summary>
    /// Whether to require dual approval for critical violations.
    /// Default: false
    /// </summary>
    public bool RequireDualApprovalForCritical { get; set; } = false;

    /// <summary>
    /// Roles that can provide second approval for dual approval workflow.
    /// </summary>
    public List<string> DualApprovalRoles { get; set; } = new() { "ComplianceDirector" };

    /// <summary>
    /// Violation types that require dual approval (if enabled).
    /// </summary>
    public List<string> CriticalViolationTypes { get; set; } = new()
    {
        "LICENCE_EXPIRED",
        "LICENCE_MISSING",
        "CUSTOMER_SUSPENDED",
        "MISSING_PERMIT"
    };

    /// <summary>
    /// Automatically reject overrides after this many hours of pending status.
    /// Default: 0 (disabled)
    /// </summary>
    public int AutoRejectAfterHours { get; set; } = 0;

    /// <summary>
    /// Message to include in auto-rejection.
    /// </summary>
    public string AutoRejectMessage { get; set; } = "Override request expired without approval.";
}
