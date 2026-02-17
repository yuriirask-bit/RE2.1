using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP site management web UI.
/// T192: Web UI for GDP site browsing, configuration, and WDA coverage management.
/// </summary>
[Authorize]
public class GdpSitesController : Controller
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILicenceService _licenceService;
    private readonly ILogger<GdpSitesController> _logger;

    public GdpSitesController(
        IGdpComplianceService gdpService,
        ILicenceService licenceService,
        ILogger<GdpSitesController> logger)
    {
        _gdpService = gdpService;
        _licenceService = licenceService;
        _logger = logger;
    }

    /// <summary>
    /// Shows GDP-configured sites.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var sites = await _gdpService.GetAllGdpSitesAsync(cancellationToken);
        return View(sites);
    }

    /// <summary>
    /// Browses all D365FO warehouses.
    /// </summary>
    public async Task<IActionResult> Browse(CancellationToken cancellationToken = default)
    {
        var warehouses = await _gdpService.GetAllWarehousesAsync(cancellationToken);
        return View(warehouses);
    }

    /// <summary>
    /// Shows the Configure GDP form for a warehouse.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Configure(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _gdpService.GetWarehouseAsync(warehouseId, dataAreaId, cancellationToken);
        if (warehouse == null)
        {
            return NotFound();
        }

        var model = new GdpSiteConfigureViewModel
        {
            WarehouseId = warehouse.WarehouseId,
            WarehouseName = warehouse.WarehouseName,
            OperationalSiteName = warehouse.OperationalSiteName,
            DataAreaId = warehouse.DataAreaId,
            FormattedAddress = warehouse.FormattedAddress
        };

        ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList();
        return View(model);
    }

    /// <summary>
    /// Handles Configure GDP form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Configure(GdpSiteConfigureViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList(model.GdpSiteType.ToString());
            return View(model);
        }

        var activities = GdpSiteActivity.None;
        if (model.StorageOver72h)
        {
            activities |= GdpSiteActivity.StorageOver72h;
        }

        if (model.TemperatureControlled)
        {
            activities |= GdpSiteActivity.TemperatureControlled;
        }

        if (model.Outsourced)
        {
            activities |= GdpSiteActivity.Outsourced;
        }

        if (model.TransportOnly)
        {
            activities |= GdpSiteActivity.TransportOnly;
        }

        var site = new GdpSite
        {
            WarehouseId = model.WarehouseId,
            DataAreaId = model.DataAreaId,
            GdpSiteType = model.GdpSiteType,
            PermittedActivities = activities,
            IsGdpActive = true
        };

        var (id, result) = await _gdpService.ConfigureGdpAsync(site, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList(model.GdpSiteType.ToString());
            return View(model);
        }

        _logger.LogInformation("Configured GDP for warehouse {WarehouseId} via web UI", model.WarehouseId);
        TempData["SuccessMessage"] = $"GDP configured for warehouse '{model.WarehouseName}'.";

        return RedirectToAction(nameof(Details), new { warehouseId = model.WarehouseId, dataAreaId = model.DataAreaId });
    }

    /// <summary>
    /// Shows GDP site details including WDA coverages.
    /// </summary>
    public async Task<IActionResult> Details(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var site = await _gdpService.GetGdpSiteAsync(warehouseId, dataAreaId, cancellationToken);
        if (site == null)
        {
            return NotFound();
        }

        var coverages = await _gdpService.GetWdaCoverageAsync(warehouseId, dataAreaId, cancellationToken);

        // Enrich coverages with licence info
        var coverageList = coverages.ToList();
        foreach (var coverage in coverageList)
        {
            coverage.Licence = await _licenceService.GetByIdAsync(coverage.LicenceId, cancellationToken);
        }

        ViewBag.WdaCoverages = coverageList;
        return View(site);
    }

    /// <summary>
    /// Shows the Edit GDP configuration form.
    /// </summary>
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Edit(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var site = await _gdpService.GetGdpSiteAsync(warehouseId, dataAreaId, cancellationToken);
        if (site == null)
        {
            return NotFound();
        }

        var model = new GdpSiteConfigureViewModel
        {
            WarehouseId = site.WarehouseId,
            WarehouseName = site.WarehouseName,
            OperationalSiteName = site.OperationalSiteName,
            DataAreaId = site.DataAreaId,
            FormattedAddress = site.FormattedAddress,
            GdpSiteType = site.GdpSiteType,
            StorageOver72h = site.HasActivity(GdpSiteActivity.StorageOver72h),
            TemperatureControlled = site.HasActivity(GdpSiteActivity.TemperatureControlled),
            Outsourced = site.HasActivity(GdpSiteActivity.Outsourced),
            TransportOnly = site.HasActivity(GdpSiteActivity.TransportOnly)
        };

        ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList(model.GdpSiteType.ToString());
        return View(model);
    }

    /// <summary>
    /// Handles Edit GDP configuration form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "QAUser")]
    public async Task<IActionResult> Edit(string warehouseId, GdpSiteConfigureViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList(model.GdpSiteType.ToString());
            return View(model);
        }

        var activities = GdpSiteActivity.None;
        if (model.StorageOver72h)
        {
            activities |= GdpSiteActivity.StorageOver72h;
        }

        if (model.TemperatureControlled)
        {
            activities |= GdpSiteActivity.TemperatureControlled;
        }

        if (model.Outsourced)
        {
            activities |= GdpSiteActivity.Outsourced;
        }

        if (model.TransportOnly)
        {
            activities |= GdpSiteActivity.TransportOnly;
        }

        var site = new GdpSite
        {
            WarehouseId = warehouseId,
            DataAreaId = model.DataAreaId,
            GdpSiteType = model.GdpSiteType,
            PermittedActivities = activities,
            IsGdpActive = true
        };

        var result = await _gdpService.UpdateGdpConfigAsync(site, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.GdpSiteTypes = GetGdpSiteTypeSelectList(model.GdpSiteType.ToString());
            return View(model);
        }

        _logger.LogInformation("Updated GDP configuration for warehouse {WarehouseId} via web UI", warehouseId);
        TempData["SuccessMessage"] = "GDP configuration updated successfully.";

        return RedirectToAction(nameof(Details), new { warehouseId, dataAreaId = model.DataAreaId });
    }

    /// <summary>
    /// Removes GDP configuration from a warehouse.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RemoveGdpConfig(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.RemoveGdpConfigAsync(warehouseId, dataAreaId, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to remove GDP configuration.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Removed GDP configuration for warehouse {WarehouseId} via web UI", warehouseId);
        TempData["SuccessMessage"] = "GDP configuration removed successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Adds WDA coverage to a GDP site.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> AddWdaCoverage(GdpSiteAddWdaCoverageViewModel model, CancellationToken cancellationToken = default)
    {
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = model.WarehouseId,
            DataAreaId = model.DataAreaId,
            LicenceId = model.LicenceId,
            EffectiveDate = model.EffectiveDate.HasValue ? DateOnly.FromDateTime(model.EffectiveDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null
        };

        var (id, result) = await _gdpService.AddWdaCoverageAsync(coverage, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Violations.Select(v => v.Message));
            return RedirectToAction(nameof(Details), new { warehouseId = model.WarehouseId, dataAreaId = model.DataAreaId });
        }

        _logger.LogInformation("Added WDA coverage for warehouse {WarehouseId} via web UI", model.WarehouseId);
        TempData["SuccessMessage"] = "WDA coverage added successfully.";

        return RedirectToAction(nameof(Details), new { warehouseId = model.WarehouseId, dataAreaId = model.DataAreaId });
    }

    /// <summary>
    /// Removes WDA coverage from a GDP site.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RemoveWdaCoverage(Guid coverageId, string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        await _gdpService.RemoveWdaCoverageAsync(coverageId, cancellationToken);

        _logger.LogInformation("Removed WDA coverage {CoverageId} from warehouse {WarehouseId} via web UI", coverageId, warehouseId);
        TempData["SuccessMessage"] = "WDA coverage removed successfully.";

        return RedirectToAction(nameof(Details), new { warehouseId, dataAreaId });
    }

    #region Helper Methods

    private static SelectList GetGdpSiteTypeSelectList(string? selectedValue = null)
    {
        var types = Enum.GetValues<GdpSiteType>()
            .Select(t => new { Value = t.ToString(), Text = FormatEnumName(t.ToString()) });
        return new SelectList(types, "Value", "Text", selectedValue);
    }

    private static string FormatEnumName(string enumName)
    {
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for configuring/editing GDP for a warehouse.
/// </summary>
public class GdpSiteConfigureViewModel
{
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string OperationalSiteName { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public GdpSiteType GdpSiteType { get; set; }
    public bool StorageOver72h { get; set; }
    public bool TemperatureControlled { get; set; }
    public bool Outsourced { get; set; }
    public bool TransportOnly { get; set; }
}

/// <summary>
/// View model for adding WDA coverage.
/// </summary>
public class GdpSiteAddWdaCoverageViewModel
{
    public string WarehouseId { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public Guid LicenceId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

#endregion
