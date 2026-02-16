namespace RE2.ComplianceCore.Models;

/// <summary>
/// Read-only D365 F&O product with resolved substance classification attributes.
/// Represents a released product from ReleasedProductsV2 enriched with
/// ProductAttributeValuesV2 for substance identification.
/// </summary>
public class Product
{
    #region D365 F&O Released Product Fields (read-only)

    /// <summary>
    /// D365 item number (unique within DataAreaId).
    /// </summary>
    public string ItemNumber { get; set; } = string.Empty;

    /// <summary>
    /// D365 legal entity.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Shared product number (links to product attributes).
    /// </summary>
    public string ProductNumber { get; set; } = string.Empty;

    /// <summary>
    /// Product display name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// </summary>
    public string? ProductDescription { get; set; }

    #endregion

    #region Resolved Attribute Fields

    /// <summary>
    /// Substance code resolved from the ControlledSubstance product attribute.
    /// Null if the product has no substance attribute (not a controlled product).
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Opium Act List value resolved from the OpiumActList product attribute.
    /// </summary>
    public string? OpiumActListValue { get; set; }

    /// <summary>
    /// Precursor Category value resolved from the PrecursorCategory product attribute.
    /// </summary>
    public string? PrecursorCategoryValue { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this product is a controlled substance (has a SubstanceCode attribute).
    /// </summary>
    public bool IsControlledSubstance => !string.IsNullOrWhiteSpace(SubstanceCode);

    #endregion
}
