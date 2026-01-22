using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Controlled substance master list management API endpoints.
/// T073c: ControlledSubstancesController v1 with GET, POST, PUT, DELETE endpoints per FR-003.
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
    /// <param name="search">Search term for name or internal code.</param>
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
    /// Gets a specific controlled substance by ID.
    /// </summary>
    /// <param name="id">Substance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The substance details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubstance(Guid id, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetByIdAsync(id, cancellationToken);

        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(ControlledSubstanceResponseDto.FromDomainModel(substance));
    }

    /// <summary>
    /// Gets a controlled substance by internal code.
    /// </summary>
    /// <param name="code">The internal code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The substance details.</returns>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubstanceByCode(string code, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetByInternalCodeAsync(code, cancellationToken);

        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with code '{code}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(ControlledSubstanceResponseDto.FromDomainModel(substance));
    }

    /// <summary>
    /// Creates a new controlled substance.
    /// Only ComplianceManager role can create substances per FR-031.
    /// </summary>
    /// <param name="request">Substance creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created substance details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubstance(
        [FromBody] CreateControlledSubstanceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var substance = request.ToDomainModel();

        var (id, result) = await _substanceService.CreateAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Substance validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        substance.SubstanceId = id!.Value;

        _logger.LogInformation("Created controlled substance {Name} ({Code}) with ID {Id}",
            substance.SubstanceName, substance.InternalCode, id);

        return CreatedAtAction(
            nameof(GetSubstance),
            new { id = id },
            ControlledSubstanceResponseDto.FromDomainModel(substance));
    }

    /// <summary>
    /// Updates an existing controlled substance.
    /// Only ComplianceManager role can modify substances per FR-031.
    /// </summary>
    /// <param name="id">Substance ID.</param>
    /// <param name="request">Updated substance data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubstance(
        Guid id,
        [FromBody] UpdateControlledSubstanceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var substance = request.ToDomainModel(id);

        var result = await _substanceService.UpdateAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.SUBSTANCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Controlled substance with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = "Substance validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Updated controlled substance {Id}", id);

        var updated = await _substanceService.GetByIdAsync(id, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a controlled substance.
    /// Only ComplianceManager role can delete substances per FR-031.
    /// </summary>
    /// <param name="id">Substance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubstance(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                Message = $"Controlled substance with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted controlled substance {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Deactivates a controlled substance (soft delete).
    /// Only ComplianceManager role can deactivate substances per FR-031.
    /// </summary>
    /// <param name="id">Substance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateSubstance(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeactivateAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.SUBSTANCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Controlled substance with ID '{id}' not found",
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

        _logger.LogInformation("Deactivated controlled substance {Id}", id);

        var updated = await _substanceService.GetByIdAsync(id, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Reactivates a previously deactivated controlled substance.
    /// Only ComplianceManager role can reactivate substances per FR-031.
    /// </summary>
    /// <param name="id">Substance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated substance details.</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ControlledSubstanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateSubstance(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.ReactivateAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.SUBSTANCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Controlled substance with ID '{id}' not found",
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

        _logger.LogInformation("Reactivated controlled substance {Id}", id);

        var updated = await _substanceService.GetByIdAsync(id, cancellationToken);
        return Ok(ControlledSubstanceResponseDto.FromDomainModel(updated!));
    }
}

#region DTOs

/// <summary>
/// Controlled substance response DTO for API responses.
/// </summary>
public class ControlledSubstanceResponseDto
{
    public Guid SubstanceId { get; set; }
    public required string SubstanceName { get; set; }
    public required string InternalCode { get; set; }
    public SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? ClassificationEffectiveDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static ControlledSubstanceResponseDto FromDomainModel(ControlledSubstance substance)
    {
        return new ControlledSubstanceResponseDto
        {
            SubstanceId = substance.SubstanceId,
            SubstanceName = substance.SubstanceName,
            InternalCode = substance.InternalCode,
            OpiumActList = substance.OpiumActList,
            PrecursorCategory = substance.PrecursorCategory,
            RegulatoryRestrictions = substance.RegulatoryRestrictions,
            IsActive = substance.IsActive,
            ClassificationEffectiveDate = substance.ClassificationEffectiveDate,
            CreatedDate = substance.CreatedDate,
            ModifiedDate = substance.ModifiedDate
        };
    }
}

/// <summary>
/// Request DTO for creating a new controlled substance.
/// </summary>
public class CreateControlledSubstanceRequestDto
{
    public required string SubstanceName { get; set; }
    public required string InternalCode { get; set; }
    public SubstanceCategories.OpiumActList OpiumActList { get; set; } = SubstanceCategories.OpiumActList.None;
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; } = SubstanceCategories.PrecursorCategory.None;
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? ClassificationEffectiveDate { get; set; }

    public ControlledSubstance ToDomainModel()
    {
        return new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = SubstanceName,
            InternalCode = InternalCode,
            OpiumActList = OpiumActList,
            PrecursorCategory = PrecursorCategory,
            RegulatoryRestrictions = RegulatoryRestrictions,
            IsActive = IsActive,
            ClassificationEffectiveDate = ClassificationEffectiveDate
        };
    }
}

/// <summary>
/// Request DTO for updating a controlled substance.
/// </summary>
public class UpdateControlledSubstanceRequestDto
{
    public required string SubstanceName { get; set; }
    public required string InternalCode { get; set; }
    public SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? ClassificationEffectiveDate { get; set; }

    public ControlledSubstance ToDomainModel(Guid substanceId)
    {
        return new ControlledSubstance
        {
            SubstanceId = substanceId,
            SubstanceName = SubstanceName,
            InternalCode = InternalCode,
            OpiumActList = OpiumActList,
            PrecursorCategory = PrecursorCategory,
            RegulatoryRestrictions = RegulatoryRestrictions,
            IsActive = IsActive,
            ClassificationEffectiveDate = ClassificationEffectiveDate
        };
    }
}

#endregion
