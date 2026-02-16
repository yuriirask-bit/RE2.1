using System.Text.Json.Serialization;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// DTO matching D365FO OperationalSitesV2 OData entity.
/// T187: Data transfer object for site name lookups.
/// </summary>
public class OperationalSiteDto
{
    [JsonPropertyName("SiteId")]
    public string SiteId { get; set; } = string.Empty;

    [JsonPropertyName("SiteName")]
    public string SiteName { get; set; } = string.Empty;

    [JsonPropertyName("dataAreaId")]
    public string DataAreaId { get; set; } = string.Empty;

    [JsonPropertyName("FormattedPrimaryAddress")]
    public string FormattedPrimaryAddress { get; set; } = string.Empty;
}

/// <summary>
/// OData response wrapper for OperationalSitesV2 entity set.
/// </summary>
public class OperationalSiteODataResponse
{
    [JsonPropertyName("value")]
    public List<OperationalSiteDto> Value { get; set; } = new();
}
