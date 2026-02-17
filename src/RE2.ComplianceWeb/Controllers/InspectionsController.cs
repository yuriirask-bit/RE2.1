using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// Web UI controller for regulatory inspection management.
/// T167: Implements FR-028 inspection recording UI.
/// </summary>
[Authorize(Policy = "ComplianceManagerOrQAUser")]
public class InspectionsController : Controller
{
    private readonly IRegulatoryInspectionRepository _inspectionRepository;
    private readonly ILogger<InspectionsController> _logger;

    public InspectionsController(
        IRegulatoryInspectionRepository inspectionRepository,
        ILogger<InspectionsController> logger)
    {
        _inspectionRepository = inspectionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Displays the list of regulatory inspections.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        InspectingAuthority? authority = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        IEnumerable<RegulatoryInspection> inspections;

        if (fromDate.HasValue && toDate.HasValue)
        {
            inspections = await _inspectionRepository.GetByDateRangeAsync(fromDate.Value, toDate.Value);
        }
        else if (authority.HasValue)
        {
            inspections = await _inspectionRepository.GetByAuthorityAsync(authority.Value);
        }
        else
        {
            inspections = await _inspectionRepository.GetAllAsync();
        }

        ViewBag.SelectedAuthority = authority;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View(inspections);
    }

    /// <summary>
    /// Displays the form to create a new inspection record.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreateInspectionViewModel
        {
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        return View(model);
    }

    /// <summary>
    /// Creates a new inspection record.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInspectionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var inspection = new RegulatoryInspection
        {
            InspectionDate = model.InspectionDate,
            Authority = model.Authority,
            InspectorName = model.InspectorName,
            ReferenceNumber = model.ReferenceNumber,
            Outcome = model.Outcome,
            FindingsSummary = model.FindingsSummary,
            CorrectiveActions = model.CorrectiveActions,
            CorrectiveActionsDueDate = model.CorrectiveActionsDueDate,
            Notes = model.Notes,
            RecordedBy = GetCurrentUserId()
        };

        var validationResult = inspection.Validate();
        if (!validationResult.IsValid)
        {
            foreach (var violation in validationResult.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        await _inspectionRepository.CreateAsync(inspection);

        _logger.LogInformation(
            "Created regulatory inspection record: {InspectionId}, Authority: {Authority}, Date: {Date}",
            inspection.InspectionId, inspection.Authority, inspection.InspectionDate);

        TempData["SuccessMessage"] = "Inspection record created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays details of an inspection.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(id);

        if (inspection == null)
        {
            return NotFound();
        }

        return View(inspection);
    }

    /// <summary>
    /// Displays the form to edit an inspection record.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(id);

        if (inspection == null)
        {
            return NotFound();
        }

        var model = new EditInspectionViewModel
        {
            InspectionId = inspection.InspectionId,
            InspectionDate = inspection.InspectionDate,
            Authority = inspection.Authority,
            InspectorName = inspection.InspectorName,
            ReferenceNumber = inspection.ReferenceNumber,
            Outcome = inspection.Outcome,
            FindingsSummary = inspection.FindingsSummary,
            CorrectiveActions = inspection.CorrectiveActions,
            CorrectiveActionsDueDate = inspection.CorrectiveActionsDueDate,
            CorrectiveActionsCompletedDate = inspection.CorrectiveActionsCompletedDate,
            Notes = inspection.Notes
        };

        return View(model);
    }

    /// <summary>
    /// Updates an existing inspection record.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditInspectionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var inspection = await _inspectionRepository.GetByIdAsync(model.InspectionId);

        if (inspection == null)
        {
            return NotFound();
        }

        inspection.InspectionDate = model.InspectionDate;
        inspection.Authority = model.Authority;
        inspection.InspectorName = model.InspectorName;
        inspection.ReferenceNumber = model.ReferenceNumber;
        inspection.Outcome = model.Outcome;
        inspection.FindingsSummary = model.FindingsSummary;
        inspection.CorrectiveActions = model.CorrectiveActions;
        inspection.CorrectiveActionsDueDate = model.CorrectiveActionsDueDate;
        inspection.CorrectiveActionsCompletedDate = model.CorrectiveActionsCompletedDate;
        inspection.Notes = model.Notes;

        var validationResult = inspection.Validate();
        if (!validationResult.IsValid)
        {
            foreach (var violation in validationResult.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        await _inspectionRepository.UpdateAsync(inspection);

        _logger.LogInformation(
            "Updated regulatory inspection record: {InspectionId}",
            inspection.InspectionId);

        TempData["SuccessMessage"] = "Inspection record updated successfully.";

        return RedirectToAction(nameof(Details), new { id = model.InspectionId });
    }

    /// <summary>
    /// Displays inspections with overdue corrective actions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> OverdueCorrectiveActions()
    {
        var inspections = await _inspectionRepository.GetWithOverdueCorrectiveActionsAsync();
        return View(inspections);
    }

    private Guid GetCurrentUserId()
    {
        // In production, extract from claims
        var userIdClaim = User.FindFirst("oid")?.Value ??
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// View model for creating a new inspection.
/// </summary>
public class CreateInspectionViewModel
{
    public DateOnly InspectionDate { get; set; }
    public InspectingAuthority Authority { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public InspectionOutcome Outcome { get; set; }
    public string? FindingsSummary { get; set; }
    public string? CorrectiveActions { get; set; }
    public DateOnly? CorrectiveActionsDueDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// View model for editing an inspection.
/// </summary>
public class EditInspectionViewModel : CreateInspectionViewModel
{
    public Guid InspectionId { get; set; }
    public DateOnly? CorrectiveActionsCompletedDate { get; set; }
}
