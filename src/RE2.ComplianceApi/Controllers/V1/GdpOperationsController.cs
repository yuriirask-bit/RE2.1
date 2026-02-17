using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP Operational Checks and Equipment Qualification API endpoints.
/// T264: REST API per US11 (FR-046, FR-047, FR-048).
/// </summary>
[ApiController]
[Route("api/v1/gdp-operations")]
[Authorize]
public class GdpOperationsController : ControllerBase
{
    private readonly IGdpOperationalService _operationalService;
    private readonly ILogger<GdpOperationsController> _logger;

    public GdpOperationsController(IGdpOperationalService operationalService, ILogger<GdpOperationsController> logger)
    {
        _operationalService = operationalService;
        _logger = logger;
    }

    #region Site Validation (FR-046)

    /// <summary>
    /// Validates whether a warehouse is eligible for GDP-regulated operations.
    /// </summary>
    [HttpPost("validate/site-assignment")]
    [ProducesResponseType(typeof(SiteAssignmentValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateSiteAssignment(
        [FromBody] SiteAssignmentValidationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.WarehouseId) || string.IsNullOrWhiteSpace(request.DataAreaId))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "WarehouseId and DataAreaId are required"
            });
        }

        var (isAllowed, reason) = await _operationalService.ValidateSiteAssignmentAsync(
            request.WarehouseId, request.DataAreaId, cancellationToken);

        return Ok(new SiteAssignmentValidationResponse
        {
            WarehouseId = request.WarehouseId,
            DataAreaId = request.DataAreaId,
            IsAllowed = isAllowed,
            Reason = reason
        });
    }

    #endregion

    #region Provider Validation (FR-046/FR-047)

    /// <summary>
    /// Validates whether a service provider is eligible for GDP operations.
    /// </summary>
    [HttpPost("validate/provider-assignment")]
    [ProducesResponseType(typeof(ProviderAssignmentValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateProviderAssignment(
        [FromBody] ProviderAssignmentValidationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProviderId == Guid.Empty)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ProviderId is required"
            });
        }

        var (isAllowed, reason) = await _operationalService.ValidateProviderAssignmentAsync(
            request.ProviderId, cancellationToken);

        return Ok(new ProviderAssignmentValidationResponse
        {
            ProviderId = request.ProviderId,
            IsAllowed = isAllowed,
            Reason = reason
        });
    }

    /// <summary>
    /// Gets approved providers, optionally filtered by temperature-controlled capability.
    /// </summary>
    [HttpGet("approved-providers")]
    [ProducesResponseType(typeof(IEnumerable<ApprovedProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovedProviders(
        [FromQuery] bool? tempControlled = null, CancellationToken cancellationToken = default)
    {
        var providers = await _operationalService.GetApprovedProvidersAsync(tempControlled, cancellationToken);
        return Ok(providers.Select(ApprovedProviderDto.FromDomain));
    }

    #endregion

    #region Equipment Qualifications (FR-048)

    /// <summary>
    /// Gets all equipment qualifications.
    /// </summary>
    [HttpGet("equipment")]
    [ProducesResponseType(typeof(IEnumerable<EquipmentQualificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEquipment(CancellationToken cancellationToken = default)
    {
        var equipment = await _operationalService.GetAllEquipmentAsync(cancellationToken);
        return Ok(equipment.Select(EquipmentQualificationResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific equipment qualification.
    /// </summary>
    [HttpGet("equipment/{equipmentId:guid}")]
    [ProducesResponseType(typeof(EquipmentQualificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEquipmentById(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var equipment = await _operationalService.GetEquipmentAsync(equipmentId, cancellationToken);
        if (equipment == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Equipment qualification '{equipmentId}' not found"
            });
        }

        return Ok(EquipmentQualificationResponseDto.FromDomain(equipment));
    }

    /// <summary>
    /// Gets equipment due for re-qualification.
    /// </summary>
    [HttpGet("equipment/due-for-requalification")]
    [ProducesResponseType(typeof(IEnumerable<EquipmentQualificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEquipmentDueForRequalification(
        [FromQuery] int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var equipment = await _operationalService.GetEquipmentDueForRequalificationAsync(daysAhead, cancellationToken);
        return Ok(equipment.Select(EquipmentQualificationResponseDto.FromDomain));
    }

    /// <summary>
    /// Creates a new equipment qualification.
    /// </summary>
    [HttpPost("equipment")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(EquipmentQualificationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEquipment(
        [FromBody] CreateEquipmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var equipment = request.ToDomain();
        var (id, result) = await _operationalService.CreateEquipmentAsync(equipment, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = result.Violations.First().ErrorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        equipment.EquipmentQualificationId = id!.Value;
        return CreatedAtAction(nameof(GetEquipmentById), new { equipmentId = id }, EquipmentQualificationResponseDto.FromDomain(equipment));
    }

    /// <summary>
    /// Updates an existing equipment qualification.
    /// </summary>
    [HttpPut("equipment/{equipmentId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEquipment(
        Guid equipmentId, [FromBody] CreateEquipmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var equipment = request.ToDomain();
        equipment.EquipmentQualificationId = equipmentId;
        var result = await _operationalService.UpdateEquipmentAsync(equipment, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes an equipment qualification.
    /// </summary>
    [HttpDelete("equipment/{equipmentId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEquipment(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var result = await _operationalService.DeleteEquipmentAsync(equipmentId, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = result.Violations.First().ErrorCode,
                Message = result.Violations.First().Message
            });
        }

        return NoContent();
    }

    #endregion
}

#region DTOs

public class SiteAssignmentValidationRequest
{
    public string WarehouseId { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
}

public class SiteAssignmentValidationResponse
{
    public string WarehouseId { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ProviderAssignmentValidationRequest
{
    public Guid ProviderId { get; set; }
}

public class ProviderAssignmentValidationResponse
{
    public Guid ProviderId { get; set; }
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ApprovedProviderDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public bool TemperatureControlledCapability { get; set; }

    public static ApprovedProviderDto FromDomain(GdpServiceProvider provider) => new()
    {
        ProviderId = provider.ProviderId,
        ProviderName = provider.ProviderName,
        ProviderType = provider.ServiceType.ToString(),
        TemperatureControlledCapability = provider.TemperatureControlledCapability
    };
}

public class EquipmentQualificationResponseDto
{
    public Guid EquipmentQualificationId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string EquipmentType { get; set; } = string.Empty;
    public Guid? ProviderId { get; set; }
    public Guid? SiteId { get; set; }
    public DateOnly QualificationDate { get; set; }
    public DateOnly? RequalificationDueDate { get; set; }
    public string QualificationStatus { get; set; } = string.Empty;
    public string QualifiedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsExpired { get; set; }

    public static EquipmentQualificationResponseDto FromDomain(GdpEquipmentQualification e) => new()
    {
        EquipmentQualificationId = e.EquipmentQualificationId,
        EquipmentName = e.EquipmentName,
        EquipmentType = e.EquipmentType.ToString(),
        ProviderId = e.ProviderId,
        SiteId = e.SiteId,
        QualificationDate = e.QualificationDate,
        RequalificationDueDate = e.RequalificationDueDate,
        QualificationStatus = e.QualificationStatus.ToString(),
        QualifiedBy = e.QualifiedBy,
        Notes = e.Notes,
        IsExpired = e.IsExpired()
    };
}

public class CreateEquipmentRequestDto
{
    public string EquipmentName { get; set; } = string.Empty;
    public GdpEquipmentType EquipmentType { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? SiteId { get; set; }
    public DateOnly QualificationDate { get; set; }
    public DateOnly? RequalificationDueDate { get; set; }
    public GdpQualificationStatusType QualificationStatus { get; set; }
    public string QualifiedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public GdpEquipmentQualification ToDomain() => new()
    {
        EquipmentName = EquipmentName,
        EquipmentType = EquipmentType,
        ProviderId = ProviderId,
        SiteId = SiteId,
        QualificationDate = QualificationDate,
        RequalificationDueDate = RequalificationDueDate,
        QualificationStatus = QualificationStatus,
        QualifiedBy = QualifiedBy,
        Notes = Notes
    };
}

#endregion
