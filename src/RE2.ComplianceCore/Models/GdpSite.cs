using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Composite domain model combining D365FO warehouse data with GDP-specific extensions.
/// T185: GDP site domain model per User Story 7 (FR-033, FR-034, FR-035).
/// D365FO warehouse data is read-only; GDP extensions are stored in Dataverse phr_gdpwarehouseextension.
/// </summary>
public class GdpSite
{
    #region D365FO Warehouse Data (read-only)

    /// <summary>
    /// D365FO warehouse identifier.
    /// </summary>
    public string WarehouseId { get; set; } = string.Empty;

    /// <summary>
    /// D365FO warehouse name.
    /// </summary>
    public string WarehouseName { get; set; } = string.Empty;

    /// <summary>
    /// Parent operational site ID in D365FO.
    /// </summary>
    public string OperationalSiteId { get; set; } = string.Empty;

    /// <summary>
    /// Parent operational site name.
    /// </summary>
    public string OperationalSiteName { get; set; } = string.Empty;

    /// <summary>
    /// Legal entity (data area) in D365FO.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// D365FO warehouse type (Standard, Quarantine, Transit).
    /// </summary>
    public string WarehouseType { get; set; } = string.Empty;

    /// <summary>
    /// Street address from D365FO.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Street number from D365FO.
    /// </summary>
    public string StreetNumber { get; set; } = string.Empty;

    /// <summary>
    /// City from D365FO.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Postal/zip code from D365FO.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Country/region ID from D365FO (ISO code).
    /// </summary>
    public string CountryRegionId { get; set; } = string.Empty;

    /// <summary>
    /// State/province ID from D365FO.
    /// </summary>
    public string StateId { get; set; } = string.Empty;

    /// <summary>
    /// Pre-formatted address from D365FO.
    /// </summary>
    public string FormattedAddress { get; set; } = string.Empty;

    /// <summary>
    /// Geographic latitude from D365FO.
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Geographic longitude from D365FO.
    /// </summary>
    public decimal? Longitude { get; set; }

    #endregion

    #region GDP Extensions (Dataverse phr_gdpwarehouseextension)

    /// <summary>
    /// Unique identifier for the GDP extension record.
    /// </summary>
    public Guid GdpExtensionId { get; set; }

    /// <summary>
    /// GDP site classification.
    /// </summary>
    public GdpSiteType GdpSiteType { get; set; }

    /// <summary>
    /// Permitted GDP activities (flags).
    /// </summary>
    public GdpSiteActivity PermittedActivities { get; set; }

    /// <summary>
    /// Whether GDP configuration is active.
    /// Default: true.
    /// </summary>
    public bool IsGdpActive { get; set; } = true;

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    #endregion

    #region Business Logic

    /// <summary>
    /// Whether this warehouse has been configured for GDP compliance.
    /// A warehouse is configured when it has a non-empty GdpExtensionId.
    /// </summary>
    public bool IsConfiguredForGdp => GdpExtensionId != Guid.Empty;

    /// <summary>
    /// Checks if this site has a specific GDP activity configured.
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if the activity is permitted.</returns>
    public bool HasActivity(GdpSiteActivity activity)
    {
        return PermittedActivities.HasFlag(activity);
    }

    /// <summary>
    /// Validates the GDP site configuration according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(WarehouseId))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "WarehouseId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(DataAreaId))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "DataAreaId is required"
            });
        }

        if (PermittedActivities == GdpSiteActivity.None)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "At least one permitted activity must be selected"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    #endregion
}

/// <summary>
/// GDP site classification type.
/// Per User Story 7: Different GDP facility classifications.
/// </summary>
public enum GdpSiteType
{
    /// <summary>
    /// Standard warehouse facility.
    /// </summary>
    Warehouse = 0,

    /// <summary>
    /// Cross-docking facility for transshipment.
    /// </summary>
    CrossDock = 1,

    /// <summary>
    /// Transport hub for distribution logistics.
    /// </summary>
    TransportHub = 2
}

/// <summary>
/// GDP permitted activities (flags enum).
/// Per FR-035: Activities that can be performed at a GDP site.
/// </summary>
[Flags]
public enum GdpSiteActivity
{
    /// <summary>
    /// No activities configured.
    /// </summary>
    None = 0,

    /// <summary>
    /// Storage of pharmaceutical products for more than 72 hours.
    /// </summary>
    StorageOver72h = 1,

    /// <summary>
    /// Temperature-controlled storage capability.
    /// </summary>
    TemperatureControlled = 2,

    /// <summary>
    /// Activities outsourced to third-party provider.
    /// </summary>
    Outsourced = 4,

    /// <summary>
    /// Transport-only operations (no storage).
    /// </summary>
    TransportOnly = 8
}
