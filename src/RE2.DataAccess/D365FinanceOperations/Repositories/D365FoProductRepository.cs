using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RE2.ComplianceCore.Configuration;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;

namespace RE2.DataAccess.D365FinanceOperations.Repositories;

/// <summary>
/// D365 Finance &amp; Operations implementation of IProductRepository.
/// Retrieves released products from ReleasedProductsV2 and enriches them
/// with substance classification attributes from ProductAttributeValuesV2.
/// </summary>
public class D365FoProductRepository : IProductRepository
{
    private readonly ID365FoClient _client;
    private readonly ProductAttributeConfiguration _config;
    private readonly ILogger<D365FoProductRepository> _logger;

    private const string ReleasedProductsEntitySet = "ReleasedProductsV2";
    private const string AttributeValuesEntitySet = "ProductAttributeValuesV2";

    public D365FoProductRepository(
        ID365FoClient client,
        IOptions<ProductAttributeConfiguration> config,
        ILogger<D365FoProductRepository> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Product?> GetProductAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"ItemNumber='{itemNumber}',dataAreaId='{dataAreaId}'";
            var dto = await _client.GetByKeyAsync<ReleasedProductDto>(ReleasedProductsEntitySet, key, cancellationToken);

            if (dto == null)
            {
                return null;
            }

            var product = dto.ToDomainModel();
            await EnrichWithAttributesAsync(product, cancellationToken);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ItemNumber}/{DataAreaId}", itemNumber, dataAreaId);
            return null;
        }
    }

    public async Task<IEnumerable<Product>> GetControlledProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Get all substance attribute values to find which products are controlled
            var substanceAttributes = await GetSubstanceAttributeValuesAsync(cancellationToken);
            if (!substanceAttributes.Any())
            {
                _logger.LogInformation("No products found with substance attribute '{AttributeName}'", _config.SubstanceAttributeName);
                return Enumerable.Empty<Product>();
            }

            // Step 2: Get all released products
            var query = "$select=ItemNumber,dataAreaId,ProductNumber,SearchName,ProductSearchName";
            var response = await _client.GetAsync<ReleasedProductODataResponse>(ReleasedProductsEntitySet, query, cancellationToken);

            if (response?.value == null || !response.value.Any())
            {
                return Enumerable.Empty<Product>();
            }

            // Step 3: Build attribute lookup by ProductNumber
            var allAttributes = await GetAllSubstanceClassificationAttributesAsync(cancellationToken);
            var attrByProduct = BuildAttributeLookup(allAttributes);

            // Step 4: Join products with attributes, return only controlled ones
            var products = new List<Product>();
            foreach (var dto in response.value)
            {
                var product = dto.ToDomainModel();
                if (attrByProduct.TryGetValue(product.ProductNumber, out var attrs))
                {
                    ApplyAttributes(product, attrs);
                }

                if (product.IsControlledSubstance)
                {
                    products.Add(product);
                }
            }

            _logger.LogInformation("Found {Count} controlled products from D365 F&O", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving controlled products");
            return Enumerable.Empty<Product>();
        }
    }

    public async Task<IEnumerable<Product>> GetProductsBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get attribute values filtered by substance code
            var filter = $"$filter=AttributeName eq '{_config.SubstanceAttributeName}' and TextValue eq '{substanceCode}'";
            var attrResponse = await _client.GetAsync<ProductAttributeValueODataResponse>(AttributeValuesEntitySet, filter, cancellationToken);

            if (attrResponse?.value == null || !attrResponse.value.Any())
            {
                return Enumerable.Empty<Product>();
            }

            var productNumbers = attrResponse.value.Select(a => a.ProductNumber).Distinct().ToHashSet();

            // Get released products and filter by matching product numbers
            var query = "$select=ItemNumber,dataAreaId,ProductNumber,SearchName,ProductSearchName";
            var response = await _client.GetAsync<ReleasedProductODataResponse>(ReleasedProductsEntitySet, query, cancellationToken);

            if (response?.value == null)
            {
                return Enumerable.Empty<Product>();
            }

            var allAttributes = await GetAllSubstanceClassificationAttributesAsync(cancellationToken);
            var attrByProduct = BuildAttributeLookup(allAttributes);

            return response.value
                .Where(dto => productNumbers.Contains(dto.ProductNumber))
                .Select(dto =>
                {
                    var product = dto.ToDomainModel();
                    if (attrByProduct.TryGetValue(product.ProductNumber, out var attrs))
                    {
                        ApplyAttributes(product, attrs);
                    }
                    return product;
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by substance code {SubstanceCode}", substanceCode);
            return Enumerable.Empty<Product>();
        }
    }

    public async Task<IEnumerable<string>> GetDistinctSubstanceCodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var attributes = await GetSubstanceAttributeValuesAsync(cancellationToken);

            return attributes
                .Select(a => a.GetEffectiveValue())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .Order()
                .ToList()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving distinct substance codes");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<string?> ResolveSubstanceCodeAsync(string itemNumber, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            // First get the product to find its ProductNumber
            var key = $"ItemNumber='{itemNumber}',dataAreaId='{dataAreaId}'";
            var dto = await _client.GetByKeyAsync<ReleasedProductDto>(ReleasedProductsEntitySet, key, cancellationToken);

            if (dto == null)
            {
                return null;
            }

            // Then look up the substance attribute for that product number
            var filter = $"$filter=ProductNumber eq '{dto.ProductNumber}' and AttributeName eq '{_config.SubstanceAttributeName}'&$top=1";
            var attrResponse = await _client.GetAsync<ProductAttributeValueODataResponse>(AttributeValuesEntitySet, filter, cancellationToken);

            return attrResponse?.value?.FirstOrDefault()?.GetEffectiveValue();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving substance code for {ItemNumber}/{DataAreaId}", itemNumber, dataAreaId);
            return null;
        }
    }

    #region Private Helpers

    private async Task<List<ProductAttributeValueDto>> GetSubstanceAttributeValuesAsync(CancellationToken cancellationToken)
    {
        var filter = $"$filter=AttributeName eq '{_config.SubstanceAttributeName}'";
        var response = await _client.GetAsync<ProductAttributeValueODataResponse>(AttributeValuesEntitySet, filter, cancellationToken);
        return response?.value ?? new List<ProductAttributeValueDto>();
    }

    private async Task<List<ProductAttributeValueDto>> GetAllSubstanceClassificationAttributesAsync(CancellationToken cancellationToken)
    {
        var attributeNames = new[]
        {
            _config.SubstanceAttributeName,
            _config.OpiumActListAttributeName,
            _config.PrecursorCategoryAttributeName
        }.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

        var filterParts = attributeNames.Select(n => $"AttributeName eq '{n}'");
        var filter = $"$filter={string.Join(" or ", filterParts)}";

        var response = await _client.GetAsync<ProductAttributeValueODataResponse>(AttributeValuesEntitySet, filter, cancellationToken);
        return response?.value ?? new List<ProductAttributeValueDto>();
    }

    private static Dictionary<string, List<ProductAttributeValueDto>> BuildAttributeLookup(List<ProductAttributeValueDto> attributes)
    {
        return attributes
            .GroupBy(a => a.ProductNumber)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private void ApplyAttributes(Product product, List<ProductAttributeValueDto> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeName == _config.SubstanceAttributeName)
            {
                product.SubstanceCode = attr.GetEffectiveValue();
            }
            else if (attr.AttributeName == _config.OpiumActListAttributeName)
            {
                product.OpiumActListValue = attr.GetEffectiveValue();
            }
            else if (attr.AttributeName == _config.PrecursorCategoryAttributeName)
            {
                product.PrecursorCategoryValue = attr.GetEffectiveValue();
            }
        }
    }

    private async Task EnrichWithAttributesAsync(Product product, CancellationToken cancellationToken)
    {
        var filter = $"$filter=ProductNumber eq '{product.ProductNumber}'";
        var response = await _client.GetAsync<ProductAttributeValueODataResponse>(AttributeValuesEntitySet, filter, cancellationToken);

        if (response?.value != null)
        {
            ApplyAttributes(product, response.value);
        }
    }

    #endregion
}
