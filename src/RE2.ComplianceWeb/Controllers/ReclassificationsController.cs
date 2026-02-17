using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for substance reclassification management web UI.
/// T080i: Web UI for substance reclassification workflow per FR-066.
/// </summary>
[Authorize]
public class ReclassificationsController : Controller
{
    private readonly ISubstanceReclassificationService _reclassificationService;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<ReclassificationsController> _logger;

    public ReclassificationsController(
        ISubstanceReclassificationService reclassificationService,
        IControlledSubstanceRepository substanceRepository,
        ILogger<ReclassificationsController> logger)
    {
        _reclassificationService = reclassificationService;
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Loads substances for dropdown lists.
    /// </summary>
    private async Task<SelectList> GetSubstanceSelectListAsync(string? selectedCode = null, CancellationToken cancellationToken = default)
    {
        var substances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        return new SelectList(substances, "SubstanceCode", "SubstanceName", selectedCode);
    }

    /// <summary>
    /// Gets Opium Act List options for dropdown.
    /// </summary>
    private static SelectList GetOpiumActListSelectList(int? selectedValue = null)
    {
        var options = new[]
        {
            new { Value = 0, Text = "None" },
            new { Value = 1, Text = "List I (Hard drugs)" },
            new { Value = 2, Text = "List II (Therapeutic)" }
        };
        return new SelectList(options, "Value", "Text", selectedValue);
    }

    /// <summary>
    /// Gets Precursor Category options for dropdown.
    /// </summary>
    private static SelectList GetPrecursorCategorySelectList(int? selectedValue = null)
    {
        var options = new[]
        {
            new { Value = 0, Text = "None" },
            new { Value = 1, Text = "Category 1 (Strictest)" },
            new { Value = 2, Text = "Category 2" },
            new { Value = 3, Text = "Category 3" }
        };
        return new SelectList(options, "Value", "Text", selectedValue);
    }

    /// <summary>
    /// Displays the reclassification list page.
    /// </summary>
    public async Task<IActionResult> Index(string? status = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<SubstanceReclassification> reclassifications;

        if (status == "Pending")
        {
            reclassifications = await _reclassificationService.GetPendingReclassificationsAsync(cancellationToken);
        }
        else
        {
            // For now, just get pending reclassifications
            // In a full implementation, we'd have a GetAllAsync method
            reclassifications = await _reclassificationService.GetPendingReclassificationsAsync(cancellationToken);
        }

        // Load substance info for each reclassification
        foreach (var r in reclassifications)
        {
            r.Substance = await _substanceRepository.GetBySubstanceCodeAsync(r.SubstanceCode, cancellationToken);
        }

        ViewBag.StatusFilter = status;
        return View(reclassifications);
    }

    /// <summary>
    /// Displays the create reclassification form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(string? substanceCode = null, CancellationToken cancellationToken = default)
    {
        var model = new ReclassificationCreateViewModel
        {
            EffectiveDate = DateTime.Today.AddDays(30)
        };

        if (!string.IsNullOrEmpty(substanceCode))
        {
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
            if (substance != null)
            {
                model.SubstanceCode = substance.SubstanceCode;
                model.PreviousOpiumActList = (int)substance.OpiumActList;
                model.PreviousPrecursorCategory = (int)substance.PrecursorCategory;
                model.NewOpiumActList = (int)substance.OpiumActList;
                model.NewPrecursorCategory = (int)substance.PrecursorCategory;
                ViewBag.SubstanceName = substance.SubstanceName;
            }
        }

        await PrepareCreateViewBag(model, cancellationToken);
        return View(model);
    }

    /// <summary>
    /// Handles reclassification creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(ReclassificationCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PrepareCreateViewBag(model, cancellationToken);
            return View(model);
        }

        var substance = await _substanceRepository.GetBySubstanceCodeAsync(model.SubstanceCode, cancellationToken);
        if (substance == null)
        {
            ModelState.AddModelError(nameof(model.SubstanceCode), "Substance not found");
            await PrepareCreateViewBag(model, cancellationToken);
            return View(model);
        }

        var reclassification = new SubstanceReclassification
        {
            SubstanceCode = model.SubstanceCode,
            PreviousOpiumActList = substance.OpiumActList,
            NewOpiumActList = (SubstanceCategories.OpiumActList)model.NewOpiumActList,
            PreviousPrecursorCategory = substance.PrecursorCategory,
            NewPrecursorCategory = (SubstanceCategories.PrecursorCategory)model.NewPrecursorCategory,
            EffectiveDate = DateOnly.FromDateTime(model.EffectiveDate),
            RegulatoryReference = model.RegulatoryReference,
            RegulatoryAuthority = model.RegulatoryAuthority,
            Reason = model.Reason
        };

