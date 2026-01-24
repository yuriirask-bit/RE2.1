using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.AlertGeneration;
using RE2.ComplianceWeb.Models;

namespace RE2.ComplianceWeb.Controllers;

public class HomeController : Controller
{
    private readonly AlertGenerationService _alertService;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        AlertGenerationService alertService,
        ILicenceRepository licenceRepository,
        ICustomerRepository customerRepository,
        ILogger<HomeController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the compliance dashboard on the home page.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get alert dashboard summary
            var alertSummary = await _alertService.GetDashboardSummaryAsync(cancellationToken);

            // Get licence statistics
            var licencesList = (await _licenceRepository.GetAllAsync(cancellationToken)).ToList();
            var activeLicenceCount = licencesList.Count(l => l.Status == "Valid" && !l.IsExpired());
            var expiredLicenceCount = licencesList.Count(l => l.IsExpired());

            // Get expiring licences count from source data (not alerts)
            var expiringLicences = await _licenceRepository.GetExpiringLicencesAsync(90, cancellationToken);
            var expiringLicenceCount = expiringLicences.Count();

            // Get customer statistics
            var customers = await _customerRepository.GetAllAsync(cancellationToken);
            var qualifiedCustomerCount = customers.Count(c => c.ApprovalStatus == RE2.ComplianceCore.Models.ApprovalStatus.Approved);
            var pendingQualificationCount = customers.Count(c => c.ApprovalStatus == RE2.ComplianceCore.Models.ApprovalStatus.Pending);
            var suspendedCustomerCount = customers.Count(c => c.IsSuspended);

            // Get re-verification due count from source data (not alerts)
            var reVerificationDue = await _customerRepository.GetReVerificationDueAsync(90, cancellationToken);
            var reVerificationDueCount = reVerificationDue.Count();

            var viewModel = new DashboardViewModel
            {
                AlertSummary = alertSummary,
                ActiveLicenceCount = activeLicenceCount,
                ExpiringLicenceCount = expiringLicenceCount,
                ExpiredLicenceCount = expiredLicenceCount,
                QualifiedCustomerCount = qualifiedCustomerCount,
                PendingQualificationCount = pendingQualificationCount,
                SuspendedCustomerCount = suspendedCustomerCount,
                ReVerificationDueCount = reVerificationDueCount
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
