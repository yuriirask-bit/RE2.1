using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents an external system authorized to call compliance APIs.
/// T047a: IntegrationSystem domain model per data-model.md entity 27.
/// Used for tracking which external systems (ERP, WMS, Order Management) can access the validation APIs
/// and recording their identity in transaction audit trails per FR-061.
/// </summary>
public class IntegrationSystem
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid IntegrationSystemId { get; set; }

    /// <summary>
    /// System name (e.g., "SAP ERP", "WMS").
    /// Required, unique, indexed.
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Type of external system.
    /// Required.
    /// </summary>
    public IntegrationSystemType SystemType { get; set; }

    /// <summary>
    /// Hashed API key for authentication (if using API key auth).
    /// Nullable.
    /// </summary>
    public string? ApiKeyHash { get; set; }

    /// <summary>
    /// OAuth client ID (if using OAuth).
    /// Nullable.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// Comma-separated list of authorized API endpoints.
    /// Nullable.
    /// </summary>
    public string? AuthorizedEndpoints { get; set; }

    /// <summary>
    /// Comma-separated IP addresses allowed (optional).
    /// Nullable.
    /// </summary>
    public string? IpWhitelist { get; set; }

    /// <summary>
    /// Whether system can call APIs.
    /// Required, default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Technical contact for this integration.
    /// Nullable.
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Validates the integration system according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(SystemName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SystemName is required"
            });
        }

        // At least one authentication method should be specified for active systems
        if (IsActive && string.IsNullOrWhiteSpace(ApiKeyHash) && string.IsNullOrWhiteSpace(OAuthClientId))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Active integration systems must have either ApiKeyHash or OAuthClientId specified"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if a specific endpoint is authorized for this system.
    /// </summary>
    /// <param name="endpoint">The endpoint path to check (e.g., "/api/v1/transactions/validate").</param>
    /// <returns>True if the endpoint is authorized or if AuthorizedEndpoints is empty (all endpoints allowed).</returns>
    public bool IsEndpointAuthorized(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(AuthorizedEndpoints))
        {
            // No restrictions - all endpoints allowed
            return true;
        }

        var authorizedList = AuthorizedEndpoints
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return authorizedList.Any(e =>
            endpoint.Equals(e, StringComparison.OrdinalIgnoreCase) ||
            endpoint.StartsWith(e.TrimEnd('*'), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a specific IP address is allowed for this system.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if the IP is allowed or if IpWhitelist is empty (all IPs allowed).</returns>
    public bool IsIpAllowed(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(IpWhitelist))
        {
            // No restrictions - all IPs allowed
            return true;
        }

        var allowedIps = IpWhitelist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return allowedIps.Contains(ipAddress, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deactivates the integration system.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the integration system.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list of authorized endpoints as an array.
    /// </summary>
    /// <returns>Array of authorized endpoint paths, or empty array if none specified.</returns>
    public string[] GetAuthorizedEndpointsArray()
    {
        if (string.IsNullOrWhiteSpace(AuthorizedEndpoints))
        {
            return Array.Empty<string>();
        }

        return AuthorizedEndpoints
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Gets the list of whitelisted IPs as an array.
    /// </summary>
    /// <returns>Array of whitelisted IP addresses, or empty array if none specified.</returns>
    public string[] GetIpWhitelistArray()
    {
        if (string.IsNullOrWhiteSpace(IpWhitelist))
        {
            return Array.Empty<string>();
        }

        return IpWhitelist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

/// <summary>
/// Type of integration system.
/// Per data-model.md entity 27 SystemType enum.
/// </summary>
public enum IntegrationSystemType
{
    /// <summary>
    /// Enterprise Resource Planning system (e.g., SAP, D365 F&O).
    /// </summary>
    ERP = 1,

    /// <summary>
    /// Order Management System.
    /// </summary>
    OrderManagement = 2,

    /// <summary>
    /// Warehouse Management System.
    /// </summary>
    WMS = 3,

    /// <summary>
    /// Custom or third-party system.
    /// </summary>
    CustomSystem = 4
}
