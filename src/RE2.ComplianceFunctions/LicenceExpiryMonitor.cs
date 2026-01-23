using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Services.AlertGeneration;

namespace RE2.ComplianceFunctions;

/// <summary>
/// Azure Function for monitoring licence expiry and generating alerts.
/// T121: Timer trigger runs daily at 2 AM UTC to check for expiring licences.
/// Per FR-007: Generate alerts at 90/60/30 day intervals before licence expiry.
/// </summary>
public class LicenceExpiryMonitor
{
    private readonly AlertGenerationService _alertService;
    private readonly ILogger<LicenceExpiryMonitor> _logger;

    public LicenceExpiryMonitor(
        AlertGenerationService alertService,
        ILogger<LicenceExpiryMonitor> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Timer-triggered function that runs daily at 2 AM UTC.
    /// Generates licence expiry alerts and customer re-verification alerts.
    /// CRON expression: "0 0 2 * * *" = At 02:00 every day
    /// </summary>
    /// <param name="timerInfo">Timer trigger information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Function("LicenceExpiryMonitor")]
    public async Task Run(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("LicenceExpiryMonitor started at {Time}", DateTime.UtcNow);

        try
        {
            // Generate alerts for licences expiring within 90 days (FR-007)
            var expiryAlertCount = await _alertService.GenerateLicenceExpiryAlertsAsync(90, cancellationToken);
            _logger.LogInformation("Generated {Count} licence expiry alerts (90-day window)", expiryAlertCount);

            // Generate alerts for already expired licences
            var expiredAlertCount = await _alertService.GenerateExpiredLicenceAlertsAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} expired licence alerts", expiredAlertCount);

            // Generate alerts for customer re-verifications due within 30 days (FR-017)
            var reVerificationAlertCount = await _alertService.GenerateCustomerReVerificationAlertsAsync(30, cancellationToken);
            _logger.LogInformation("Generated {Count} customer re-verification alerts", reVerificationAlertCount);

            var totalAlerts = expiryAlertCount + expiredAlertCount + reVerificationAlertCount;
            _logger.LogInformation(
                "LicenceExpiryMonitor completed. Total alerts generated: {Total} (Expiring: {Expiring}, Expired: {Expired}, ReVerification: {ReVerification})",
                totalAlerts, expiryAlertCount, expiredAlertCount, reVerificationAlertCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LicenceExpiryMonitor");
            throw;
        }
    }

    /// <summary>
    /// HTTP-triggered function for manual alert generation (admin use).
    /// Allows compliance managers to trigger alert generation on demand.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Function("GenerateAlertsManual")]
    public async Task<AlertGenerationResult> GenerateAlertsManual(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "alerts/generate")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual alert generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var expiryAlertCount = await _alertService.GenerateLicenceExpiryAlertsAsync(90, cancellationToken);
            var expiredAlertCount = await _alertService.GenerateExpiredLicenceAlertsAsync(cancellationToken);
            var reVerificationAlertCount = await _alertService.GenerateCustomerReVerificationAlertsAsync(30, cancellationToken);

            var result = new AlertGenerationResult
            {
                Success = true,
                ExpiryAlertsGenerated = expiryAlertCount,
                ExpiredAlertsGenerated = expiredAlertCount,
                ReVerificationAlertsGenerated = reVerificationAlertCount,
                TotalAlertsGenerated = expiryAlertCount + expiredAlertCount + reVerificationAlertCount,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Manual alert generation completed. Total: {Total}", result.TotalAlertsGenerated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual alert generation");
            return new AlertGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Result of manual alert generation.
/// </summary>
public class AlertGenerationResult
{
    public bool Success { get; set; }
    public int ExpiryAlertsGenerated { get; set; }
    public int ExpiredAlertsGenerated { get; set; }
    public int ReVerificationAlertsGenerated { get; set; }
    public int TotalAlertsGenerated { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
