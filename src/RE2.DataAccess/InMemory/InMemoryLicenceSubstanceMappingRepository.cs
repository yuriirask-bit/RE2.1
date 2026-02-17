using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ILicenceSubstanceMappingRepository for local development and testing.
/// T079b: In-memory repository for licence-substance mappings.
/// </summary>
public class InMemoryLicenceSubstanceMappingRepository : ILicenceSubstanceMappingRepository
{
    private readonly ConcurrentDictionary<Guid, LicenceSubstanceMapping> _mappings = new();

    public Task<LicenceSubstanceMapping?> GetByIdAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        _mappings.TryGetValue(mappingId, out var mapping);
        return Task.FromResult(mapping);
    }

    public Task<IEnumerable<LicenceSubstanceMapping>> GetByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var mappings = _mappings.Values
            .Where(m => m.LicenceId == licenceId)
            .ToList();
        return Task.FromResult<IEnumerable<LicenceSubstanceMapping>>(mappings);
    }

    public Task<IEnumerable<LicenceSubstanceMapping>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        var mappings = _mappings.Values
            .Where(m => m.SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<LicenceSubstanceMapping>>(mappings);
    }

    public Task<IEnumerable<LicenceSubstanceMapping>> GetActiveMappingsByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mappings = _mappings.Values
            .Where(m => m.LicenceId == licenceId &&
                       m.EffectiveDate <= today &&
                       (!m.ExpiryDate.HasValue || m.ExpiryDate.Value >= today))
            .ToList();
        return Task.FromResult<IEnumerable<LicenceSubstanceMapping>>(mappings);
    }

    public Task<LicenceSubstanceMapping?> GetByLicenceSubstanceEffectiveDateAsync(
        Guid licenceId,
        string substanceCode,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
    {
        var mapping = _mappings.Values
            .FirstOrDefault(m => m.LicenceId == licenceId &&
                                m.SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase) &&
                                m.EffectiveDate == effectiveDate);
        return Task.FromResult(mapping);
    }

    public Task<IEnumerable<LicenceSubstanceMapping>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<LicenceSubstanceMapping>>(_mappings.Values.ToList());
    }

    public Task<Guid> CreateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        if (mapping.MappingId == Guid.Empty)
        {
            mapping.MappingId = Guid.NewGuid();
        }

        _mappings.TryAdd(mapping.MappingId, mapping);
        return Task.FromResult(mapping.MappingId);
    }

    public Task UpdateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        _mappings[mapping.MappingId] = mapping;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        _mappings.TryRemove(mappingId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial data for testing.
    /// </summary>
    internal void Seed(IEnumerable<LicenceSubstanceMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            _mappings.TryAdd(mapping.MappingId, mapping);
        }
    }
}
