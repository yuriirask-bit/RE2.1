namespace RE2.ComplianceCore.Configuration;

/// <summary>
/// Configuration for D365 F&O product attribute names used to resolve substance classification.
/// Maps configurable attribute names to the three substance classification attributes.
/// Section name: "ProductAttributes"
/// </summary>
public class ProductAttributeConfiguration
{
    public const string SectionName = "ProductAttributes";

    /// <summary>
    /// D365 product attribute name that holds the substance code (e.g., "Morphine", "Fentanyl").
    /// Default: "ControlledSubstance"
    /// </summary>
    public string SubstanceAttributeName { get; set; } = "ControlledSubstance";

    /// <summary>
    /// D365 product attribute name that holds the Opium Act List classification.
    /// Default: "OpiumActList"
    /// </summary>
    public string OpiumActListAttributeName { get; set; } = "OpiumActList";

    /// <summary>
    /// D365 product attribute name that holds the Precursor Category classification.
    /// Default: "PrecursorCategory"
    /// </summary>
    public string PrecursorCategoryAttributeName { get; set; } = "PrecursorCategory";
}
