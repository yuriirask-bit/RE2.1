using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Threshold entity operations.
/// Per FR-020/FR-022: Quantity and frequency threshold storage.
/// </summary>
public interface IThresholdRepository
{
    #region Core Operations

    Task<Threshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Threshold>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Threshold>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Threshold threshold, CancellationToken cancellationToken = default);
    Task UpdateAsync(Threshold threshold, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    Task<IEnumerable<Threshold>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Threshold>> GetByLicenceTypeIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Threshold>> GetByCustomerCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);
    Task<IEnumerable<Threshold>> GetByTypeAsync(ThresholdType type, CancellationToken cancellationToken = default);

    #endregion

    #region Threshold Lookup

    Task<Threshold?> GetApplicableThresholdAsync(
        string substanceCode,
        ThresholdType type,
        CancellationToken cancellationToken = default);

    Task<Threshold?> GetApplicableThresholdAsync(
        string substanceCode,
        Guid customerId,
        BusinessCategory customerCategory,
        ThresholdType type,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Threshold>> GetApplicableThresholdsAsync(
        IEnumerable<string> substanceCodes,
        Guid customerId,
        BusinessCategory customerCategory,
        CancellationToken cancellationToken = default);

    #endregion
}
