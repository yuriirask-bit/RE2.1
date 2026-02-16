using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.Reporting;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Compliance reports API endpoints.
/// T164: API controller for report generation per FR-026, FR-029.
/// T163c: Added licence correction impact endpoint per SC-038.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "ComplianceManager,QAUser")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILicenceCorrectionImpactService _licenceCorrectionImpactService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportingService reportingService,
        ILicenceCorrectionImpactService licenceCorrectionImpactService,
        ILogger<ReportsController> logger)
    {
        _reportingService = reportingService;
        _licenceCorrectionImpactService = licenceCorrectionImpactService;
        _logger = logger;
    }

    #region Transaction Audit Reports (FR-026)

    /// <summary>
    /// Generates a transaction audit report.
    /// T161/FR-026: Report by substance, customer, or country.
    /// </summary>
    [HttpGet("transaction-audit")]
    [ProducesResponseType(typeof(TransactionAuditReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTransactionAuditReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? substanceCode = null,
        [FromQuery] string? customerAccount = null,
        [FromQuery] string? customerDataAreaId = null,
        [FromQuery] string? countryCode = null,
        [FromQuery] bool includeLicenceDetails = false,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new TransactionAuditReportCriteria
            {
                FromDate = fromDate,
                ToDate = toDate,
                SubstanceCode = substanceCode,
                CustomerAccount = customerAccount,
                CustomerDataAreaId = customerDataAreaId,
                CountryCode = countryCode,
                IncludeLicenceDetails = includeLicenceDetails
            };

            var report = await _reportingService.GenerateTransactionAuditReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated transaction audit report: {Count} transactions from {FromDate} to {ToDate}",
                report.TotalCount, fromDate, toDate);

            return Ok(TransactionAuditReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transaction audit report");
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the transaction audit report"
            });
        }
    }

    /// <summary>
    /// Generates a transaction audit report (POST for complex criteria).
    /// T161/FR-026: Alternative endpoint for complex filter criteria.
    /// </summary>
    [HttpPost("transaction-audit")]
    [ProducesResponseType(typeof(TransactionAuditReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateTransactionAuditReport(
        [FromBody] TransactionAuditReportRequestDto request,
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

        if (request.FromDate > request.ToDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new TransactionAuditReportCriteria
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SubstanceCode = request.SubstanceCode,
                CustomerAccount = request.CustomerAccount,
                CustomerDataAreaId = request.CustomerDataAreaId,
                CountryCode = request.CountryCode,
                IncludeLicenceDetails = request.IncludeLicenceDetails
            };

            var report = await _reportingService.GenerateTransactionAuditReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated transaction audit report: {Count} transactions from {FromDate} to {ToDate}",
                report.TotalCount, request.FromDate, request.ToDate);

            return Ok(TransactionAuditReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transaction audit report");
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the transaction audit report"
            });
        }
    }

    #endregion

    #region Licence Usage Reports (FR-026)

    /// <summary>
    /// Generates a licence usage report.
    /// T162/FR-026: Shows which licences were used in transactions.
    /// </summary>
    [HttpGet("licence-usage")]
    [ProducesResponseType(typeof(LicenceUsageReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLicenceUsageReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? licenceId = null,
        [FromQuery] Guid? holderId = null,
        [FromQuery] string? holderType = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new LicenceUsageReportCriteria
            {
                FromDate = fromDate,
                ToDate = toDate,
                LicenceId = licenceId,
                HolderId = holderId,
                HolderType = holderType
            };

            var report = await _reportingService.GenerateLicenceUsageReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated licence usage report: {Count} licences from {FromDate} to {ToDate}",
                report.TotalLicences, fromDate, toDate);

            return Ok(LicenceUsageReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating licence usage report");
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the licence usage report"
            });
        }
    }

    /// <summary>
    /// Generates a licence usage report (POST for complex criteria).
    /// T162/FR-026: Alternative endpoint for complex filter criteria.
    /// </summary>
    [HttpPost("licence-usage")]
    [ProducesResponseType(typeof(LicenceUsageReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateLicenceUsageReport(
        [FromBody] LicenceUsageReportRequestDto request,
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

        if (request.FromDate > request.ToDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new LicenceUsageReportCriteria
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                LicenceId = request.LicenceId,
                HolderId = request.HolderId,
                HolderType = request.HolderType
            };

            var report = await _reportingService.GenerateLicenceUsageReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated licence usage report: {Count} licences from {FromDate} to {ToDate}",
                report.TotalLicences, request.FromDate, request.ToDate);

            return Ok(LicenceUsageReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating licence usage report");
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the licence usage report"
            });
        }
    }

    #endregion

    #region Customer Compliance History Reports (FR-029)

    /// <summary>
    /// Generates a customer compliance history report.
    /// T163/FR-029: Complete compliance history for a customer.
    /// </summary>
    [HttpGet("customer-compliance/{customerAccount}/{dataAreaId}")]
    [ProducesResponseType(typeof(CustomerComplianceHistoryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerComplianceHistory(
        string customerAccount,
        string dataAreaId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeLicenceStatus = false,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new CustomerComplianceHistoryCriteria
            {
                CustomerAccount = customerAccount,
                DataAreaId = dataAreaId,
                FromDate = fromDate,
                ToDate = toDate,
                IncludeLicenceStatus = includeLicenceStatus
            };

            var report = await _reportingService.GenerateCustomerComplianceHistoryAsync(criteria, cancellationToken);

            if (report == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.CUSTOMER_NOT_FOUND,
                    Message = $"Customer with account '{customerAccount}' in data area '{dataAreaId}' not found"
                });
            }

            _logger.LogInformation(
                "Generated customer compliance history for {CustomerAccount}/{DataAreaId}: {Count} events",
                customerAccount, dataAreaId, report.Events.Count);

            return Ok(CustomerComplianceHistoryReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer compliance history for {CustomerAccount}/{DataAreaId}", customerAccount, dataAreaId);
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the customer compliance history"
            });
        }
    }

    /// <summary>
    /// Generates a customer compliance history report (POST for complex criteria).
    /// T163/FR-029: Alternative endpoint for complex filter criteria.
    /// </summary>
    [HttpPost("customer-compliance")]
    [ProducesResponseType(typeof(CustomerComplianceHistoryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCustomerComplianceHistory(
        [FromBody] CustomerComplianceHistoryRequestDto request,
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

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FromDate must be before or equal to ToDate"
            });
        }

        try
        {
            var criteria = new CustomerComplianceHistoryCriteria
            {
                CustomerAccount = request.CustomerAccount,
                DataAreaId = request.DataAreaId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                IncludeLicenceStatus = request.IncludeLicenceStatus
            };

            var report = await _reportingService.GenerateCustomerComplianceHistoryAsync(criteria, cancellationToken);

            if (report == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.CUSTOMER_NOT_FOUND,
                    Message = $"Customer with account '{request.CustomerAccount}' in data area '{request.DataAreaId}' not found"
                });
            }

            _logger.LogInformation(
                "Generated customer compliance history for {CustomerAccount}/{DataAreaId}: {Count} events",
                request.CustomerAccount, request.DataAreaId, report.Events.Count);

            return Ok(CustomerComplianceHistoryReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer compliance history for {CustomerAccount}/{DataAreaId}", request.CustomerAccount, request.DataAreaId);
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred generating the customer compliance history"
            });
        }
    }

    #endregion

    #region Licence Correction Impact Reports (SC-038)

    /// <summary>
    /// Analyzes the impact of a licence correction on historical transactions.
    /// T163c/SC-038: Historical validation report for licence date corrections.
    /// </summary>
    [HttpGet("licence-correction-impact")]
    [ProducesResponseType(typeof(LicenceCorrectionImpactReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLicenceCorrectionImpact(
        [FromQuery] Guid licenceId,
        [FromQuery] DateTime correctionDate,
        [FromQuery] DateTime? originalEffectiveDate = null,
        [FromQuery] DateTime? correctedEffectiveDate = null,
        [FromQuery] DateOnly? originalExpiryDate = null,
        [FromQuery] DateOnly? correctedExpiryDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var criteria = new LicenceCorrectionImpactCriteria
            {
                LicenceId = licenceId,
                CorrectionDate = correctionDate,
                OriginalEffectiveDate = originalEffectiveDate,
                CorrectedEffectiveDate = correctedEffectiveDate,
                OriginalExpiryDate = originalExpiryDate,
                CorrectedExpiryDate = correctedExpiryDate
            };

            var report = await _licenceCorrectionImpactService.AnalyzeImpactAsync(criteria, cancellationToken);

            if (report == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licenceId}' not found"
                });
            }

            _logger.LogInformation(
                "Generated licence correction impact report for {LicenceId}: {Affected} of {Total} transactions affected",
                licenceId, report.TotalAffectedTransactions, report.TotalTransactionsAnalyzed);

            return Ok(LicenceCorrectionImpactReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing licence correction impact for {LicenceId}", licenceId);
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred analyzing the licence correction impact"
            });
        }
    }

    /// <summary>
    /// Analyzes the impact of a licence correction (POST for complex criteria).
    /// T163c/SC-038: Alternative endpoint for complex criteria.
    /// </summary>
    [HttpPost("licence-correction-impact")]
    [ProducesResponseType(typeof(LicenceCorrectionImpactReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeLicenceCorrectionImpact(
        [FromBody] LicenceCorrectionImpactRequestDto request,
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
            var criteria = new LicenceCorrectionImpactCriteria
            {
                LicenceId = request.LicenceId,
                CorrectionDate = request.CorrectionDate,
                OriginalEffectiveDate = request.OriginalEffectiveDate,
                CorrectedEffectiveDate = request.CorrectedEffectiveDate,
                OriginalExpiryDate = request.OriginalExpiryDate,
                CorrectedExpiryDate = request.CorrectedExpiryDate
            };

            var report = await _licenceCorrectionImpactService.AnalyzeImpactAsync(criteria, cancellationToken);

            if (report == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{request.LicenceId}' not found"
                });
            }

            _logger.LogInformation(
                "Generated licence correction impact report for {LicenceId}: {Affected} of {Total} transactions affected",
                request.LicenceId, report.TotalAffectedTransactions, report.TotalTransactionsAnalyzed);

            return Ok(LicenceCorrectionImpactReportDto.FromDomain(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing licence correction impact for {LicenceId}", request.LicenceId);
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "An error occurred analyzing the licence correction impact"
            });
        }
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Transaction audit report request DTO.
/// </summary>
public class TransactionAuditReportRequestDto
{
    /// <summary>
    /// Start date of the reporting period.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Optional filter by substance code.
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Optional filter by customer account number.
    /// </summary>
    public string? CustomerAccount { get; set; }

    /// <summary>
    /// Optional filter by customer data area ID.
    /// </summary>
    public string? CustomerDataAreaId { get; set; }

    /// <summary>
    /// Optional filter by country code (ISO 3166-1 alpha-2).
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Whether to include licence details in the report.
    /// </summary>
    public bool IncludeLicenceDetails { get; set; }
}

/// <summary>
/// Licence usage report request DTO.
/// </summary>
public class LicenceUsageReportRequestDto
{
    /// <summary>
    /// Start date of the reporting period.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Optional filter by licence ID.
    /// </summary>
    public Guid? LicenceId { get; set; }

    /// <summary>
    /// Optional filter by holder ID.
    /// </summary>
    public Guid? HolderId { get; set; }

    /// <summary>
    /// Optional filter by holder type (Customer, Company).
    /// </summary>
    public string? HolderType { get; set; }
}

/// <summary>
/// Customer compliance history request DTO.
/// </summary>
public class CustomerComplianceHistoryRequestDto
{
    /// <summary>
    /// Customer account number to generate history for.
    /// </summary>
    public string CustomerAccount { get; set; } = string.Empty;

    /// <summary>
    /// Data area ID for the customer.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Optional start date filter.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Optional end date filter.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Whether to include current licence status.
    /// </summary>
    public bool IncludeLicenceStatus { get; set; }
}

/// <summary>
/// Licence correction impact request DTO.
/// T163c/SC-038: Request for historical validation impact analysis.
/// </summary>
public class LicenceCorrectionImpactRequestDto
{
    /// <summary>
    /// ID of the licence being corrected.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Date the correction was made.
    /// </summary>
    public DateTime CorrectionDate { get; set; }

    /// <summary>
    /// Original effective date before correction (if applicable).
    /// </summary>
    public DateTime? OriginalEffectiveDate { get; set; }

    /// <summary>
    /// Corrected effective date.
    /// </summary>
    public DateTime? CorrectedEffectiveDate { get; set; }

    /// <summary>
    /// Original expiry date before correction (if applicable).
    /// </summary>
    public DateOnly? OriginalExpiryDate { get; set; }

    /// <summary>
    /// Corrected expiry date.
    /// </summary>
    public DateOnly? CorrectedExpiryDate { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Transaction audit report response DTO.
/// </summary>
public class TransactionAuditReportDto
{
    /// <summary>
    /// Date the report was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// Start date of the reporting period.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// List of transactions in the report.
    /// </summary>
    public List<TransactionAuditItemDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total count of transactions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Substance code filter applied (if any).
    /// </summary>
    public string? FilteredBySubstanceCode { get; set; }

    /// <summary>
    /// Customer account filter applied (if any).
    /// </summary>
    public string? FilteredByCustomerAccount { get; set; }

    /// <summary>
    /// Customer data area ID filter applied (if any).
    /// </summary>
    public string? FilteredByCustomerDataAreaId { get; set; }

    /// <summary>
    /// Country filter applied (if any).
    /// </summary>
    public string? FilteredByCountry { get; set; }

    public static TransactionAuditReportDto FromDomain(TransactionAuditReport report)
    {
        return new TransactionAuditReportDto
        {
            GeneratedDate = report.GeneratedDate,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            Transactions = report.Transactions.Select(TransactionAuditItemDto.FromDomain).ToList(),
            TotalCount = report.TotalCount,
            FilteredBySubstanceCode = report.FilteredBySubstanceCode,
            FilteredByCustomerAccount = report.FilteredByCustomerAccount,
            FilteredByCustomerDataAreaId = report.FilteredByCustomerDataAreaId,
            FilteredByCountry = report.FilteredByCountry
        };
    }
}

/// <summary>
/// Transaction audit item DTO.
/// </summary>
public class TransactionAuditItemDto
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public List<LicenceReportDetailDto>? LicencesUsed { get; set; }

    public static TransactionAuditItemDto FromDomain(TransactionAuditReportItem item)
    {
        return new TransactionAuditItemDto
        {
            TransactionId = item.TransactionId,
            ExternalTransactionId = item.ExternalTransactionId,
            TransactionDate = item.TransactionDate,
            CustomerAccount = item.CustomerAccount,
            CustomerDataAreaId = item.CustomerDataAreaId,
            TransactionType = item.TransactionType,
            ValidationStatus = item.ValidationStatus,
            TotalQuantity = item.TotalQuantity,
            LicencesUsed = item.LicencesUsed?.Select(LicenceReportDetailDto.FromDomain).ToList()
        };
    }
}

/// <summary>
/// Licence report detail DTO.
/// </summary>
public class LicenceReportDetailDto
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;

    public static LicenceReportDetailDto FromDomain(LicenceReportDetail detail)
    {
        return new LicenceReportDetailDto
        {
            LicenceId = detail.LicenceId,
            LicenceNumber = detail.LicenceNumber,
            Status = detail.Status,
            IssuingAuthority = detail.IssuingAuthority
        };
    }
}

/// <summary>
/// Licence usage report response DTO.
/// </summary>
public class LicenceUsageReportDto
{
    /// <summary>
    /// Date the report was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// Start date of the reporting period.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date of the reporting period.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// List of licence usages in the report.
    /// </summary>
    public List<LicenceUsageItemDto> LicenceUsages { get; set; } = new();

    /// <summary>
    /// Total count of licences.
    /// </summary>
    public int TotalLicences { get; set; }

    /// <summary>
    /// Total count of transactions using these licences.
    /// </summary>
    public int TotalTransactions { get; set; }

    public static LicenceUsageReportDto FromDomain(LicenceUsageReport report)
    {
        return new LicenceUsageReportDto
        {
            GeneratedDate = report.GeneratedDate,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            LicenceUsages = report.LicenceUsages.Select(LicenceUsageItemDto.FromDomain).ToList(),
            TotalLicences = report.TotalLicences,
            TotalTransactions = report.TotalTransactions
        };
    }
}

/// <summary>
/// Licence usage item DTO.
/// </summary>
public class LicenceUsageItemDto
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalQuantityProcessed { get; set; }
    public DateTime? LastUsedDate { get; set; }

    public static LicenceUsageItemDto FromDomain(LicenceUsageReportItem item)
    {
        return new LicenceUsageItemDto
        {
            LicenceId = item.LicenceId,
            LicenceNumber = item.LicenceNumber,
            Status = item.Status,
            IssuingAuthority = item.IssuingAuthority,
            ExpiryDate = item.ExpiryDate,
            TransactionCount = item.TransactionCount,
            TotalQuantityProcessed = item.TotalQuantityProcessed,
            LastUsedDate = item.LastUsedDate
        };
    }
}

