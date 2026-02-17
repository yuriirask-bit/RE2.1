using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP Inspections, Findings, and CAPA API endpoints.
/// T225: REST API for GDP inspection and CAPA management per User Story 9 (FR-040, FR-041, FR-042).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GdpInspectionsController : ControllerBase
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpInspectionsController> _logger;

    public GdpInspectionsController(IGdpComplianceService gdpService, ILogger<GdpInspectionsController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    #region Inspections

    /// <summary>
    /// Gets all GDP inspections.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GdpInspectionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInspections(CancellationToken cancellationToken = default)
    {
        var inspections = await _gdpService.GetAllInspectionsAsync(cancellationToken);
        return Ok(inspections.Select(GdpInspectionResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific GDP inspection by ID.
    /// </summary>
    [HttpGet("{inspectionId:guid}")]
    [ProducesResponseType(typeof(GdpInspectionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInspection(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var inspection = await _gdpService.GetInspectionAsync(inspectionId, cancellationToken);
        if (inspection == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"GDP inspection '{inspectionId}' not found"
            });
        }

        return Ok(GdpInspectionResponseDto.FromDomain(inspection));
    }

    /// <summary>
    /// Gets inspections for a specific GDP site.
    /// </summary>
    [HttpGet("by-site/{siteId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<GdpInspectionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInspectionsBySite(Guid siteId, CancellationToken cancellationToken = default)
    {
        var inspections = await _gdpService.GetInspectionsBySiteAsync(siteId, cancellationToken);
        return Ok(inspections.Select(GdpInspectionResponseDto.FromDomain));
    }

    /// <summary>
    /// Creates a new GDP inspection.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpInspectionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInspection([FromBody] CreateInspectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var inspection = request.ToDomain();
        var (id, result) = await _gdpService.CreateInspectionAsync(inspection, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetInspectionAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetInspection), new { inspectionId = id }, GdpInspectionResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Updates a GDP inspection.
    /// </summary>
    [HttpPut("{inspectionId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpInspectionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInspection(Guid inspectionId, [FromBody] CreateInspectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var inspection = request.ToDomain();
        inspection.InspectionId = inspectionId;

        var result = await _gdpService.UpdateInspectionAsync(inspection, cancellationToken);
        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var updated = await _gdpService.GetInspectionAsync(inspectionId, cancellationToken);
        return Ok(GdpInspectionResponseDto.FromDomain(updated!));
    }

    #endregion

    #region Findings

    /// <summary>
    /// Gets findings for an inspection.
    /// </summary>
    [HttpGet("{inspectionId:guid}/findings")]
    [ProducesResponseType(typeof(IEnumerable<FindingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFindings(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var findings = await _gdpService.GetFindingsAsync(inspectionId, cancellationToken);
        return Ok(findings.Select(FindingResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific finding by ID.
    /// </summary>
    [HttpGet("findings/{findingId:guid}")]
    [ProducesResponseType(typeof(FindingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinding(Guid findingId, CancellationToken cancellationToken = default)
    {
        var finding = await _gdpService.GetFindingAsync(findingId, cancellationToken);
        if (finding == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Finding '{findingId}' not found"
            });
        }

        return Ok(FindingResponseDto.FromDomain(finding));
    }

    /// <summary>
    /// Creates a finding for an inspection.
    /// </summary>
    [HttpPost("findings")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(FindingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFinding([FromBody] CreateFindingRequestDto request, CancellationToken cancellationToken = default)
    {
        var finding = request.ToDomain();
        var (id, result) = await _gdpService.CreateFindingAsync(finding, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetFindingAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetFinding), new { findingId = id }, FindingResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Deletes a finding.
    /// </summary>
    [HttpDelete("findings/{findingId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFinding(Guid findingId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.DeleteFindingAsync(findingId, cancellationToken);
        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        return NoContent();
    }

    #endregion

    #region CAPAs

    /// <summary>
    /// Gets all CAPAs.
    /// </summary>
    [HttpGet("capas")]
    [ProducesResponseType(typeof(IEnumerable<CapaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapas(CancellationToken cancellationToken = default)
    {
        var capas = await _gdpService.GetAllCapasAsync(cancellationToken);
        return Ok(capas.Select(CapaResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific CAPA by ID.
    /// </summary>
    [HttpGet("capas/{capaId:guid}")]
    [ProducesResponseType(typeof(CapaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapa(Guid capaId, CancellationToken cancellationToken = default)
    {
        var capa = await _gdpService.GetCapaAsync(capaId, cancellationToken);
        if (capa == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"CAPA '{capaId}' not found"
            });
        }

        return Ok(CapaResponseDto.FromDomain(capa));
    }

    /// <summary>
    /// Gets CAPAs for a specific finding.
    /// </summary>
    [HttpGet("capas/by-finding/{findingId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CapaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapasByFinding(Guid findingId, CancellationToken cancellationToken = default)
    {
        var capas = await _gdpService.GetCapasByFindingAsync(findingId, cancellationToken);
        return Ok(capas.Select(CapaResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets overdue CAPAs.
    /// Per FR-042.
    /// </summary>
    [HttpGet("capas/overdue")]
    [ProducesResponseType(typeof(IEnumerable<CapaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueCapas(CancellationToken cancellationToken = default)
    {
        var capas = await _gdpService.GetOverdueCapasAsync(cancellationToken);
        return Ok(capas.Select(CapaResponseDto.FromDomain));
    }

    /// <summary>
    /// Creates a new CAPA.
    /// </summary>
    [HttpPost("capas")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(CapaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCapa([FromBody] CreateCapaRequestDto request, CancellationToken cancellationToken = default)
    {
        var capa = request.ToDomain();
        var (id, result) = await _gdpService.CreateCapaAsync(capa, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetCapaAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetCapa), new { capaId = id }, CapaResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Updates a CAPA.
    /// </summary>
    [HttpPut("capas/{capaId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(CapaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCapa(Guid capaId, [FromBody] CreateCapaRequestDto request, CancellationToken cancellationToken = default)
    {
        var capa = request.ToDomain();
        capa.CapaId = capaId;

        var result = await _gdpService.UpdateCapaAsync(capa, cancellationToken);
        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var updated = await _gdpService.GetCapaAsync(capaId, cancellationToken);
        return Ok(CapaResponseDto.FromDomain(updated!));
    }

    /// <summary>
    /// Completes a CAPA with verification notes.
    /// Per FR-041.
    /// </summary>
    [HttpPost("capas/{capaId:guid}/complete")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(CapaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteCapa(Guid capaId, [FromBody] CompleteCapaRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.CompleteCapaAsync(capaId, request.CompletionDate, request.VerificationNotes, cancellationToken);
        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var completed = await _gdpService.GetCapaAsync(capaId, cancellationToken);
        return Ok(CapaResponseDto.FromDomain(completed!));
    }

    #endregion
}

#region DTOs

/// <summary>
/// Response DTO for GDP inspection.
/// </summary>
public class GdpInspectionResponseDto
{
    public Guid InspectionId { get; set; }
    public DateOnly InspectionDate { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public GdpInspectionType InspectionType { get; set; }
    public Guid SiteId { get; set; }
    public Guid? WdaLicenceId { get; set; }
    public string? FindingsSummary { get; set; }
    public string? ReportReferenceUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static GdpInspectionResponseDto FromDomain(GdpInspection inspection) => new()
    {
        InspectionId = inspection.InspectionId,
        InspectionDate = inspection.InspectionDate,
        InspectorName = inspection.InspectorName,
        InspectionType = inspection.InspectionType,
        SiteId = inspection.SiteId,
        WdaLicenceId = inspection.WdaLicenceId,
        FindingsSummary = inspection.FindingsSummary,
        ReportReferenceUrl = inspection.ReportReferenceUrl,
        CreatedDate = inspection.CreatedDate,
        ModifiedDate = inspection.ModifiedDate
    };
}

/// <summary>
/// Request DTO for creating/updating a GDP inspection.
/// </summary>
public class CreateInspectionRequestDto
{
    public DateOnly InspectionDate { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public GdpInspectionType InspectionType { get; set; }
    public Guid SiteId { get; set; }
    public Guid? WdaLicenceId { get; set; }
    public string? FindingsSummary { get; set; }
    public string? ReportReferenceUrl { get; set; }

    public GdpInspection ToDomain() => new()
    {
        InspectionDate = InspectionDate,
        InspectorName = InspectorName,
        InspectionType = InspectionType,
        SiteId = SiteId,
        WdaLicenceId = WdaLicenceId,
        FindingsSummary = FindingsSummary,
        ReportReferenceUrl = ReportReferenceUrl
    };
}

/// <summary>
/// Response DTO for GDP inspection finding.
/// </summary>
public class FindingResponseDto
{
    public Guid FindingId { get; set; }
    public Guid InspectionId { get; set; }
    public string FindingDescription { get; set; } = string.Empty;
    public FindingClassification Classification { get; set; }
    public string? FindingNumber { get; set; }
    public bool IsCritical { get; set; }

    public static FindingResponseDto FromDomain(GdpInspectionFinding finding) => new()
    {
        FindingId = finding.FindingId,
        InspectionId = finding.InspectionId,
        FindingDescription = finding.FindingDescription,
        Classification = finding.Classification,
        FindingNumber = finding.FindingNumber,
        IsCritical = finding.IsCritical()
    };
}

/// <summary>
/// Request DTO for creating a finding.
/// </summary>
public class CreateFindingRequestDto
{
    public Guid InspectionId { get; set; }
    public string FindingDescription { get; set; } = string.Empty;
    public FindingClassification Classification { get; set; }
    public string? FindingNumber { get; set; }

    public GdpInspectionFinding ToDomain() => new()
    {
        InspectionId = InspectionId,
        FindingDescription = FindingDescription,
        Classification = Classification,
        FindingNumber = FindingNumber
    };
}

/// <summary>
/// Response DTO for CAPA.
/// </summary>
public class CapaResponseDto
{
    public Guid CapaId { get; set; }
    public string CapaNumber { get; set; } = string.Empty;
    public Guid FindingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public CapaStatus Status { get; set; }
    public string? VerificationNotes { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static CapaResponseDto FromDomain(Capa capa) => new()
    {
        CapaId = capa.CapaId,
        CapaNumber = capa.CapaNumber,
        FindingId = capa.FindingId,
        Description = capa.Description,
        OwnerName = capa.OwnerName,
        DueDate = capa.DueDate,
        CompletionDate = capa.CompletionDate,
        Status = capa.Status,
        VerificationNotes = capa.VerificationNotes,
        IsOverdue = capa.IsOverdue(),
        CreatedDate = capa.CreatedDate,
        ModifiedDate = capa.ModifiedDate
    };
}

/// <summary>
/// Request DTO for creating/updating a CAPA.
/// </summary>
public class CreateCapaRequestDto
{
    public string CapaNumber { get; set; } = string.Empty;
    public Guid FindingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }

    public Capa ToDomain() => new()
    {
        CapaNumber = CapaNumber,
        FindingId = FindingId,
        Description = Description,
        OwnerName = OwnerName,
        DueDate = DueDate
    };
}

/// <summary>
/// Request DTO for completing a CAPA.
/// </summary>
public class CompleteCapaRequestDto
{
    public DateOnly CompletionDate { get; set; }
    public string? VerificationNotes { get; set; }
}

#endregion
