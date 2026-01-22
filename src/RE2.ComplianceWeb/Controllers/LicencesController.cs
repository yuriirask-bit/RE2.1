using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for licence management web UI.
/// T077: Web UI controller for licence CRUD operations.
/// </summary>
[Authorize]
public class LicencesController : Controller
{
    private readonly ILicenceService _licenceService;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<LicencesController> _logger;

    public LicencesController(
        ILicenceService licenceService,
        ILicenceTypeRepository licenceTypeRepository,
        ILicenceSubstanceMappingService mappingService,
        IControlledSubstanceRepository substanceRepository,
        ILogger<LicencesController> logger)
    {
        _licenceService = licenceService;
        _licenceTypeRepository = licenceTypeRepository;
        _mappingService = mappingService;
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Loads licence types for dropdown lists.
    /// </summary>
    private async Task<SelectList> GetLicenceTypeSelectListAsync(Guid? selectedId = null, CancellationToken cancellationToken = default)
    {
        var licenceTypes = await _licenceTypeRepository.GetAllActiveAsync(cancellationToken);
        return new SelectList(licenceTypes, "LicenceTypeId", "Name", selectedId);
    }

    /// <summary>
    /// Displays the licence list page.
    /// </summary>
    public async Task<IActionResult> Index(string? status = null, CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(status))
        {
            licences = licences.Where(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.StatusFilter = status;
        return View(licences);
    }

    /// <summary>
    /// Displays the create licence form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(cancellationToken: cancellationToken);
        return View(new LicenceCreateViewModel());
    }

    /// <summary>
    /// Handles licence creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(LicenceCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            return View(model);
        }

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = model.LicenceNumber,
            LicenceTypeId = model.LicenceTypeId,
            HolderType = model.HolderType,
            HolderId = model.HolderId,
            IssuingAuthority = model.IssuingAuthority,
            IssueDate = DateOnly.FromDateTime(model.IssueDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            Status = "Valid",
            Scope = model.Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities
        };

        var (id, result) = await _licenceService.CreateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Created licence {LicenceNumber} via web UI", licence.LicenceNumber);
        TempData["SuccessMessage"] = $"Licence {licence.LicenceNumber} created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays licence details with substance mappings (FR-004).
    /// T079f: Loads substance mappings for the licence details view.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound();
        }

        // Load substance mappings for this licence (FR-004)
        var mappings = await _mappingService.GetByLicenceIdAsync(id, cancellationToken);
        ViewBag.SubstanceMappings = mappings;
        ViewBag.LicenceId = id;
        ViewBag.LicenceNumber = licence.LicenceNumber;

        // Load available substances for add mapping modal
        var substances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        ViewBag.Substances = substances;

        return View(licence);
    }

    /// <summary>
    /// Displays the edit licence form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound();
        }

        var model = new LicenceEditViewModel
        {
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeId = licence.LicenceTypeId,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            IssuingAuthority = licence.IssuingAuthority,
            IssueDate = licence.IssueDate.ToDateTime(TimeOnly.MinValue),
            ExpiryDate = licence.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
            Status = licence.Status,
            Scope = licence.Scope,
            PermittedActivities = (int)licence.PermittedActivities
        };

        ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
        return View(model);
    }

    /// <summary>
    /// Handles licence edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, LicenceEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.LicenceId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            return View(model);
        }

        var licence = new Licence
        {
            LicenceId = model.LicenceId,
            LicenceNumber = model.LicenceNumber,
            LicenceTypeId = model.LicenceTypeId,
            HolderType = model.HolderType,
            HolderId = model.HolderId,
            IssuingAuthority = model.IssuingAuthority,
            IssueDate = DateOnly.FromDateTime(model.IssueDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            Status = model.Status,
            Scope = model.Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities
        };

        var result = await _licenceService.UpdateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Updated licence {LicenceNumber} via web UI", licence.LicenceNumber);
        TempData["SuccessMessage"] = $"Licence {licence.LicenceNumber} updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles licence deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _licenceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to delete licence.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted licence {Id} via web UI", id);
        TempData["SuccessMessage"] = "Licence deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays expiring licences.
    /// Per FR-007: Generate alerts for licences expiring within configurable period.
    /// </summary>
    public async Task<IActionResult> Expiring(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetExpiringLicencesAsync(daysAhead, cancellationToken);

        ViewBag.DaysAhead = daysAhead;
        return View(licences);
    }
}

#region View Models

/// <summary>
/// View model for creating a new licence.
/// </summary>
public class LicenceCreateViewModel
{
    public string LicenceNumber { get; set; } = string.Empty;
    public Guid LicenceTypeId { get; set; }
    public string HolderType { get; set; } = "Company";
    public Guid HolderId { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime? ExpiryDate { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
}

/// <summary>
/// View model for editing a licence.
/// </summary>
public class LicenceEditViewModel
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public Guid LicenceTypeId { get; set; }
    public string HolderType { get; set; } = "Company";
    public Guid HolderId { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "Valid";
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
}

#endregion
