using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for threshold configuration management web UI.
/// T132f: Web UI controller for threshold CRUD operations per FR-022.
/// </summary>
[Authorize]
public class ThresholdsController : Controller
{
    private readonly IThresholdService _thresholdService;
    private readonly IControlledSubstanceService _substanceService;
    private readonly ILogger<ThresholdsController> _logger;

    public ThresholdsController(
        IThresholdService thresholdService,
        IControlledSubstanceService substanceService,
        ILogger<ThresholdsController> logger)
    {
        _thresholdService = thresholdService;
        _substanceService = substanceService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the thresholds list page with optional filtering.
    /// </summary>
    public async Task<IActionResult> Index(
        ThresholdType? type = null,
        string? search = null,
        bool showInactive = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Threshold> thresholds;

        if (!string.IsNullOrWhiteSpace(search))
        {
            thresholds = await _thresholdService.SearchAsync(search, cancellationToken);
        }
        else if (type.HasValue)
        {
            thresholds = await _thresholdService.GetByTypeAsync(type.Value, cancellationToken);
        }
        else if (!showInactive)
        {
            thresholds = await _thresholdService.GetActiveAsync(cancellationToken);
        }
        else
        {
            thresholds = await _thresholdService.GetAllAsync(cancellationToken);
        }

        // Apply inactive filter if not showing inactive
        if (!showInactive && string.IsNullOrWhiteSpace(search))
        {
            thresholds = thresholds.Where(t => t.IsActive);
        }

        ViewBag.TypeFilter = type;
        ViewBag.SearchTerm = search;
        ViewBag.ShowInactive = showInactive;

        return View(thresholds);
    }

    /// <summary>
    /// Displays threshold details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var threshold = await _thresholdService.GetByIdAsync(id, cancellationToken);

        if (threshold == null)
        {
            return NotFound();
        }

        return View(threshold);
    }

    /// <summary>
    /// Displays the create threshold form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        await PopulateDropdownsAsync(cancellationToken);

