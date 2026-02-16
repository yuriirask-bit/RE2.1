using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IThresholdRepository for local development and testing.
/// T132: In-memory threshold repository implementation.
/// </summary>
public class InMemoryThresholdRepository : IThresholdRepository
{
    private readonly ConcurrentDictionary<Guid, Threshold> _thresholds = new();

    #region Core Operations

    public Task<Threshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _thresholds.TryGetValue(id, out var threshold);
        return Task.FromResult(threshold);
    }

    public Task<IEnumerable<Threshold>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Threshold>>(_thresholds.Values.ToList());
    }

    public Task<IEnumerable<Threshold>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var thresholds = _thresholds.Values
            .Where(t => t.IsEffective())
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    public Task<Guid> CreateAsync(Threshold threshold, CancellationToken cancellationToken = default)
    {
        if (threshold.Id == Guid.Empty)
        {
            threshold.Id = Guid.NewGuid();
        }
        threshold.CreatedDate = DateTime.UtcNow;
        threshold.ModifiedDate = DateTime.UtcNow;

        _thresholds.TryAdd(threshold.Id, threshold);
        return Task.FromResult(threshold.Id);
    }

    public Task UpdateAsync(Threshold threshold, CancellationToken cancellationToken = default)
    {
        threshold.ModifiedDate = DateTime.UtcNow;
        _thresholds[threshold.Id] = threshold;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _thresholds.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region Filtered Queries

    public Task<IEnumerable<Threshold>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        var thresholds = _thresholds.Values
            .Where(t => t.AppliesToSubstance(substanceCode) && t.IsEffective())
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    public Task<IEnumerable<Threshold>> GetByLicenceTypeIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default)
    {
        var thresholds = _thresholds.Values
            .Where(t => t.LicenceTypeId == licenceTypeId && t.IsEffective())
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    public Task<IEnumerable<Threshold>> GetByCustomerCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var thresholds = _thresholds.Values
            .Where(t => t.CustomerCategory == category && t.IsEffective())
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    public Task<IEnumerable<Threshold>> GetByTypeAsync(ThresholdType type, CancellationToken cancellationToken = default)
    {
        var thresholds = _thresholds.Values
            .Where(t => t.ThresholdType == type && t.IsEffective())
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    #endregion

    #region Threshold Lookup

    public Task<Threshold?> GetApplicableThresholdAsync(
        string substanceCode,
        ThresholdType type,
        CancellationToken cancellationToken = default)
    {
        // Find the most specific applicable threshold
        // Priority: substance-specific > global
        var threshold = _thresholds.Values
            .Where(t => t.IsEffective() &&
                        t.ThresholdType == type &&
                        t.AppliesToSubstance(substanceCode))
            .OrderByDescending(t => !string.IsNullOrEmpty(t.SubstanceCode)) // Substance-specific first
            .FirstOrDefault();

        return Task.FromResult(threshold);
    }

    public Task<Threshold?> GetApplicableThresholdAsync(
        string substanceCode,
        Guid customerId,
        BusinessCategory customerCategory,
        ThresholdType type,
        CancellationToken cancellationToken = default)
    {
        // Find the most specific applicable threshold
        // Priority: customer-specific > category-specific > substance-specific > global
        var threshold = _thresholds.Values
            .Where(t => t.IsEffective() &&
                        t.ThresholdType == type &&
                        t.AppliesToSubstance(substanceCode) &&
                        t.AppliesToCustomer(customerId, customerCategory))
            .OrderByDescending(t => t.CustomerId.HasValue) // Customer-specific first
            .ThenByDescending(t => t.CustomerCategory.HasValue) // Category-specific next
            .ThenByDescending(t => !string.IsNullOrEmpty(t.SubstanceCode)) // Substance-specific next
            .FirstOrDefault();

        return Task.FromResult(threshold);
    }

    public Task<IEnumerable<Threshold>> GetApplicableThresholdsAsync(
        IEnumerable<string> substanceCodes,
        Guid customerId,
        BusinessCategory customerCategory,
        CancellationToken cancellationToken = default)
    {
        var substanceCodeSet = substanceCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var thresholds = _thresholds.Values
            .Where(t => t.IsEffective() &&
                        (string.IsNullOrEmpty(t.SubstanceCode) || substanceCodeSet.Contains(t.SubstanceCode)) &&
                        t.AppliesToCustomer(customerId, customerCategory))
            .ToList();

        return Task.FromResult<IEnumerable<Threshold>>(thresholds);
    }

    #endregion

    #region Seeding

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Threshold> thresholds)
    {
        foreach (var threshold in thresholds)
        {
            _thresholds.TryAdd(threshold.Id, threshold);
        }
    }

    #endregion
}