/// <summary>
/// Customer compliance history report response DTO.
/// </summary>
public class CustomerComplianceHistoryReportDto
{
    /// <summary>
    /// Date the report was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// Customer account number.
    /// </summary>
    public string CustomerAccount { get; set; } = string.Empty;

    /// <summary>
    /// Data area ID for the customer.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Business category.
    /// </summary>
    public string BusinessCategory { get; set; } = string.Empty;

    /// <summary>
    /// Current approval status.
    /// </summary>
    public string ApprovalStatus { get; set; } = string.Empty;

    /// <summary>
    /// List of compliance events.
    /// </summary>
    public List<ComplianceEventItemDto> Events { get; set; } = new();

    /// <summary>
    /// Current licences (if requested).
    /// </summary>
    public List<LicenceStatusItemDto>? CurrentLicences { get; set; }

    public static CustomerComplianceHistoryReportDto FromDomain(CustomerComplianceHistoryReport report)
    {
        return new CustomerComplianceHistoryReportDto
        {
            GeneratedDate = report.GeneratedDate,
            CustomerAccount = report.CustomerAccount,
            DataAreaId = report.DataAreaId,
            CustomerName = report.CustomerName,
            BusinessCategory = report.BusinessCategory,
            ApprovalStatus = report.ApprovalStatus,
            Events = report.Events.Select(ComplianceEventItemDto.FromDomain).ToList(),
            CurrentLicences = report.CurrentLicences?.Select(LicenceStatusItemDto.FromDomain).ToList()
        };
    }
}

