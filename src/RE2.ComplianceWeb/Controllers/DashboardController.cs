using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
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
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        AlertGenerationService alertService,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        ILogger<DashboardController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
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

            var viewModel = new DashboardViewModel
            {
                AlertSummary = alertSummary,
                ActiveLicenceCount = activeLicenceCount,
                QualifiedCustomerCount = qualifiedCustomerCount,
                PendingQualificationCount = pendingQualificationCount,
                SuspendedCustomerCount = suspendedCustomerCount
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
}
