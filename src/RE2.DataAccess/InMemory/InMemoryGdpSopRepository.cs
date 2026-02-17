using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpSopRepository for local development and testing.
/// T277: Stores GDP SOPs and site-SOP links in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpSopRepository : IGdpSopRepository
{
    private readonly ConcurrentDictionary<Guid, GdpSop> _sops = new();
    private readonly ConcurrentDictionary<Guid, GdpSiteSop> _siteSops = new();

    #region GdpSop Operations

    public Task<IEnumerable<GdpSop>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GdpSop>>(
            _sops.Values.Select(CloneSop).ToList());
    }

    public Task<GdpSop?> GetByIdAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        _sops.TryGetValue(sopId, out var sop);
        return Task.FromResult(sop != null ? CloneSop(sop) : null);
    }

    public Task<IEnumerable<GdpSop>> GetByCategoryAsync(GdpSopCategory category, CancellationToken cancellationToken = default)
    {
        var results = _sops.Values
            .Where(s => s.Category == category)
            .Select(CloneSop)
            .ToList();
        return Task.FromResult<IEnumerable<GdpSop>>(results);
    }

    public Task<Guid> CreateAsync(GdpSop sop, CancellationToken cancellationToken = default)
    {
        if (sop.SopId == Guid.Empty)
            sop.SopId = Guid.NewGuid();

        _sops[sop.SopId] = CloneSop(sop);
        return Task.FromResult(sop.SopId);
    }

    public Task UpdateAsync(GdpSop sop, CancellationToken cancellationToken = default)
    {
        _sops[sop.SopId] = CloneSop(sop);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        _sops.TryRemove(sopId, out _);
        // Also remove site links
        var linksToRemove = _siteSops.Values.Where(l => l.SopId == sopId).Select(l => l.SiteSopId).ToList();
        foreach (var linkId in linksToRemove)
        {
            _siteSops.TryRemove(linkId, out _);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region GdpSiteSop Operations

    public Task<IEnumerable<GdpSop>> GetSiteSopsAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var sopIds = _siteSops.Values
            .Where(l => l.SiteId == siteId)
            .Select(l => l.SopId)
            .ToHashSet();

        var results = _sops.Values
            .Where(s => sopIds.Contains(s.SopId))
            .Select(CloneSop)
            .ToList();
        return Task.FromResult<IEnumerable<GdpSop>>(results);
    }

    public Task<Guid> LinkSopToSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default)
    {
        // Check if link already exists
        var existing = _siteSops.Values.FirstOrDefault(l => l.SiteId == siteId && l.SopId == sopId);
        if (existing != null)
            return Task.FromResult(existing.SiteSopId);

        var link = new GdpSiteSop
        {
            SiteSopId = Guid.NewGuid(),
            SiteId = siteId,
            SopId = sopId
        };
        _siteSops[link.SiteSopId] = link;
        return Task.FromResult(link.SiteSopId);
    }

    public Task UnlinkSopFromSiteAsync(Guid siteId, Guid sopId, CancellationToken cancellationToken = default)
    {
        var link = _siteSops.Values.FirstOrDefault(l => l.SiteId == siteId && l.SopId == sopId);
        if (link != null)
        {
            _siteSops.TryRemove(link.SiteSopId, out _);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Seed Methods

    internal void SeedSops(IEnumerable<GdpSop> sops)
    {
        foreach (var sop in sops)
        {
            _sops.TryAdd(sop.SopId, sop);
        }
    }

    internal void SeedSiteSops(IEnumerable<GdpSiteSop> siteSops)
    {
        foreach (var link in siteSops)
        {
            _siteSops.TryAdd(link.SiteSopId, link);
        }
    }

    #endregion

    #region Clone Helpers

    private static GdpSop CloneSop(GdpSop source) => new()
    {
        SopId = source.SopId,
        SopNumber = source.SopNumber,
        Title = source.Title,
        Category = source.Category,
        Version = source.Version,
        EffectiveDate = source.EffectiveDate,
        DocumentUrl = source.DocumentUrl,
        IsActive = source.IsActive
    };

    #endregion
}
