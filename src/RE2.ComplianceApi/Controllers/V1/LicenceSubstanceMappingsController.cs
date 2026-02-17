using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Licence-substance mapping API endpoints.
/// T079e: LicenceSubstanceMappingsController v1 with GET, POST, PUT, DELETE endpoints per FR-004.
/// Manages which controlled substances are authorized under each licence.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LicenceSubstanceMappingsController : ControllerBase
{
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly ILogger<LicenceSubstanceMappingsController> _logger;

    public LicenceSubstanceMappingsController(
        ILicenceSubstanceMappingService mappingService,
        ILogger<LicenceSubstanceMappingsController> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all licence-substance mappings with optional filtering.
    /// </summary>
    /// <param name="licenceId">Optional filter by licence ID.</param>
    /// <param name="substanceCode">Optional filter by substance code.</param>
    /// <param name="activeOnly">If true, returns only currently active mappings (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of mappings.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LicenceSubstanceMappingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMappings(
        [FromQuery] Guid? licenceId = null,
        [FromQuery] string? substanceCode = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<LicenceSubstanceMapping> mappings;

        if (licenceId.HasValue && activeOnly)
        {
            mappings = await _mappingService.GetActiveMappingsByLicenceIdAsync(licenceId.Value, cancellationToken);
        }
        else if (licenceId.HasValue)
        {
            mappings = await _mappingService.GetByLicenceIdAsync(licenceId.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(substanceCode))
        {
            mappings = await _mappingService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        }
        else
        {
            mappings = await _mappingService.GetAllAsync(cancellationToken);
        }

        var response = mappings.Select(LicenceSubstanceMappingResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific mapping by ID.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LicenceSubstanceMappingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMapping(Guid id, CancellationToken cancellationToken = default)
    {
        var mapping = await _mappingService.GetByIdAsync(id, cancellationToken);

        if (mapping == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.MAPPING_NOT_FOUND,
                Message = $"Mapping with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceSubstanceMappingResponseDto.FromDomainModel(mapping));
    }

    /// <summary>
    /// Checks if a substance is authorized under a licence.
    /// Used by transaction validation per FR-018.
    /// </summary>
    /// <param name="licenceId">Licence ID.</param>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization status.</returns>
    [HttpGet("check-authorization")]
    [ProducesResponseType(typeof(SubstanceAuthorizationCheckDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckSubstanceAuthorization(
        [FromQuery] Guid licenceId,
        [FromQuery] string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var isAuthorized = await _mappingService.IsSubstanceAuthorizedByLicenceAsync(
            licenceId, substanceCode, cancellationToken);

        return Ok(new SubstanceAuthorizationCheckDto
        {
            LicenceId = licenceId,
            SubstanceCode = substanceCode,
            IsAuthorized = isAuthorized
        });
    }

    /// <summary>
    /// Creates a new licence-substance mapping.
    /// T080: Only ComplianceManager role can create mappings.
    /// Validates per data-model.md:
    /// - LicenceId + SubstanceId + EffectiveDate must be unique
    /// - ExpiryDate must not exceed licence's ExpiryDate
    /// </summary>
    /// <param name="request">Mapping creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created mapping details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceSubstanceMappingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMapping(
        [FromBody] CreateLicenceSubstanceMappingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var mapping = request.ToDomainModel();
        var (id, result) = await _mappingService.CreateAsync(mapping, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Mapping validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var created = await _mappingService.GetByIdAsync(id!.Value, cancellationToken);
        _logger.LogInformation("Created mapping {Id} for licence {LicenceId} and substance {SubstanceCode}",
            id, created!.LicenceId, created.SubstanceCode);

        return CreatedAtAction(
            nameof(GetMapping),
            new { id = id },
            LicenceSubstanceMappingResponseDto.FromDomainModel(created));
    }

    /// <summary>
    /// Updates an existing licence-substance mapping.
    /// T080: Only ComplianceManager role can modify mappings.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="request">Updated mapping data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated mapping details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceSubstanceMappingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMapping(
        Guid id,
        [FromBody] UpdateLicenceSubstanceMappingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var mapping = request.ToDomainModel(id);
        var result = await _mappingService.UpdateAsync(mapping, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.NOT_FOUND || errorCode == ErrorCodes.MAPPING_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.MAPPING_NOT_FOUND,
                    Message = $"Mapping with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Mapping validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var updated = await _mappingService.GetByIdAsync(id, cancellationToken);
        _logger.LogInformation("Updated mapping {Id}", id);

        return Ok(LicenceSubstanceMappingResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a licence-substance mapping.
    /// T080: Only ComplianceManager role can delete mappings.
    /// </summary>
    /// <param name="id">Mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMapping(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mappingService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.MAPPING_NOT_FOUND,
                Message = $"Mapping with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted mapping {Id}", id);
        return NoContent();
    }
}

#region DTOs

/// <summary>
/// Licence-substance mapping response DTO for API responses.
/// </summary>
public class LicenceSubstanceMappingResponseDto
{
    public Guid MappingId { get; set; }
    public Guid LicenceId { get; set; }
    public string? LicenceNumber { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public string? SubstanceName { get; set; }
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? Restrictions { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsActive { get; set; }

    public static LicenceSubstanceMappingResponseDto FromDomainModel(LicenceSubstanceMapping mapping)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isActive = mapping.EffectiveDate <= today &&
                       (!mapping.ExpiryDate.HasValue || mapping.ExpiryDate.Value >= today);

        return new LicenceSubstanceMappingResponseDto
        {
            MappingId = mapping.MappingId,
            LicenceId = mapping.LicenceId,
            LicenceNumber = mapping.Licence?.LicenceNumber,
            SubstanceCode = mapping.SubstanceCode,
            SubstanceName = mapping.Substance?.SubstanceName,
            MaxQuantityPerTransaction = mapping.MaxQuantityPerTransaction,
            MaxQuantityPerPeriod = mapping.MaxQuantityPerPeriod,
            PeriodType = mapping.PeriodType,
            Restrictions = mapping.Restrictions,
            EffectiveDate = mapping.EffectiveDate,
            ExpiryDate = mapping.ExpiryDate,
            IsActive = isActive
        };
    }
}

/// <summary>
/// Request DTO for creating a new licence-substance mapping.
/// </summary>
public class CreateLicenceSubstanceMappingRequestDto
{
    public Guid LicenceId { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? Restrictions { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public LicenceSubstanceMapping ToDomainModel()
    {
        return new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = LicenceId,
            SubstanceCode = SubstanceCode,
            MaxQuantityPerTransaction = MaxQuantityPerTransaction,
            MaxQuantityPerPeriod = MaxQuantityPerPeriod,
            PeriodType = PeriodType,
            Restrictions = Restrictions,
            EffectiveDate = EffectiveDate,
            ExpiryDate = ExpiryDate
        };
    }
}

/// <summary>
/// Request DTO for updating a licence-substance mapping.
/// </summary>
public class UpdateLicenceSubstanceMappingRequestDto
{
    public Guid LicenceId { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? Restrictions { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public LicenceSubstanceMapping ToDomainModel(Guid mappingId)
    {
        return new LicenceSubstanceMapping
        {
            MappingId = mappingId,
            LicenceId = LicenceId,
            SubstanceCode = SubstanceCode,
            MaxQuantityPerTransaction = MaxQuantityPerTransaction,
            MaxQuantityPerPeriod = MaxQuantityPerPeriod,
            PeriodType = PeriodType,
            Restrictions = Restrictions,
            EffectiveDate = EffectiveDate,
            ExpiryDate = ExpiryDate
        };
    }
}

/// <summary>
/// Response DTO for substance authorization check.
/// </summary>
public class SubstanceAuthorizationCheckDto
{
    public Guid LicenceId { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
}

#endregion
