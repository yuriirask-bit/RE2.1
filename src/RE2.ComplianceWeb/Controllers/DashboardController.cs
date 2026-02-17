using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.AlertGeneration;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for the compliance dashboard.
/// T122: Alert display dashboard per FR-007 and FR-017.
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly AlertGenerationService _alertService;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRegulatoryInspectionRepository _inspectionRepository;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        AlertGenerationService alertService,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        IRegulatoryInspectionRepository inspectionRepository,
        ILogger<DashboardController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _inspectionRepository = inspectionRepository ?? throw new ArgumentNullException(nameof(inspectionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the main compliance dashboard.
    /// T122: Dashboard with alert summary cards, recent alerts table, and compliance overview.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get alert dashboard summary
            var alertSummary = await _alertService.GetDashboardSummaryAsync(cancellationToken);

            // Get licence statistics
            var licences = await _licenceRepository.GetAllAsync(cancellationToken);
            var activeLicenceCount = licences.Count(l => l.Status == "Valid" && !l.IsExpired());

            // Get customer statistics
            var customers = await _customerRepository.GetAllAsync(cancellationToken);
            var qualifiedCustomerCount = customers.Count(c => c.ApprovalStatus == RE2.ComplianceCore.Models.ApprovalStatus.Approved);
            var pendingQualificationCount = customers.Count(c => c.ApprovalStatus == RE2.ComplianceCore.Models.ApprovalStatus.Pending);
            var suspendedCustomerCount = customers.Count(c => c.IsSuspended);

            // Get inspection statistics
            var allInspections = (await _inspectionRepository.GetAllAsync(cancellationToken)).ToList();
            var overdueInspections = (await _inspectionRepository.GetWithOverdueCorrectiveActionsAsync(cancellationToken)).ToList();
            var recentInspections = allInspections
                .Where(i => i.InspectionDate >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)))
                .ToList();

            var viewModel = new DashboardViewModel
            {
                AlertSummary = alertSummary,
                ActiveLicenceCount = activeLicenceCount,
                QualifiedCustomerCount = qualifiedCustomerCount,
                PendingQualificationCount = pendingQualificationCount,
                SuspendedCustomerCount = suspendedCustomerCount,
                TotalInspections = allInspections.Count,
                RecentInspections = recentInspections.Count,
                OverdueCorrectiveActions = overdueInspections.Count,
                InspectionsWithFindings = allInspections.Count(i =>
                    i.Outcome == InspectionOutcome.MinorFindings ||
                    i.Outcome == InspectionOutcome.MajorFindings ||
                    i.Outcome == InspectionOutcome.CriticalFindings)
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");

            // Return empty dashboard with error message
            TempData["ErrorMessage"] = "Error loading dashboard data. Please try again.";
            return View(new DashboardViewModel());
        }
    }

    /// <summary>
    /// Displays the compliance risk dashboard.
    /// T177: Compliance risk dashboard per FR-032.
    /// T178: Highlights customers with expiring licences, blocked orders, abnormal volumes.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ComplianceRisks(CancellationToken cancellationToken = default)
    {
        try
        {
            var licences = (await _licenceRepository.GetAllAsync(cancellationToken)).ToList();
            var customers = (await _customerRepository.GetAllAsync(cancellationToken)).ToList();
            var alertSummary = await _alertService.GetDashboardSummaryAsync(cancellationToken);

            var expiringLicences = licences
                .Where(l => l.Status == "Valid" && l.ExpiryDate.HasValue &&
                            l.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)) &&
                            l.ExpiryDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                .OrderBy(l => l.ExpiryDate)
                .ToList();

            var expiredLicences = licences
                .Where(l => l.IsExpired())
                .ToList();

            var suspendedCustomers = customers
                .Where(c => c.IsSuspended)
                .ToList();

            var pendingReVerification = customers
                .Where(c => c.NextReVerificationDate.HasValue &&
                            c.NextReVerificationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))
                .OrderBy(c => c.NextReVerificationDate)
                .ToList();

            var viewModel = new ComplianceRiskDashboardViewModel
            {
                ExpiringLicences = expiringLicences,
                ExpiredLicences = expiredLicences,
                SuspendedCustomers = suspendedCustomers,
                PendingReVerificationCustomers = pendingReVerification,
                TotalActiveLicences = licences.Count(l => l.Status == "Valid" && !l.IsExpired()),
                TotalCustomers = customers.Count,
                CriticalAlertCount = alertSummary?.CriticalCount ?? 0,
                WarningAlertCount = alertSummary?.WarningCount ?? 0
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compliance risk dashboard");
            TempData["ErrorMessage"] = "Error loading compliance risk data. Please try again.";
            return View(new ComplianceRiskDashboardViewModel());
        }
    }
}

/// <summary>
/// View model for the compliance dashboard.
/// T122: Dashboard data structure.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Alert summary statistics.
    /// </summary>
    public AlertDashboardSummary? AlertSummary { get; set; }

    /// <summary>
    /// Count of active (valid, non-expired) licences.
    /// </summary>
    public int ActiveLicenceCount { get; set; }

    /// <summary>
    /// Count of qualified customers.
    /// </summary>
    public int QualifiedCustomerCount { get; set; }

    /// <summary>
    /// Count of customers pending qualification.
    /// </summary>
    public int PendingQualificationCount { get; set; }

    /// <summary>
    /// Count of suspended customers.
    /// </summary>
    public int SuspendedCustomerCount { get; set; }

    /// <summary>
    /// Count of licences expiring within 90 days (from source data, not alerts).
    /// </summary>
    public int ExpiringLicenceCount { get; set; }

    /// <summary>
    /// Count of customers with re-verification due within 30 days (from source data, not alerts).
    /// </summary>
    public int ReVerificationDueCount { get; set; }

    /// <summary>
    /// Count of expired licences (from source data, not alerts).
    /// </summary>
    public int ExpiredLicenceCount { get; set; }

    /// <summary>
    /// Total number of regulatory inspections on record.
    /// </summary>
    public int TotalInspections { get; set; }

    /// <summary>
    /// Number of inspections in the last 90 days.
    /// </summary>
    public int RecentInspections { get; set; }

    /// <summary>
    /// Number of inspections with overdue corrective actions.
    /// </summary>
    public int OverdueCorrectiveActions { get; set; }

    /// <summary>
    /// Number of inspections that had findings (minor, major, or critical).
    /// </summary>
    public int InspectionsWithFindings { get; set; }
}

/// <summary>
/// T177: View model for the compliance risk dashboard per FR-032.
/// T178: Highlights customers with expiring licences, blocked orders, abnormal volumes.
/// </summary>
public class ComplianceRiskDashboardViewModel
{
    public List<RE2.ComplianceCore.Models.Licence> ExpiringLicences { get; set; } = new();
    public List<RE2.ComplianceCore.Models.Licence> ExpiredLicences { get; set; } = new();
    public List<RE2.ComplianceCore.Models.Customer> SuspendedCustomers { get; set; } = new();
    public List<RE2.ComplianceCore.Models.Customer> PendingReVerificationCustomers { get; set; } = new();
    public int TotalActiveLicences { get; set; }
    public int TotalCustomers { get; set; }
    public int CriticalAlertCount { get; set; }
    public int WarningAlertCount { get; set; }
}