/// <summary>
/// Compliance event item DTO.
/// </summary>
public class ComplianceEventItemDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }

    public static ComplianceEventItemDto FromDomain(ComplianceEventItem item)
    {
        return new ComplianceEventItemDto
        {
            EventId = item.EventId,
            EventType = item.EventType,
            EventDate = item.EventDate,
            PerformedBy = item.PerformedBy,
            Details = item.Details
        };
    }
}

/// <summary>
/// Licence status item DTO.
/// </summary>
public class LicenceStatusItemDto
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;

    public static LicenceStatusItemDto FromDomain(LicenceStatusItem item)
    {
        return new LicenceStatusItemDto
        {
            LicenceId = item.LicenceId,
            LicenceNumber = item.LicenceNumber,
            Status = item.Status,
            ExpiryDate = item.ExpiryDate,
            IssuingAuthority = item.IssuingAuthority
        };
    }
}

/// <summary>
/// Licence correction impact report response DTO.
/// T163c/SC-038: Response for historical validation impact analysis.
/// </summary>
public class LicenceCorrectionImpactReportDto
{
    /// <summary>
    /// Date the report was generated.
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// ID of the corrected licence.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Licence number.
    /// </summary>
    public string LicenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date the correction was made.
    /// </summary>
    public DateTime CorrectionDate { get; set; }

