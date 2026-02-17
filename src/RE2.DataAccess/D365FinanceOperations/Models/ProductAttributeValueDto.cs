namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// DTO for D365 F&O ProductAttributeValuesV2 OData entity.
/// Attributes are at shared product level (not per legal entity).
/// </summary>
public class ProductAttributeValueDto
{
    public string ProductNumber { get; set; } = string.Empty;
    public string AttributeTypeName { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;

    // Polymorphic value fields
    public string? TextValue { get; set; }
    public int? IntegerValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BooleanValue { get; set; }

    /// <summary>
    /// Gets the effective value from the polymorphic value fields.
    /// Returns TextValue if set, then string of IntegerValue, DecimalValue, or BooleanValue.
    /// </summary>
    public string? GetEffectiveValue()
    {
        if (!string.IsNullOrWhiteSpace(TextValue))
        {
            return TextValue;
        }

        if (IntegerValue.HasValue)
        {
            return IntegerValue.Value.ToString();
        }

        if (DecimalValue.HasValue)
        {
            return DecimalValue.Value.ToString();
        }

        if (BooleanValue.HasValue)
        {
            return BooleanValue.Value.ToString();
        }

        return null;
    }
}

/// <summary>
/// OData response wrapper for ProductAttributeValuesV2 collection.
/// </summary>
public class ProductAttributeValueODataResponse
{
    public List<ProductAttributeValueDto> value { get; set; } = new();
}
