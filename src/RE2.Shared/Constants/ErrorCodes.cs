namespace RE2.Shared.Constants;

/// <summary>
/// Standardized error codes for API responses (FR-064)
/// T048: Error codes constants
/// </summary>
public static class ErrorCodes
{
    // Licence-related errors
    public const string LICENCE_EXPIRED = "LICENCE_EXPIRED";
    public const string LICENCE_MISSING = "LICENCE_MISSING";
    public const string LICENCE_INVALID = "LICENCE_INVALID";
    public const string LICENCE_SUSPENDED = "LICENCE_SUSPENDED";
    public const string LICENCE_SCOPE_INSUFFICIENT = "LICENCE_SCOPE_INSUFFICIENT";

    // Customer-related errors
    public const string CUSTOMER_NOT_APPROVED = "CUSTOMER_NOT_APPROVED";
    public const string CUSTOMER_SUSPENDED = "CUSTOMER_SUSPENDED";
    public const string CUSTOMER_NOT_QUALIFIED = "CUSTOMER_NOT_QUALIFIED";

    // Transaction-related errors
    public const string TRANSACTION_BLOCKED = "TRANSACTION_BLOCKED";
    public const string THRESHOLD_EXCEEDED = "THRESHOLD_EXCEEDED";
    public const string PERMIT_MISSING = "PERMIT_MISSING";
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    // GDP-related errors
    public const string GDP_SITE_NOT_AUTHORIZED = "GDP_SITE_NOT_AUTHORIZED";
    public const string GDP_CERTIFICATE_EXPIRED = "GDP_CERTIFICATE_EXPIRED";
    public const string GDP_PROVIDER_NOT_QUALIFIED = "GDP_PROVIDER_NOT_QUALIFIED";

    // System errors
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string EXTERNAL_SERVICE_ERROR = "EXTERNAL_SERVICE_ERROR";
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string NOT_FOUND = "NOT_FOUND";
}
