using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for threshold management business logic.
/// T132c: ThresholdService with CRUD operations and validation logic per FR-022.
/// </summary>
public interface IThresholdService
{
    #region CRUD Operations

    /// <summary>
    /// Gets a threshold by its ID.
    /// </summary>
    Task<Threshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all thresholds.
    /// </summary>
    Task<IEnumerable<Threshold>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active thresholds.
    /// </summary>
    Task<IEnumerable<Threshold>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new threshold.
    /// </summary>
    /// <returns>Tuple of created ID (if successful) and validation result.</returns>
    Task<(Guid? Id, ValidationResult Result)> CreateAsync(Threshold threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing threshold.
    /// </summary>
    Task<ValidationResult> UpdateAsync(Threshold threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a threshold.
    /// </summary>
    Task<ValidationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    /// <summary>
    /// Gets thresholds by type.
    /// </summary>
    Task<IEnumerable<Threshold>> GetByTypeAsync(ThresholdType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thresholds for a specific substance.
    /// </summary>
    Task<IEnumerable<Threshold>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thresholds for a specific customer category.
    /// </summary>
    Task<IEnumerable<Threshold>> GetByCustomerCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches thresholds by name.
    /// </summary>
    Task<IEnumerable<Threshold>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Validates a threshold configuration.
    /// </summary>
    Task<ValidationResult> ValidateAsync(Threshold threshold, CancellationToken cancellationToken = default);

    #endregion
}
