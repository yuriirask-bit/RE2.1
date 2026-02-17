using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP equipment qualification management.
/// T265: Web UI per US11 (FR-048).
/// </summary>
[Authorize]
public class GdpEquipmentController : Controller
{
    private readonly IGdpOperationalService _operationalService;
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpEquipmentController> _logger;

    public GdpEquipmentController(
        IGdpOperationalService operationalService,
        IGdpComplianceService gdpService,
        ILogger<GdpEquipmentController> logger)
    {
        _operationalService = operationalService;
        _gdpService = gdpService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var equipment = (await _operationalService.GetAllEquipmentAsync()).ToList();
        var model = new GdpEquipmentIndexViewModel
        {
            Equipment = equipment,
            TotalCount = equipment.Count,
            QualifiedCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.Qualified),
            DueCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.DueForRequalification),
            ExpiredCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.Expired)
        };
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var equipment = await _operationalService.GetEquipmentAsync(id);
        if (equipment == null) return NotFound();
        return View(equipment);
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new GdpEquipmentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(GdpEquipmentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var equipment = new GdpEquipmentQualification
        {
            EquipmentName = model.EquipmentName,
            EquipmentType = model.EquipmentType,
            ProviderId = model.ProviderId,
            SiteId = model.SiteId,
            QualificationDate = model.QualificationDate,
            RequalificationDueDate = model.RequalificationDueDate,
            QualificationStatus = model.QualificationStatus,
            QualifiedBy = model.QualifiedBy,
            Notes = model.Notes
        };

        var (id, result) = await _operationalService.CreateEquipmentAsync(equipment);
        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            await PopulateDropdowns();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Equipment qualification '{model.EquipmentName}' created successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var equipment = await _operationalService.GetEquipmentAsync(id);
        if (equipment == null) return NotFound();

        await PopulateDropdowns();
        var model = new GdpEquipmentEditViewModel
        {
            EquipmentQualificationId = equipment.EquipmentQualificationId,
            EquipmentName = equipment.EquipmentName,
            EquipmentType = equipment.EquipmentType,
            ProviderId = equipment.ProviderId,
            SiteId = equipment.SiteId,
            QualificationDate = equipment.QualificationDate,
            RequalificationDueDate = equipment.RequalificationDueDate,
            QualificationStatus = equipment.QualificationStatus,
            QualifiedBy = equipment.QualifiedBy,
            Notes = equipment.Notes
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Edit(GdpEquipmentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var equipment = new GdpEquipmentQualification
        {
            EquipmentQualificationId = model.EquipmentQualificationId,
            EquipmentName = model.EquipmentName,
            EquipmentType = model.EquipmentType,
            ProviderId = model.ProviderId,
            SiteId = model.SiteId,
            QualificationDate = model.QualificationDate,
            RequalificationDueDate = model.RequalificationDueDate,
            QualificationStatus = model.QualificationStatus,
            QualifiedBy = model.QualifiedBy,
            Notes = model.Notes
        };

        var result = await _operationalService.UpdateEquipmentAsync(equipment);
        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            await PopulateDropdowns();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Equipment qualification '{model.EquipmentName}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.EquipmentQualificationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _operationalService.DeleteEquipmentAsync(id);
        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
        }
        else
        {
            TempData["SuccessMessage"] = "Equipment qualification deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        var providers = await _gdpService.GetAllProvidersAsync();
        var sites = await _gdpService.GetAllGdpSitesAsync();

        ViewBag.Providers = new SelectList(providers, "ProviderId", "ProviderName");
        ViewBag.Sites = new SelectList(sites, "GdpExtensionId", "WarehouseId");
        ViewBag.EquipmentTypes = new SelectList(Enum.GetValues<GdpEquipmentType>().Select(t => new { Value = (int)t, Text = FormatEnumName(t.ToString()) }), "Value", "Text");
        ViewBag.QualificationStatuses = new SelectList(Enum.GetValues<GdpQualificationStatusType>().Select(s => new { Value = (int)s, Text = FormatEnumName(s.ToString()) }), "Value", "Text");
    }

    private static string FormatEnumName(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

#region View Models

public class GdpEquipmentIndexViewModel
{
    public List<GdpEquipmentQualification> Equipment { get; set; } = new();
    public int TotalCount { get; set; }
    public int QualifiedCount { get; set; }
    public int DueCount { get; set; }
    public int ExpiredCount { get; set; }
}

public class GdpEquipmentCreateViewModel
{
    public string EquipmentName { get; set; } = string.Empty;
    public GdpEquipmentType EquipmentType { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? SiteId { get; set; }
    public DateOnly QualificationDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? RequalificationDueDate { get; set; }
    public GdpQualificationStatusType QualificationStatus { get; set; } = GdpQualificationStatusType.Qualified;
    public string QualifiedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class GdpEquipmentEditViewModel : GdpEquipmentCreateViewModel
{
    public Guid EquipmentQualificationId { get; set; }
}

#endregion