    /// <summary>
    /// Original effective date (if changed).
    /// </summary>
    public DateTime? OriginalEffectiveDate { get; set; }

    /// <summary>
    /// Corrected effective date.
    /// </summary>
    public DateTime? CorrectedEffectiveDate { get; set; }

    /// <summary>
    /// Original expiry date (if changed).
    /// </summary>
    public DateOnly? OriginalExpiryDate { get; set; }

    /// <summary>
    /// Corrected expiry date.
    /// </summary>
    public DateOnly? CorrectedExpiryDate { get; set; }

    /// <summary>
    /// Start of analysis window.
    /// </summary>
    public DateTime AnalysisFromDate { get; set; }

    /// <summary>
    /// End of analysis window.
    /// </summary>
    public DateTime AnalysisToDate { get; set; }

    /// <summary>
    /// Transactions affected by the correction.
    /// </summary>
    public List<LicenceCorrectionImpactItemDto> AffectedTransactions { get; set; } = new();

    /// <summary>
    /// Total transactions analyzed.
    /// </summary>
    public int TotalTransactionsAnalyzed { get; set; }

    /// <summary>
    /// Total affected transactions.
    /// </summary>
    public int TotalAffectedTransactions { get; set; }

    /// <summary>
    /// Count of transactions with critical impact.
    /// </summary>
    public int CriticalImpactCount { get; set; }

