using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Transaction compliance validation API endpoints.
/// T144-T146: API controller for transaction validation per FR-018 through FR-024.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionComplianceService _complianceService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionComplianceService complianceService,
        ILogger<TransactionsController> logger)
    {
        _complianceService = complianceService;
        _logger = logger;
    }

    #region Validation Endpoints

    /// <summary>
    /// Validates a transaction against compliance rules.
    /// Per FR-018: Real-time transaction compliance validation.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(TransactionValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateTransaction(
        [FromBody] TransactionValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Invalid request data"
            });
        }

        try
        {
            var transaction = request.ToDomainModel();
            var result = await _complianceService.ValidateTransactionAsync(transaction, cancellationToken);

            _logger.LogInformation(
                "Transaction {ExternalId} validation completed: {Status}",
                transaction.ExternalId,
                result.ValidationResult.IsValid ? "Passed" : "Failed");

            return Ok(TransactionValidationResultDto.FromDomain(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction");
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred during validation"
            });
        }
    }

    #endregion

    #region Transaction Retrieval Endpoints

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _complianceService.GetTransactionByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.TRANSACTION_NOT_FOUND,
                Message = $"Transaction with ID '{id}' not found"
            });
        }

        return Ok(TransactionResponseDto.FromDomain(transaction));
    }

    /// <summary>
    /// Gets a transaction by external ID (ERP order number).
    /// </summary>
    [HttpGet("by-external/{externalId}")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionByExternalId(string externalId, CancellationToken cancellationToken = default)
    {
        var transaction = await _complianceService.GetTransactionByExternalIdAsync(externalId, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.TRANSACTION_NOT_FOUND,
                Message = $"Transaction with external ID '{externalId}' not found"
            });
        }

        return Ok(TransactionResponseDto.FromDomain(transaction));
    }

    /// <summary>
    /// Gets transactions with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidationStatus? validationStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ValidationStatus>(status, true, out var parsedStatus))
        {
            validationStatus = parsedStatus;
        }

        var transactions = await _complianceService.GetTransactionsAsync(
            validationStatus,
            customerId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(transactions.Select(TransactionResponseDto.FromDomain));
    }

    #endregion

    #region Override Management Endpoints

    /// <summary>
    /// Gets transactions pending override approval.
    /// Per FR-019a: Override approval queue.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingOverrides(CancellationToken cancellationToken = default)
    {
        var transactions = await _complianceService.GetPendingOverridesAsync(cancellationToken);
        return Ok(transactions.Select(TransactionResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets count of transactions pending override approval.
    /// Used for dashboard widget.
    /// </summary>
    [HttpGet("pending/count")]
    [ProducesResponseType(typeof(PendingOverrideCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingOverrideCount(CancellationToken cancellationToken = default)
    {
        var count = await _complianceService.GetPendingOverrideCountAsync(cancellationToken);
        return Ok(new PendingOverrideCountDto { Count = count });
    }

    /// <summary>
    /// Approves an override for a failed transaction.
    /// Per FR-019a: ComplianceManager override approval.
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveOverride(
        Guid id,
        [FromBody] OverrideApprovalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Justification is required for override approval"
            });
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _complianceService.ApproveOverrideAsync(
            id,
            userId,
            request.Justification,
            cancellationToken);

        if (!result.IsValid)
        {
            var violation = result.Violations.First();
            if (violation.ErrorCode == ErrorCodes.TRANSACTION_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = violation.ErrorCode,
                    Message = violation.Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = violation.ErrorCode,
                Message = violation.Message
            });
        }

        var transaction = await _complianceService.GetTransactionByIdAsync(id, cancellationToken);
        return Ok(TransactionResponseDto.FromDomain(transaction!));
    }

    /// <summary>
    /// Rejects an override request for a failed transaction.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectOverride(
        Guid id,
        [FromBody] OverrideRejectionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Reason is required for override rejection"
            });
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _complianceService.RejectOverrideAsync(
            id,
            userId,
            request.Reason,
            cancellationToken);

        if (!result.IsValid)
        {
            var violation = result.Violations.First();
            if (violation.ErrorCode == ErrorCodes.TRANSACTION_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = violation.ErrorCode,
                    Message = violation.Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = violation.ErrorCode,
                Message = violation.Message
            });
        }

        var transaction = await _complianceService.GetTransactionByIdAsync(id, cancellationToken);
        return Ok(TransactionResponseDto.FromDomain(transaction!));
    }

    #endregion

    #region Warehouse Operations Endpoints

    /// <summary>
    /// Validates whether a warehouse operation can proceed.
    /// T144/FR-023: Warehouse operation validation endpoint.
    /// </summary>
    [HttpPost("~/api/v1/warehouse/operations/validate")]
    [ProducesResponseType(typeof(WarehouseOperationValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateWarehouseOperation(
        [FromBody] WarehouseOperationValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Invalid request data"
            });
        }

        try
        {
            // Look up the transaction by external ID
            var transaction = await _complianceService.GetTransactionByExternalIdAsync(
                request.ExternalTransactionId, cancellationToken);

            if (transaction == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.TRANSACTION_NOT_FOUND,
                    Message = $"No compliance validation found for transaction {request.ExternalTransactionId}. Order may not have been validated yet."
                });
            }

            // Determine if operation is allowed based on validation status
            var isAllowed = transaction.ValidationStatus == ValidationStatus.Passed ||
                           transaction.ValidationStatus == ValidationStatus.ApprovedWithOverride;

            string message;
            string? blockReason = null;

            if (isAllowed)
            {
                message = "Operation allowed. All compliance checks passed.";
            }
            else if (transaction.ValidationStatus == ValidationStatus.Pending)
            {
                message = "Operation blocked. Transaction pending compliance review.";
                blockReason = transaction.RequiresOverride
                    ? "Awaiting compliance manager approval."
                    : "Transaction is pending validation.";
            }
            else if (transaction.ValidationStatus == ValidationStatus.Failed)
            {
                message = "Operation blocked. Transaction failed compliance validation.";
                blockReason = transaction.Violations.FirstOrDefault()?.Message ??
                             "Transaction has unresolved compliance violations.";
            }
            else
            {
                message = "Operation blocked. Transaction status does not allow warehouse operations.";
                blockReason = $"Transaction status: {transaction.ValidationStatus}";
            }

            _logger.LogInformation(
                "Warehouse operation {OperationType} for transaction {ExternalId}: {Allowed}",
                request.OperationType,
                request.ExternalTransactionId,
                isAllowed ? "ALLOWED" : "BLOCKED");

            return Ok(new WarehouseOperationValidationResultDto
            {
                Allowed = isAllowed,
                Message = message,
                TransactionId = transaction.Id,
                ComplianceStatus = transaction.ValidationStatus.ToString(),
                BlockReason = blockReason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating warehouse operation for {ExternalId}", request.ExternalTransactionId);
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred during warehouse operation validation"
            });
        }
    }

    #endregion
}

