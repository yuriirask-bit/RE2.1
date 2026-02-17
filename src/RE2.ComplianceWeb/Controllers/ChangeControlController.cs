using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP change control management.
/// T295: Web UI per US12 (FR-051).
/// </summary>
[Authorize]
public class ChangeControlController : Controller
{
    private readonly IGdpChangeRepository _changeRepository;
    private readonly ILogger<ChangeControlController> _logger;

    public ChangeControlController(
        IGdpChangeRepository changeRepository,
        ILogger<ChangeControlController> logger)
    {
        _changeRepository = changeRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? statusFilter = null)
    {
        var records = (await _changeRepository.GetAllAsync()).ToList();
        var pendingCount = records.Count(r => r.IsPending());

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ChangeApprovalStatus>(statusFilter, out var status))
        {
            records = records.Where(r => r.ApprovalStatus == status).ToList();
        }

        var model = new ChangeIndexViewModel
        {
            Records = records,
            TotalCount = records.Count,
            PendingCount = pendingCount,
            StatusFilter = statusFilter
        };
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var record = await _changeRepository.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        return View(record);
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public IActionResult Create()
    {
        PopulateDropdowns();
        return View(new ChangeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(ChangeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDropdowns();
            return View(model);
        }

        var record = new GdpChangeRecord
        {
            ChangeNumber = model.ChangeNumber,
            ChangeType = model.ChangeType,
            Description = model.Description,
            RiskAssessment = model.RiskAssessment,
            ApprovalStatus = ChangeApprovalStatus.Pending
        };

        var validationResult = record.Validate();
        if (!validationResult.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", validationResult.Violations.Select(v => v.Message));
            PopulateDropdowns();
            return View(model);
        }

        var id = await _changeRepository.CreateAsync(record);
        TempData["SuccessMessage"] = $"Change record '{model.ChangeNumber}' created successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ComplianceManager")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var record = await _changeRepository.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        // In a real app, UserId would come from the authenticated user claims
        await _changeRepository.ApproveAsync(id, Guid.Empty);
        TempData["SuccessMessage"] = $"Change record '{record.ChangeNumber}' approved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ComplianceManager")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var record = await _changeRepository.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        await _changeRepository.RejectAsync(id, Guid.Empty);
        TempData["SuccessMessage"] = $"Change record '{record.ChangeNumber}' rejected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void PopulateDropdowns()
    {
        ViewBag.ChangeTypes = new SelectList(
            Enum.GetValues<GdpChangeType>().Select(t => new { Value = (int)t, Text = FormatEnumName(t.ToString()) }),
            "Value", "Text");
    }

    private static string FormatEnumName(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

#region View Models

public class ChangeIndexViewModel
{
    public List<GdpChangeRecord> Records { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public string? StatusFilter { get; set; }
}

public class ChangeCreateViewModel
{
    public string ChangeNumber { get; set; } = string.Empty;
    public GdpChangeType ChangeType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RiskAssessment { get; set; }
}

#endregion
