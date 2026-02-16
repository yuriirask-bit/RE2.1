using System.Text.Json.Serialization;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// DTO matching D365FO Warehouses OData entity.
/// T187: Data transfer object for warehouse master data.
/// </summary>
public class WarehouseDto
{
    [JsonPropertyName("WarehouseId")]
    public string WarehouseId { get; set; } = string.Empty;

    [JsonPropertyName("WarehouseName")]
    public string WarehouseName { get; set; } = string.Empty;

    [JsonPropertyName("OperationalSiteId")]
    public string OperationalSiteId { get; set; } = string.Empty;

    [JsonPropertyName("dataAreaId")]
    public string DataAreaId { get; set; } = string.Empty;

    [JsonPropertyName("WarehouseType")]
    public string WarehouseType { get; set; } = string.Empty;

    [JsonPropertyName("FormattedPrimaryAddress")]
    public string FormattedPrimaryAddress { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressStreet")]
    public string PrimaryAddressStreet { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressStreetNumber")]
    public string PrimaryAddressStreetNumber { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressCity")]
    public string PrimaryAddressCity { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressZipCode")]
    public string PrimaryAddressZipCode { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressCountryRegionId")]
    public string PrimaryAddressCountryRegionId { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressStateId")]
    public string PrimaryAddressStateId { get; set; } = string.Empty;

    [JsonPropertyName("PrimaryAddressLatitude")]
    public decimal? PrimaryAddressLatitude { get; set; }

    [JsonPropertyName("PrimaryAddressLongitude")]
    public decimal? PrimaryAddressLongitude { get; set; }

    /// <summary>
    /// Maps warehouse DTO to the D365FO-sourced fields of GdpSite domain model.
    /// GDP extension fields are left at defaults (must be merged separately).
    /// </summary>
    public GdpSite ToDomainModel(string? operationalSiteName = null)
    {
        return new GdpSite
        {
            WarehouseId = WarehouseId,
            WarehouseName = WarehouseName,
            OperationalSiteId = OperationalSiteId,
            OperationalSiteName = operationalSiteName ?? string.Empty,
            DataAreaId = DataAreaId,
            WarehouseType = WarehouseType,
            Street = PrimaryAddressStreet,
            StreetNumber = PrimaryAddressStreetNumber,
            City = PrimaryAddressCity,
            ZipCode = PrimaryAddressZipCode,
            CountryRegionId = PrimaryAddressCountryRegionId,
            StateId = PrimaryAddressStateId,
            FormattedAddress = FormattedPrimaryAddress,
            Latitude = PrimaryAddressLatitude,
            Longitude = PrimaryAddressLongitude
        };
    }
}

/// <summary>
/// OData response wrapper for Warehouses entity set.
/// </summary>
public class WarehouseODataResponse
{
    [JsonPropertyName("value")]
    public List<WarehouseDto> Value { get; set; } = new();
}