#region DTOs

/// <summary>
/// Transaction validation request DTO.
/// </summary>
public class TransactionValidationRequestDto
{
    /// <summary>
    /// External order/shipment number from ERP system.
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Type of transaction (Order, Shipment, Return, Transfer).
    /// </summary>
    public string TransactionType { get; set; } = "Order";

    /// <summary>
    /// Direction for cross-border (Internal, Inbound, Outbound).
    /// </summary>
    public string Direction { get; set; } = "Internal";

    /// <summary>
    /// Customer ID.
    /// </summary>
    public required Guid CustomerId { get; set; }

    /// <summary>
    /// Origin country (ISO 3166-1 alpha-2).
    /// </summary>
    public string OriginCountry { get; set; } = "NL";

    /// <summary>
    /// Destination country (ISO 3166-1 alpha-2).
    /// </summary>
    public string? DestinationCountry { get; set; }

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// T149b: Integration system ID that is submitting the transaction.
    /// Per FR-061: Record calling system identity in transaction audit.
    /// Examples: "D365FO", "WMS", "OrderManagement", "Portal"
    /// </summary>
    public string? IntegrationSystemId { get; set; }

    /// <summary>
    /// External user ID from the calling system who initiated the transaction.
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// Warehouse site ID for warehouse-related transactions.
    /// </summary>
    public string? WarehouseSiteId { get; set; }

    /// <summary>
    /// Transaction line items.
    /// </summary>
    public List<TransactionLineDto> Lines { get; set; } = new();

