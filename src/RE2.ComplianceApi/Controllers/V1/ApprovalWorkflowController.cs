using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// API endpoints for approval workflow management.
/// T176: ApprovalWorkflowController for high-risk event approvals per FR-030.
/// Integrates with Azure Logic Apps for workflow orchestration.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Authorize]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<ApprovalWorkflowController> _logger;
    private readonly IConfiguration _configuration;

    // In-memory workflow state store (production would use Dataverse or Redis)
    private static readonly Dictionary<Guid, WorkflowInstance> _workflowInstances = new();
    private static readonly object _lock = new();

    public ApprovalWorkflowController(
        IAuditRepository auditRepository,
        ILogger<ApprovalWorkflowController> logger,
        IConfiguration configuration)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Triggers a new approval workflow for a high-risk event.
    /// T176: POST /api/v1/workflows/trigger per FR-030.
    /// Only ComplianceManager role can trigger workflows.
    /// </summary>
    [HttpPost("trigger")]
    [Authorize(Policy = "ComplianceManager")]
    [ProducesResponseType(typeof(WorkflowTriggerResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerWorkflow(
        [FromBody] WorkflowTriggerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Invalid workflow trigger request"
            });
        }

        var workflowId = Guid.NewGuid();
        var userId = GetCurrentUserId();
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        var instance = new WorkflowInstance
        {
            WorkflowId = workflowId,
            EventType = request.EventType,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Details = request.Details,
            RiskLevel = request.RiskLevel,
            InitiatedBy = userId,
            InitiatedByName = userName,
            Status = WorkflowStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        lock (_lock)
        {
            _workflowInstances[workflowId] = instance;
        }

        // T176a: Create audit event for workflow trigger
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.WorkflowTriggered,
            EventDate = DateTime.UtcNow,
            PerformedBy = userId,
            PerformedByName = userName,
            EntityType = AuditEntityType.Workflow,
            EntityId = workflowId
        };
        auditEvent.SetDetails(new
        {
            request.EventType,
            request.EntityType,
            request.EntityId,
            request.Details,
            request.RiskLevel
        });

        await _auditRepository.CreateAsync(auditEvent, cancellationToken);

        _logger.LogInformation(
            "Workflow {WorkflowId} triggered by {User} for {EventType} on {EntityType}/{EntityId}",
            workflowId, userName, request.EventType, request.EntityType, request.EntityId);

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/v1/workflows/callback";

        return Accepted(new WorkflowTriggerResponseDto
        {
            WorkflowId = workflowId,
            Status = "Pending",
            Message = $"Workflow triggered for {request.EventType}. Awaiting approval.",
            CallbackUrl = callbackUrl
        });
    }

    /// <summary>
    /// Receives approval/rejection callback from Azure Logic App.
    /// T176: POST /api/v1/workflows/callback per FR-030.
    /// T176a: Updates internal approval status and generates audit events.
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous] // Logic App uses Managed Identity, validated via separate mechanism
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WorkflowCallback(
        [FromBody] WorkflowCallbackDto callback,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || callback.WorkflowId == Guid.Empty)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Invalid callback data"
            });
        }

        WorkflowInstance? instance;
        lock (_lock)
        {
            _workflowInstances.TryGetValue(callback.WorkflowId, out instance);
        }

        if (instance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = "WORKFLOW_NOT_FOUND",
                Message = $"Workflow {callback.WorkflowId} not found"
            });
        }

        // T176a: Update workflow state
        var previousStatus = instance.Status;
        instance.Status = callback.Status switch
        {
            "Approved" => WorkflowStatus.Approved,
            "Rejected" => WorkflowStatus.Rejected,
            _ => WorkflowStatus.Pending
        };
        instance.ApprovedBy = callback.ApprovedBy;
        instance.ApprovalDate = callback.ApprovalDate ?? DateTime.UtcNow;
        instance.Comments = callback.Comments;
        instance.CompletedDate = DateTime.UtcNow;

        // T176a: Generate audit event for approval/rejection
        var auditEventType = instance.Status == WorkflowStatus.Approved
            ? AuditEventType.WorkflowApproved
            : AuditEventType.WorkflowRejected;

        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = auditEventType,
            EventDate = DateTime.UtcNow,
            PerformedBy = instance.InitiatedBy, // Original initiator for traceability
            PerformedByName = callback.ApprovedBy ?? "Logic App",
            EntityType = AuditEntityType.Workflow,
            EntityId = callback.WorkflowId
        };
        auditEvent.SetDetails(new
        {
            previousStatus = previousStatus.ToString(),
            newStatus = instance.Status.ToString(),
            approvedBy = callback.ApprovedBy,
            comments = callback.Comments,
            entityType = instance.EntityType,
            entityId = instance.EntityId
        });

        await _auditRepository.CreateAsync(auditEvent, cancellationToken);

        _logger.LogInformation(
            "Workflow {WorkflowId} completed with status {Status} by {ApprovedBy}",
            callback.WorkflowId, instance.Status, callback.ApprovedBy);

        return Ok(new { workflowId = callback.WorkflowId, status = instance.Status.ToString() });
    }

    /// <summary>
    /// Gets the current status of a workflow.
    /// T176: GET /api/v1/workflows/{workflowId}/status per FR-030.
    /// </summary>
    [HttpGet("{workflowId}/status")]
    [ProducesResponseType(typeof(WorkflowStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public IActionResult GetWorkflowStatus(Guid workflowId)
    {
        WorkflowInstance? instance;
        lock (_lock)
        {
            _workflowInstances.TryGetValue(workflowId, out instance);
        }

        if (instance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = "WORKFLOW_NOT_FOUND",
                Message = $"Workflow {workflowId} not found"
            });
        }

        return Ok(new WorkflowStatusDto
        {
            WorkflowId = instance.WorkflowId,
            EventType = instance.EventType,
            EntityType = instance.EntityType,
            EntityId = instance.EntityId,
            Status = instance.Status.ToString(),
            InitiatedBy = instance.InitiatedByName,
            CreatedDate = instance.CreatedDate,
            ApprovedBy = instance.ApprovedBy,
            ApprovalDate = instance.ApprovalDate,
            Comments = instance.Comments
        });
    }

    private Guid GetCurrentUserId()
    {
        var nameId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(nameId, out var id) ? id : Guid.Empty;
    }
}

#region DTOs

/// <summary>
/// Request to trigger a new approval workflow.
/// </summary>
public class WorkflowTriggerRequestDto
{
    /// <summary>
    /// Type of high-risk event requiring approval.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity being modified (Customer, Licence, Substance).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being modified.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Risk classification of the event.
    /// </summary>
    public string RiskLevel { get; set; } = "High";
}

/// <summary>
/// Response after triggering a workflow.
/// </summary>
public class WorkflowTriggerResponseDto
{
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}

/// <summary>
/// Callback data from Azure Logic App.
/// </summary>
public class WorkflowCallbackDto
{
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Workflow status response.
/// </summary>
public class WorkflowStatusDto
{
    public Guid WorkflowId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InitiatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Comments { get; set; }
}

#endregion

#region Internal Models

/// <summary>
/// Internal workflow instance state.
/// </summary>
internal class WorkflowInstance
{
    public Guid WorkflowId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "High";
    public Guid InitiatedBy { get; set; }
    public string InitiatedByName { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Workflow status.
/// </summary>
internal enum WorkflowStatus
{
    Pending,
    Approved,
    Rejected
}

#endregion
