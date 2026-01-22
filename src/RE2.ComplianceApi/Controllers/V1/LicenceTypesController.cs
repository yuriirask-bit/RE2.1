using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Licence type management API endpoints.
/// T076: LicenceTypesController v1 with GET, POST, PUT, DELETE endpoints.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LicenceTypesController : ControllerBase
{
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILogger<LicenceTypesController> _logger;

    public LicenceTypesController(
        ILicenceTypeRepository licenceTypeRepository,
        ILogger<LicenceTypesController> logger)
    {
        _licenceTypeRepository = licenceTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all licence types.
    /// </summary>
    /// <param name="activeOnly">If true, only returns active licence types. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of licence types.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LicenceTypeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicenceTypes(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var licenceTypes = activeOnly
            ? await _licenceTypeRepository.GetAllActiveAsync(cancellationToken)
            : await _licenceTypeRepository.GetAllAsync(cancellationToken);

        var response = licenceTypes.Select(LicenceTypeResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific licence type by ID.
    /// </summary>
    /// <param name="id">Licence type ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence type details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LicenceTypeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenceType(Guid id, CancellationToken cancellationToken = default)
    {
        var licenceType = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);

        if (licenceType == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_TYPE_NOT_FOUND,
                Message = $"Licence type with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceTypeResponseDto.FromDomainModel(licenceType));
    }

    /// <summary>
    /// Gets a licence type by name.
    /// </summary>
    /// <param name="name">The licence type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence type details.</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(LicenceTypeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenceTypeByName(string name, CancellationToken cancellationToken = default)
    {
        var licenceType = await _licenceTypeRepository.GetByNameAsync(name, cancellationToken);

        if (licenceType == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_TYPE_NOT_FOUND,
                Message = $"Licence type with name '{name}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceTypeResponseDto.FromDomainModel(licenceType));
    }

    /// <summary>
    /// Creates a new licence type.
    /// Only ComplianceManager role can create licence types.
    /// </summary>
    /// <param name="request">Licence type creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created licence type details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceTypeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLicenceType(
        [FromBody] CreateLicenceTypeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var licenceType = request.ToDomainModel();

        // Validate the licence type
        var validationResult = licenceType.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence type validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate name
        var existing = await _licenceTypeRepository.GetByNameAsync(licenceType.Name, cancellationToken);
        if (existing != null)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence type with name '{licenceType.Name}' already exists",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var id = await _licenceTypeRepository.CreateAsync(licenceType, cancellationToken);
        licenceType.LicenceTypeId = id;

        _logger.LogInformation("Created licence type {Name} with ID {Id}", licenceType.Name, id);

        return CreatedAtAction(
            nameof(GetLicenceType),
            new { id = id },
            LicenceTypeResponseDto.FromDomainModel(licenceType));
    }

    /// <summary>
    /// Updates an existing licence type.
    /// Only ComplianceManager role can modify licence types.
    /// </summary>
    /// <param name="id">Licence type ID.</param>
    /// <param name="request">Updated licence type data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated licence type details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceTypeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLicenceType(
        Guid id,
        [FromBody] UpdateLicenceTypeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Check licence type exists
        var existing = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_TYPE_NOT_FOUND,
                Message = $"Licence type with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var licenceType = request.ToDomainModel(id);

        // Validate the licence type
        var validationResult = licenceType.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence type validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate name (if changed)
        if (existing.Name != licenceType.Name)
        {
            var duplicate = await _licenceTypeRepository.GetByNameAsync(licenceType.Name, cancellationToken);
            if (duplicate != null)
            {
                return BadRequest(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Licence type with name '{licenceType.Name}' already exists",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        await _licenceTypeRepository.UpdateAsync(licenceType, cancellationToken);
        _logger.LogInformation("Updated licence type {Id}", id);

        var updated = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        return Ok(LicenceTypeResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a licence type.
    /// Only ComplianceManager role can delete licence types.
    /// </summary>
    /// <param name="id">Licence type ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLicenceType(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_TYPE_NOT_FOUND,
                Message = $"Licence type with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        await _licenceTypeRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted licence type {Id}", id);

        return NoContent();
    }
}

#region DTOs

/// <summary>
/// Licence type response DTO for API responses.
/// </summary>
public class LicenceTypeResponseDto
{
    public Guid LicenceTypeId { get; set; }
    public required string Name { get; set; }
    public required string IssuingAuthority { get; set; }
    public int? TypicalValidityMonths { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsActive { get; set; }

    public static LicenceTypeResponseDto FromDomainModel(LicenceType licenceType)
    {
        return new LicenceTypeResponseDto
        {
            LicenceTypeId = licenceType.LicenceTypeId,
            Name = licenceType.Name,
            IssuingAuthority = licenceType.IssuingAuthority,
            TypicalValidityMonths = licenceType.TypicalValidityMonths,
            PermittedActivities = (int)licenceType.PermittedActivities,
            IsActive = licenceType.IsActive
        };
    }
}

/// <summary>
/// Request DTO for creating a new licence type.
/// </summary>
public class CreateLicenceTypeRequestDto
{
    public required string Name { get; set; }
    public required string IssuingAuthority { get; set; }
    public int? TypicalValidityMonths { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsActive { get; set; } = true;

    public LicenceType ToDomainModel()
    {
        return new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = Name,
            IssuingAuthority = IssuingAuthority,
            TypicalValidityMonths = TypicalValidityMonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities,
            IsActive = IsActive
        };
    }
}

/// <summary>
/// Request DTO for updating a licence type.
/// </summary>
public class UpdateLicenceTypeRequestDto
{
    public required string Name { get; set; }
    public required string IssuingAuthority { get; set; }
    public int? TypicalValidityMonths { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsActive { get; set; }

    public LicenceType ToDomainModel(Guid licenceTypeId)
    {
        return new LicenceType
        {
            LicenceTypeId = licenceTypeId,
            Name = Name,
            IssuingAuthority = IssuingAuthority,
            TypicalValidityMonths = TypicalValidityMonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities,
            IsActive = IsActive
        };
    }
}

#endregion
