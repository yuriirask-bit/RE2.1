using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Threshold management API endpoints.
/// T132d: ThresholdsController v1 with GET, POST, PUT, DELETE endpoints per FR-022.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ThresholdsController : ControllerBase
{
    private readonly IThresholdService _thresholdService;
    private readonly ILogger<ThresholdsController> _logger;

    public ThresholdsController(
        IThresholdService thresholdService,
        ILogger<ThresholdsController> logger)
    {
        _thresholdService = thresholdService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all thresholds.
    /// </summary>
    /// <param name="activeOnly">If true, only returns active thresholds. Default: false.</param>
    /// <param name="type">Optional filter by threshold type.</param>
    /// <param name="substanceCode">Optional filter by substance code.</param>
    /// <param name="search">Optional search term for name/description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of thresholds.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ThresholdResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThresholds(
        [FromQuery] bool activeOnly = false,
        [FromQuery] ThresholdType? type = null,
        [FromQuery] string? substanceCode = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Threshold> thresholds;

        if (!string.IsNullOrWhiteSpace(search))
        {
            thresholds = await _thresholdService.SearchAsync(search, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(substanceCode))
        {
            thresholds = await _thresholdService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        }
        else if (type.HasValue)
        {
            thresholds = await _thresholdService.GetByTypeAsync(type.Value, cancellationToken);
        }
        else if (activeOnly)
        {
            thresholds = await _thresholdService.GetActiveAsync(cancellationToken);
        }
        else
        {
            thresholds = await _thresholdService.GetAllAsync(cancellationToken);
        }

        var response = thresholds.Select(ThresholdResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific threshold by ID.
    /// </summary>
    /// <param name="id">Threshold ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The threshold details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ThresholdResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThreshold(Guid id, CancellationToken cancellationToken = default)
    {
        var threshold = await _thresholdService.GetByIdAsync(id, cancellationToken);

        if (threshold == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.THRESHOLD_NOT_FOUND,
                Message = $"Threshold with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(ThresholdResponseDto.FromDomainModel(threshold));
    }

    /// <summary>
    /// Gets thresholds by substance code.
    /// </summary>
    /// <param name="substanceCode">The substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of thresholds for the substance.</returns>
    [HttpGet("by-substance/{substanceCode}")]
    [ProducesResponseType(typeof(IEnumerable<ThresholdResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThresholdsBySubstance(
        string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var thresholds = await _thresholdService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        var response = thresholds.Select(ThresholdResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets thresholds by customer category.
    /// </summary>
    /// <param name="category">The business category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of thresholds for the category.</returns>
    [HttpGet("by-category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ThresholdResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThresholdsByCategory(
        BusinessCategory category,
        CancellationToken cancellationToken = default)
    {
        var thresholds = await _thresholdService.GetByCustomerCategoryAsync(category, cancellationToken);
        var response = thresholds.Select(ThresholdResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new threshold.
    /// Only ComplianceManager role can create thresholds.
    /// </summary>
    /// <param name="request">Threshold creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created threshold details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ThresholdResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateThreshold(
        [FromBody] CreateThresholdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var threshold = request.ToDomainModel();

        var (id, result) = await _thresholdService.CreateAsync(threshold, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Threshold validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        threshold.Id = id!.Value;
        _logger.LogInformation("Created threshold {Name} with ID {Id} via API", threshold.Name, id);

        return CreatedAtAction(
            nameof(GetThreshold),
            new { id = id },
            ThresholdResponseDto.FromDomainModel(threshold));
    }

    /// <summary>
    /// Updates an existing threshold.
    /// Only ComplianceManager role can modify thresholds.
    /// </summary>
    /// <param name="id">Threshold ID.</param>
    /// <param name="request">Updated threshold data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated threshold details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ThresholdResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateThreshold(
        Guid id,
        [FromBody] UpdateThresholdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var threshold = request.ToDomainModel(id);

        var result = await _thresholdService.UpdateAsync(threshold, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.Any(v => v.ErrorCode == ErrorCodes.THRESHOLD_NOT_FOUND)
                ? ErrorCodes.THRESHOLD_NOT_FOUND
                : ErrorCodes.VALIDATION_ERROR;

            var statusCode = errorCode == ErrorCodes.THRESHOLD_NOT_FOUND
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Updated threshold {Id} via API", id);

        var updated = await _thresholdService.GetByIdAsync(id, cancellationToken);
        return Ok(ThresholdResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a threshold.
    /// Only ComplianceManager role can delete thresholds.
    /// </summary>
    /// <param name="id">Threshold ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteThreshold(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _thresholdService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.THRESHOLD_NOT_FOUND,
                Message = $"Threshold with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted threshold {Id} via API", id);

        return NoContent();
    }
}

#region DTOs

/// <summary>
/// Threshold response DTO for API responses.
/// </summary>
public class ThresholdResponseDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ThresholdType ThresholdType { get; set; }
    public ThresholdPeriod Period { get; set; }

    // Scope
    public string? SubstanceCode { get; set; }
    public string? SubstanceName { get; set; }
    public Guid? LicenceTypeId { get; set; }
    public string? LicenceTypeName { get; set; }
    public BusinessCategory? CustomerCategory { get; set; }
    public Guid? CustomerId { get; set; }
    public string? OpiumActList { get; set; }

    // Limits
    public decimal LimitValue { get; set; }
    public required string LimitUnit { get; set; }
    public decimal WarningThresholdPercent { get; set; }

    // Override settings
    public bool AllowOverride { get; set; }
    public decimal? MaxOverridePercent { get; set; }

    // Status
    public bool IsActive { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? RegulatoryReference { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public static ThresholdResponseDto FromDomainModel(Threshold threshold)
    {
        return new ThresholdResponseDto
        {
            Id = threshold.Id,
            Name = threshold.Name,
            Description = threshold.Description,
            ThresholdType = threshold.ThresholdType,
            Period = threshold.Period,
            SubstanceCode = threshold.SubstanceCode,
            SubstanceName = threshold.SubstanceName,
            LicenceTypeId = threshold.LicenceTypeId,
            LicenceTypeName = threshold.LicenceTypeName,
            CustomerCategory = threshold.CustomerCategory,
            CustomerId = threshold.CustomerId,
            OpiumActList = threshold.OpiumActList,
            LimitValue = threshold.LimitValue,
            LimitUnit = threshold.LimitUnit,
            WarningThresholdPercent = threshold.WarningThresholdPercent,
            AllowOverride = threshold.AllowOverride,
            MaxOverridePercent = threshold.MaxOverridePercent,
            IsActive = threshold.IsActive,
            EffectiveFrom = threshold.EffectiveFrom,
            EffectiveTo = threshold.EffectiveTo,
            RegulatoryReference = threshold.RegulatoryReference,
            CreatedDate = threshold.CreatedDate,
            ModifiedDate = threshold.ModifiedDate
        };
    }
}

/// <summary>
/// Request DTO for creating a new threshold.
/// </summary>
public class CreateThresholdRequestDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ThresholdType ThresholdType { get; set; }
    public ThresholdPeriod Period { get; set; }

    // Scope
    public string? SubstanceCode { get; set; }
    public Guid? LicenceTypeId { get; set; }
    public BusinessCategory? CustomerCategory { get; set; }
    public Guid? CustomerId { get; set; }
    public string? OpiumActList { get; set; }

    // Limits
    public decimal LimitValue { get; set; }
    public string LimitUnit { get; set; } = "g";
    public decimal WarningThresholdPercent { get; set; } = 80m;

    // Override settings
    public bool AllowOverride { get; set; } = true;
    public decimal? MaxOverridePercent { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? RegulatoryReference { get; set; }

    public Threshold ToDomainModel()
    {
        return new Threshold
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            ThresholdType = ThresholdType,
            Period = Period,
            SubstanceCode = SubstanceCode,
            LicenceTypeId = LicenceTypeId,
            CustomerCategory = CustomerCategory,
            CustomerId = CustomerId,
            OpiumActList = OpiumActList,
            LimitValue = LimitValue,
            LimitUnit = LimitUnit,
            WarningThresholdPercent = WarningThresholdPercent,
            AllowOverride = AllowOverride,
            MaxOverridePercent = MaxOverridePercent,
            IsActive = IsActive,
            EffectiveFrom = EffectiveFrom,
            EffectiveTo = EffectiveTo,
            RegulatoryReference = RegulatoryReference,
            CreatedDate = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Request DTO for updating a threshold.
/// </summary>
public class UpdateThresholdRequestDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ThresholdType ThresholdType { get; set; }
    public ThresholdPeriod Period { get; set; }

    // Scope
    public string? SubstanceCode { get; set; }
    public Guid? LicenceTypeId { get; set; }
    public BusinessCategory? CustomerCategory { get; set; }
    public Guid? CustomerId { get; set; }
    public string? OpiumActList { get; set; }

    // Limits
    public decimal LimitValue { get; set; }
    public string LimitUnit { get; set; } = "g";
    public decimal WarningThresholdPercent { get; set; } = 80m;

    // Override settings
    public bool AllowOverride { get; set; } = true;
    public decimal? MaxOverridePercent { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? RegulatoryReference { get; set; }

    public Threshold ToDomainModel(Guid id)
    {
        return new Threshold
        {
            Id = id,
            Name = Name,
            Description = Description,
            ThresholdType = ThresholdType,
            Period = Period,
            SubstanceCode = SubstanceCode,
            LicenceTypeId = LicenceTypeId,
            CustomerCategory = CustomerCategory,
            CustomerId = CustomerId,
            OpiumActList = OpiumActList,
            LimitValue = LimitValue,
            LimitUnit = LimitUnit,
            WarningThresholdPercent = WarningThresholdPercent,
            AllowOverride = AllowOverride,
            MaxOverridePercent = MaxOverridePercent,
            IsActive = IsActive,
            EffectiveFrom = EffectiveFrom,
            EffectiveTo = EffectiveTo,
            RegulatoryReference = RegulatoryReference
        };
    }
}

#endregion