    /// <summary>
    /// Count of transactions with major impact.
    /// </summary>
    public int MajorImpactCount { get; set; }

    /// <summary>
    /// Count of transactions with minor impact.
    /// </summary>
    public int MinorImpactCount { get; set; }

    public static LicenceCorrectionImpactReportDto FromDomain(LicenceCorrectionImpactReport report)
    {
        return new LicenceCorrectionImpactReportDto
        {
            GeneratedDate = report.GeneratedDate,
            LicenceId = report.LicenceId,
            LicenceNumber = report.LicenceNumber,
            CorrectionDate = report.CorrectionDate,
            OriginalEffectiveDate = report.OriginalEffectiveDate,
            CorrectedEffectiveDate = report.CorrectedEffectiveDate,
            OriginalExpiryDate = report.OriginalExpiryDate,
            CorrectedExpiryDate = report.CorrectedExpiryDate,
            AnalysisFromDate = report.AnalysisFromDate,
            AnalysisToDate = report.AnalysisToDate,
            AffectedTransactions = report.AffectedTransactions.Select(LicenceCorrectionImpactItemDto.FromDomain).ToList(),
            TotalTransactionsAnalyzed = report.TotalTransactionsAnalyzed,
            TotalAffectedTransactions = report.TotalAffectedTransactions,
            CriticalImpactCount = report.CriticalImpactCount,
            MajorImpactCount = report.MajorImpactCount,
            MinorImpactCount = report.MinorImpactCount
        };
    }
}

/// <summary>
/// Licence correction impact item DTO.
/// </summary>
public class LicenceCorrectionImpactItemDto
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string OriginalStatus { get; set; } = string.Empty;
    public string CorrectedStatus { get; set; } = string.Empty;
    public string ImpactSeverity { get; set; } = string.Empty;
    public string ImpactDetails { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }

    public static LicenceCorrectionImpactItemDto FromDomain(LicenceCorrectionImpactItem item)
    {
        return new LicenceCorrectionImpactItemDto
        {
            TransactionId = item.TransactionId,
            ExternalTransactionId = item.ExternalTransactionId,
            TransactionDate = item.TransactionDate,
            CustomerAccount = item.CustomerAccount,
            CustomerDataAreaId = item.CustomerDataAreaId,
            CustomerName = item.CustomerName,
            OriginalStatus = item.OriginalStatus,
            CorrectedStatus = item.CorrectedStatus,
            ImpactSeverity = item.ImpactSeverity.ToString(),
            ImpactDetails = item.ImpactDetails,
            RequiresReview = item.RequiresReview
        };
    }
}

#endregion
