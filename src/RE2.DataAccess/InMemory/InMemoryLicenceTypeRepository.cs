using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ILicenceTypeRepository for local development and testing.
/// </summary>
public class InMemoryLicenceTypeRepository : ILicenceTypeRepository
{
    private readonly ConcurrentDictionary<Guid, LicenceType> _licenceTypes = new();

    public Task<LicenceType?> GetByIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default)
    {
        _licenceTypes.TryGetValue(licenceTypeId, out var licenceType);
        return Task.FromResult(licenceType);
    }

    public Task<LicenceType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var licenceType = _licenceTypes.Values.FirstOrDefault(lt =>
            lt.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(licenceType);
    }

    public Task<IEnumerable<LicenceType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var licenceTypes = _licenceTypes.Values
            .Where(lt => lt.IsActive)
            .ToList();
        return Task.FromResult<IEnumerable<LicenceType>>(licenceTypes);
    }

    public Task<IEnumerable<LicenceType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<LicenceType>>(_licenceTypes.Values.ToList());
    }

    public Task<Guid> CreateAsync(LicenceType licenceType, CancellationToken cancellationToken = default)
    {
        if (licenceType.LicenceTypeId == Guid.Empty)
        {
            licenceType.LicenceTypeId = Guid.NewGuid();
        }

        _licenceTypes.TryAdd(licenceType.LicenceTypeId, licenceType);
        return Task.FromResult(licenceType.LicenceTypeId);
    }

    public Task UpdateAsync(LicenceType licenceType, CancellationToken cancellationToken = default)
    {
        _licenceTypes[licenceType.LicenceTypeId] = licenceType;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid licenceTypeId, CancellationToken cancellationToken = default)
    {
        _licenceTypes.TryRemove(licenceTypeId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<LicenceType> licenceTypes)
    {
        foreach (var licenceType in licenceTypes)
        {
            _licenceTypes.TryAdd(licenceType.LicenceTypeId, licenceType);
        }
    }
}
