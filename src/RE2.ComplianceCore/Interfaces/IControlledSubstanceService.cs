using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Interface for controlled substance management service.
/// T073a: Enables management of the controlled substance master list per FR-003.
/// </summary>
public interface IControlledSubstanceService
{
    /// <summary>
    /// Gets a controlled substance by ID.
    /// </summary>
    Task<ControlledSubstance?> GetByIdAsync(Guid substanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a controlled substance by internal code.
    /// </summary>
    Task<ControlledSubstance?> GetByInternalCodeAsync(string internalCode, CancellationToken cancellationToken = default);

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
    /// Per FR-003: Master list mapped to Dutch Opium Act lists.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetByOpiumActListAsync(
        SubstanceCategories.OpiumActList opiumActList,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets controlled substances by precursor category.
    /// Per FR-003: Master list mapped to precursor categories.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> GetByPrecursorCategoryAsync(
        SubstanceCategories.PrecursorCategory precursorCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches controlled substances by name or internal code.
    /// </summary>
    Task<IEnumerable<ControlledSubstance>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new controlled substance after validation.
    /// Validates: unique InternalCode, valid classification combination.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing controlled substance after validation.
    /// </summary>
    Task<ValidationResult> UpdateAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a controlled substance.
    /// </summary>
    Task<ValidationResult> DeleteAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a controlled substance meets business rules.
    /// Per data-model.md: InternalCode unique, at least one classification required.
    /// </summary>
    Task<ValidationResult> ValidateSubstanceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a controlled substance (soft delete).
    /// Sets IsActive = false instead of physical deletion.
    /// </summary>
    Task<ValidationResult> DeactivateAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated controlled substance.
    /// </summary>
    Task<ValidationResult> ReactivateAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default);
}
