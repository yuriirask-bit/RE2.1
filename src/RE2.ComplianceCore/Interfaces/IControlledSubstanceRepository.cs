using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for ControlledSubstance composite entity operations.
/// SubstanceCode (string) is the business key.
/// Classification comes from D365 product attributes; compliance extension from Dataverse.
/// </summary>
public interface IControlledSubstanceRepository
{
    Task<ControlledSubstance?> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all substances from D365 product attributes (read-only classification data).
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetAllD365SubstancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new compliance extension in Dataverse for a D365-discovered substance.
    /// </summary>
    Task SaveComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing compliance extension in Dataverse.
    /// </summary>
    Task UpdateComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a compliance extension from Dataverse.
    /// </summary>
    Task DeleteComplianceExtensionAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the full substance record (used by InMemory implementation).
    /// </summary>
    Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default);
}
