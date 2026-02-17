using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Services.AlertGeneration;

namespace RE2.ComplianceFunctions;

/// <summary>
/// Azure Function for monitoring GDP credential expiry and generating alerts.
/// T241: Timer trigger runs daily at 3 AM UTC (1 hour after LicenceExpiryMonitor).
/// Per FR-043: Automated expiry monitoring for GDP credentials, provider re-qualification, and overdue CAPAs.
/// </summary>
public class GdpCertificateMonitor
{
    private readonly AlertGenerationService _alertService;
    private readonly ILogger<GdpCertificateMonitor> _logger;

    public GdpCertificateMonitor(
        AlertGenerationService alertService,
        ILogger<GdpCertificateMonitor> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Timer-triggered function that runs daily at 3 AM UTC.
    /// Generates GDP credential expiry, provider re-qualification, and CAPA overdue alerts.
    /// CRON expression: "0 0 3 * * *" = At 03:00 every day
    /// </summary>
    [Function("GdpCertificateMonitor")]
    public async Task Run(
        [TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GdpCertificateMonitor started at {Time}", DateTime.UtcNow);

        try
        {
            // Generate alerts for GDP credentials expiring within 90 days (FR-043)
            var credentialExpiryCount = await _alertService.GenerateGdpCredentialExpiryAlertsAsync(90, cancellationToken);
            _logger.LogInformation("Generated {Count} GDP credential expiry alerts (90-day window)", credentialExpiryCount);

            // Generate alerts for providers needing re-qualification (FR-039)
            var requalificationCount = await _alertService.GenerateProviderRequalificationAlertsAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} provider re-qualification alerts", requalificationCount);

            // Generate alerts for overdue CAPAs (FR-042)
            var capaOverdueCount = await _alertService.GenerateCapaOverdueAlertsAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} CAPA overdue alerts", capaOverdueCount);

            var totalAlerts = credentialExpiryCount + requalificationCount + capaOverdueCount;
            _logger.LogInformation(
                "GdpCertificateMonitor completed. Total alerts generated: {Total} (CredentialExpiry: {CredentialExpiry}, Requalification: {Requalification}, CapaOverdue: {CapaOverdue})",
                totalAlerts, credentialExpiryCount, requalificationCount, capaOverdueCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GdpCertificateMonitor");
            throw;
        }
    }

    /// <summary>
    /// HTTP-triggered function for manual GDP alert generation (admin use).
    /// Allows compliance managers to trigger GDP alert generation on demand.
    /// </summary>
    [Function("GenerateGdpAlertsManual")]
    public async Task<GdpAlertGenerationResult> GenerateGdpAlertsManual(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "gdp-alerts/generate")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual GDP alert generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var credentialExpiryCount = await _alertService.GenerateGdpCredentialExpiryAlertsAsync(90, cancellationToken);
            var requalificationCount = await _alertService.GenerateProviderRequalificationAlertsAsync(cancellationToken);
            var capaOverdueCount = await _alertService.GenerateCapaOverdueAlertsAsync(cancellationToken);

            var result = new GdpAlertGenerationResult
            {
                Success = true,
                CredentialExpiryAlertsGenerated = credentialExpiryCount,
                RequalificationAlertsGenerated = requalificationCount,
                CapaOverdueAlertsGenerated = capaOverdueCount,
                TotalAlertsGenerated = credentialExpiryCount + requalificationCount + capaOverdueCount,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Manual GDP alert generation completed. Total: {Total}", result.TotalAlertsGenerated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual GDP alert generation");
            return new GdpAlertGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Result of GDP manual alert generation.
/// </summary>
public class GdpAlertGenerationResult
{
    public bool Success { get; set; }
    public int CredentialExpiryAlertsGenerated { get; set; }
    public int RequalificationAlertsGenerated { get; set; }
    public int CapaOverdueAlertsGenerated { get; set; }
    public int TotalAlertsGenerated { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
