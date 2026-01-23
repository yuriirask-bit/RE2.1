using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.AlertGeneration;

/// <summary>
/// Unified service for generating compliance alerts across multiple entity types.
/// T120: AlertGenerationService supporting Licence, GdpCredential, Customer re-verification alerts.
/// Per FR-007: Generate alerts at 90/60/30 day intervals before licence expiry.
/// </summary>
public class AlertGenerationService
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<AlertGenerationService> _logger;

    /// <summary>
    /// Default expiry warning thresholds in days (90, 60, 30 per FR-007).
    /// </summary>
    public static readonly int[] DefaultExpiryThresholds = { 90, 60, 30 };

    public AlertGenerationService(
        IAlertRepository alertRepository,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        ILogger<AlertGenerationService> logger)
    {
        _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Licence Alerts

    /// <summary>
    /// Generates expiry alerts for licences expiring within the specified period.
    /// Per FR-007: Generate alerts at 90/60/30 day intervals.
    /// </summary>
    /// <param name="daysAhead">Days ahead to check for expiring licences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of alerts generated.</returns>
    public async Task<int> GenerateLicenceExpiryAlertsAsync(
        int daysAhead = 90,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating licence expiry alerts for licences expiring within {Days} days", daysAhead);

        var expiringLicences = await _licenceRepository.GetExpiringLicencesAsync(daysAhead, cancellationToken);
        var alerts = new List<Alert>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var licence in expiringLicences)
        {
            if (!licence.ExpiryDate.HasValue) continue;

            // Calculate days until expiry
            var daysUntilExpiry = (licence.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days;
            if (daysUntilExpiry < 0) continue; // Skip already expired licences (handled separately)

            // Check if alert already exists to prevent duplicates
            var alertExists = await _alertRepository.ExistsAsync(
                AlertType.LicenceExpiring,
                TargetEntityType.Licence,
                licence.LicenceId,
                cancellationToken);

            if (alertExists)
            {
                _logger.LogDebug("Skipping duplicate alert for licence {LicenceId}", licence.LicenceId);
                continue;
            }

            var alert = Alert.CreateLicenceExpiryAlert(
                licence.LicenceId,
                licence.LicenceNumber,
                daysUntilExpiry,
                licence.ExpiryDate.Value);

            alerts.Add(alert);
        }

        if (alerts.Any())
        {
            await _alertRepository.CreateBatchAsync(alerts, cancellationToken);
            _logger.LogInformation("Generated {Count} licence expiry alerts", alerts.Count);
        }
        else
        {
            _logger.LogInformation("No new licence expiry alerts to generate");
        }

        return alerts.Count;
    }

    /// <summary>
    /// Generates alerts for already expired licences.
    /// </summary>
    public async Task<int> GenerateExpiredLicenceAlertsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating alerts for expired licences");

        // Get all licences and filter for expired ones
        var allLicences = await _licenceRepository.GetAllAsync(cancellationToken);
        var expiredLicences = allLicences.Where(l => l.IsExpired()).ToList();

        var alerts = new List<Alert>();

        foreach (var licence in expiredLicences)
        {
            // Check if alert already exists
            var alertExists = await _alertRepository.ExistsAsync(
                AlertType.LicenceExpired,
                TargetEntityType.Licence,
                licence.LicenceId,
                cancellationToken);

            if (alertExists) continue;

            var alert = new Alert
            {
                AlertId = Guid.NewGuid(),
                AlertType = AlertType.LicenceExpired,
                Severity = AlertSeverity.Critical,
                TargetEntityType = TargetEntityType.Licence,
                TargetEntityId = licence.LicenceId,
                GeneratedDate = DateTime.UtcNow,
                Message = $"Licence {licence.LicenceNumber} has expired",
                Details = $"Expired on {licence.ExpiryDate:yyyy-MM-dd}"
            };

            alerts.Add(alert);
        }

        if (alerts.Any())
        {
            await _alertRepository.CreateBatchAsync(alerts, cancellationToken);
            _logger.LogInformation("Generated {Count} expired licence alerts", alerts.Count);
        }

        return alerts.Count;
    }

    #endregion

    #region Customer Re-verification Alerts

    /// <summary>
    /// Generates alerts for customers requiring re-verification.
    /// Per FR-017: Alert when customer re-verification is due.
    /// </summary>
    public async Task<int> GenerateCustomerReVerificationAlertsAsync(
        int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating customer re-verification alerts for due within {Days} days", daysAhead);

        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        var alerts = new List<Alert>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(daysAhead);

        foreach (var customer in customers)
        {
            if (!customer.NextReVerificationDate.HasValue) continue;
            if (customer.NextReVerificationDate.Value > threshold) continue;

            // Check if alert already exists
            var alertExists = await _alertRepository.ExistsAsync(
                AlertType.ReVerificationDue,
                TargetEntityType.Customer,
                customer.CustomerId,
                cancellationToken);

            if (alertExists) continue;

            var alert = Alert.CreateReVerificationAlert(
                customer.CustomerId,
                customer.BusinessName,
                customer.NextReVerificationDate.Value);

            alerts.Add(alert);
        }

        if (alerts.Any())
        {
            await _alertRepository.CreateBatchAsync(alerts, cancellationToken);
            _logger.LogInformation("Generated {Count} customer re-verification alerts", alerts.Count);
        }

        return alerts.Count;
    }

    #endregion

    #region Alert Management

    /// <summary>
    /// Gets all unacknowledged alerts.
    /// </summary>
    public async Task<IEnumerable<Alert>> GetUnacknowledgedAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await _alertRepository.GetUnacknowledgedAsync(cancellationToken);
    }

    /// <summary>
    /// Gets alerts with filtering options.
    /// </summary>
    public async Task<IEnumerable<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertSeverity? severity = null,
        TargetEntityType? entityType = null,
        bool? isAcknowledged = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        return await _alertRepository.GetAlertsAsync(
            type, severity, entityType, isAcknowledged, maxResults, cancellationToken);
    }

    /// <summary>
    /// Gets alerts for a specific entity.
    /// </summary>
    public async Task<IEnumerable<Alert>> GetAlertsForEntityAsync(
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _alertRepository.GetByTargetEntityAsync(entityType, entityId, cancellationToken);
    }

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    public async Task AcknowledgeAlertAsync(
        Guid alertId,
        Guid acknowledgedBy,
        string? acknowledgerName = null,
        CancellationToken cancellationToken = default)
    {
        await _alertRepository.AcknowledgeAsync(alertId, acknowledgedBy, acknowledgerName, cancellationToken);
    }

    /// <summary>
    /// Gets summary statistics for the alert dashboard.
    /// T122: Dashboard data provider.
    /// </summary>
    public async Task<AlertDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var unacknowledged = (await _alertRepository.GetUnacknowledgedAsync(cancellationToken)).ToList();
        var overdue = (await _alertRepository.GetOverdueAlertsAsync(cancellationToken)).ToList();

        return new AlertDashboardSummary
        {
            TotalUnacknowledged = unacknowledged.Count,
            CriticalCount = unacknowledged.Count(a => a.Severity == AlertSeverity.Critical),
            WarningCount = unacknowledged.Count(a => a.Severity == AlertSeverity.Warning),
            InfoCount = unacknowledged.Count(a => a.Severity == AlertSeverity.Info),
            OverdueCount = overdue.Count,
            LicenceExpiringCount = unacknowledged.Count(a => a.AlertType == AlertType.LicenceExpiring),
            LicenceExpiredCount = unacknowledged.Count(a => a.AlertType == AlertType.LicenceExpired),
            ReVerificationCount = unacknowledged.Count(a => a.AlertType == AlertType.ReVerificationDue),
            MissingDocumentationCount = unacknowledged.Count(a => a.AlertType == AlertType.MissingDocumentation),
            RecentAlerts = unacknowledged.Take(10).ToList()
        };
    }

    #endregion

    #region Helper Methods

    private static (AlertSeverity Severity, string Message) GetExpiryAlertDetails(int daysUntilExpiry, string licenceNumber)
    {
        if (daysUntilExpiry <= 30)
        {
            return (AlertSeverity.Critical, $"Licence {licenceNumber} expires in {daysUntilExpiry} days - URGENT ACTION REQUIRED");
        }
        else if (daysUntilExpiry <= 60)
        {
            return (AlertSeverity.Warning, $"Licence {licenceNumber} expires in {daysUntilExpiry} days - action required");
        }
        else
        {
            return (AlertSeverity.Info, $"Licence {licenceNumber} expires in {daysUntilExpiry} days - renewal recommended");
        }
    }

    #endregion
}

/// <summary>
/// Summary statistics for the alert dashboard.
/// T122: Data structure for dashboard display.
/// </summary>
public class AlertDashboardSummary
{
    public int TotalUnacknowledged { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int OverdueCount { get; set; }
    public int LicenceExpiringCount { get; set; }
    public int LicenceExpiredCount { get; set; }
    public int ReVerificationCount { get; set; }
    public int MissingDocumentationCount { get; set; }
    public List<Alert> RecentAlerts { get; set; } = new();
}
