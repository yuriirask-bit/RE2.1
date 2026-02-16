using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for licence-substance mapping business logic.
/// T079c: Service interface for FR-004 substance-to-licence mappings.
/// </summary>
public interface ILicenceSubstanceMappingService
{
    /// <summary>
    /// Gets a mapping by ID with related entities populated.
    /// </summary>
    Task<LicenceSubstanceMapping?> GetByIdAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings for a specific licence with related entities populated.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings for a specific substance with related entities populated.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (currently effective and not expired) mappings for a licence.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetActiveMappingsByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new mapping after validation.
    /// Validates per data-model.md:
    /// - LicenceId + SubstanceCode + EffectiveDate must be unique
    /// - ExpiryDate must not exceed licence's ExpiryDate
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing mapping after validation.
    /// </summary>
    Task<ValidationResult> UpdateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mapping.
    /// </summary>
    Task<ValidationResult> DeleteAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a mapping according to business rules.
    /// </summary>
    Task<ValidationResult> ValidateMappingAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a licence authorizes a specific substance (has active mapping).
    /// Used by transaction validation per FR-018.
    /// </summary>
    Task<bool> IsSubstanceAuthorizedByLicenceAsync(Guid licenceId, string substanceCode, CancellationToken cancellationToken = default);
}
