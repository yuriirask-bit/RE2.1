using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP service provider management web UI.
/// T208: Web UI for GDP provider browsing, qualification review, and credential management.
/// </summary>
[Authorize]
public class GdpProvidersController : Controller
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpProvidersController> _logger;

    public GdpProvidersController(
        IGdpComplianceService gdpService,
        ILogger<GdpProvidersController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all GDP service providers.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var providers = await _gdpService.GetAllProvidersAsync(cancellationToken);
        return View(providers);
    }

    /// <summary>
    /// Shows provider details with credentials, reviews, and verifications.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _gdpService.GetProviderAsync(id, cancellationToken);
        if (provider == null)
            return NotFound();

        var credentials = await _gdpService.GetCredentialsByEntityAsync(
            GdpCredentialEntityType.ServiceProvider, id, cancellationToken);
        var reviews = await _gdpService.GetReviewsByEntityAsync(
            ReviewEntityType.ServiceProvider, id, cancellationToken);
        var documents = await _gdpService.GetDocumentsByEntityAsync(
            GdpDocumentEntityType.Provider, id, cancellationToken);

        ViewBag.Credentials = credentials.ToList();
        ViewBag.Reviews = reviews.OrderByDescending(r => r.ReviewDate).ToList();
        ViewBag.Documents = documents.ToList();
        return View(provider);
    }

    /// <summary>
    /// Shows the Create provider form.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public IActionResult Create()
    {
        ViewBag.ServiceTypes = GetServiceTypeSelectList();
        return View(new GdpProviderCreateViewModel());
    }

    /// <summary>
    /// Handles Create provider form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Create(GdpProviderCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ServiceTypes = GetServiceTypeSelectList(model.ServiceType.ToString());
            return View(model);
        }

        var provider = new GdpServiceProvider
        {
            ProviderName = model.ProviderName,
            ServiceType = model.ServiceType,
            TemperatureControlledCapability = model.TemperatureControlledCapability,
            ApprovedRoutes = model.ApprovedRoutes,
            ReviewFrequencyMonths = model.ReviewFrequencyMonths
        };

        var (id, result) = await _gdpService.CreateProviderAsync(provider, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.ServiceTypes = GetServiceTypeSelectList(model.ServiceType.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = $"GDP service provider '{model.ProviderName}' created.";
        return RedirectToAction(nameof(Details), new { id = id!.Value });
    }

    /// <summary>
    /// Shows the Edit provider form.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _gdpService.GetProviderAsync(id, cancellationToken);
        if (provider == null)
            return NotFound();

        var model = new GdpProviderCreateViewModel
        {
            ProviderName = provider.ProviderName,
            ServiceType = provider.ServiceType,
            TemperatureControlledCapability = provider.TemperatureControlledCapability,
            ApprovedRoutes = provider.ApprovedRoutes,
            ReviewFrequencyMonths = provider.ReviewFrequencyMonths,
            IsActive = provider.IsActive
        };

        ViewBag.ServiceTypes = GetServiceTypeSelectList(model.ServiceType.ToString());
        return View(model);
    }

    /// <summary>
    /// Handles Edit provider form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Edit(Guid id, GdpProviderCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ServiceTypes = GetServiceTypeSelectList(model.ServiceType.ToString());
            return View(model);
        }

        var provider = new GdpServiceProvider
        {
            ProviderId = id,
            ProviderName = model.ProviderName,
            ServiceType = model.ServiceType,
            TemperatureControlledCapability = model.TemperatureControlledCapability,
            ApprovedRoutes = model.ApprovedRoutes,
            ReviewFrequencyMonths = model.ReviewFrequencyMonths,
            IsActive = model.IsActive
        };

        var result = await _gdpService.UpdateProviderAsync(provider, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.ServiceTypes = GetServiceTypeSelectList(model.ServiceType.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = $"GDP service provider '{model.ProviderName}' updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Shows the qualification review form for a provider.
    /// T207: QualificationReview view.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> RecordReview(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _gdpService.GetProviderAsync(providerId, cancellationToken);
        if (provider == null)
            return NotFound();

        var model = new QualificationReviewViewModel
        {
            ProviderId = providerId,
            ProviderName = provider.ProviderName,
            ReviewDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        ViewBag.ReviewMethods = GetReviewMethodSelectList();
        ViewBag.ReviewOutcomes = GetReviewOutcomeSelectList();
        return View(model);
    }

    /// <summary>
    /// Handles qualification review form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> RecordReview(QualificationReviewViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReviewMethods = GetReviewMethodSelectList(model.ReviewMethod.ToString());
            ViewBag.ReviewOutcomes = GetReviewOutcomeSelectList(model.ReviewOutcome.ToString());
            return View(model);
        }

        var review = QualificationReview.CreateForServiceProvider(
            model.ProviderId,
            model.ReviewDate,
            model.ReviewMethod,
            model.ReviewOutcome,
            model.ReviewerName,
            model.Notes);

        if (model.NextReviewMonths > 0)
            review.SetNextReviewDate(model.NextReviewMonths);

        var (id, result) = await _gdpService.RecordReviewAsync(review, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.ReviewMethods = GetReviewMethodSelectList(model.ReviewMethod.ToString());
            ViewBag.ReviewOutcomes = GetReviewOutcomeSelectList(model.ReviewOutcome.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = $"Qualification review recorded for '{model.ProviderName}'.";
        return RedirectToAction(nameof(Details), new { id = model.ProviderId });
    }

    /// <summary>
    /// Shows the EudraGMDP verification recording form.
    /// T210: EudraGMDP verification recording UI per FR-045.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> RecordVerification(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _gdpService.GetCredentialAsync(credentialId, cancellationToken);
        if (credential == null)
            return NotFound();

        var model = new VerificationRecordViewModel
        {
            CredentialId = credentialId,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EudraGmdpEntryUrl = credential.EudraGmdpEntryUrl
        };

        ViewBag.VerificationMethods = GetVerificationMethodSelectList();
        ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList();
        return View(model);
    }

    /// <summary>
    /// Handles EudraGMDP verification form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> RecordVerification(VerificationRecordViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VerificationMethods = GetVerificationMethodSelectList(model.VerificationMethod.ToString());
            ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList(model.Outcome.ToString());
            return View(model);
        }

        var verification = new GdpCredentialVerification
        {
            CredentialId = model.CredentialId,
            VerificationDate = model.VerificationDate,
            VerificationMethod = model.VerificationMethod,
            VerifiedBy = model.VerifiedBy,
            Outcome = model.Outcome,
            Notes = model.Notes
        };

        var (id, result) = await _gdpService.RecordVerificationAsync(verification, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.VerificationMethods = GetVerificationMethodSelectList(model.VerificationMethod.ToString());
            ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList(model.Outcome.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = "Credential verification recorded.";

        // Redirect to the credential's entity details
        var credential = await _gdpService.GetCredentialAsync(model.CredentialId, cancellationToken);
        if (credential?.EntityType == GdpCredentialEntityType.ServiceProvider)
            return RedirectToAction(nameof(Details), new { id = credential.EntityId });

        return RedirectToAction(nameof(Index));
    }

    #region Helpers

    private static List<SelectListItem> GetServiceTypeSelectList(string? selected = null) =>
        Enum.GetValues<GdpServiceType>().Select(t => new SelectListItem
        {
            Value = t.ToString(),
            Text = t switch
            {
                GdpServiceType.ThirdPartyLogistics => "Third-Party Logistics (3PL)",
                GdpServiceType.Transporter => "Transporter",
                GdpServiceType.ExternalWarehouse => "External Warehouse",
                _ => t.ToString()
            },
            Selected = t.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetReviewMethodSelectList(string? selected = null) =>
        Enum.GetValues<ReviewMethod>().Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = m switch
            {
                ReviewMethod.OnSiteAudit => "On-Site Audit",
                ReviewMethod.Questionnaire => "Questionnaire",
                ReviewMethod.DocumentReview => "Document Review",
                _ => m.ToString()
            },
            Selected = m.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetReviewOutcomeSelectList(string? selected = null) =>
        Enum.GetValues<ReviewOutcome>().Select(o => new SelectListItem
        {
            Value = o.ToString(),
            Text = o switch
            {
                ReviewOutcome.Approved => "Approved",
                ReviewOutcome.ConditionallyApproved => "Conditionally Approved",
                ReviewOutcome.Rejected => "Rejected",
                _ => o.ToString()
            },
            Selected = o.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetVerificationMethodSelectList(string? selected = null) =>
        Enum.GetValues<GdpVerificationMethod>().Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = m switch
            {
                GdpVerificationMethod.EudraGMDP => "EudraGMDP Database",
                GdpVerificationMethod.NationalDatabase => "National Database",
                GdpVerificationMethod.Other => "Other",
                _ => m.ToString()
            },
            Selected = m.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetVerificationOutcomeSelectList(string? selected = null) =>
        Enum.GetValues<GdpVerificationOutcome>().Select(o => new SelectListItem
        {
            Value = o.ToString(),
            Text = o.ToString(),
            Selected = o.ToString() == selected
        }).ToList();

    #endregion
}

#region ViewModels

public class GdpProviderCreateViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public GdpServiceType ServiceType { get; set; }
    public bool TemperatureControlledCapability { get; set; }
    public string? ApprovedRoutes { get; set; }
    public int ReviewFrequencyMonths { get; set; } = 24;
    public bool IsActive { get; set; } = true;
}

public class QualificationReviewViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateOnly ReviewDate { get; set; }
    public ReviewMethod ReviewMethod { get; set; }
    public ReviewOutcome ReviewOutcome { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int NextReviewMonths { get; set; } = 24;
}

public class VerificationRecordViewModel
{
    public Guid CredentialId { get; set; }
    public string? EudraGmdpEntryUrl { get; set; }
    public DateOnly VerificationDate { get; set; }
    public GdpVerificationMethod VerificationMethod { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public GdpVerificationOutcome Outcome { get; set; }
    public string? Notes { get; set; }
}

#endregion