    public Transaction ToDomainModel()
    {
        var transaction = new Transaction
        {
            Id = Guid.Empty, // Will be assigned on create
            ExternalId = ExternalId,
            TransactionType = Enum.Parse<TransactionTypes.TransactionType>(TransactionType, true),
            Direction = Enum.Parse<TransactionDirection>(Direction, true),
            CustomerId = CustomerId,
            OriginCountry = OriginCountry,
            DestinationCountry = DestinationCountry,
            TransactionDate = TransactionDate,
            IntegrationSystemId = IntegrationSystemId, // T149b: Record calling system identity
            ExternalUserId = ExternalUserId,
            WarehouseSiteId = WarehouseSiteId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        var lineNumber = 1;
        foreach (var lineDto in Lines)
        {
            transaction.Lines.Add(new TransactionLine
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                LineNumber = lineNumber++,
                SubstanceId = lineDto.SubstanceId,
                SubstanceCode = lineDto.SubstanceCode ?? string.Empty,
                ProductCode = lineDto.ProductCode,
                ProductDescription = lineDto.ProductDescription,
                BatchNumber = lineDto.BatchNumber,
                Quantity = lineDto.Quantity,
                UnitOfMeasure = lineDto.UnitOfMeasure ?? "EA",
                BaseUnitQuantity = lineDto.BaseUnitQuantity ?? lineDto.Quantity,
                BaseUnit = lineDto.BaseUnit ?? "g",
                LineValue = lineDto.LineValue,
                UnitPrice = lineDto.UnitPrice
            });
        }

        transaction.TotalQuantity = transaction.Lines.Sum(l => l.BaseUnitQuantity);
        transaction.TotalValue = transaction.Lines.Sum(l => l.LineValue);

        return transaction;
    }
}

/// <summary>
/// Transaction line DTO.
/// </summary>
public class TransactionLineDto
{
    /// <summary>
    /// Controlled substance ID.
    /// </summary>
    public required Guid SubstanceId { get; set; }

    /// <summary>
    /// Substance internal code.
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Product/item number from ERP.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product description.
    /// </summary>
    public string? ProductDescription { get; set; }

    /// <summary>
    /// Batch/lot number.
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Quantity in base unit (for threshold comparison).
    /// </summary>
    public decimal? BaseUnitQuantity { get; set; }

    /// <summary>
    /// Base unit (e.g., "g", "mg").
    /// </summary>
    public string? BaseUnit { get; set; }

    /// <summary>
    /// Line value.
    /// </summary>
    public decimal? LineValue { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal? UnitPrice { get; set; }

    public static TransactionLineDto FromDomain(TransactionLine line)
    {
        return new TransactionLineDto
        {
            SubstanceId = line.SubstanceId,
            SubstanceCode = line.SubstanceCode,
            ProductCode = line.ProductCode,
            ProductDescription = line.ProductDescription,
            BatchNumber = line.BatchNumber,
            Quantity = line.Quantity,
            UnitOfMeasure = line.UnitOfMeasure,
            BaseUnitQuantity = line.BaseUnitQuantity,
            BaseUnit = line.BaseUnit,
            LineValue = line.LineValue,
            UnitPrice = line.UnitPrice
        };
    }
}

/// <summary>
/// Transaction validation result DTO.
/// </summary>
public class TransactionValidationResultDto
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether the transaction can proceed (passed or approved with override).
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Whether override is available for this transaction.
    /// </summary>
    public bool CanOverride { get; set; }

    /// <summary>
    /// Validation status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// External ID.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Validation time in milliseconds.
    /// </summary>
    public long ValidationTimeMs { get; set; }

    /// <summary>
    /// List of violations.
    /// </summary>
    public List<TransactionViolationDto> Violations { get; set; } = new();

    /// <summary>
    /// Licences used to cover the transaction.
    /// </summary>
    public List<LicenceUsageDto> LicenceUsages { get; set; } = new();

    public static TransactionValidationResultDto FromDomain(TransactionValidationResult result)
    {
        return new TransactionValidationResultDto
        {
            IsValid = result.ValidationResult.IsValid,
            CanProceed = result.CanProceed,
            CanOverride = result.ValidationResult.CanOverride,
            Status = result.Transaction.ValidationStatus.ToString(),
            TransactionId = result.Transaction.Id,
            ExternalId = result.Transaction.ExternalId,
            ValidationTimeMs = result.ValidationTimeMs,
            Violations = result.ValidationResult.Violations
                .Select(v => new TransactionViolationDto
                {
                    ErrorCode = v.ErrorCode,
                    Message = v.Message,
                    Severity = v.Severity.ToString(),
                    CanOverride = v.CanOverride,
                    LineNumber = v.LineNumber,
                    SubstanceCode = v.SubstanceCode
                }).ToList(),
            LicenceUsages = result.LicenceUsages
                .Select(u => new LicenceUsageDto
                {
                    LicenceId = u.LicenceId,
                    LicenceNumber = u.LicenceNumber,
                    LicenceTypeName = u.LicenceTypeName,
                    CoveredLineNumbers = u.CoveredLineNumbers,
                    CoveredQuantity = u.CoveredQuantity,
                    CoveredQuantityUnit = u.CoveredQuantityUnit
                }).ToList()
        };
    }
}

/// <summary>
/// Transaction violation DTO.
/// </summary>
public class TransactionViolationDto
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool CanOverride { get; set; }
    public int? LineNumber { get; set; }
    public string? SubstanceCode { get; set; }
}

