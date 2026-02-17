using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP Operations dashboard.
/// T267: Web UI per US11 (FR-046, FR-047, FR-048).
/// </summary>
[Authorize]
public class GdpOperationsController : Controller
{
    private readonly IGdpOperationalService _operationalService;
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpOperationsController> _logger;

    public GdpOperationsController(
        IGdpOperationalService operationalService,
        IGdpComplianceService gdpService,
        ILogger<GdpOperationsController> logger)
    {
        _operationalService = operationalService;
        _gdpService = gdpService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var approvedProviders = (await _operationalService.GetApprovedProvidersAsync()).ToList();
        var tempControlledProviders = approvedProviders.Where(p => p.TemperatureControlledCapability).ToList();
        var equipment = (await _operationalService.GetAllEquipmentAsync()).ToList();
        var dueEquipment = (await _operationalService.GetEquipmentDueForRequalificationAsync(30)).ToList();

        var model = new GdpOperationsDashboardViewModel
        {
            ApprovedProviderCount = approvedProviders.Count,
            TempControlledProviderCount = tempControlledProviders.Count,
            TotalEquipmentCount = equipment.Count,
            QualifiedEquipmentCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.Qualified),
            DueEquipmentCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.DueForRequalification),
            ExpiredEquipmentCount = equipment.Count(e => e.QualificationStatus == GdpQualificationStatusType.Expired),
            EquipmentDueIn30Days = dueEquipment
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateSite(string warehouseId, string dataAreaId)
    {
        if (string.IsNullOrWhiteSpace(warehouseId) || string.IsNullOrWhiteSpace(dataAreaId))
        {
            TempData["ErrorMessage"] = "WarehouseId and DataAreaId are required.";
            return RedirectToAction(nameof(Index));
        }

        var (isAllowed, reason) = await _operationalService.ValidateSiteAssignmentAsync(warehouseId, dataAreaId);
        TempData[isAllowed ? "SuccessMessage" : "ErrorMessage"] = $"Site {warehouseId}: {reason}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateProvider(Guid providerId)
    {
        if (providerId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ProviderId is required.";
            return RedirectToAction(nameof(Index));
        }

        var (isAllowed, reason) = await _operationalService.ValidateProviderAssignmentAsync(providerId);
        TempData[isAllowed ? "SuccessMessage" : "ErrorMessage"] = $"Provider: {reason}";
        return RedirectToAction(nameof(Index));
    }
}

#region View Models

public class GdpOperationsDashboardViewModel
{
    public int ApprovedProviderCount { get; set; }
    public int TempControlledProviderCount { get; set; }
    public int TotalEquipmentCount { get; set; }
    public int QualifiedEquipmentCount { get; set; }
    public int DueEquipmentCount { get; set; }
    public int ExpiredEquipmentCount { get; set; }
    public List<GdpEquipmentQualification> EquipmentDueIn30Days { get; set; } = new();
}

#endregion
