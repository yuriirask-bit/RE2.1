using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.Reporting;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for compliance reports.
/// T166: Reports UI controller per FR-026, FR-029.
/// </summary>
[Authorize(Policy = "ComplianceManagerOrQAUser")]
public class ReportsController : Controller
{
    private readonly IReportingService _reportingService;
    private readonly ILicenceCorrectionImpactService _licenceCorrectionImpactService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportingService reportingService,
        ILicenceCorrectionImpactService licenceCorrectionImpactService,
        ICustomerRepository customerRepository,
        IControlledSubstanceRepository substanceRepository,
        ILicenceRepository licenceRepository,
        ILogger<ReportsController> logger)
    {
        _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
        _licenceCorrectionImpactService = licenceCorrectionImpactService ?? throw new ArgumentNullException(nameof(licenceCorrectionImpactService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _substanceRepository = substanceRepository ?? throw new ArgumentNullException(nameof(substanceRepository));
        _licenceRepository = licenceRepository ?? throw new ArgumentNullException(nameof(licenceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reports index page with links to all available reports.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    #region Transaction Audit Report (FR-026)

    /// <summary>
    /// Transaction audit report form.
    /// </summary>
    public async Task<IActionResult> TransactionAudit(CancellationToken cancellationToken = default)
    {
        // Populate filter dropdowns
        ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
        ViewBag.Substances = await _substanceRepository.GetAllAsync(cancellationToken);

        return View(new TransactionAuditReportViewModel
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Generates and displays transaction audit report.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransactionAudit(
        TransactionAuditReportViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
            ViewBag.Substances = await _substanceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var criteria = new TransactionAuditReportCriteria
            {
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                SubstanceCode = model.SubstanceCode,
                CustomerAccount = model.CustomerAccount,
                CustomerDataAreaId = model.CustomerDataAreaId,
                CountryCode = model.CountryCode,
                IncludeLicenceDetails = model.IncludeLicenceDetails
            };

            var report = await _reportingService.GenerateTransactionAuditReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated transaction audit report: {Count} transactions from {FromDate} to {ToDate}",
                report.TotalCount, model.FromDate, model.ToDate);

            return View("TransactionAuditResult", report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating transaction audit report");
            TempData["ErrorMessage"] = "An error occurred generating the report.";
            ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
            ViewBag.Substances = await _substanceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }
    }

    #endregion

    #region Licence Usage Report (FR-026)

    /// <summary>
    /// Licence usage report form.
    /// </summary>
    public async Task<IActionResult> LicenceUsage(CancellationToken cancellationToken = default)
    {
        ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);

        return View(new LicenceUsageReportViewModel
        {
            FromDate = DateTime.UtcNow.AddDays(-90),
            ToDate = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Generates and displays licence usage report.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LicenceUsage(
        LicenceUsageReportViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var criteria = new LicenceUsageReportCriteria
            {
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                LicenceId = model.LicenceId,
                HolderId = model.HolderId,
                HolderType = model.HolderType
            };

            var report = await _reportingService.GenerateLicenceUsageReportAsync(criteria, cancellationToken);

            _logger.LogInformation(
                "Generated licence usage report: {Count} licences from {FromDate} to {ToDate}",
                report.TotalLicences, model.FromDate, model.ToDate);

            return View("LicenceUsageResult", report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating licence usage report");
            TempData["ErrorMessage"] = "An error occurred generating the report.";
            ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }
    }

    #endregion

    #region Customer Compliance History (FR-029)

    /// <summary>
    /// Customer compliance history report form.
    /// </summary>
    public async Task<IActionResult> CustomerCompliance(CancellationToken cancellationToken = default)
    {
        ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);

        return View(new CustomerComplianceReportViewModel());
    }

    /// <summary>
    /// Generates and displays customer compliance history report.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerCompliance(
        CustomerComplianceReportViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var criteria = new CustomerComplianceHistoryCriteria
            {
                CustomerAccount = model.CustomerAccount,
                DataAreaId = model.DataAreaId,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                IncludeLicenceStatus = model.IncludeLicenceStatus
            };

            var report = await _reportingService.GenerateCustomerComplianceHistoryAsync(criteria, cancellationToken);

            if (report == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
                return View(model);
            }

            _logger.LogInformation(
                "Generated customer compliance history: {Count} events for {CustomerAccount}/{DataAreaId}",
                report.Events.Count, model.CustomerAccount, model.DataAreaId);

            return View("CustomerComplianceResult", report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer compliance history for {CustomerAccount}/{DataAreaId}", model.CustomerAccount, model.DataAreaId);
            TempData["ErrorMessage"] = "An error occurred generating the report.";
            ViewBag.Customers = await _customerRepository.GetAllAsync(cancellationToken);
            return View(model);
        }
    }

    #endregion

    #region Licence Correction Impact (SC-038)

    /// <summary>
    /// Licence correction impact report form.
    /// T163d: UI for SC-038 historical validation report.
    /// </summary>
    public async Task<IActionResult> LicenceCorrectionImpact(CancellationToken cancellationToken = default)
    {
        ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);

        return View(new LicenceCorrectionImpactViewModel
        {
            CorrectionDate = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Generates and displays licence correction impact report.
    /// T163d: Shows affected transactions with original vs. corrected compliance status.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LicenceCorrectionImpact(
        LicenceCorrectionImpactViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var criteria = new LicenceCorrectionImpactCriteria
            {
                LicenceId = model.LicenceId,
                CorrectionDate = model.CorrectionDate,
                OriginalEffectiveDate = model.OriginalEffectiveDate,
                CorrectedEffectiveDate = model.CorrectedEffectiveDate,
                OriginalExpiryDate = model.OriginalExpiryDate,
                CorrectedExpiryDate = model.CorrectedExpiryDate
            };

            var report = await _licenceCorrectionImpactService.AnalyzeImpactAsync(criteria, cancellationToken);

            if (report == null)
            {
                TempData["ErrorMessage"] = "Licence not found.";
                ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);
                return View(model);
            }

            _logger.LogInformation(
                "Generated licence correction impact report: {Affected} of {Total} transactions affected for licence {LicenceId}",
                report.TotalAffectedTransactions, report.TotalTransactionsAnalyzed, model.LicenceId);

            return View("LicenceCorrectionImpactResult", report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating licence correction impact report for {LicenceId}", model.LicenceId);
            TempData["ErrorMessage"] = "An error occurred generating the report.";
            ViewBag.Licences = await _licenceRepository.GetAllAsync(cancellationToken);
            return View(model);
        }
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for transaction audit report form.
/// </summary>
public class TransactionAuditReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? SubstanceCode { get; set; }
    public string? CustomerAccount { get; set; }
    public string? CustomerDataAreaId { get; set; }
    public string? CountryCode { get; set; }
    public bool IncludeLicenceDetails { get; set; }
}

/// <summary>
/// View model for licence usage report form.
/// </summary>
public class LicenceUsageReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? LicenceId { get; set; }
    public Guid? HolderId { get; set; }
    public string? HolderType { get; set; }
}

/// <summary>
/// View model for customer compliance report form.
/// </summary>
public class CustomerComplianceReportViewModel
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeLicenceStatus { get; set; }
}

/// <summary>
/// View model for licence correction impact report form.
/// T163d: SC-038 historical validation report.
/// </summary>
public class LicenceCorrectionImpactViewModel
{
    public Guid LicenceId { get; set; }
    public DateTime CorrectionDate { get; set; }
    public DateTime? OriginalEffectiveDate { get; set; }
    public DateTime? CorrectedEffectiveDate { get; set; }
    public DateOnly? OriginalExpiryDate { get; set; }
    public DateOnly? CorrectedExpiryDate { get; set; }
}

#endregion
