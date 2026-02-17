using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for GDP operational validation and equipment management.
/// T262: Business logic for operational checks per US11 (FR-046, FR-047, FR-048).
/// </summary>
public interface IGdpOperationalService
{
    #region Site Validation (FR-046)

    /// <summary>
    /// Validates whether a warehouse is eligible for GDP-regulated operations.
    /// Checks: GDP active, valid WDA coverage.
    /// </summary>
    Task<(bool IsAllowed, string Reason)> ValidateSiteAssignmentAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region Provider Validation (FR-046/FR-047)

    /// <summary>
    /// Validates whether a service provider is eligible for GDP operations.
    /// Checks: has valid approved credentials.
    /// </summary>
    Task<(bool IsAllowed, string Reason)> ValidateProviderAssignmentAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets providers that are approved for GDP operations.
    /// Optionally filter by temperature-controlled capability.
    /// </summary>
    Task<IEnumerable<GdpServiceProvider>> GetApprovedProvidersAsync(bool? requireTempControl = null, CancellationToken cancellationToken = default);

    #endregion

    #region Equipment Qualification (FR-048)

    /// <summary>
    /// Gets all equipment qualifications.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetAllEquipmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific equipment qualification by ID.
    /// </summary>
    Task<GdpEquipmentQualification?> GetEquipmentAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new equipment qualification with validation.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateEquipmentAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing equipment qualification.
    /// </summary>
    Task<ValidationResult> UpdateEquipmentAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an equipment qualification.
    /// </summary>
    Task<ValidationResult> DeleteEquipmentAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets equipment qualifications due for re-qualification within the specified days.
    /// </summary>
    Task<IEnumerable<GdpEquipmentQualification>> GetEquipmentDueForRequalificationAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    #endregion
}
