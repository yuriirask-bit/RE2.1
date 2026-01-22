using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for licence-substance mapping management.
/// T079f: Web UI controller for FR-004 substance mapping operations.
/// Handles CRUD operations from the _SubstanceMappings partial view.
/// </summary>
[Authorize]
public class LicenceMappingsController : Controller
{
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly ILogger<LicenceMappingsController> _logger;

    public LicenceMappingsController(
        ILicenceSubstanceMappingService mappingService,
        ILogger<LicenceMappingsController> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new substance mapping.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(MappingCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid mapping data.";
            return RedirectToReturnUrl(model.ReturnUrl);
        }

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = model.LicenceId,
            SubstanceId = model.SubstanceId,
            EffectiveDate = DateOnly.FromDateTime(model.EffectiveDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            MaxQuantityPerTransaction = model.MaxQuantityPerTransaction,
            MaxQuantityPerPeriod = model.MaxQuantityPerPeriod,
            PeriodType = model.PeriodType,
            Restrictions = model.Restrictions
        };

        var (id, result) = await _mappingService.CreateAsync(mapping, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToReturnUrl(model.ReturnUrl);
        }

        _logger.LogInformation("Created substance mapping {MappingId} for licence {LicenceId} via web UI",
            id, model.LicenceId);
        TempData["SuccessMessage"] = "Substance authorization added successfully.";

        return RedirectToReturnUrl(model.ReturnUrl);
    }

    /// <summary>
    /// Updates an existing substance mapping.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(MappingEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid mapping data.";
            return RedirectToReturnUrl(model.ReturnUrl);
        }

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = model.MappingId,
            LicenceId = model.LicenceId,
            SubstanceId = model.SubstanceId,
            EffectiveDate = DateOnly.FromDateTime(model.EffectiveDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            MaxQuantityPerTransaction = model.MaxQuantityPerTransaction,
            MaxQuantityPerPeriod = model.MaxQuantityPerPeriod,
            PeriodType = model.PeriodType,
            Restrictions = model.Restrictions
        };

        var result = await _mappingService.UpdateAsync(mapping, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToReturnUrl(model.ReturnUrl);
        }

        _logger.LogInformation("Updated substance mapping {MappingId} via web UI", model.MappingId);
        TempData["SuccessMessage"] = "Substance authorization updated successfully.";

        return RedirectToReturnUrl(model.ReturnUrl);
    }

    /// <summary>
    /// Deletes a substance mapping.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid mappingId, string? returnUrl, CancellationToken cancellationToken = default)
    {
        var result = await _mappingService.DeleteAsync(mappingId, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to remove substance authorization.";
            return RedirectToReturnUrl(returnUrl);
        }

        _logger.LogInformation("Deleted substance mapping {MappingId} via web UI", mappingId);
        TempData["SuccessMessage"] = "Substance authorization removed successfully.";

        return RedirectToReturnUrl(returnUrl);
    }

    /// <summary>
    /// Gets mapping data for the edit modal (AJAX endpoint).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMapping(Guid id, CancellationToken cancellationToken = default)
    {
        var mapping = await _mappingService.GetByIdAsync(id, cancellationToken);

        if (mapping == null)
        {
            return NotFound();
        }

        return Json(new
        {
            mappingId = mapping.MappingId,
            licenceId = mapping.LicenceId,
            substanceId = mapping.SubstanceId,
            substanceName = mapping.Substance?.SubstanceName ?? "Unknown",
            effectiveDate = mapping.EffectiveDate.ToString("yyyy-MM-dd"),
            expiryDate = mapping.ExpiryDate?.ToString("yyyy-MM-dd"),
            maxQuantityPerTransaction = mapping.MaxQuantityPerTransaction,
            maxQuantityPerPeriod = mapping.MaxQuantityPerPeriod,
            periodType = mapping.PeriodType,
            restrictions = mapping.Restrictions
        });
    }

    private IActionResult RedirectToReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Licences");
    }
}

#region View Models

/// <summary>
/// View model for creating a substance mapping.
/// </summary>
public class MappingCreateViewModel
{
    public Guid LicenceId { get; set; }
    public Guid SubstanceId { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
    public DateTime? ExpiryDate { get; set; }
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? Restrictions { get; set; }
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// View model for editing a substance mapping.
/// </summary>
public class MappingEditViewModel
{
    public Guid MappingId { get; set; }
    public Guid LicenceId { get; set; }
    public Guid SubstanceId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? Restrictions { get; set; }
    public string? ReturnUrl { get; set; }
}

#endregion
