using System.Text.Json.Serialization;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// DTO matching D365FO CustomersV3 OData entity (minimal fields).
/// Maps to the D365FO read-only fields of the Customer domain model.
/// </summary>
public class CustomerDto
{
    [JsonPropertyName("CustomerAccount")]
    public string CustomerAccount { get; set; } = string.Empty;

    [JsonPropertyName("dataAreaId")]
    public string DataAreaId { get; set; } = string.Empty;

    [JsonPropertyName("OrganizationName")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonPropertyName("AddressCountryRegionId")]
    public string AddressCountryRegionId { get; set; } = string.Empty;

    /// <summary>
    /// Maps D365FO customer DTO to the D365FO-sourced fields of Customer domain model.
    /// Compliance extension fields are left at defaults (must be merged separately).
    /// </summary>
    public Customer ToDomainModel()
    {
        return new Customer
        {
            CustomerAccount = CustomerAccount,
            DataAreaId = DataAreaId,
            OrganizationName = OrganizationName,
            AddressCountryRegionId = AddressCountryRegionId
        };
    }
}

/// <summary>
/// OData response wrapper for CustomersV3 entity set.
/// </summary>
public class CustomerODataResponse
{
    [JsonPropertyName("value")]
    public List<CustomerDto> Value { get; set; } = new();
}
