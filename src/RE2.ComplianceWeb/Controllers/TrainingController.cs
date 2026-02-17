using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP training record management.
/// T293: Web UI per US12 (FR-050).
/// </summary>
[Authorize]
public class TrainingController : Controller
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly IGdpSopRepository _sopRepository;
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(
        ITrainingRepository trainingRepository,
        IGdpSopRepository sopRepository,
        IGdpComplianceService gdpService,
        ILogger<TrainingController> logger)
    {
        _trainingRepository = trainingRepository;
        _sopRepository = sopRepository;
        _gdpService = gdpService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var records = (await _trainingRepository.GetAllAsync()).ToList();
        var expired = records.Where(r => r.IsExpired()).ToList();

        var model = new TrainingIndexViewModel
        {
            Records = records,
            TotalCount = records.Count,
            ExpiredCount = expired.Count,
            PassCount = records.Count(r => r.AssessmentResult == AssessmentResult.Pass),
            FailCount = records.Count(r => r.AssessmentResult == AssessmentResult.Fail)
        };
        return View(model);
    }

    public async Task<IActionResult> StaffReport(Guid staffId)
    {
        var records = (await _trainingRepository.GetByStaffAsync(staffId)).ToList();
        if (!records.Any())
        {
            TempData["ErrorMessage"] = "No training records found for this staff member.";
            return RedirectToAction(nameof(Index));
        }

        var model = new StaffTrainingReportViewModel
        {
            StaffMemberId = staffId,
            StaffMemberName = records.First().StaffMemberName,
            Records = records,
            ExpiredCount = records.Count(r => r.IsExpired())
        };
        return View(model);
    }

    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new TrainingCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> Create(TrainingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var record = new TrainingRecord
        {
            StaffMemberId = model.StaffMemberId,
            StaffMemberName = model.StaffMemberName,
            TrainingCurriculum = model.TrainingCurriculum,
            SopId = model.SopId,
            SiteId = model.SiteId,
            CompletionDate = model.CompletionDate,
            ExpiryDate = model.ExpiryDate,
            TrainerName = model.TrainerName,
            AssessmentResult = model.AssessmentResult
        };

        var validationResult = record.Validate();
        if (!validationResult.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", validationResult.Violations.Select(v => v.Message));
            await PopulateDropdowns();
            return View(model);
        }

        var id = await _trainingRepository.CreateAsync(record);
        TempData["SuccessMessage"] = $"Training record for '{model.StaffMemberName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        var sops = await _sopRepository.GetAllAsync();
        var sites = await _gdpService.GetAllGdpSitesAsync();

        ViewBag.Sops = new SelectList(
            sops.Select(s => new { Id = s.SopId, Name = $"{s.SopNumber} - {s.Title}" }),
            "Id", "Name");
        ViewBag.Sites = new SelectList(
            sites.Select(s => new { Id = s.GdpExtensionId, Name = $"{s.WarehouseId} - {s.WarehouseName}" }),
            "Id", "Name");
        ViewBag.AssessmentResults = new SelectList(
            Enum.GetValues<AssessmentResult>().Select(r => new { Value = (int)r, Text = FormatEnumName(r.ToString()) }),
            "Value", "Text");
    }

    private static string FormatEnumName(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

#region View Models

public class TrainingIndexViewModel
{
    public List<TrainingRecord> Records { get; set; } = new();
    public int TotalCount { get; set; }
    public int ExpiredCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
}

public class StaffTrainingReportViewModel
{
    public Guid StaffMemberId { get; set; }
    public string StaffMemberName { get; set; } = string.Empty;
    public List<TrainingRecord> Records { get; set; } = new();
    public int ExpiredCount { get; set; }
}

public class TrainingCreateViewModel
{
    public Guid StaffMemberId { get; set; }
    public string StaffMemberName { get; set; } = string.Empty;
    public string TrainingCurriculum { get; set; } = string.Empty;
    public Guid? SopId { get; set; }
    public Guid? SiteId { get; set; }
    public DateOnly CompletionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ExpiryDate { get; set; }
    public string? TrainerName { get; set; }
    public AssessmentResult AssessmentResult { get; set; } = AssessmentResult.NotAssessed;
}

#endregion
