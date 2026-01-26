using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;

namespace RE2.ComplianceFunctions;

/// <summary>
/// Azure Function for generating periodic compliance reports.
/// T168: Timer trigger runs weekly on Sunday at 3 AM UTC.
/// Generates summary reports for transaction audits, licence usage, and customer compliance.
/// </summary>
public class ComplianceReportGenerator
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ComplianceReportGenerator> _logger;

    public ComplianceReportGenerator(
        IReportingService reportingService,
        ILogger<ComplianceReportGenerator> logger)
    {
        _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Timer-triggered function that runs weekly on Sunday at 3 AM UTC.
    /// Generates weekly compliance summary reports per User Story 5.
    /// CRON expression: "0 0 3 * * 0" = At 03:00 on Sunday
    /// </summary>
    /// <param name="timerInfo">Timer trigger information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Function("ComplianceReportGenerator")]
    public async Task Run(
        [TimerTrigger("0 0 3 * * 0")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ComplianceReportGenerator started at {Time}", DateTime.UtcNow);

        try
        {
            var reportResult = await GenerateWeeklyReportsAsync(cancellationToken);

            _logger.LogInformation(
                "ComplianceReportGenerator completed. Reports generated: TransactionAudit={TransactionCount}, LicenceUsage={LicenceCount}, Status={Status}",
                reportResult.TransactionAuditCount,
                reportResult.LicenceUsageCount,
                reportResult.Success ? "Success" : "Partial");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ComplianceReportGenerator");
            throw;
        }
    }

    /// <summary>
    /// HTTP-triggered function for manual report generation (admin use).
    /// Allows compliance managers to trigger report generation on demand.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Function("GenerateReportsManual")]
    public async Task<ReportGenerationResult> GenerateReportsManual(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "reports/generate")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual report generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var result = await GenerateWeeklyReportsAsync(cancellationToken);

            _logger.LogInformation(
                "Manual report generation completed. TransactionAudit={TransactionCount}, LicenceUsage={LicenceCount}",
                result.TransactionAuditCount,
                result.LicenceUsageCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual report generation");
            return new ReportGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// HTTP-triggered function for generating a specific report on demand.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="reportType">Type of report to generate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Function("GenerateSpecificReport")]
    public async Task<object?> GenerateSpecificReport(
        [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "reports/{reportType}")] HttpRequestData req,
        string reportType,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating {ReportType} report on demand at {Time}", reportType, DateTime.UtcNow);

        try
        {
            // Default to last 7 days for on-demand reports
            var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = toDate.AddDays(-7);

            return reportType.ToLowerInvariant() switch
            {
                "transaction-audit" => await GenerateTransactionAuditReportAsync(fromDate, toDate, cancellationToken),
                "licence-usage" => await GenerateLicenceUsageReportAsync(fromDate, toDate, cancellationToken),
                _ => new { error = $"Unknown report type: {reportType}. Valid types: transaction-audit, licence-usage" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating {ReportType} report", reportType);
            return new { error = ex.Message };
        }
    }

    private async Task<ReportGenerationResult> GenerateWeeklyReportsAsync(CancellationToken cancellationToken)
    {
        var result = new ReportGenerationResult
        {
            Success = true,
            GeneratedAt = DateTime.UtcNow
        };

        // Calculate last week's date range (Sunday to Saturday)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastSunday = today.AddDays(-(int)today.DayOfWeek - 7);
        var lastSaturday = lastSunday.AddDays(6);

        _logger.LogInformation(
            "Generating weekly reports for period {FromDate} to {ToDate}",
            lastSunday, lastSaturday);

        // Generate transaction audit report
        try
        {
            var transactionReport = await GenerateTransactionAuditReportAsync(lastSunday, lastSaturday, cancellationToken);
            result.TransactionAuditCount = transactionReport?.Transactions?.Count ?? 0;
            _logger.LogInformation(
                "Generated transaction audit report with {Count} transactions",
                result.TransactionAuditCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate transaction audit report");
            result.Errors.Add($"Transaction audit: {ex.Message}");
            result.Success = false;
        }

        // Generate licence usage report
        try
        {
            var licenceReport = await GenerateLicenceUsageReportAsync(lastSunday, lastSaturday, cancellationToken);
            result.LicenceUsageCount = licenceReport?.TotalLicences ?? 0;
            _logger.LogInformation(
                "Generated licence usage report with {Count} licences",
                result.LicenceUsageCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate licence usage report");
            result.Errors.Add($"Licence usage: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    private async Task<RE2.ComplianceCore.Services.Reporting.TransactionAuditReport?> GenerateTransactionAuditReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var criteria = new RE2.ComplianceCore.Services.Reporting.TransactionAuditReportCriteria
        {
            FromDate = fromDate.ToDateTime(TimeOnly.MinValue),
            ToDate = toDate.ToDateTime(TimeOnly.MaxValue),
            SubstanceId = null,
            CustomerId = null,
            CountryCode = null,
            IncludeLicenceDetails = true
        };

        return await _reportingService.GenerateTransactionAuditReportAsync(criteria, cancellationToken);
    }

    private async Task<RE2.ComplianceCore.Services.Reporting.LicenceUsageReport?> GenerateLicenceUsageReportAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var criteria = new RE2.ComplianceCore.Services.Reporting.LicenceUsageReportCriteria
        {
            FromDate = fromDate.ToDateTime(TimeOnly.MinValue),
            ToDate = toDate.ToDateTime(TimeOnly.MaxValue),
            LicenceId = null,
            HolderId = null,
            HolderType = null
        };

        return await _reportingService.GenerateLicenceUsageReportAsync(criteria, cancellationToken);
    }
}

/// <summary>
/// Result of report generation operation.
/// </summary>
public class ReportGenerationResult
{
    public bool Success { get; set; }
    public int TransactionAuditCount { get; set; }
    public int LicenceUsageCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
}
