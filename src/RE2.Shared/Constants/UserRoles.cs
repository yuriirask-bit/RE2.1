namespace RE2.Shared.Constants;

/// <summary>
/// User role constants for authorization and access control.
/// Per data-model.md entity 28: User roles determine access to compliance functions.
/// Roles are stored in Azure AD App Roles and mapped to JWT token claims.
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Compliance Manager role.
    /// Can manage licences, approve overrides, configure compliance rules.
    /// Highest privilege level for compliance operations.
    /// </summary>
    public const string COMPLIANCE_MANAGER = "ComplianceManager";

    /// <summary>
    /// Quality Assurance User role.
    /// Can manage GDP compliance, inspections, CAPAs, and training records.
    /// Focus on quality and GDP aspects.
    /// </summary>
    public const string QA_USER = "QAUser";

    /// <summary>
    /// Sales Administration role.
    /// Can manage customer records, record customer licences, and qualify customers.
    /// Cannot approve compliance overrides.
    /// </summary>
    public const string SALES_ADMIN = "SalesAdmin";

    /// <summary>
    /// Training Coordinator role.
    /// Can manage GDP training records and training completion tracking.
    /// Read-only access to other compliance data.
    /// </summary>
    public const string TRAINING_COORDINATOR = "TrainingCoordinator";

    /// <summary>
    /// Warehouse Manager role.
    /// Can view compliance status for warehouse operations.
    /// Read-only access to transaction validation results.
    /// </summary>
    public const string WAREHOUSE_MANAGER = "WarehouseManager";

    /// <summary>
    /// Order Entry User role.
    /// Can view customer compliance status during order entry.
    /// Read-only access to compliance validation.
    /// </summary>
    public const string ORDER_ENTRY = "OrderEntry";

    /// <summary>
    /// Auditor/Read-Only role.
    /// Can view all compliance data but cannot modify anything.
    /// Used for internal audits and regulatory inspections.
    /// </summary>
    public const string AUDITOR = "Auditor";

    /// <summary>
    /// Gets all role names as an array (for display and configuration).
    /// </summary>
    public static string[] AllRoles => new[]
    {
        COMPLIANCE_MANAGER,
        QA_USER,
        SALES_ADMIN,
        TRAINING_COORDINATOR,
        WAREHOUSE_MANAGER,
        ORDER_ENTRY,
        AUDITOR
    };

    /// <summary>
    /// Gets roles that can manage licences and compliance data.
    /// </summary>
    public static string[] ManagementRoles => new[]
    {
        COMPLIANCE_MANAGER,
        QA_USER,
        SALES_ADMIN
    };

    /// <summary>
    /// Gets roles that have read-only access.
    /// </summary>
    public static string[] ReadOnlyRoles => new[]
    {
        WAREHOUSE_MANAGER,
        ORDER_ENTRY,
        AUDITOR
    };
}

/// <summary>
/// Authentication method enum per data-model.md entity 28.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Azure AD single sign-on (internal employees).
    /// </summary>
    AzureAD = 1,

    /// <summary>
    /// Local credentials stored in system (external users).
    /// </summary>
    LocalCredentials = 2
}
