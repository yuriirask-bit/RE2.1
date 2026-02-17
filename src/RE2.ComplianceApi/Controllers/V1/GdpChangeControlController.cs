using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP Change Control API endpoints.
/// T290: REST API per US12 (FR-051).
/// </summary>
[ApiController]
[Route("api/v1/gdp-changes")]
[Authorize]
public class GdpChangeControlController : ControllerBase
{
    private readonly IGdpChangeRepository _changeRepository;
    private readonly ILogger<GdpChangeControlController> _logger;

    public GdpChangeControlController(
        IGdpChangeRepository changeRepository,
        ILogger<GdpChangeControlController> logger)
    {
        _changeRepository = changeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all change records.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GdpChangeRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChangeRecords(CancellationToken cancellationToken = default)
    {
        var records = await _changeRepository.GetAllAsync(cancellationToken);
        return Ok(records.Select(GdpChangeRecordResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific change record.
    /// </summary>
    [HttpGet("{changeId:guid}")]
    [ProducesResponseType(typeof(GdpChangeRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChangeRecord(Guid changeId, CancellationToken cancellationToken = default)
    {
        var record = await _changeRepository.GetByIdAsync(changeId, cancellationToken);
        if (record == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"Change record '{changeId}' not found" });
        }
        return Ok(GdpChangeRecordResponseDto.FromDomain(record));
    }

    /// <summary>
    /// Gets pending change records.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<GdpChangeRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingChanges(CancellationToken cancellationToken = default)
    {
        var records = await _changeRepository.GetPendingAsync(cancellationToken);
        return Ok(records.Select(GdpChangeRecordResponseDto.FromDomain));
    }

    /// <summary>
    /// Creates a new change record.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpChangeRecordResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChangeRecord(
        [FromBody] CreateChangeRecordRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = request.ToDomain();
        var validationResult = record.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = validationResult.Violations.First().ErrorCode,
                Message = string.Join("; ", validationResult.Violations.Select(v => v.Message))
            });
        }

        var id = await _changeRepository.CreateAsync(record, cancellationToken);
        record.ChangeRecordId = id;
        return CreatedAtAction(nameof(GetChangeRecord), new { changeId = id }, GdpChangeRecordResponseDto.FromDomain(record));
    }

    /// <summary>
    /// Approves a change record.
    /// </summary>
    [HttpPost("{changeId:guid}/approve")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveChange(Guid changeId, [FromBody] ApproveRejectRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _changeRepository.GetByIdAsync(changeId, cancellationToken);
        if (record == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"Change record '{changeId}' not found" });
        }

        await _changeRepository.ApproveAsync(changeId, request.UserId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Rejects a change record.
    /// </summary>
    [HttpPost("{changeId:guid}/reject")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectChange(Guid changeId, [FromBody] ApproveRejectRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _changeRepository.GetByIdAsync(changeId, cancellationToken);
        if (record == null)
        {
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = $"Change record '{changeId}' not found" });
        }

        await _changeRepository.RejectAsync(changeId, request.UserId, cancellationToken);
        return NoContent();
    }
}

#region DTOs

public class GdpChangeRecordResponseDto
{
    public Guid ChangeRecordId { get; set; }
    public string ChangeNumber { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RiskAssessment { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public Guid? ApprovedBy { get; set; }
    public DateOnly? ApprovalDate { get; set; }
    public DateOnly? ImplementationDate { get; set; }
    public string? UpdatedDocumentationRefs { get; set; }
    public DateTime CreatedDate { get; set; }

    public static GdpChangeRecordResponseDto FromDomain(GdpChangeRecord r) => new()
    {
        ChangeRecordId = r.ChangeRecordId,
        ChangeNumber = r.ChangeNumber,
        ChangeType = r.ChangeType.ToString(),
        Description = r.Description,
        RiskAssessment = r.RiskAssessment,
        ApprovalStatus = r.ApprovalStatus.ToString(),
        ApprovedBy = r.ApprovedBy,
        ApprovalDate = r.ApprovalDate,
        ImplementationDate = r.ImplementationDate,
        UpdatedDocumentationRefs = r.UpdatedDocumentationRefs,
        CreatedDate = r.CreatedDate
    };
}

public class CreateChangeRecordRequestDto
{
    public string ChangeNumber { get; set; } = string.Empty;
    public GdpChangeType ChangeType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RiskAssessment { get; set; }

    public GdpChangeRecord ToDomain() => new()
    {
        ChangeNumber = ChangeNumber,
        ChangeType = ChangeType,
        Description = Description,
        RiskAssessment = RiskAssessment,
        ApprovalStatus = ChangeApprovalStatus.Pending
    };
}

public class ApproveRejectRequest
{
    public Guid UserId { get; set; }
}

#endregion
