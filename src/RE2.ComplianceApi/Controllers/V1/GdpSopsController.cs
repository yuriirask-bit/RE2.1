using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP SOP management API endpoints.
/// T289: REST API per US12 (FR-049).
/// </summary>
[ApiController]
[Route("api/v1/gdp-sops")]
[Authorize]
public class GdpSopsController : ControllerBase
{
    private readonly IGdpComplianceService _gdpService;
    private readonly IGdpSopRepository _sopRepository;
    private readonly ILogger<GdpSopsController> _logger;

    public GdpSopsController(
        IGdpComplianceService gdpService,
        IGdpSopRepository sopRepository,
        ILogger<GdpSopsController> logger)
    {
        _gdpService = gdpService;
        _sopRepository = sopRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all SOPs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GdpSopResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSops(CancellationToken cancellationToken = default)
    {
        var sops = await _sopRepository.GetAllAsync(cancellationToken);
        return Ok(sops.Select(GdpSopResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific SOP by ID.
    /// </summary>
    [HttpGet("{sopId:guid}")]
    [ProducesResponseType(typeof(GdpSopResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSop(Guid sopId, CancellationToken cancellationToken = default)
    {
        var sop = await _sopRepository.GetByIdAsync(sopId, cancellationToken);
        if (sop == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"SOP '{sopId}' not found" });
        }
        return Ok(GdpSopResponseDto.FromDomain(sop));
    }

    /// <summary>
    /// Creates a new SOP.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpSopResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSop(
        [FromBody] CreateGdpSopRequestDto request, CancellationToken cancellationToken = default)
    {
        var sop = request.ToDomain();
        var validationResult = sop.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = validationResult.Violations.First().ErrorCode,
                Message = string.Join("; ", validationResult.Violations.Select(v => v.Message))
            });
        }

        var id = await _sopRepository.CreateAsync(sop, cancellationToken);
        sop.SopId = id;
        return CreatedAtAction(nameof(GetSop), new { sopId = id }, GdpSopResponseDto.FromDomain(sop));
    }

    /// <summary>
    /// Updates an existing SOP.
    /// </summary>
    [HttpPut("{sopId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSop(
        Guid sopId, [FromBody] CreateGdpSopRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _sopRepository.GetByIdAsync(sopId, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"SOP '{sopId}' not found" });
        }

        var sop = request.ToDomain();
        sop.SopId = sopId;
        var validationResult = sop.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = validationResult.Violations.First().ErrorCode,
                Message = string.Join("; ", validationResult.Violations.Select(v => v.Message))
            });
        }

        await _sopRepository.UpdateAsync(sop, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a SOP.
    /// </summary>
    [HttpDelete("{sopId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSop(Guid sopId, CancellationToken cancellationToken = default)
    {
        var existing = await _sopRepository.GetByIdAsync(sopId, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"SOP '{sopId}' not found" });
        }

        await _sopRepository.DeleteAsync(sopId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets SOPs linked to a specific site.
    /// </summary>
    [HttpGet("{sopId:guid}/sites")]
    [ProducesResponseType(typeof(IEnumerable<GdpSopResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSiteSops(Guid sopId, CancellationToken cancellationToken = default)
    {
        // This is actually "get sites linked to SOP", but we return SOPs linked to a site via the other endpoint
        var sops = await _sopRepository.GetSiteSopsAsync(sopId, cancellationToken);
        return Ok(sops.Select(GdpSopResponseDto.FromDomain));
    }

    /// <summary>
    /// Links a SOP to a site.
    /// </summary>
    [HttpPost("{sopId:guid}/sites/{siteId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LinkSopToSite(Guid sopId, Guid siteId, CancellationToken cancellationToken = default)
    {
        await _sopRepository.LinkSopToSiteAsync(siteId, sopId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unlinks a SOP from a site.
    /// </summary>
    [HttpDelete("{sopId:guid}/sites/{siteId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkSopFromSite(Guid sopId, Guid siteId, CancellationToken cancellationToken = default)
    {
        await _sopRepository.UnlinkSopFromSiteAsync(siteId, sopId, cancellationToken);
        return NoContent();
    }
}

#region DTOs

public class GdpSopResponseDto
{
    public Guid SopId { get; set; }
    public string SopNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsActive { get; set; }

    public static GdpSopResponseDto FromDomain(GdpSop sop) => new()
    {
        SopId = sop.SopId,
        SopNumber = sop.SopNumber,
        Title = sop.Title,
        Category = sop.Category.ToString(),
        Version = sop.Version,
        EffectiveDate = sop.EffectiveDate,
        DocumentUrl = sop.DocumentUrl,
        IsActive = sop.IsActive
    };
}

public class CreateGdpSopRequestDto
{
    public string SopNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public GdpSopCategory Category { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public GdpSop ToDomain() => new()
    {
        SopNumber = SopNumber,
        Title = Title,
        Category = Category,
        Version = Version,
        EffectiveDate = EffectiveDate,
        DocumentUrl = DocumentUrl,
        IsActive = IsActive
    };
}

#endregion
