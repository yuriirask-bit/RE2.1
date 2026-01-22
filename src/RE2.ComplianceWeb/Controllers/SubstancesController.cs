using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for controlled substance management web UI.
/// T073e: Web UI controller for substance CRUD operations per FR-003.
/// </summary>
[Authorize]
public class SubstancesController : Controller
{
    private readonly IControlledSubstanceService _substanceService;
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly ILogger<SubstancesController> _logger;

    public SubstancesController(
        IControlledSubstanceService substanceService,
        ILicenceSubstanceMappingService mappingService,
        ILogger<SubstancesController> logger)
    {
        _substanceService = substanceService;
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the controlled substances list page with optional filtering.
    /// </summary>
    public async Task<IActionResult> Index(
        string? opiumActList = null,
        string? precursorCategory = null,
        string? search = null,
        bool showInactive = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ControlledSubstance> substances;

        if (!string.IsNullOrWhiteSpace(search))
        {
            substances = await _substanceService.SearchAsync(search, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(opiumActList) && Enum.TryParse<SubstanceCategories.OpiumActList>(opiumActList, out var opiumList))
        {
            substances = await _substanceService.GetByOpiumActListAsync(opiumList, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(precursorCategory) && Enum.TryParse<SubstanceCategories.PrecursorCategory>(precursorCategory, out var precursor))
        {
            substances = await _substanceService.GetByPrecursorCategoryAsync(precursor, cancellationToken);
        }
        else if (!showInactive)
        {
            substances = await _substanceService.GetAllActiveAsync(cancellationToken);
        }
        else
        {
            substances = await _substanceService.GetAllAsync(cancellationToken);
        }

        // Apply inactive filter if not showing inactive
        if (!showInactive && string.IsNullOrWhiteSpace(search))
        {
            substances = substances.Where(s => s.IsActive);
        }

        ViewBag.OpiumActFilter = opiumActList;
        ViewBag.PrecursorFilter = precursorCategory;
        ViewBag.SearchTerm = search;
        ViewBag.ShowInactive = showInactive;

        return View(substances);
    }

    /// <summary>
    /// Displays substance details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetByIdAsync(id, cancellationToken);

        if (substance == null)
        {
            return NotFound();
        }

        // Get associated licences for this substance via mappings (FR-004)
        var mappings = await _mappingService.GetBySubstanceIdAsync(id, cancellationToken);
        var associatedLicences = mappings
            .Where(m => m.Licence != null)
            .GroupBy(m => m.LicenceId) // Group by licence to avoid duplicates if multiple mappings exist
            .Select(g => g.First())
            .Select(m => new
            {
                m.LicenceId,
                m.Licence!.LicenceNumber,
                HolderName = m.Licence.HolderType, // Show holder type for now
                m.Licence.Status,
                m.Licence.ExpiryDate,
                MappingEffectiveDate = m.EffectiveDate,
                MappingExpiryDate = m.ExpiryDate
            })
            .ToList();

        ViewBag.AssociatedLicences = associatedLicences;

        return View(substance);
    }

    /// <summary>
    /// Displays the create substance form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public IActionResult Create()
    {
        return View(new SubstanceCreateViewModel
        {
            IsActive = true,
            ClassificationEffectiveDate = DateTime.Today
        });
    }

    /// <summary>
    /// Handles substance creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(SubstanceCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = model.SubstanceName,
            InternalCode = model.InternalCode,
            OpiumActList = model.OpiumActList,
            PrecursorCategory = model.PrecursorCategory,
            RegulatoryRestrictions = model.RegulatoryRestrictions,
            IsActive = model.IsActive,
            ClassificationEffectiveDate = model.ClassificationEffectiveDate.HasValue
                ? DateOnly.FromDateTime(model.ClassificationEffectiveDate.Value)
                : null
        };

        var (id, result) = await _substanceService.CreateAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        _logger.LogInformation("Created controlled substance {Name} ({Code}) via web UI", substance.SubstanceName, substance.InternalCode);
        TempData["SuccessMessage"] = $"Substance '{substance.SubstanceName}' ({substance.InternalCode}) created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit substance form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetByIdAsync(id, cancellationToken);

        if (substance == null)
        {
            return NotFound();
        }

        var model = new SubstanceEditViewModel
        {
            SubstanceId = substance.SubstanceId,
            SubstanceName = substance.SubstanceName,
            InternalCode = substance.InternalCode,
            OpiumActList = substance.OpiumActList,
            PrecursorCategory = substance.PrecursorCategory,
            RegulatoryRestrictions = substance.RegulatoryRestrictions,
            IsActive = substance.IsActive,
            ClassificationEffectiveDate = substance.ClassificationEffectiveDate?.ToDateTime(TimeOnly.MinValue),
            CreatedDate = substance.CreatedDate,
            ModifiedDate = substance.ModifiedDate
        };

        return View(model);
    }

    /// <summary>
    /// Handles substance edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, SubstanceEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.SubstanceId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var substance = new ControlledSubstance
        {
            SubstanceId = model.SubstanceId,
            SubstanceName = model.SubstanceName,
            InternalCode = model.InternalCode,
            OpiumActList = model.OpiumActList,
            PrecursorCategory = model.PrecursorCategory,
            RegulatoryRestrictions = model.RegulatoryRestrictions,
            IsActive = model.IsActive,
            ClassificationEffectiveDate = model.ClassificationEffectiveDate.HasValue
                ? DateOnly.FromDateTime(model.ClassificationEffectiveDate.Value)
                : null
        };

        var result = await _substanceService.UpdateAsync(substance, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        _logger.LogInformation("Updated controlled substance {Id} ({Code}) via web UI", id, model.InternalCode);
        TempData["SuccessMessage"] = $"Substance '{model.SubstanceName}' updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles substance deactivation (soft delete).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeactivateAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deactivated controlled substance {Id} via web UI", id);
        TempData["SuccessMessage"] = "Substance deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles substance reactivation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.ReactivateAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Reactivated controlled substance {Id} via web UI", id);
        TempData["SuccessMessage"] = "Substance reactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles substance deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to delete substance. It may have associated licence mappings.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted controlled substance {Id} via web UI", id);
        TempData["SuccessMessage"] = "Substance deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}

#region View Models

/// <summary>
/// View model for creating a new controlled substance.
/// </summary>
public class SubstanceCreateViewModel
{
    public string SubstanceName { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public SubstanceCategories.OpiumActList OpiumActList { get; set; } = SubstanceCategories.OpiumActList.None;
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; } = SubstanceCategories.PrecursorCategory.None;
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ClassificationEffectiveDate { get; set; }
}

/// <summary>
/// View model for editing a controlled substance.
/// </summary>
public class SubstanceEditViewModel
{
    public Guid SubstanceId { get; set; }
    public string SubstanceName { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ClassificationEffectiveDate { get; set; }

    // Audit fields for display only
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

#endregion
