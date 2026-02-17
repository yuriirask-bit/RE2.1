using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP Inspections, Findings, and CAPA management.
/// T226-T228: Web UI for GDP inspection and CAPA tracking per User Story 9 (FR-040, FR-041, FR-042).
/// </summary>
[Authorize]
public class GdpInspectionsController : Controller
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpInspectionsController> _logger;

    public GdpInspectionsController(
        IGdpComplianceService gdpService,
        ILogger<GdpInspectionsController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    #region Inspections

    /// <summary>
    /// Lists all GDP inspections.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var inspections = await _gdpService.GetAllInspectionsAsync(cancellationToken);
        return View(inspections);
    }

    /// <summary>
    /// Shows inspection details including findings and CAPAs.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var inspection = await _gdpService.GetInspectionAsync(id, cancellationToken);
        if (inspection == null)
            return NotFound();

        var findings = await _gdpService.GetFindingsAsync(id, cancellationToken);
        ViewBag.Findings = findings.ToList();

        // Load CAPAs for each finding
        var allCapas = new Dictionary<Guid, List<Capa>>();
        foreach (var finding in findings)
        {
            var capas = await _gdpService.GetCapasByFindingAsync(finding.FindingId, cancellationToken);
            allCapas[finding.FindingId] = capas.ToList();
        }
        ViewBag.CapasByFinding = allCapas;

        return View(inspection);
    }

    /// <summary>
    /// Shows the Create Inspection form.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        await PopulateSiteSelectListAsync(cancellationToken);
        PopulateInspectionTypeSelectList();
        return View(new GdpInspectionCreateViewModel());
    }

    /// <summary>
    /// Handles Create Inspection form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(GdpInspectionCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSiteSelectListAsync(cancellationToken);
            PopulateInspectionTypeSelectList();
            return View(model);
        }

        var inspection = new GdpInspection
        {
            InspectionDate = model.InspectionDate,
            InspectorName = model.InspectorName,
            InspectionType = model.InspectionType,
            SiteId = model.SiteId,
            WdaLicenceId = model.WdaLicenceId,
            FindingsSummary = model.FindingsSummary,
            ReportReferenceUrl = model.ReportReferenceUrl
        };

        var (id, result) = await _gdpService.CreateInspectionAsync(inspection, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            await PopulateSiteSelectListAsync(cancellationToken);
            PopulateInspectionTypeSelectList();
            return View(model);
        }

        TempData["SuccessMessage"] = "GDP inspection recorded successfully.";
        return RedirectToAction(nameof(Details), new { id = id });
    }

    #endregion

    #region Findings

    /// <summary>
    /// Shows the Create Finding form for an inspection.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CreateFinding(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var inspection = await _gdpService.GetInspectionAsync(inspectionId, cancellationToken);
        if (inspection == null)
            return NotFound();

        ViewBag.InspectionId = inspectionId;
        ViewBag.InspectorName = inspection.InspectorName;
        PopulateClassificationSelectList();
        return View(new GdpFindingCreateViewModel { InspectionId = inspectionId });
    }

    /// <summary>
    /// Handles Create Finding form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CreateFinding(GdpFindingCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            PopulateClassificationSelectList();
            return View(model);
        }

        var finding = new GdpInspectionFinding
        {
            InspectionId = model.InspectionId,
            FindingDescription = model.FindingDescription,
            Classification = model.Classification,
            FindingNumber = model.FindingNumber
        };

        var (id, result) = await _gdpService.CreateFindingAsync(finding, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            PopulateClassificationSelectList();
            return View(model);
        }

        TempData["SuccessMessage"] = "Finding recorded successfully.";
        return RedirectToAction(nameof(Details), new { id = model.InspectionId });
    }

    #endregion

    #region CAPAs

    /// <summary>
    /// Shows the CAPA dashboard with overdue highlights.
    /// Per FR-042.
    /// </summary>
    public async Task<IActionResult> Capas(CancellationToken cancellationToken = default)
    {
        var allCapas = await _gdpService.GetAllCapasAsync(cancellationToken);
        var overdueCapas = await _gdpService.GetOverdueCapasAsync(cancellationToken);

        ViewBag.OverdueCount = overdueCapas.Count();
        ViewBag.OpenCount = allCapas.Count(c => c.Status == CapaStatus.Open);
        ViewBag.CompletedCount = allCapas.Count(c => c.Status == CapaStatus.Completed);

        return View(allCapas);
    }

    /// <summary>
    /// Shows the Create CAPA form for a finding.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CreateCapa(Guid findingId, CancellationToken cancellationToken = default)
    {
        var finding = await _gdpService.GetFindingAsync(findingId, cancellationToken);
        if (finding == null)
            return NotFound();

        ViewBag.Finding = finding;
        return View(new CapaCreateViewModel { FindingId = findingId });
    }

    /// <summary>
    /// Handles Create CAPA form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CreateCapa(CapaCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var finding = await _gdpService.GetFindingAsync(model.FindingId, cancellationToken);
            ViewBag.Finding = finding;
            return View(model);
        }

        var capa = new Capa
        {
            CapaNumber = model.CapaNumber,
            FindingId = model.FindingId,
            Description = model.Description,
            OwnerName = model.OwnerName,
            DueDate = model.DueDate
        };

        var (id, result) = await _gdpService.CreateCapaAsync(capa, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            var finding = await _gdpService.GetFindingAsync(model.FindingId, cancellationToken);
            ViewBag.Finding = finding;
            return View(model);
        }

        TempData["SuccessMessage"] = $"CAPA {model.CapaNumber} created successfully.";
        return RedirectToAction(nameof(Capas));
    }

    /// <summary>
    /// Shows the Complete CAPA form.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CompleteCapa(Guid id, CancellationToken cancellationToken = default)
    {
        var capa = await _gdpService.GetCapaAsync(id, cancellationToken);
        if (capa == null)
            return NotFound();

        ViewBag.Capa = capa;
        return View(new CapaCompleteViewModel
        {
            CapaId = id,
            CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
    }

    /// <summary>
    /// Handles Complete CAPA form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> CompleteCapa(CapaCompleteViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var capa = await _gdpService.GetCapaAsync(model.CapaId, cancellationToken);
            ViewBag.Capa = capa;
            return View(model);
        }

        var result = await _gdpService.CompleteCapaAsync(model.CapaId, model.CompletionDate, model.VerificationNotes, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            var capa = await _gdpService.GetCapaAsync(model.CapaId, cancellationToken);
            ViewBag.Capa = capa;
            return View(model);
        }

        TempData["SuccessMessage"] = "CAPA completed successfully.";
        return RedirectToAction(nameof(Capas));
    }

    #endregion

    #region Helpers

    private async Task PopulateSiteSelectListAsync(CancellationToken cancellationToken)
    {
        var sites = await _gdpService.GetAllGdpSitesAsync(cancellationToken);
        ViewBag.Sites = new SelectList(
            sites.Select(s => new { Id = s.GdpExtensionId, Name = $"{s.WarehouseId} - {s.WarehouseName}" }),
            "Id", "Name");
    }

    private void PopulateInspectionTypeSelectList()
    {
        ViewBag.InspectionTypes = new SelectList(
            Enum.GetValues<GdpInspectionType>().Select(t => new { Value = (int)t, Text = FormatEnumName(t.ToString()) }),
            "Value", "Text");
    }

    private void PopulateClassificationSelectList()
    {
        ViewBag.Classifications = new SelectList(
            Enum.GetValues<FindingClassification>().Select(c => new { Value = (int)c, Text = FormatEnumName(c.ToString()) }),
            "Value", "Text");
    }

    private static string FormatEnumName(string enumName)
    {
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    #endregion
}

#region View Models

public class GdpInspectionCreateViewModel
{
    public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string InspectorName { get; set; } = string.Empty;
    public GdpInspectionType InspectionType { get; set; }
    public Guid SiteId { get; set; }
    public Guid? WdaLicenceId { get; set; }
    public string? FindingsSummary { get; set; }
    public string? ReportReferenceUrl { get; set; }
}

public class GdpFindingCreateViewModel
{
    public Guid InspectionId { get; set; }
    public string FindingDescription { get; set; } = string.Empty;
    public FindingClassification Classification { get; set; }
    public string? FindingNumber { get; set; }
}

public class CapaCreateViewModel
{
    public string CapaNumber { get; set; } = string.Empty;
    public Guid FindingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);
}

public class CapaCompleteViewModel
{
    public Guid CapaId { get; set; }
    public DateOnly CompletionDate { get; set; }
    public string? VerificationNotes { get; set; }
}

#endregion
