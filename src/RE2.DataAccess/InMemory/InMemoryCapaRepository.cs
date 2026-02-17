using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ICapaRepository for local development and testing.
/// T223: Stores CAPAs in ConcurrentDictionary.
/// </summary>
public class InMemoryCapaRepository : ICapaRepository
{
    private readonly ConcurrentDictionary<Guid, Capa> _capas = new();

    public Task<IEnumerable<Capa>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Capa>>(
            _capas.Values.Select(CloneCapa).ToList());
    }

    public Task<Capa?> GetByIdAsync(Guid capaId, CancellationToken cancellationToken = default)
    {
        _capas.TryGetValue(capaId, out var capa);
        return Task.FromResult(capa != null ? CloneCapa(capa) : null);
    }

    public Task<IEnumerable<Capa>> GetByStatusAsync(CapaStatus status, CancellationToken cancellationToken = default)
    {
        var results = _capas.Values
            .Where(c => c.Status == status)
            .Select(CloneCapa)
            .ToList();
        return Task.FromResult<IEnumerable<Capa>>(results);
    }

    public Task<IEnumerable<Capa>> GetByFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        var results = _capas.Values
            .Where(c => c.FindingId == findingId)
            .Select(CloneCapa)
            .ToList();
        return Task.FromResult<IEnumerable<Capa>>(results);
    }

    public Task<IEnumerable<Capa>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var results = _capas.Values
            .Where(c => c.IsOverdue())
            .Select(CloneCapa)
            .ToList();
        return Task.FromResult<IEnumerable<Capa>>(results);
    }

    public Task<IEnumerable<Capa>> GetByOwnerAsync(string ownerName, CancellationToken cancellationToken = default)
    {
        var results = _capas.Values
            .Where(c => c.OwnerName.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
            .Select(CloneCapa)
            .ToList();
        return Task.FromResult<IEnumerable<Capa>>(results);
    }

    public Task<Guid> CreateAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        if (capa.CapaId == Guid.Empty)
            capa.CapaId = Guid.NewGuid();

        capa.CreatedDate = DateTime.UtcNow;
        capa.ModifiedDate = DateTime.UtcNow;

        _capas[capa.CapaId] = CloneCapa(capa);
        return Task.FromResult(capa.CapaId);
    }

    public Task UpdateAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        capa.ModifiedDate = DateTime.UtcNow;
        _capas[capa.CapaId] = CloneCapa(capa);
        return Task.CompletedTask;
    }

    #region Seed Methods

    /// <summary>
    /// Seeds CAPA data for local development.
    /// </summary>
    internal void SeedCapas(IEnumerable<Capa> capas)
    {
        foreach (var capa in capas)
        {
            _capas.TryAdd(capa.CapaId, capa);
        }
    }

    #endregion

    #region Clone Helpers

    private static Capa CloneCapa(Capa source) => new()
    {
        CapaId = source.CapaId,
        CapaNumber = source.CapaNumber,
        FindingId = source.FindingId,
        Description = source.Description,
        OwnerName = source.OwnerName,
        DueDate = source.DueDate,
        CompletionDate = source.CompletionDate,
        Status = source.Status,
        VerificationNotes = source.VerificationNotes,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate,
        RowVersion = source.RowVersion.ToArray()
    };

    #endregion
}
