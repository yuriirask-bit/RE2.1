using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP sites API endpoints.
/// T191: REST API for GDP site management per User Story 7 (FR-033, FR-034, FR-035).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GdpSitesController : ControllerBase
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpSitesController> _logger;

    public GdpSitesController(IGdpComplianceService gdpService, ILogger<GdpSitesController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    #region D365FO Warehouse Browsing

    /// <summary>
    /// Gets all D365FO warehouses for browsing.
    /// </summary>
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(IEnumerable<WarehouseResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken = default)
    {
        var warehouses = await _gdpService.GetAllWarehousesAsync(cancellationToken);
        return Ok(warehouses.Select(WarehouseResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific D365FO warehouse.
    /// </summary>
    [HttpGet("warehouses/{warehouseId}")]
    [ProducesResponseType(typeof(WarehouseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouse(string warehouseId, [FromQuery] string dataAreaId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _gdpService.GetWarehouseAsync(warehouseId, dataAreaId, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Warehouse '{warehouseId}' not found for data area '{dataAreaId}'"
            });
        }

        return Ok(WarehouseResponseDto.FromDomain(warehouse));
    }

    #endregion

    #region GDP-Configured Sites

    /// <summary>
    /// Gets all GDP-configured sites.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GdpSiteResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGdpSites(CancellationToken cancellationToken = default)
    {
        var sites = await _gdpService.GetAllGdpSitesAsync(cancellationToken);
        return Ok(sites.Select(GdpSiteResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific GDP-configured site.
    /// </summary>
    [HttpGet("{warehouseId}")]
    [ProducesResponseType(typeof(GdpSiteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGdpSite(string warehouseId, [FromQuery] string dataAreaId, CancellationToken cancellationToken = default)
    {
        var site = await _gdpService.GetGdpSiteAsync(warehouseId, dataAreaId, cancellationToken);
        if (site == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"GDP configuration not found for warehouse '{warehouseId}'"
            });
        }

        return Ok(GdpSiteResponseDto.FromDomain(site));
    }

    #endregion

    #region GDP Configuration

    /// <summary>
    /// Configures GDP for a D365FO warehouse.
    /// T195: Only QAUser or ComplianceManager can configure GDP.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpSiteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfigureGdp([FromBody] ConfigureGdpRequestDto request, CancellationToken cancellationToken = default)
    {
        var site = request.ToDomain();
        var (id, result) = await _gdpService.ConfigureGdpAsync(site, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetGdpSiteAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        return CreatedAtAction(
            nameof(GetGdpSite),
            new { warehouseId = site.WarehouseId, dataAreaId = site.DataAreaId },
            GdpSiteResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Updates GDP configuration for a warehouse.
    /// T195: Only QAUser or ComplianceManager can update GDP configuration.
    /// </summary>
    [HttpPut("{warehouseId}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpSiteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGdpConfig(string warehouseId, [FromBody] ConfigureGdpRequestDto request, CancellationToken cancellationToken = default)
    {
        var site = request.ToDomain();
        site.WarehouseId = warehouseId;

        var result = await _gdpService.UpdateGdpConfigAsync(site, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var updated = await _gdpService.GetGdpSiteAsync(warehouseId, site.DataAreaId, cancellationToken);
        return Ok(GdpSiteResponseDto.FromDomain(updated!));
    }

    /// <summary>
    /// Removes GDP configuration from a warehouse.
    /// T195: Only ComplianceManager can remove GDP configuration.
    /// </summary>
    [HttpDelete("{warehouseId}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGdpConfig(string warehouseId, [FromQuery] string dataAreaId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.RemoveGdpConfigAsync(warehouseId, dataAreaId, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        return NoContent();
    }

    #endregion

    #region WDA Coverage

    /// <summary>
    /// Gets WDA coverage records for a warehouse.
    /// </summary>
    [HttpGet("{warehouseId}/wda-coverage")]
    [ProducesResponseType(typeof(IEnumerable<WdaCoverageResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWdaCoverage(string warehouseId, [FromQuery] string dataAreaId, CancellationToken cancellationToken = default)
    {
        var coverages = await _gdpService.GetWdaCoverageAsync(warehouseId, dataAreaId, cancellationToken);
        return Ok(coverages.Select(WdaCoverageResponseDto.FromDomain));
    }

    /// <summary>
    /// Adds WDA coverage for a warehouse.
    /// FR-033: Only WDA licences can be used for coverage.
    /// T195: Only ComplianceManager can add WDA coverage.
    /// </summary>
    [HttpPost("{warehouseId}/wda-coverage")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(WdaCoverageResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddWdaCoverage(string warehouseId, [FromBody] CreateWdaCoverageRequestDto request, CancellationToken cancellationToken = default)
    {
        var coverage = request.ToDomain(warehouseId);
        var (id, result) = await _gdpService.AddWdaCoverageAsync(coverage, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND || errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        coverage.CoverageId = id!.Value;
        return CreatedAtAction(
            nameof(GetWdaCoverage),
            new { warehouseId, dataAreaId = coverage.DataAreaId },
            WdaCoverageResponseDto.FromDomain(coverage));
    }

    /// <summary>
    /// Removes WDA coverage from a warehouse.
    /// </summary>
    [HttpDelete("{warehouseId}/wda-coverage/{coverageId}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveWdaCoverage(string warehouseId, Guid coverageId, CancellationToken cancellationToken = default)
    {
        await _gdpService.RemoveWdaCoverageAsync(coverageId, cancellationToken);
        return NoContent();
    }

    #endregion
}

#region DTOs

/// <summary>
/// Response DTO for D365FO warehouse data.
/// </summary>
public class WarehouseResponseDto
{
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string OperationalSiteId { get; set; } = string.Empty;
    public string OperationalSiteName { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryRegionId { get; set; } = string.Empty;
    public bool IsConfiguredForGdp { get; set; }

    public static WarehouseResponseDto FromDomain(GdpSite site)
    {
        return new WarehouseResponseDto
        {
            WarehouseId = site.WarehouseId,
            WarehouseName = site.WarehouseName,
            OperationalSiteId = site.OperationalSiteId,
            OperationalSiteName = site.OperationalSiteName,
            DataAreaId = site.DataAreaId,
            WarehouseType = site.WarehouseType,
            FormattedAddress = site.FormattedAddress,
            City = site.City,
            CountryRegionId = site.CountryRegionId,
            IsConfiguredForGdp = site.IsConfiguredForGdp
        };
    }
}

/// <summary>
/// Response DTO for GDP-configured site.
/// </summary>
public class GdpSiteResponseDto
{
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string OperationalSiteId { get; set; } = string.Empty;
    public string OperationalSiteName { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryRegionId { get; set; } = string.Empty;
    public Guid GdpExtensionId { get; set; }
    public string GdpSiteType { get; set; } = string.Empty;
    public int PermittedActivities { get; set; }
    public List<string> PermittedActivityNames { get; set; } = new();
    public bool IsGdpActive { get; set; }

    public static GdpSiteResponseDto FromDomain(GdpSite site)
    {
        var activityNames = new List<string>();
        if (site.HasActivity(GdpSiteActivity.StorageOver72h)) activityNames.Add("Storage Over 72h");
        if (site.HasActivity(GdpSiteActivity.TemperatureControlled)) activityNames.Add("Temperature Controlled");
        if (site.HasActivity(GdpSiteActivity.Outsourced)) activityNames.Add("Outsourced");
        if (site.HasActivity(GdpSiteActivity.TransportOnly)) activityNames.Add("Transport Only");

        return new GdpSiteResponseDto
        {
            WarehouseId = site.WarehouseId,
            WarehouseName = site.WarehouseName,
            OperationalSiteId = site.OperationalSiteId,
            OperationalSiteName = site.OperationalSiteName,
            DataAreaId = site.DataAreaId,
            WarehouseType = site.WarehouseType,
            FormattedAddress = site.FormattedAddress,
            City = site.City,
            CountryRegionId = site.CountryRegionId,
            GdpExtensionId = site.GdpExtensionId,
            GdpSiteType = site.GdpSiteType.ToString(),
            PermittedActivities = (int)site.PermittedActivities,
            PermittedActivityNames = activityNames,
            IsGdpActive = site.IsGdpActive
        };
    }
}

/// <summary>
/// Request DTO for configuring GDP on a warehouse.
/// </summary>
public class ConfigureGdpRequestDto
{
    public required string WarehouseId { get; set; }
    public required string DataAreaId { get; set; }
    public required string GdpSiteType { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsGdpActive { get; set; } = true;

    public GdpSite ToDomain()
    {
        return new GdpSite
        {
            WarehouseId = WarehouseId,
            DataAreaId = DataAreaId,
            GdpSiteType = Enum.Parse<GdpSiteType>(GdpSiteType, true),
            PermittedActivities = (GdpSiteActivity)PermittedActivities,
            IsGdpActive = IsGdpActive
        };
    }
}

/// <summary>
/// Response DTO for WDA coverage.
/// </summary>
public class WdaCoverageResponseDto
{
    public Guid CoverageId { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public Guid LicenceId { get; set; }
    public string EffectiveDate { get; set; } = string.Empty;
    public string? ExpiryDate { get; set; }
    public bool IsActive { get; set; }

    public static WdaCoverageResponseDto FromDomain(GdpSiteWdaCoverage coverage)
    {
        return new WdaCoverageResponseDto
        {
            CoverageId = coverage.CoverageId,
            WarehouseId = coverage.WarehouseId,
            DataAreaId = coverage.DataAreaId,
            LicenceId = coverage.LicenceId,
            EffectiveDate = coverage.EffectiveDate.ToString("yyyy-MM-dd"),
            ExpiryDate = coverage.ExpiryDate?.ToString("yyyy-MM-dd"),
            IsActive = coverage.IsActive()
        };
    }
}

/// <summary>
/// Request DTO for creating WDA coverage.
/// </summary>
public class CreateWdaCoverageRequestDto
{
    public required string DataAreaId { get; set; }
    public required Guid LicenceId { get; set; }
    public required string EffectiveDate { get; set; }
    public string? ExpiryDate { get; set; }

    public GdpSiteWdaCoverage ToDomain(string warehouseId)
    {
        return new GdpSiteWdaCoverage
        {
            WarehouseId = warehouseId,
            DataAreaId = DataAreaId,
            LicenceId = LicenceId,
            EffectiveDate = DateOnly.Parse(EffectiveDate),
            ExpiryDate = string.IsNullOrEmpty(ExpiryDate) ? null : DateOnly.Parse(ExpiryDate)
        };
    }
}

#endregion
