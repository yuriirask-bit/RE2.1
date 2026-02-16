using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IProductRepository for local development and testing.
/// Keyed by composite key {ItemNumber}|{DataAreaId}.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<string, Product> _products = new(StringComparer.OrdinalIgnoreCase);

    private static string MakeKey(string itemNumber, string dataAreaId)
        => $"{itemNumber}|{dataAreaId}";

    public Task<Product?> GetProductAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(MakeKey(itemNumber, dataAreaId), out var product);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<Product>> GetControlledProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = _products.Values
            .Where(p => p.IsControlledSubstance)
            .ToList();
        return Task.FromResult<IEnumerable<Product>>(products);
    }

    public Task<IEnumerable<Product>> GetProductsBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        var products = _products.Values
            .Where(p => !string.IsNullOrWhiteSpace(p.SubstanceCode) &&
                        p.SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Product>>(products);
    }

    public Task<IEnumerable<string>> GetDistinctSubstanceCodesAsync(CancellationToken cancellationToken = default)
    {
        var codes = _products.Values
            .Where(p => !string.IsNullOrWhiteSpace(p.SubstanceCode))
            .Select(p => p.SubstanceCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
        return Task.FromResult<IEnumerable<string>>(codes);
    }

    public Task<string?> ResolveSubstanceCodeAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(MakeKey(itemNumber, dataAreaId), out var product);
        return Task.FromResult(product?.SubstanceCode);
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Product> products)
    {
        foreach (var product in products)
        {
            _products.TryAdd(MakeKey(product.ItemNumber, product.DataAreaId), product);
        }
    }
}
