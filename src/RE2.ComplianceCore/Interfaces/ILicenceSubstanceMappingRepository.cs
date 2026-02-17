using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for LicenceSubstanceMapping entity operations.
/// T079a: Repository interface for substance-to-licence mapping CRUD operations.
/// </summary>
public interface ILicenceSubstanceMappingRepository
{
    /// <summary>
    /// Gets a mapping by ID.
    /// </summary>
    Task<LicenceSubstanceMapping?> GetByIdAsync(Guid mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings for a specific licence.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings for a specific substance.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active mappings for a licence (not expired).
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetActiveMappingsByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a mapping already exists for the given licence, substance, and effective date combination.
    /// Per data-model.md: LicenceId + SubstanceId + EffectiveDate must be unique.
    /// </summary>
    Task<LicenceSubstanceMapping?> GetByLicenceSubstanceEffectiveDateAsync(
        Guid licenceId,
        string substanceCode,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings.
    /// </summary>
    Task<IEnumerable<LicenceSubstanceMapping>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new mapping.
    /// </summary>
    Task<Guid> CreateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing mapping.
    /// </summary>
    Task UpdateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mapping.
    /// </summary>
    Task DeleteAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
