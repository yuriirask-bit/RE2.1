using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Product browsing API endpoints.
/// Products are sourced from D365 F&O with resolved substance classification attributes.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository productRepository,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <summary>
    /// Browse products, optionally filtering to controlled products only.
    /// </summary>
    /// <param name="controlled">If true, only returns products with a controlled substance attribute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] bool controlled = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> products;

        if (controlled)
        {
            products = await _productRepository.GetControlledProductsAsync(cancellationToken);
        }
        else
        {
            // Default to controlled products (primary use case for compliance)
            products = await _productRepository.GetControlledProductsAsync(cancellationToken);
        }

        var response = products.Select(ProductResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific product by item number, with substance info.
    /// </summary>
    /// <param name="itemNumber">D365 item number.</param>
    /// <param name="dataAreaId">D365 legal entity (data area ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product details with substance info.</returns>
    [HttpGet("{itemNumber}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(
        string itemNumber,
        [FromQuery] string? dataAreaId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataAreaId))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = "MISSING_DATA_AREA_ID",
                Message = "dataAreaId query parameter is required"
            });
        }

        var product = await _productRepository.GetProductAsync(itemNumber, dataAreaId, cancellationToken);

        if (product == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = "PRODUCT_NOT_FOUND",
                Message = $"Product with item number '{itemNumber}' in data area '{dataAreaId}' not found"
            });
        }

        return Ok(ProductResponseDto.FromDomainModel(product));
    }

    /// <summary>
    /// Gets all products containing a specific substance.
    /// </summary>
    /// <param name="substanceCode">Substance code to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products for the substance.</returns>
    [HttpGet("by-substance/{substanceCode}")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsBySubstance(
        string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetProductsBySubstanceCodeAsync(substanceCode, cancellationToken);
        var response = products.Select(ProductResponseDto.FromDomainModel);
        return Ok(response);
    }
}

#region DTOs

/// <summary>
/// Product response DTO for API responses.
/// </summary>
public class ProductResponseDto
{
    public string ItemNumber { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? SubstanceCode { get; set; }
    public string? OpiumActListValue { get; set; }
    public string? PrecursorCategoryValue { get; set; }
    public bool IsControlledSubstance { get; set; }

    public static ProductResponseDto FromDomainModel(Product product)
    {
        return new ProductResponseDto
        {
            ItemNumber = product.ItemNumber,
            DataAreaId = product.DataAreaId,
            ProductNumber = product.ProductNumber,
            ProductName = product.ProductName,
            ProductDescription = product.ProductDescription,
            SubstanceCode = product.SubstanceCode,
            OpiumActListValue = product.OpiumActListValue,
            PrecursorCategoryValue = product.PrecursorCategoryValue,
            IsControlledSubstance = product.IsControlledSubstance
        };
    }
}

#endregion