/// <summary>
/// Licence usage DTO.
/// </summary>
public class LicenceUsageDto
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string? LicenceTypeName { get; set; }
    public List<int> CoveredLineNumbers { get; set; } = new();
    public decimal CoveredQuantity { get; set; }
    public string CoveredQuantityUnit { get; set; } = string.Empty;
}

/// <summary>
/// Transaction response DTO.
/// </summary>
public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string OriginCountry { get; set; } = string.Empty;
    public string? DestinationCountry { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? TotalValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public DateTime? ValidationDate { get; set; }
    public bool RequiresOverride { get; set; }
    public string OverrideStatus { get; set; } = string.Empty;
    public string? OverrideJustification { get; set; }
    public string? OverrideRejectionReason { get; set; }
    public List<TransactionLineDto> Lines { get; set; } = new();
    public List<TransactionViolationDto> Violations { get; set; } = new();
    public List<string> ComplianceWarnings { get; set; } = new();
    public List<string> ComplianceErrors { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public static TransactionResponseDto FromDomain(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            ExternalId = transaction.ExternalId,
            TransactionType = transaction.TransactionType.ToString(),
            Direction = transaction.Direction.ToString(),
            CustomerId = transaction.CustomerId,
            CustomerName = transaction.CustomerName,
            OriginCountry = transaction.OriginCountry,
            DestinationCountry = transaction.DestinationCountry,
            TransactionDate = transaction.TransactionDate,
            TotalQuantity = transaction.TotalQuantity,
            TotalValue = transaction.TotalValue,
            Status = transaction.Status,
            ValidationStatus = transaction.ValidationStatus.ToString(),
            ValidationDate = transaction.ValidationDate,
            RequiresOverride = transaction.RequiresOverride,
            OverrideStatus = transaction.OverrideStatus.ToString(),
            OverrideJustification = transaction.OverrideJustification,
            OverrideRejectionReason = transaction.OverrideRejectionReason,
            Lines = transaction.Lines.Select(TransactionLineDto.FromDomain).ToList(),
            Violations = transaction.Violations.Select(v => new TransactionViolationDto
            {
                ErrorCode = v.ErrorCode,
                Message = v.Message,
                Severity = v.Severity.ToString(),
                CanOverride = v.CanOverride,
                LineNumber = v.LineNumber,
                SubstanceCode = v.SubstanceCode
            }).ToList(),
            ComplianceWarnings = transaction.ComplianceWarnings,
            ComplianceErrors = transaction.ComplianceErrors,
            CreatedAt = transaction.CreatedAt
        };
    }
}

/// <summary>
/// Override approval request DTO.
/// </summary>
public class OverrideApprovalRequestDto
{
    /// <summary>
    /// Justification for approving the override.
    /// </summary>
    public required string Justification { get; set; }
}

/// <summary>
/// Override rejection request DTO.
/// </summary>
public class OverrideRejectionRequestDto
{
    /// <summary>
    /// Reason for rejecting the override.
    /// </summary>
    public required string Reason { get; set; }
}

/// <summary>
/// Pending override count DTO.
/// </summary>
public class PendingOverrideCountDto
{
    public int Count { get; set; }
}

/// <summary>
/// Warehouse operation validation request DTO.
/// T144/FR-023: Validates whether warehouse operations can proceed.
/// </summary>
public class WarehouseOperationValidationRequestDto
{
    /// <summary>
    /// External transaction ID (sales order number, shipment ID).
    /// </summary>
    public required string ExternalTransactionId { get; set; }

    /// <summary>
    /// Type of warehouse operation (Pick, Pack, PrintLabels, ShipmentRelease).
    /// </summary>
    public required string OperationType { get; set; }

    /// <summary>
    /// Warehouse site ID where operation will occur.
    /// </summary>
    public string? WarehouseSiteId { get; set; }

    /// <summary>
    /// When the operation was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// T149b: Integration system ID making the validation request.
    /// Per FR-061: Record calling system identity for audit.
    /// </summary>
    public string? IntegrationSystemId { get; set; }
}

/// <summary>
/// Warehouse operation validation result DTO.
/// T144/FR-023: Response from warehouse operation validation.
/// </summary>
public class WarehouseOperationValidationResultDto
{
    /// <summary>
    /// Whether the warehouse operation is allowed.
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Explanation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Internal transaction ID.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Transaction compliance status.
    /// </summary>
    public string? ComplianceStatus { get; set; }

    /// <summary>
    /// Reason operation is blocked (if not allowed).
    /// </summary>
    public string? BlockReason { get; set; }
}

#endregion
