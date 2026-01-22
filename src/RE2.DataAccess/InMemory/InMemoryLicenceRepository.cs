using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ILicenceRepository for local development and testing.
/// </summary>
public class InMemoryLicenceRepository : ILicenceRepository
{
    private readonly ConcurrentDictionary<Guid, Licence> _licences = new();

    public Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        _licences.TryGetValue(licenceId, out var licence);
        return Task.FromResult(licence);
    }

    public Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = _licences.Values.FirstOrDefault(l =>
            l.LicenceNumber.Equals(licenceNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(licence);
    }

    public Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        var licences = _licences.Values
            .Where(l => l.HolderId == holderId &&
                        l.HolderType.Equals(holderType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    public Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead));
        var licences = _licences.Values
            .Where(l => l.ExpiryDate.HasValue &&
                        l.ExpiryDate.Value <= cutoffDate &&
                        l.ExpiryDate.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(l => l.ExpiryDate)
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    public Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Licence>>(_licences.Values.ToList());
    }

    public Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        if (licence.LicenceId == Guid.Empty)
        {
            licence.LicenceId = Guid.NewGuid();
        }
        licence.CreatedDate = DateTime.UtcNow;
        licence.ModifiedDate = DateTime.UtcNow;

        _licences.TryAdd(licence.LicenceId, licence);
        return Task.FromResult(licence.LicenceId);
    }

    public Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        licence.ModifiedDate = DateTime.UtcNow;
        _licences[licence.LicenceId] = licence;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        _licences.TryRemove(licenceId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Licence>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        // In-memory implementation: filter licences that have substance mappings for this substance
        var licences = _licences.Values
            .Where(l => l.Status == "Valid" &&
                        l.SubstanceMappings != null &&
                        l.SubstanceMappings.Any(m => m.SubstanceId == substanceId))
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Licence> licences)
    {
        foreach (var licence in licences)
        {
            _licences.TryAdd(licence.LicenceId, licence);
        }
    }
}
