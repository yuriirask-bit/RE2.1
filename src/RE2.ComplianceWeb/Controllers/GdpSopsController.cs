using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP SOP management.
/// T291: Web UI per US12 (FR-049).
/// </summary>
[Authorize]
public class GdpSopsController : Controller
{
    private readonly IGdpSopRepository _sopRepository;
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpSopsController> _logger;

    public GdpSopsController(
        IGdpSopRepository sopRepository,
        IGdpComplianceService gdpService,
        ILogger<GdpSopsController> logger)
    {
        _sopRepository = sopRepository;
        _gdpService = gdpService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(bool? activeOnly = null)
    {
        var sops = (await _sopRepository.GetAllAsync()).ToList();
        if (activeOnly == true)
            sops = sops.Where(s => s.IsActive).ToList();

        var model = new SopIndexViewModel
        {
            Sops = sops,
            TotalCount = sops.Count,
            ActiveCount = sops.Count(s => s.IsActive),
            ActiveOnly = activeOnly ?? false
        };
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var sop = await _sopRepository.GetByIdAsync(id);
        if (sop == null) return NotFound();

        var linkedSops = (await _sopRepository.GetSiteSopsAsync(id)).ToList();
        var sites = (await _gdpService.GetAllGdpSitesAsync()).ToList();
        ViewBag.LinkedSites = linkedSops;
        ViewBag.AvailableSites = new SelectList(
            sites.Select(s => new { Id = s.GdpExtensionId, Name = $"{s.WarehouseId} - {s.WarehouseName}" }),
            "Id", "Name");

        return View(sop);
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public IActionResult Create()
    {
        PopulateDropdowns();
        return View(new SopCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(SopCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDropdowns();
            return View(model);
        }

        var sop = new GdpSop
        {
            SopNumber = model.SopNumber,
            Title = model.Title,
            Category = model.Category,
            Version = model.Version,
            EffectiveDate = model.EffectiveDate,
            DocumentUrl = model.DocumentUrl,
            IsActive = model.IsActive
        };

        var validationResult = sop.Validate();
        if (!validationResult.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", validationResult.Violations.Select(v => v.Message));
            PopulateDropdowns();
            return View(model);
        }

        var id = await _sopRepository.CreateAsync(sop);
        TempData["SuccessMessage"] = $"SOP '{model.SopNumber}' created successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var sop = await _sopRepository.GetByIdAsync(id);
        if (sop == null) return NotFound();

        PopulateDropdowns();
        var model = new SopEditViewModel
        {
            SopId = sop.SopId,
            SopNumber = sop.SopNumber,
            Title = sop.Title,
            Category = sop.Category,
            Version = sop.Version,
            EffectiveDate = sop.EffectiveDate,
            DocumentUrl = sop.DocumentUrl,
            IsActive = sop.IsActive
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Edit(SopEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDropdowns();
            return View(model);
        }

        var sop = new GdpSop
        {
            SopId = model.SopId,
            SopNumber = model.SopNumber,
            Title = model.Title,
            Category = model.Category,
            Version = model.Version,
            EffectiveDate = model.EffectiveDate,
            DocumentUrl = model.DocumentUrl,
            IsActive = model.IsActive
        };

        var validationResult = sop.Validate();
        if (!validationResult.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", validationResult.Violations.Select(v => v.Message));
            PopulateDropdowns();
            return View(model);
        }

        await _sopRepository.UpdateAsync(sop);
        TempData["SuccessMessage"] = $"SOP '{model.SopNumber}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.SopId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _sopRepository.DeleteAsync(id);
        TempData["SuccessMessage"] = "SOP deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> LinkSite(Guid sopId, Guid siteId)
    {
        await _sopRepository.LinkSopToSiteAsync(siteId, sopId);
        TempData["SuccessMessage"] = "SOP linked to site successfully.";
        return RedirectToAction(nameof(Details), new { id = sopId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> UnlinkSite(Guid sopId, Guid siteId)
    {
        await _sopRepository.UnlinkSopFromSiteAsync(siteId, sopId);
        TempData["SuccessMessage"] = "SOP unlinked from site successfully.";
        return RedirectToAction(nameof(Details), new { id = sopId });
    }

    private void PopulateDropdowns()
    {
        ViewBag.Categories = new SelectList(
            Enum.GetValues<GdpSopCategory>().Select(c => new { Value = (int)c, Text = FormatEnumName(c.ToString()) }),
            "Value", "Text");
    }

    private static string FormatEnumName(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

#region View Models

public class SopIndexViewModel
{
    public List<GdpSop> Sops { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public bool ActiveOnly { get; set; }
}

public class SopCreateViewModel
{
    public string SopNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public GdpSopCategory Category { get; set; }
    public string Version { get; set; } = "1.0";
    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? DocumentUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SopEditViewModel : SopCreateViewModel
{
    public Guid SopId { get; set; }
}

#endregion
