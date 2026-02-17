using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Substance reclassification API endpoints.
/// T080h: SubstanceReclassificationController v1 per FR-066.
/// Handles: POST /api/v1/substances/{id}/reclassify and related operations.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class SubstanceReclassificationController : ControllerBase
{
    private readonly ISubstanceReclassificationService _reclassificationService;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<SubstanceReclassificationController> _logger;

    public SubstanceReclassificationController(
        ISubstanceReclassificationService reclassificationService,
        IControlledSubstanceRepository substanceRepository,
        ILogger<SubstanceReclassificationController> logger)
    {
        _reclassificationService = reclassificationService;
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a reclassification for a substance.
    /// Per FR-066: Records the new classification with effective date.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="request">Reclassification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created reclassification details.</returns>
    [HttpPost("substances/{substanceCode}/reclassify")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ReclassificationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReclassification(
        string substanceCode,
        [FromBody] CreateReclassificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Verify substance exists
        var substance = await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        if (substance == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Substance with code '{substanceCode}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var reclassification = request.ToDomainModel(substanceCode, substance);
        var (id, result) = await _reclassificationService.CreateReclassificationAsync(reclassification, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Reclassification validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var created = await _reclassificationService.GetByIdAsync(id!.Value, cancellationToken);
        _logger.LogInformation("Created reclassification {Id} for substance {SubstanceCode}", id, substanceCode);

        return CreatedAtAction(
            nameof(GetReclassification),
            new { id = id },
            ReclassificationResponseDto.FromDomainModel(created!));
    }

    /// <summary>
    /// Gets a specific reclassification by ID.
    /// </summary>
    /// <param name="id">Reclassification ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reclassification details.</returns>
    [HttpGet("reclassifications/{id:guid}")]
    [ProducesResponseType(typeof(ReclassificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReclassification(Guid id, CancellationToken cancellationToken = default)
    {
        var reclassification = await _reclassificationService.GetByIdAsync(id, cancellationToken);

        if (reclassification == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Reclassification with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(ReclassificationResponseDto.FromDomainModel(reclassification));
    }

    /// <summary>
    /// Gets all reclassifications for a substance.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of reclassifications.</returns>
    [HttpGet("substances/{substanceCode}/reclassifications")]
    [ProducesResponseType(typeof(IEnumerable<ReclassificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubstanceReclassifications(
        string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var reclassifications = await _reclassificationService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        var response = reclassifications.Select(ReclassificationResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets pending reclassifications.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending reclassifications.</returns>
    [HttpGet("reclassifications/pending")]
    [ProducesResponseType(typeof(IEnumerable<ReclassificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReclassifications(CancellationToken cancellationToken = default)
    {
        var reclassifications = await _reclassificationService.GetPendingReclassificationsAsync(cancellationToken);
        var response = reclassifications.Select(ReclassificationResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Processes a reclassification: runs impact analysis, flags customers, generates notifications.
    /// Per FR-066: Full reclassification workflow.
    /// </summary>
    /// <param name="id">Reclassification ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing result.</returns>
    [HttpPost("reclassifications/{id:guid}/process")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(ReclassificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessReclassification(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _reclassificationService.ProcessReclassificationAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Reclassification with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Processing failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var processed = await _reclassificationService.GetByIdAsync(id, cancellationToken);
        _logger.LogInformation("Processed reclassification {Id}", id);

        return Ok(ReclassificationResponseDto.FromDomainModel(processed!));
    }

    /// <summary>
    /// Performs impact analysis for a reclassification without processing.
    /// Returns preview of affected customers.
    /// </summary>
    /// <param name="id">Reclassification ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impact analysis results.</returns>
    [HttpGet("reclassifications/{id:guid}/impact-analysis")]
    [ProducesResponseType(typeof(ImpactAnalysisResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImpactAnalysis(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var analysis = await _reclassificationService.AnalyzeCustomerImpactAsync(id, cancellationToken);
            return Ok(ImpactAnalysisResponseDto.FromAnalysis(analysis));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Generates compliance notification for a reclassification.
    /// Per FR-066 (T080k): Lists affected customers and required actions.
    /// </summary>
    /// <param name="id">Reclassification ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compliance notification data.</returns>
    [HttpGet("reclassifications/{id:guid}/notification")]
    [ProducesResponseType(typeof(ComplianceNotification), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComplianceNotification(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _reclassificationService.GenerateComplianceNotificationAsync(id, cancellationToken);
            return Ok(notification);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Marks a customer as re-qualified after licence update.
    /// Per FR-066: Clears the "Requires Re-Qualification" flag.
    /// </summary>
    /// <param name="id">Reclassification ID.</param>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("reclassifications/{id:guid}/customers/{customerId:guid}/requalify")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkCustomerReQualified(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var result = await _reclassificationService.MarkCustomerReQualifiedAsync(id, customerId, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Re-qualification failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Marked customer {CustomerId} as re-qualified for reclassification {Id}", customerId, id);
        return Ok(new { Message = "Customer marked as re-qualified" });
    }

    /// <summary>
    /// Checks if a customer is blocked from transactions due to reclassification.
    /// Per FR-066 (T080l): Returns blocking status for transaction validation.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Blocking status and impacts.</returns>
    [HttpGet("customers/{customerId:guid}/reclassification-status")]
    [ProducesResponseType(typeof(CustomerReclassificationStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerReclassificationStatus(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var (isBlocked, impacts) = await _reclassificationService.CheckCustomerBlockedAsync(customerId, cancellationToken);

        return Ok(new CustomerReclassificationStatusDto
        {
            CustomerId = customerId,
            IsBlocked = isBlocked,
            RequiresReQualification = isBlocked,
            BlockingImpacts = impacts.Select(CustomerImpactDto.FromDomainModel).ToList()
        });
    }

    /// <summary>
    /// Gets the effective classification for a substance at a specific date.
    /// Per FR-066 (T080m): Historical transaction validation support.
    /// </summary>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="asOfDate">Date to check classification for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Classification effective at that date.</returns>
    [HttpGet("substances/{substanceCode}/classification")]
    [ProducesResponseType(typeof(SubstanceClassification), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectiveClassification(
        string substanceCode,
        [FromQuery] DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var date = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var classification = await _reclassificationService.GetEffectiveClassificationAsync(substanceCode, date, cancellationToken);
            return Ok(classification);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}

#region DTOs

/// <summary>
/// Request DTO for creating a reclassification.
/// </summary>
public class CreateReclassificationRequestDto
{
    public int NewOpiumActList { get; set; }
    public int NewPrecursorCategory { get; set; }
    public required DateOnly EffectiveDate { get; set; }
    public required string RegulatoryReference { get; set; }
    public required string RegulatoryAuthority { get; set; }
    public string? Reason { get; set; }

    public SubstanceReclassification ToDomainModel(string substanceCode, ControlledSubstance substance)
    {
        return new SubstanceReclassification
        {
            SubstanceCode = substanceCode,
            PreviousOpiumActList = substance.OpiumActList,
            NewOpiumActList = (SubstanceCategories.OpiumActList)NewOpiumActList,
            PreviousPrecursorCategory = substance.PrecursorCategory,
            NewPrecursorCategory = (SubstanceCategories.PrecursorCategory)NewPrecursorCategory,
            EffectiveDate = EffectiveDate,
            RegulatoryReference = RegulatoryReference,
            RegulatoryAuthority = RegulatoryAuthority,
            Reason = Reason
        };
    }
}

/// <summary>
/// Response DTO for reclassification.
/// </summary>
public class ReclassificationResponseDto
{
    public Guid ReclassificationId { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public string? SubstanceName { get; set; }
    public int PreviousOpiumActList { get; set; }
    public int NewOpiumActList { get; set; }
    public int PreviousPrecursorCategory { get; set; }
    public int NewPrecursorCategory { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public required string RegulatoryReference { get; set; }
    public required string RegulatoryAuthority { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public int AffectedCustomerCount { get; set; }
    public int FlaggedCustomerCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public bool IsEffective { get; set; }
    public bool IsUpgrade { get; set; }

    public static ReclassificationResponseDto FromDomainModel(SubstanceReclassification reclassification)
    {
        return new ReclassificationResponseDto
        {
            ReclassificationId = reclassification.ReclassificationId,
            SubstanceCode = reclassification.SubstanceCode,
            SubstanceName = reclassification.Substance?.SubstanceName,
            PreviousOpiumActList = (int)reclassification.PreviousOpiumActList,
            NewOpiumActList = (int)reclassification.NewOpiumActList,
            PreviousPrecursorCategory = (int)reclassification.PreviousPrecursorCategory,
            NewPrecursorCategory = (int)reclassification.NewPrecursorCategory,
            EffectiveDate = reclassification.EffectiveDate,
            RegulatoryReference = reclassification.RegulatoryReference,
            RegulatoryAuthority = reclassification.RegulatoryAuthority,
            Reason = reclassification.Reason,
            Status = reclassification.Status.ToString(),
            AffectedCustomerCount = reclassification.AffectedCustomerCount,
            FlaggedCustomerCount = reclassification.FlaggedCustomerCount,
            CreatedDate = reclassification.CreatedDate,
            ProcessedDate = reclassification.ProcessedDate,
            IsEffective = reclassification.IsEffective(),
            IsUpgrade = reclassification.IsUpgrade()
        };
    }
}

/// <summary>
/// Response DTO for impact analysis.
/// </summary>
public class ImpactAnalysisResponseDto
{
    public Guid ReclassificationId { get; set; }
    public int TotalAffectedCustomers { get; set; }
    public int CustomersWithSufficientLicences { get; set; }
    public int CustomersFlaggedForReQualification { get; set; }
    public List<CustomerImpactDto> CustomerImpacts { get; set; } = new();

    public static ImpactAnalysisResponseDto FromAnalysis(ReclassificationImpactAnalysis analysis)
    {
        return new ImpactAnalysisResponseDto
        {
            ReclassificationId = analysis.Reclassification.ReclassificationId,
            TotalAffectedCustomers = analysis.TotalAffectedCustomers,
            CustomersWithSufficientLicences = analysis.CustomersWithSufficientLicences,
            CustomersFlaggedForReQualification = analysis.CustomersFlaggedForReQualification,
            CustomerImpacts = analysis.CustomerImpacts.Select(CustomerImpactDto.FromDomainModel).ToList()
        };
    }
}

/// <summary>
/// Response DTO for customer impact.
/// </summary>
public class CustomerImpactDto
{
    public Guid ImpactId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool HasSufficientLicence { get; set; }
    public bool RequiresReQualification { get; set; }
    public string? LicenceGapSummary { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? ReQualificationDate { get; set; }

    public static CustomerImpactDto FromDomainModel(ReclassificationCustomerImpact impact)
    {
        return new CustomerImpactDto
        {
            ImpactId = impact.ImpactId,
            CustomerId = impact.CustomerId,
            CustomerName = impact.CustomerName,
            HasSufficientLicence = impact.HasSufficientLicence,
            RequiresReQualification = impact.RequiresReQualification,
            LicenceGapSummary = impact.LicenceGapSummary,
            NotificationSent = impact.NotificationSent,
            ReQualificationDate = impact.ReQualificationDate
        };
    }
}

/// <summary>
/// Response DTO for customer reclassification status.
/// </summary>
public class CustomerReclassificationStatusDto
{
    public Guid CustomerId { get; set; }
    public bool IsBlocked { get; set; }
    public bool RequiresReQualification { get; set; }
    public List<CustomerImpactDto> BlockingImpacts { get; set; } = new();
}

#endregion
