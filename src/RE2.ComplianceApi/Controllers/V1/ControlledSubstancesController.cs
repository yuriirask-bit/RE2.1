using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Controlled substance management API endpoints.
/// Substances are discovered from D365 product attributes; compliance extensions are managed here.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ControlledSubstancesController : ControllerBase
{
    private readonly IControlledSubstanceService _substanceService;
    private readonly ILogger<ControlledSubstancesController> _logger;

    public ControlledSubstancesController(
        IControlledSubstanceService substanceService,
        ILogger<ControlledSubstancesController> logger)
    {
        _substanceService = substanceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all controlled substances.
    /// </summary>
    /// <param name="activeOnly">If true, only returns active substances. Default: false.</param>
    /// <param name="opiumActList">Filter by Opium Act classification (None, ListI, ListII).</param>
    /// <param name="precursorCategory">Filter by precursor category (None, Category1, Category2, Category3).</param>
    /// <param name="search">Search term for name or substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of controlled substances.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ControlledSubstanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubstances(
        [FromQuery] bool activeOnly = false,
        [FromQuery] SubstanceCategories.OpiumActList? opiumActList = null,
        [FromQuery] SubstanceCategories.PrecursorCategory? precursorCategory = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ControlledSubstance> substances;

        if (!string.IsNullOrWhiteSpace(search))
        {
            substances = await _substanceService.SearchAsync(search, cancellationToken);
        }
        else if (opiumActList.HasValue && opiumActList.Value != SubstanceCategories.OpiumActList.None)
        {
            substances = await _substanceService.GetByOpiumActListAsync(opiumActList.Value, cancellationToken);
        }
        else if (precursorCategory.HasValue && precursorCategory.Value != SubstanceCategories.PrecursorCategory.None)
        {
            substances = await _substanceService.GetByPrecursorCategoryAsync(precursorCategory.Value, cancellationToken);
        }
        else if (activeOnly)
        {
            substances = await _substanceService.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            substances = await _substanceService.GetAllAsync(cancellationToken);
        }

        var response = substances.Select(ControlledSubstanceResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific controlled substance by substance code (business key).
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The substance details.</returns>
    [HttpGet("{substanceCode}")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubstance(string substanceCode, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with code '{substanceCode}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(ControlledSubstanceResponseDto.FromDomainModel(substance));
    }

    /// <summary>
    /// Configures a compliance extension for a D365-discovered substance.
    /// Only ComplianceManager role can configure compliance per FR-031.
    /// </summary>
    /// <param name="request">Compliance configuration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPost("configure-compliance")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureCompliance(
        [FromBody] ConfigureComplianceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetBySubstanceCodeAsync(request.SubstanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with code '{request.SubstanceCode}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Apply compliance extension fields
        substance.RegulatoryRestrictions = request.RegulatoryRestrictions;
        substance.IsActive = request.IsActive;
        substance.ClassificationEffectiveDate = request.ClassificationEffectiveDate;

        var result = await _substanceService.ConfigureComplianceAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Compliance configuration failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Configured compliance for substance {SubstanceCode}", request.SubstanceCode);

        var updated = await _substanceService.GetBySubstanceCodeAsync(request.SubstanceCode, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Updates an existing compliance extension for a substance.
    /// Only ComplianceManager role can modify compliance per FR-031.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="request">Updated compliance data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPut("{substanceCode}/compliance")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompliance(
        string substanceCode,
        [FromBody] UpdateComplianceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with code '{substanceCode}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Apply compliance extension fields
        substance.RegulatoryRestrictions = request.RegulatoryRestrictions;
        substance.IsActive = request.IsActive;
        substance.ClassificationEffectiveDate = request.ClassificationEffectiveDate;

        var result = await _substanceService.UpdateComplianceAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = "Compliance update failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Updated compliance for substance {SubstanceCode}", substanceCode);

        var updated = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deactivates a controlled substance (soft delete).
    /// Only ComplianceManager role can deactivate substances per FR-031.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPost("{substanceCode}/deactivate")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateSubstance(string substanceCode, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeactivateAsync(substanceCode, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.SUBSTANCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Controlled substance with code '{substanceCode}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deactivated controlled substance {SubstanceCode}", substanceCode);

        var updated = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Reactivates a previously deactivated controlled substance.
    /// Only ComplianceManager role can reactivate substances per FR-031.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPost("{substanceCode}/reactivate")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateSubstance(string substanceCode, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.ReactivateAsync(substanceCode, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.SUBSTANCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Controlled substance with code '{substanceCode}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Reactivated controlled substance {SubstanceCode}", substanceCode);

        var updated = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }
}

#region DTOs

/// <summary>
/// Controlled substance response DTO for API responses.
/// </summary>
public class ControlledSubstanceResponseDto
{
    public required string SubstanceCode { get; set; }
    public required string SubstanceName { get; set; }
    public SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }
    public Guid ComplianceExtensionId { get; set; }
    public bool IsComplianceConfigured { get; set; }
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? ClassificationEffectiveDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static ControlledSubstanceResponseDto FromDomainModel(ControlledSubstance substance)
    {
        return new ControlledSubstanceResponseDto
        {
            SubstanceCode = substance.SubstanceCode,
            SubstanceName = substance.SubstanceName,
            OpiumActList = substance.OpiumActList,
            PrecursorCategory = substance.PrecursorCategory,
            ComplianceExtensionId = substance.ComplianceExtensionId,
            IsComplianceConfigured = substance.IsComplianceConfigured,
            RegulatoryRestrictions = substance.RegulatoryRestrictions,
            IsActive = substance.IsActive,
            ClassificationEffectiveDate = substance.ClassificationEffectiveDate,
            CreatedDate = substance.CreatedDate,
            ModifiedDate = substance.ModifiedDate
        };
    }
}

/// <summary>
/// Request DTO for configuring compliance extension on a D365-discovered substance.
/// </summary>
public class ConfigureComplianceRequestDto
{
    public required string SubstanceCode { get; set; }
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? ClassificationEffectiveDate { get; set; }
}

/// <summary>
/// Request DTO for updating a compliance extension on a substance.
/// </summary>
public class UpdateComplianceRequestDto
{
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? ClassificationEffectiveDate { get; set; }
}

#endregion
