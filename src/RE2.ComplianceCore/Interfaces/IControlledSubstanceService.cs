using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Interface for controlled substance management service.
/// Substances are discovered from D365 product attributes; compliance extensions managed in Dataverse.
/// </summary>
public interface IControlledSubstanceService
{
    /// <summary>
    /// Gets a controlled substance by substance code (business key).
    /// </summary>
    Task<ControlledSubstance?> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all controlled substances.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active controlled substances.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets controlled substances by Opium Act classification.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetByOpiumActListAsync(
        SubstanceCategories.OpiumActList opiumActList,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets controlled substances by precursor category.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetByPrecursorCategoryAsync(
        SubstanceCategories.PrecursorCategory precursorCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches controlled substances by name or substance code.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures a compliance extension in Dataverse for a D365-discovered substance.
    /// </summary>
    Task<ValidationResult> ConfigureComplianceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing compliance extension in Dataverse.
    /// </summary>
    Task<ValidationResult> UpdateComplianceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a controlled substance meets business rules.
    /// </summary>
    Task<ValidationResult> ValidateSubstanceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a controlled substance (soft delete).
    /// </summary>
    Task<ValidationResult> DeactivateAsync(
        string substanceCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated controlled substance.
    /// </summary>
    Task<ValidationResult> ReactivateAsync(
        string substanceCode,
        CancellationToken cancellationToken = default);
}
