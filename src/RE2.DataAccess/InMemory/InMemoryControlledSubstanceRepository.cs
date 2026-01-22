using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IControlledSubstanceRepository for local development and testing.
/// </summary>
public class InMemoryControlledSubstanceRepository : IControlledSubstanceRepository
{
    private readonly ConcurrentDictionary<Guid, ControlledSubstance> _substances = new();

    public Task<ControlledSubstance?> GetByIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        _substances.TryGetValue(substanceId, out var substance);
        return Task.FromResult(substance);
    }

    public Task<ControlledSubstance?> GetByInternalCodeAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        var substance = _substances.Values.FirstOrDefault(s =>
            s.InternalCode.Equals(internalCode, StringComparison.OrdinalIgnoreCase));
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

    public Task<Guid> CreateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        if (substance.SubstanceId == Guid.Empty)
        {
            substance.SubstanceId = Guid.NewGuid();
        }

        _substances.TryAdd(substance.SubstanceId, substance);
        return Task.FromResult(substance.SubstanceId);
    }

    public Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default)
    {
        _substances[substance.SubstanceId] = substance;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        _substances.TryRemove(substanceId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<ControlledSubstance> substances)
    {
        foreach (var substance in substances)
        {
            _substances.TryAdd(substance.SubstanceId, substance);
        }
    }
}
