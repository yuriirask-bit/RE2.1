using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for controlled substance management web UI.
/// Substances are discovered from D365 product attributes; compliance extensions are managed here.
/// </summary>
[Authorize]
public class SubstancesController : Controller
{
    private readonly IControlledSubstanceService _substanceService;
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<SubstancesController> _logger;

    public SubstancesController(
        IControlledSubstanceService substanceService,
        ILicenceSubstanceMappingService mappingService,
        IProductRepository productRepository,
        ILogger<SubstancesController> logger)
    {
        _substanceService = substanceService;
        _mappingService = mappingService;
        _productRepository = productRepository;
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
    public async Task<IActionResult> Details(string substanceCode, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound();
        }

        // Get associated licences for this substance via mappings (FR-004)
        var mappings = await _mappingService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
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

        // Get products associated with this substance
        var products = await _productRepository.GetProductsBySubstanceCodeAsync(substanceCode, cancellationToken);
        ViewBag.AssociatedProducts = products.ToList();

        return View(substance);
    }

    /// <summary>
    /// Browse D365-discovered substances that may need compliance configuration.
    /// </summary>
    public async Task<IActionResult> Browse(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetControlledProductsAsync(cancellationToken);
        return View(products);
    }

    /// <summary>
    /// Displays the configure compliance form for a D365-discovered substance.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Configure(string substanceCode, CancellationToken cancellationToken = default)
    {
        var substance = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound();
        }

        var model = new SubstanceConfigureViewModel
        {
            SubstanceCode = substance.SubstanceCode,
            SubstanceName = substance.SubstanceName,
            OpiumActList = substance.OpiumActList,
            PrecursorCategory = substance.PrecursorCategory,
            RegulatoryRestrictions = substance.RegulatoryRestrictions,
            IsActive = substance.IsActive,
            ClassificationEffectiveDate = substance.ClassificationEffectiveDate?.ToDateTime(TimeOnly.MinValue),
            IsComplianceConfigured = substance.IsComplianceConfigured,
            CreatedDate = substance.CreatedDate,
            ModifiedDate = substance.ModifiedDate
        };

        return View(model);
    }

    /// <summary>
    /// Handles compliance configuration form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Configure(string substanceCode, SubstanceConfigureViewModel model, CancellationToken cancellationToken = default)
    {
        if (substanceCode != model.SubstanceCode)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var substance = await _substanceService.GetBySubstanceCodeAsync(substanceCode, cancellationToken);

        if (substance == null)
        {
            return NotFound();
        }

        // Apply compliance extension fields
        substance.RegulatoryRestrictions = model.RegulatoryRestrictions;
        substance.IsActive = model.IsActive;
        substance.ClassificationEffectiveDate = model.ClassificationEffectiveDate.HasValue
            ? DateOnly.FromDateTime(model.ClassificationEffectiveDate.Value)
            : null;

        ValidationResult result;
        if (substance.IsComplianceConfigured)
        {
            result = await _substanceService.UpdateComplianceAsync(substance, cancellationToken);
        }
        else
        {
            result = await _substanceService.ConfigureComplianceAsync(substance, cancellationToken);
        }

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        _logger.LogInformation("Configured compliance for substance {SubstanceCode} via web UI", substanceCode);
        TempData["SuccessMessage"] = $"Compliance configuration for '{substance.SubstanceName}' ({substanceCode}) saved successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles substance deactivation (soft delete).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Deactivate(string substanceCode, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.DeactivateAsync(substanceCode, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deactivated controlled substance {SubstanceCode} via web UI", substanceCode);
        TempData["SuccessMessage"] = "Substance deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles substance reactivation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Reactivate(string substanceCode, CancellationToken cancellationToken = default)
    {
        var result = await _substanceService.ReactivateAsync(substanceCode, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Reactivated controlled substance {SubstanceCode} via web UI", substanceCode);
        TempData["SuccessMessage"] = "Substance reactivated successfully.";

        return RedirectToAction(nameof(Index));
    }
}

#region View Models

/// <summary>
/// View model for configuring compliance extension on a substance.
/// D365 classification fields (SubstanceName, OpiumActList, PrecursorCategory) are read-only.
/// </summary>
public class SubstanceConfigureViewModel
{
    // Business key (read-only, from D365)
    public string SubstanceCode { get; set; } = string.Empty;

    // D365 read-only fields (shown for context)
    public string SubstanceName { get; set; } = string.Empty;
    public SubstanceCategories.OpiumActList OpiumActList { get; set; }
    public SubstanceCategories.PrecursorCategory PrecursorCategory { get; set; }

    // Compliance extension fields (editable)
    public string? RegulatoryRestrictions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ClassificationEffectiveDate { get; set; }

    // Status
    public bool IsComplianceConfigured { get; set; }

    // Audit fields for display only
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

#endregion
