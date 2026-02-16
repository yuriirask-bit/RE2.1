using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.GdpCompliance;

/// <summary>
/// Service for GDP compliance business logic.
/// T190, T194: GDP site management including WDA coverage validation per FR-033.
/// </summary>
public class GdpComplianceService : IGdpComplianceService
{
    private readonly IGdpSiteRepository _gdpSiteRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILogger<GdpComplianceService> _logger;

    private const string WdaLicenceTypeName = "Wholesale Distribution Authorisation (WDA)";

    public GdpComplianceService(
        IGdpSiteRepository gdpSiteRepository,
        ILicenceRepository licenceRepository,
        ILicenceTypeRepository licenceTypeRepository,
        ILogger<GdpComplianceService> logger)
    {
        _gdpSiteRepository = gdpSiteRepository;
        _licenceRepository = licenceRepository;
        _licenceTypeRepository = licenceTypeRepository;
        _logger = logger;
    }

    #region D365FO Warehouse Browsing

    public async Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetAllWarehousesAsync(cancellationToken);
    }

    public async Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetWarehouseAsync(warehouseId, dataAreaId, cancellationToken);
    }

    #endregion

    #region GDP-Configured Sites

    public async Task<IEnumerable<GdpSite>> GetAllGdpSitesAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetAllGdpConfiguredSitesAsync(cancellationToken);
    }

    public async Task<GdpSite?> GetGdpSiteAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
    }

    #endregion

    #region GDP Configuration

    public async Task<(Guid? Id, ValidationResult Result)> ConfigureGdpAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        // Validate the site configuration
        var validationResult = site.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify warehouse exists in D365FO
        var warehouse = await _gdpSiteRepository.GetWarehouseAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (warehouse == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Warehouse '{site.WarehouseId}' not found in D365 F&O for data area '{site.DataAreaId}'"
                }
            }));
        }

        // Check if already configured
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Warehouse '{site.WarehouseId}' is already configured for GDP"
                }
            }));
        }

        var id = await _gdpSiteRepository.SaveGdpExtensionAsync(site, cancellationToken);
        _logger.LogInformation("Configured GDP for warehouse {WarehouseId} with type {GdpSiteType}", site.WarehouseId, site.GdpSiteType);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateGdpConfigAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        // Validate the site configuration
        var validationResult = site.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Verify GDP extension exists
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP configuration not found for warehouse '{site.WarehouseId}'"
                }
            });
        }

        // Preserve the extension ID
        site.GdpExtensionId = existing.GdpExtensionId;

        await _gdpSiteRepository.UpdateGdpExtensionAsync(site, cancellationToken);
        _logger.LogInformation("Updated GDP configuration for warehouse {WarehouseId}", site.WarehouseId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> RemoveGdpConfigAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        // Verify GDP extension exists
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP configuration not found for warehouse '{warehouseId}'"
                }
            });
        }

        await _gdpSiteRepository.DeleteGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
        _logger.LogInformation("Removed GDP configuration for warehouse {WarehouseId}", warehouseId);

        return ValidationResult.Success();
    }

    #endregion

    #region WDA Coverage

    public async Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetWdaCoverageAsync(warehouseId, dataAreaId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default)
    {
        // Validate coverage model
        var validationResult = coverage.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify the licence exists
        var licence = await _licenceRepository.GetByIdAsync(coverage.LicenceId, cancellationToken);
        if (licence == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{coverage.LicenceId}' not found"
                }
            }));
        }

        // FR-033: Verify licence is WDA type
        var licenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        if (licenceType == null || licenceType.Name != WdaLicenceTypeName)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Licence '{licence.LicenceNumber}' is not a Wholesale Distribution Authorisation (WDA). " +
                             $"Only WDA licences can be used for GDP site coverage."
                }
            }));
        }

        // Verify GDP configuration exists for this warehouse
        var gdpSite = await _gdpSiteRepository.GetGdpExtensionAsync(coverage.WarehouseId, coverage.DataAreaId, cancellationToken);
        if (gdpSite == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Warehouse '{coverage.WarehouseId}' is not configured for GDP. Configure GDP first."
                }
            }));
        }

        var id = await _gdpSiteRepository.AddWdaCoverageAsync(coverage, cancellationToken);
        _logger.LogInformation("Added WDA coverage {CoverageId} for warehouse {WarehouseId} with licence {LicenceNumber}",
            id, coverage.WarehouseId, licence.LicenceNumber);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> RemoveWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default)
    {
        await _gdpSiteRepository.DeleteWdaCoverageAsync(coverageId, cancellationToken);
        _logger.LogInformation("Removed WDA coverage {CoverageId}", coverageId);

        return ValidationResult.Success();
    }

    #endregion
}