        var (id, result) = await _reclassificationService.CreateReclassificationAsync(reclassification, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            await PrepareCreateViewBag(model, cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Created reclassification {Id} for substance {SubstanceName} via web UI", id, substance.SubstanceName);
        TempData["SuccessMessage"] = $"Reclassification for {substance.SubstanceName} created successfully.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Prepares ViewBag for create view.
    /// </summary>
    private async Task PrepareCreateViewBag(ReclassificationCreateViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Substances = await GetSubstanceSelectListAsync(model.SubstanceCode, cancellationToken);
        ViewBag.OpiumActLists = GetOpiumActListSelectList(model.NewOpiumActList);
        ViewBag.PrecursorCategories = GetPrecursorCategorySelectList(model.NewPrecursorCategory);
    }

    /// <summary>
    /// Displays reclassification details with impact analysis.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var reclassification = await _reclassificationService.GetByIdAsync(id, cancellationToken);

        if (reclassification == null)
        {
            return NotFound();
        }

        var model = new ReclassificationDetailsViewModel
        {
            Reclassification = reclassification
        };

        // Get impact analysis for completed reclassifications
        if (reclassification.Status == ReclassificationStatus.Completed ||
            reclassification.Status == ReclassificationStatus.Processing)
        {
            model.ImpactAnalysis = await _reclassificationService.AnalyzeCustomerImpactAsync(id, cancellationToken);
        }

        return View(model);
    }

    /// <summary>
    /// Displays the process reclassification page with impact preview.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Process(Guid id, CancellationToken cancellationToken = default)
    {
        var reclassification = await _reclassificationService.GetByIdAsync(id, cancellationToken);

        if (reclassification == null)
        {
            return NotFound();
        }

        if (reclassification.Status != ReclassificationStatus.Pending)
        {
            TempData["ErrorMessage"] = $"Reclassification is already in status {reclassification.Status}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Get impact analysis preview
        var impactAnalysis = await _reclassificationService.AnalyzeCustomerImpactAsync(id, cancellationToken);

        var model = new ReclassificationProcessViewModel
        {
            Reclassification = reclassification,
            ImpactAnalysis = impactAnalysis
        };

        return View(model);
    }

    /// <summary>
    /// Handles reclassification processing confirmation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ProcessConfirm(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _reclassificationService.ProcessReclassificationAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Process), new { id });
        }

        _logger.LogInformation("Processed reclassification {Id} via web UI", id);
        TempData["SuccessMessage"] = "Reclassification processed successfully. Affected customers have been flagged.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Marks a customer as re-qualified.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ReQualifyCustomer(Guid id, Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await _reclassificationService.MarkCustomerReQualifiedAsync(id, customerId, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
        }
        else
        {
            TempData["SuccessMessage"] = "Customer marked as re-qualified.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Generates and displays compliance notification for a reclassification.
    /// </summary>
    public async Task<IActionResult> Notification(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _reclassificationService.GenerateComplianceNotificationAsync(id, cancellationToken);
            return View(notification);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

#region View Models

/// <summary>
/// View model for creating a new reclassification.
/// </summary>
public class ReclassificationCreateViewModel
{
    public string SubstanceCode { get; set; } = string.Empty;
    public int PreviousOpiumActList { get; set; }
    public int NewOpiumActList { get; set; }
    public int PreviousPrecursorCategory { get; set; }
    public int NewPrecursorCategory { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.Today.AddDays(30);
    public string RegulatoryReference { get; set; } = string.Empty;
    public string RegulatoryAuthority { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// View model for reclassification details page.
/// </summary>
public class ReclassificationDetailsViewModel
{
    public SubstanceReclassification Reclassification { get; set; } = null!;
    public ReclassificationImpactAnalysis? ImpactAnalysis { get; set; }
}

/// <summary>
/// View model for reclassification processing page.
/// </summary>
public class ReclassificationProcessViewModel
{
    public SubstanceReclassification Reclassification { get; set; } = null!;
    public ReclassificationImpactAnalysis ImpactAnalysis { get; set; } = null!;
}

#endregion