        return View(new ThresholdCreateViewModel
        {
            IsActive = true,
            WarningThresholdPercent = 80,
            AllowOverride = true,
            LimitUnit = "g",
            Period = ThresholdPeriod.Monthly
        });
    }

    /// <summary>
    /// Handles threshold creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(ThresholdCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(cancellationToken);
            return View(model);
        }

        var threshold = model.ToDomainModel();

        var (id, result) = await _thresholdService.CreateAsync(threshold, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            await PopulateDropdownsAsync(cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Created threshold {Name} ({Type}) via web UI", threshold.Name, threshold.ThresholdType);
        TempData["SuccessMessage"] = $"Threshold '{threshold.Name}' created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit threshold form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var threshold = await _thresholdService.GetByIdAsync(id, cancellationToken);

        if (threshold == null)
        {
            return NotFound();
        }

        await PopulateDropdownsAsync(cancellationToken);

        var model = ThresholdEditViewModel.FromDomainModel(threshold);

        return View(model);
    }

    /// <summary>
    /// Handles threshold edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, ThresholdEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(cancellationToken);
            return View(model);
        }

        var threshold = model.ToDomainModel();

        var result = await _thresholdService.UpdateAsync(threshold, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            await PopulateDropdownsAsync(cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Updated threshold {Id} ({Name}) via web UI", id, model.Name);
        TempData["SuccessMessage"] = $"Threshold '{model.Name}' updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles threshold deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _thresholdService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted threshold {Id} via web UI", id);
        TempData["SuccessMessage"] = "Threshold deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Toggles threshold active status.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken cancellationToken = default)
    {
        var threshold = await _thresholdService.GetByIdAsync(id, cancellationToken);

        if (threshold == null)
        {
            TempData["ErrorMessage"] = "Threshold not found.";
            return RedirectToAction(nameof(Index));
        }

        threshold.IsActive = !threshold.IsActive;
        var result = await _thresholdService.UpdateAsync(threshold, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        var status = threshold.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Toggled threshold {Id} to {Status} via web UI", id, status);
        TempData["SuccessMessage"] = $"Threshold '{threshold.Name}' {status} successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(CancellationToken cancellationToken)
    {
        var substances = await _substanceService.GetAllActiveAsync(cancellationToken);

        ViewBag.Substances = substances.Select(s => new SelectListItem
        {
            Value = s.SubstanceId.ToString(),
            Text = $"{s.SubstanceName} ({s.InternalCode})"
        }).ToList();

        ViewBag.ThresholdTypes = Enum.GetValues<ThresholdType>()
            .Select(t => new SelectListItem
            {
                Value = ((int)t).ToString(),
                Text = t.ToString()
            }).ToList();

        ViewBag.ThresholdPeriods = Enum.GetValues<ThresholdPeriod>()
            .Select(p => new SelectListItem
            {
                Value = ((int)p).ToString(),
                Text = p.ToString()
            }).ToList();

        ViewBag.CustomerCategories = Enum.GetValues<BusinessCategory>()
            .Select(c => new SelectListItem
            {
                Value = ((int)c).ToString(),
                Text = c.ToString()
            }).ToList();
    }
}

#region View Models

/// <summary>
/// View model for creating a new threshold.
/// </summary>
public class ThresholdCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ThresholdType ThresholdType { get; set; }
    public ThresholdPeriod Period { get; set; }

    // Scope
    public Guid? SubstanceId { get; set; }
    public Guid? LicenceTypeId { get; set; }
    public BusinessCategory? CustomerCategory { get; set; }
    public string? OpiumActList { get; set; }

    // Limits
    public decimal LimitValue { get; set; }
    public string LimitUnit { get; set; } = "g";
    public decimal WarningThresholdPercent { get; set; } = 80;

    // Override settings
    public bool AllowOverride { get; set; } = true;
    public decimal? MaxOverridePercent { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? RegulatoryReference { get; set; }

    public Threshold ToDomainModel()
    {
        return new Threshold
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            ThresholdType = ThresholdType,
            Period = Period,
            SubstanceId = SubstanceId,
            LicenceTypeId = LicenceTypeId,
            CustomerCategory = CustomerCategory,
            OpiumActList = OpiumActList,
            LimitValue = LimitValue,
            LimitUnit = LimitUnit,
            WarningThresholdPercent = WarningThresholdPercent,
            AllowOverride = AllowOverride,
            MaxOverridePercent = MaxOverridePercent,
            IsActive = IsActive,
            EffectiveFrom = EffectiveFrom.HasValue ? DateOnly.FromDateTime(EffectiveFrom.Value) : null,
            EffectiveTo = EffectiveTo.HasValue ? DateOnly.FromDateTime(EffectiveTo.Value) : null,
            RegulatoryReference = RegulatoryReference,
            CreatedDate = DateTime.UtcNow
        };
    }
}

/// <summary>
/// View model for editing a threshold.
/// </summary>
public class ThresholdEditViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ThresholdType ThresholdType { get; set; }
    public ThresholdPeriod Period { get; set; }

    // Scope
    public Guid? SubstanceId { get; set; }
    public Guid? LicenceTypeId { get; set; }
    public BusinessCategory? CustomerCategory { get; set; }
    public string? OpiumActList { get; set; }

    // Limits
    public decimal LimitValue { get; set; }
    public string LimitUnit { get; set; } = "g";
    public decimal WarningThresholdPercent { get; set; } = 80;

    // Override settings
    public bool AllowOverride { get; set; } = true;
    public decimal? MaxOverridePercent { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? RegulatoryReference { get; set; }

    // Audit fields (display only)
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public static ThresholdEditViewModel FromDomainModel(Threshold threshold)
    {
        return new ThresholdEditViewModel
        {
            Id = threshold.Id,
            Name = threshold.Name,
            Description = threshold.Description,
            ThresholdType = threshold.ThresholdType,
            Period = threshold.Period,
            SubstanceId = threshold.SubstanceId,
            LicenceTypeId = threshold.LicenceTypeId,
            CustomerCategory = threshold.CustomerCategory,
            OpiumActList = threshold.OpiumActList,
            LimitValue = threshold.LimitValue,
            LimitUnit = threshold.LimitUnit,
            WarningThresholdPercent = threshold.WarningThresholdPercent,
            AllowOverride = threshold.AllowOverride,
            MaxOverridePercent = threshold.MaxOverridePercent,
            IsActive = threshold.IsActive,
            EffectiveFrom = threshold.EffectiveFrom?.ToDateTime(TimeOnly.MinValue),
            EffectiveTo = threshold.EffectiveTo?.ToDateTime(TimeOnly.MinValue),
            RegulatoryReference = threshold.RegulatoryReference,
            CreatedDate = threshold.CreatedDate,
            ModifiedDate = threshold.ModifiedDate
        };
    }

    public Threshold ToDomainModel()
    {
        return new Threshold
        {
            Id = Id,
            Name = Name,
            Description = Description,
            ThresholdType = ThresholdType,
            Period = Period,
            SubstanceId = SubstanceId,
            LicenceTypeId = LicenceTypeId,
            CustomerCategory = CustomerCategory,
            OpiumActList = OpiumActList,
            LimitValue = LimitValue,
            LimitUnit = LimitUnit,
            WarningThresholdPercent = WarningThresholdPercent,
            AllowOverride = AllowOverride,
            MaxOverridePercent = MaxOverridePercent,
            IsActive = IsActive,
            EffectiveFrom = EffectiveFrom.HasValue ? DateOnly.FromDateTime(EffectiveFrom.Value) : null,
            EffectiveTo = EffectiveTo.HasValue ? DateOnly.FromDateTime(EffectiveTo.Value) : null,
            RegulatoryReference = RegulatoryReference
        };
    }
}

#endregion
