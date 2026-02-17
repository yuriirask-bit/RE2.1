using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.AlertGeneration;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for alert management.
/// T122: Alert management UI supporting the compliance dashboard.
/// </summary>
[Authorize]
public class AlertsController : Controller
{
    private readonly AlertGenerationService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        AlertGenerationService alertService,
        ILogger<AlertsController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the alert list with optional filtering.
    /// </summary>
    public async Task<IActionResult> Index(
        AlertSeverity? severity = null,
        AlertType? type = null,
        bool showAcknowledged = false,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _alertService.GetAlertsAsync(
            type: type,
            severity: severity,
            isAcknowledged: showAcknowledged ? null : false,
            maxResults: 100,
            cancellationToken: cancellationToken);

        ViewBag.SeverityFilter = severity;
        ViewBag.TypeFilter = type;
        ViewBag.ShowAcknowledged = showAcknowledged;

        return View(alerts);
    }

    /// <summary>
    /// Displays alert details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        // Get all alerts and find the one we need
        var alerts = await _alertService.GetAlertsAsync(
            maxResults: 1000,
            cancellationToken: cancellationToken);

        var alert = alerts.FirstOrDefault(a => a.AlertId == id);

        if (alert == null)
        {
            return NotFound();
        }

        return View(alert);
    }

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        var userName = User.Identity?.Name;

        await _alertService.AcknowledgeAlertAsync(
            id,
            Guid.TryParse(userId, out var uid) ? uid : Guid.Empty,
            userName,
            cancellationToken);

        TempData["SuccessMessage"] = "Alert acknowledged successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bulk acknowledge alerts page.
    /// </summary>
    public async Task<IActionResult> BulkAcknowledge(CancellationToken cancellationToken = default)
    {
        var alerts = await _alertService.GetUnacknowledgedAlertsAsync(cancellationToken);
        return View(alerts);
    }

    /// <summary>
    /// Handles bulk acknowledgment of selected alerts.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAcknowledge(Guid[] alertIds, CancellationToken cancellationToken = default)
    {
        if (alertIds == null || alertIds.Length == 0)
        {
            TempData["ErrorMessage"] = "No alerts selected for acknowledgment.";
            return RedirectToAction(nameof(BulkAcknowledge));
        }

        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        var userName = User.Identity?.Name;
        var acknowledgedBy = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty;

        var acknowledgedCount = 0;
        foreach (var alertId in alertIds)
        {
            try
            {
                await _alertService.AcknowledgeAlertAsync(alertId, acknowledgedBy, userName, cancellationToken);
                acknowledgedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acknowledge alert {AlertId}", alertId);
            }
        }

        TempData["SuccessMessage"] = $"{acknowledgedCount} alert(s) acknowledged successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Manually triggers alert generation (ComplianceManager only).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> GenerateManual(CancellationToken cancellationToken = default)
    {
        try
        {
            var licenceExpiryAlerts = await _alertService.GenerateLicenceExpiryAlertsAsync(90, cancellationToken);
            var expiredAlerts = await _alertService.GenerateExpiredLicenceAlertsAsync(cancellationToken);
            var reVerificationAlerts = await _alertService.GenerateCustomerReVerificationAlertsAsync(30, cancellationToken);

            var totalAlerts = licenceExpiryAlerts + expiredAlerts + reVerificationAlerts;
            TempData["SuccessMessage"] = $"Alert generation completed. {totalAlerts} new alert(s) created.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating alerts manually");
            TempData["ErrorMessage"] = "Error generating alerts. Please check the logs.";
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
