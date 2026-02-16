using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IControlledSubstanceRepository for local development and testing.
/// Keyed by SubstanceCode (case-insensitive string).
/// </summary>
public class InMemoryControlledSubstanceRepository : IControlledSubstanceRepository
{
    private readonly ConcurrentDictionary<string, ControlledSubstance> _substances =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<ControlledSubstance?> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        _substances.TryGetValue(substanceCode, out var substance);
        return Task.FromResult(substance);
    }

    public Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var substances = _substances.Values
            .Where(s => s.IsActive)
            .ToList();
        return Task.FromResult<IEnumerable<ControlledSubstance>>(substances);
    }

    public Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ControlledSubstance>>(_substances.Values.ToList());
    }

    public Task<IEnumerable<ControlledSubstance>> GetAllD365SubstancesAsync(CancellationToken cancellationToken = default)
    {
        // In the in-memory implementation, all substances are treated as D365-discovered.
        return Task.FromResult<IEnumerable<ControlledSubstance>>(_substances.Values.ToList());
    }

    public Task SaveComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        if (substance.ComplianceExtensionId == Guid.Empty)
        {
            substance.ComplianceExtensionId = Guid.NewGuid();
        }

        substance.CreatedDate = DateTime.UtcNow;
        substance.ModifiedDate = DateTime.UtcNow;
        _substances[substance.SubstanceCode] = substance;
        return Task.CompletedTask;
    }

    public Task UpdateComplianceExtensionAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        substance.ModifiedDate = DateTime.UtcNow;
        _substances[substance.SubstanceCode] = substance;
        return Task.CompletedTask;
    }

    public Task DeleteComplianceExtensionAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        if (_substances.TryGetValue(substanceCode, out var substance))
        {
            // Clear compliance extension fields but keep the D365-sourced data
            substance.ComplianceExtensionId = Guid.Empty;
            substance.RegulatoryRestrictions = null;
            substance.ClassificationEffectiveDate = null;
            substance.ModifiedDate = DateTime.UtcNow;
            _substances[substanceCode] = substance;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        substance.ModifiedDate = DateTime.UtcNow;
        _substances[substance.SubstanceCode] = substance;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<ControlledSubstance> substances)
    {
        foreach (var substance in substances)
        {
            _substances.TryAdd(substance.SubstanceCode, substance);
        }
    }
}
