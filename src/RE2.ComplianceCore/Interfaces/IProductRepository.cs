using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for D365 F&O product data with resolved substance classification attributes.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by item number and data area, with resolved substance attributes.
    /// </summary>
    Task<Product?> GetProductAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products that have a controlled substance attribute set.
    /// </summary>
    Task<IEnumerable<Product>> GetControlledProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products that contain a specific substance (by SubstanceCode).
    /// </summary>
    Task<IEnumerable<Product>> GetProductsBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct substance codes across all products.
    /// Used for substance discovery from D365 product attributes.
    /// </summary>
    Task<IEnumerable<string>> GetDistinctSubstanceCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the substance code for a given product. High-frequency, cache-worthy.
    /// Returns null if the product doesn't have a substance attribute.
    /// </summary>
    Task<string?> ResolveSubstanceCodeAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default);
}
