namespace RE2.Shared.Constants;

/// <summary>
/// Standardized error codes for compliance violations and system errors.
/// Per FR-064: Consistent error codes across API responses for external system integration.
/// Maps to ComplianceViolation.violationType enum in transaction-validation-api.yaml
/// </summary>
public static class ErrorCodes
{
    #region Compliance Violation Codes (per transaction-validation-api.yaml)

    /// <summary>
    /// Customer or company licence has expired and is no longer valid for transactions.
    /// </summary>
    public const string LICENCE_EXPIRED = "LICENCE_EXPIRED";

    /// <summary>
    /// Required licence is missing for the customer or company for the controlled substance.
    /// </summary>
    public const string LICENCE_MISSING = "LICENCE_MISSING";

    /// <summary>
    /// Licence is suspended by the issuing authority and cannot be used.
    /// </summary>
    public const string LICENCE_SUSPENDED = "LICENCE_SUSPENDED";

    /// <summary>
    /// Controlled substance is not authorized under any active licence held by the customer or company.
    /// </summary>
    public const string SUBSTANCE_NOT_AUTHORIZED = "SUBSTANCE_NOT_AUTHORIZED";

    /// <summary>
    /// Transaction exceeds defined threshold (quantity or frequency) for the customer-substance combination.
    /// </summary>
    public const string THRESHOLD_EXCEEDED = "THRESHOLD_EXCEEDED";

    /// <summary>
    /// Cross-border transaction is missing required import/export permit.
    /// </summary>
    public const string MISSING_PERMIT = "MISSING_PERMIT";

    /// <summary>
    /// Customer account is suspended and cannot place orders.
    /// </summary>
    public const string CUSTOMER_SUSPENDED = "CUSTOMER_SUSPENDED";

    /// <summary>
    /// Customer has not been approved for purchasing controlled substances.
    /// </summary>
    public const string CUSTOMER_NOT_APPROVED = "CUSTOMER_NOT_APPROVED";

    #endregion

    #region Additional Licence Violations

    /// <summary>
    /// Licence with the specified ID or number was not found.
    /// </summary>
    public const string LICENCE_NOT_FOUND = "LICENCE_NOT_FOUND";

    /// <summary>
    /// Licence type with the specified ID or name was not found.
    /// </summary>
    public const string LICENCE_TYPE_NOT_FOUND = "LICENCE_TYPE_NOT_FOUND";

    /// <summary>
    /// Licence is revoked by the issuing authority.
    /// </summary>
    public const string LICENCE_REVOKED = "LICENCE_REVOKED";

    /// <summary>
    /// Licence scope is insufficient for the requested activity.
    /// </summary>
    public const string LICENCE_SCOPE_INSUFFICIENT = "LICENCE_SCOPE_INSUFFICIENT";

    #endregion

    #region Substance Violation Codes

    /// <summary>
    /// Controlled substance with the specified ID or code was not found.
    /// </summary>
    public const string SUBSTANCE_NOT_FOUND = "SUBSTANCE_NOT_FOUND";

    /// <summary>
    /// Licence-substance mapping with the specified ID was not found.
    /// </summary>
    public const string MAPPING_NOT_FOUND = "MAPPING_NOT_FOUND";

    #endregion

    #region GDP Compliance Violation Codes

    /// <summary>
    /// Wholesale Distribution Authorization (WDA) has expired.
    /// </summary>
    public const string WDA_EXPIRED = "WDA_EXPIRED";

    /// <summary>
    /// GDP certificate has expired.
    /// </summary>
    public const string GDP_CERTIFICATE_EXPIRED = "GDP_CERTIFICATE_EXPIRED";

    /// <summary>
    /// Site is not covered by an active WDA or GDP certificate.
    /// </summary>
    public const string SITE_NOT_COVERED = "SITE_NOT_COVERED";

    /// <summary>
    /// Service provider (3PL, transporter) is not GDP-qualified.
    /// </summary>
    public const string PROVIDER_NOT_QUALIFIED = "PROVIDER_NOT_QUALIFIED";

    /// <summary>
    /// CAPA (Corrective and Preventive Action) is overdue.
    /// </summary>
    public const string CAPA_OVERDUE = "CAPA_OVERDUE";

    #endregion

    #region System Error Codes

    /// <summary>
    /// External system (Dataverse, D365 F&O) is temporarily unavailable.
    /// </summary>
    public const string EXTERNAL_SYSTEM_UNAVAILABLE = "EXTERNAL_SYSTEM_UNAVAILABLE";

    /// <summary>
    /// Request validation failed due to invalid input data.
    /// </summary>
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    /// <summary>
    /// Requested resource was not found.
    /// </summary>
    public const string NOT_FOUND = "NOT_FOUND";

    /// <summary>
    /// User does not have permission to perform the requested operation.
    /// </summary>
    public const string UNAUTHORIZED = "UNAUTHORIZED";

    /// <summary>
    /// User is authenticated but does not have sufficient privileges.
    /// </summary>
    public const string FORBIDDEN = "FORBIDDEN";

    /// <summary>
    /// Request rate limit has been exceeded.
    /// </summary>
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    /// <summary>
    /// Optimistic concurrency conflict detected - data was modified by another user.
    /// </summary>
    public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";

    /// <summary>
    /// Internal server error occurred.
    /// </summary>
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";

    #endregion
}
