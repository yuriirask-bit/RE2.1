using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.GdpCompliance;

/// <summary>
/// Service for GDP operational validation and equipment management.
/// T262: Business logic per US11 (FR-046, FR-047, FR-048).
/// </summary>
public class GdpOperationalService : IGdpOperationalService
{
    private readonly IGdpComplianceService _gdpComplianceService;
    private readonly IGdpEquipmentRepository _equipmentRepository;
    private readonly ILogger<GdpOperationalService> _logger;

    public GdpOperationalService(
        IGdpComplianceService gdpComplianceService,
        IGdpEquipmentRepository equipmentRepository,
        ILogger<GdpOperationalService> logger)
    {
        _gdpComplianceService = gdpComplianceService;
        _equipmentRepository = equipmentRepository;
        _logger = logger;
    }

    #region Site Validation (FR-046)

    public async Task<(bool IsAllowed, string Reason)> ValidateSiteAssignmentAsync(
        string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        // Check if site is GDP-configured
        var site = await _gdpComplianceService.GetGdpSiteAsync(warehouseId, dataAreaId, cancellationToken);
        if (site == null || !site.IsGdpActive)
        {
            return (false, $"Warehouse {warehouseId}/{dataAreaId} is not GDP-configured or not active.");
        }

        // Check for valid WDA coverage
        var coverages = await _gdpComplianceService.GetWdaCoverageAsync(warehouseId, dataAreaId, cancellationToken);
        var validCoverage = coverages.Any(c => c.IsActive());
        if (!validCoverage)
        {
            return (false, $"Warehouse {warehouseId}/{dataAreaId} has no valid WDA licence coverage.");
        }

        return (true, "Site is GDP-active with valid WDA coverage.");
    }

    #endregion

    #region Provider Validation (FR-046/FR-047)

    public async Task<(bool IsAllowed, string Reason)> ValidateProviderAssignmentAsync(
        Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _gdpComplianceService.GetProviderAsync(providerId, cancellationToken);
        if (provider == null)
        {
            return (false, $"Provider {providerId} not found.");
        }

        // Check qualification status
        var isQualified = await _gdpComplianceService.IsPartnerQualifiedAsync(
            GdpCredentialEntityType.ServiceProvider, providerId, cancellationToken);

        if (!isQualified)
        {
            return (false, $"Provider '{provider.ProviderName}' is not GDP-qualified. Requires approved credentials.");
        }

        return (true, $"Provider '{provider.ProviderName}' is GDP-qualified.");
    }

    public async Task<IEnumerable<GdpServiceProvider>> GetApprovedProvidersAsync(
        bool? requireTempControl = null, CancellationToken cancellationToken = default)
    {
        var allProviders = await _gdpComplianceService.GetAllProvidersAsync(cancellationToken);
        var approvedProviders = new List<GdpServiceProvider>();

        foreach (var provider in allProviders)
        {
            var isQualified = await _gdpComplianceService.IsPartnerQualifiedAsync(
                GdpCredentialEntityType.ServiceProvider, provider.ProviderId, cancellationToken);

            if (!isQualified)
                continue;

            if (requireTempControl == true && !provider.TemperatureControlledCapability)
                continue;

            approvedProviders.Add(provider);
        }

        return approvedProviders;
    }

    #endregion

    #region Equipment Qualification (FR-048)

    public async Task<IEnumerable<GdpEquipmentQualification>> GetAllEquipmentAsync(
        CancellationToken cancellationToken = default)
    {
        return await _equipmentRepository.GetAllAsync(cancellationToken);
    }

    public async Task<GdpEquipmentQualification?> GetEquipmentAsync(
        Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        return await _equipmentRepository.GetByIdAsync(equipmentQualificationId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateEquipmentAsync(
        GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        var validationResult = equipment.Validate();
        if (!validationResult.IsValid)
            return (null, validationResult);

        try
        {
            var id = await _equipmentRepository.CreateAsync(equipment, cancellationToken);
            _logger.LogInformation("Created equipment qualification {Id} for {Name}",
                id, equipment.EquipmentName);
            return (id, ValidationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment qualification {Name}", equipment.EquipmentName);
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to create equipment qualification"
                }
            }));
        }
    }

    public async Task<ValidationResult> UpdateEquipmentAsync(
        GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        var validationResult = equipment.Validate();
        if (!validationResult.IsValid)
            return validationResult;

        var existing = await _equipmentRepository.GetByIdAsync(equipment.EquipmentQualificationId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Equipment qualification '{equipment.EquipmentQualificationId}' not found"
                }
            });
        }

        try
        {
            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            _logger.LogInformation("Updated equipment qualification {Id}", equipment.EquipmentQualificationId);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating equipment qualification {Id}", equipment.EquipmentQualificationId);
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to update equipment qualification"
                }
            });
        }
    }

    public async Task<ValidationResult> DeleteEquipmentAsync(
        Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        var existing = await _equipmentRepository.GetByIdAsync(equipmentQualificationId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Equipment qualification '{equipmentQualificationId}' not found"
                }
            });
        }

        try
        {
            await _equipmentRepository.DeleteAsync(equipmentQualificationId, cancellationToken);
            _logger.LogInformation("Deleted equipment qualification {Id}", equipmentQualificationId);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting equipment qualification {Id}", equipmentQualificationId);
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to delete equipment qualification"
                }
            });
        }
    }

    public async Task<IEnumerable<GdpEquipmentQualification>> GetEquipmentDueForRequalificationAsync(
        int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        return await _equipmentRepository.GetDueForRequalificationAsync(daysAhead, cancellationToken);
    }

    #endregion
}
