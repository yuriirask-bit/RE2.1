using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// DTO for D365 F&O ReleasedProductsV2 OData entity.
/// </summary>
public class ReleasedProductDto
{
    public string ItemNumber { get; set; } = string.Empty;
    public string dataAreaId { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }

    public Product ToDomainModel()
    {
        return new Product
        {
            ItemNumber = ItemNumber,
            DataAreaId = dataAreaId,
            ProductNumber = ProductNumber,
            ProductName = ProductName ?? string.Empty,
            ProductDescription = ProductDescription
        };
    }
}

/// <summary>
/// OData response wrapper for ReleasedProductsV2 collection.
/// </summary>
public class ReleasedProductODataResponse
{
    public List<ReleasedProductDto> value { get; set; } = new();
}
