using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Threshold entity operations.
/// T129-T130: Repository interface for threshold configuration management.
/// Per FR-020/FR-022: Quantity and frequency threshold storage.
/// </summary>
public interface IThresholdRepository
{
    #region Core Operations

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
    /// Creates a new threshold record.
    /// </summary>
    Task<Guid> CreateAsync(Threshold threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing threshold record.
    /// </summary>
    Task UpdateAsync(Threshold threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a threshold record.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    /// <summary>
    /// Gets thresholds for a specific substance.
    /// </summary>
    Task<IEnumerable<Threshold>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thresholds for a specific licence type.
    /// </summary>
    Task<IEnumerable<Threshold>> GetByLicenceTypeIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thresholds for a specific customer category.
    /// </summary>
    Task<IEnumerable<Threshold>> GetByCustomerCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thresholds by type (Quantity, Frequency, Value).
    /// </summary>
    Task<IEnumerable<Threshold>> GetByTypeAsync(ThresholdType type, CancellationToken cancellationToken = default);

    #endregion

    #region Threshold Lookup

    /// <summary>
    /// Gets the applicable threshold for a substance and threshold type.
    /// Returns the most specific applicable threshold (customer > category > global).
    /// </summary>
    Task<Threshold?> GetApplicableThresholdAsync(
        Guid substanceId,
        ThresholdType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable threshold for a customer-substance combination.
    /// </summary>
    Task<Threshold?> GetApplicableThresholdAsync(
        Guid substanceId,
        Guid customerId,
        BusinessCategory customerCategory,
        ThresholdType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all applicable thresholds for a transaction validation.
    /// Returns thresholds that could apply based on substances in the transaction.
    /// </summary>
    Task<IEnumerable<Threshold>> GetApplicableThresholdsAsync(
        IEnumerable<Guid> substanceIds,
        Guid customerId,
        BusinessCategory customerCategory,
        CancellationToken cancellationToken = default);

    #endregion
}
