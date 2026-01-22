using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Licence management API endpoints.
/// T075: LicencesController v1 with GET, POST, PUT, DELETE endpoints.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LicencesController : ControllerBase
{
    private readonly ILicenceService _licenceService;
    private readonly ILogger<LicencesController> _logger;

    public LicencesController(ILicenceService licenceService, ILogger<LicencesController> logger)
    {
        _licenceService = licenceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all licences with optional filtering.
    /// </summary>
    /// <param name="holderId">Optional holder ID filter.</param>
    /// <param name="holderType">Optional holder type filter (Company/Customer).</param>
    /// <param name="status">Optional status filter (Valid/Expired/Suspended/Revoked).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of licences.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LicenceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicences(
        [FromQuery] Guid? holderId = null,
        [FromQuery] string? holderType = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Licence> licences;

        if (holderId.HasValue && !string.IsNullOrEmpty(holderType))
        {
            licences = await _licenceService.GetByHolderAsync(holderId.Value, holderType, cancellationToken);
        }
        else
        {
            licences = await _licenceService.GetAllAsync(cancellationToken);
        }

        // Apply status filter if provided
        if (!string.IsNullOrEmpty(status))
        {
            licences = licences.Where(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var response = licences.Select(LicenceResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific licence by ID.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicence(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceResponseDto.FromDomainModel(licence));
    }

    /// <summary>
    /// Gets a licence by licence number.
    /// </summary>
    /// <param name="licenceNumber">The official licence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence details.</returns>
    [HttpGet("by-number/{licenceNumber}")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenceByNumber(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByLicenceNumberAsync(licenceNumber, cancellationToken);

        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with number '{licenceNumber}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceResponseDto.FromDomainModel(licence));
    }

    /// <summary>
    /// Gets licences expiring within specified days.
    /// Per FR-007: Generate alerts for expiring licences.
    /// </summary>
    /// <param name="daysAhead">Number of days ahead to check (default: 90).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expiring licences.</returns>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IEnumerable<LicenceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringLicences(
        [FromQuery] int daysAhead = 90,
        CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetExpiringLicencesAsync(daysAhead, cancellationToken);
        var response = licences.Select(LicenceResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new licence.
    /// T080: Only ComplianceManager role can create licences.
    /// </summary>
    /// <param name="request">Licence creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created licence details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLicence(
        [FromBody] CreateLicenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var licence = request.ToDomainModel();
        var (id, result) = await _licenceService.CreateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Fetch the created licence with LicenceType populated
        var created = await _licenceService.GetByIdAsync(id!.Value, cancellationToken);
        _logger.LogInformation("Created licence {LicenceNumber} with ID {Id}", created!.LicenceNumber, id);

        return CreatedAtAction(
            nameof(GetLicence),
            new { id = id },
            LicenceResponseDto.FromDomainModel(created));
    }

    /// <summary>
    /// Updates an existing licence.
    /// T080: Only ComplianceManager role can modify licences.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="request">Updated licence data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated licence details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLicence(
        Guid id,
        [FromBody] UpdateLicenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var licence = request.ToDomainModel(id);
        var result = await _licenceService.UpdateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Licence with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var updated = await _licenceService.GetByIdAsync(id, cancellationToken);
        _logger.LogInformation("Updated licence {Id}", id);

        return Ok(LicenceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a licence (soft delete).
    /// T080: Only ComplianceManager role can delete licences.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLicence(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _licenceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted licence {Id}", id);
        return NoContent();
    }
}

#region DTOs

/// <summary>
/// Licence response DTO for API responses.
/// </summary>
public class LicenceResponseDto
{
    public Guid LicenceId { get; set; }
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public string? LicenceTypeName { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string Status { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsExpired { get; set; }

    public static LicenceResponseDto FromDomainModel(Licence licence)
    {
        return new LicenceResponseDto
        {
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeId = licence.LicenceTypeId,
            LicenceTypeName = licence.LicenceType?.Name,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            IssuingAuthority = licence.IssuingAuthority,
            IssueDate = licence.IssueDate,
            ExpiryDate = licence.ExpiryDate,
            Status = licence.Status,
            Scope = licence.Scope,
            PermittedActivities = (int)licence.PermittedActivities,
            CreatedDate = licence.CreatedDate,
            ModifiedDate = licence.ModifiedDate,
            IsExpired = licence.IsExpired()
        };
    }
}

/// <summary>
/// Request DTO for creating a new licence.
/// </summary>
public class CreateLicenceRequestDto
{
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = "Valid";
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }

    public Licence ToDomainModel()
    {
        return new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = LicenceNumber,
            LicenceTypeId = LicenceTypeId,
            HolderType = HolderType,
            HolderId = HolderId,
            IssuingAuthority = IssuingAuthority,
            IssueDate = IssueDate,
            ExpiryDate = ExpiryDate,
            Status = Status,
            Scope = Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities
        };
    }
}

/// <summary>
/// Request DTO for updating a licence.
/// </summary>
public class UpdateLicenceRequestDto
{
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string Status { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }

    public Licence ToDomainModel(Guid licenceId)
    {
        return new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = LicenceNumber,
            LicenceTypeId = LicenceTypeId,
            HolderType = HolderType,
            HolderId = HolderId,
            IssuingAuthority = IssuingAuthority,
            IssueDate = IssueDate,
            ExpiryDate = ExpiryDate,
            Status = Status,
            Scope = Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities
        };
    }
}

#endregion
