using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ISubstanceReclassificationRepository for local development and testing.
/// T080e: Supports FR-066 reclassification workflow testing.
/// </summary>
public class InMemorySubstanceReclassificationRepository : ISubstanceReclassificationRepository
{
    private readonly ConcurrentDictionary<Guid, SubstanceReclassification> _reclassifications = new();
    private readonly ConcurrentDictionary<Guid, ReclassificationCustomerImpact> _impacts = new();

    public Task<SubstanceReclassification?> GetByIdAsync(Guid reclassificationId, CancellationToken cancellationToken = default)
    {
        _reclassifications.TryGetValue(reclassificationId, out var reclassification);
        return Task.FromResult(reclassification);
    }

    public Task<IEnumerable<SubstanceReclassification>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        var results = _reclassifications.Values
            .Where(r => r.SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.EffectiveDate)
            .ToList();
        return Task.FromResult<IEnumerable<SubstanceReclassification>>(results);
    }

    public Task<IEnumerable<SubstanceReclassification>> GetByStatusAsync(ReclassificationStatus status, CancellationToken cancellationToken = default)
    {
        var results = _reclassifications.Values
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.EffectiveDate)
            .ToList();
        return Task.FromResult<IEnumerable<SubstanceReclassification>>(results);
    }

    public Task<IEnumerable<SubstanceReclassification>> GetByEffectiveDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var results = _reclassifications.Values
            .Where(r => r.EffectiveDate >= from && r.EffectiveDate <= to)
            .OrderBy(r => r.EffectiveDate)
            .ToList();
        return Task.FromResult<IEnumerable<SubstanceReclassification>>(results);
    }

    public Task<IEnumerable<SubstanceReclassification>> GetPendingEffectiveReclassificationsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = _reclassifications.Values
            .Where(r => r.Status == ReclassificationStatus.Pending && r.EffectiveDate <= today)
            .OrderBy(r => r.EffectiveDate)
            .ToList();
        return Task.FromResult<IEnumerable<SubstanceReclassification>>(results);
    }

    public Task<SubstanceReclassification?> GetEffectiveReclassificationAsync(string substanceCode, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var result = _reclassifications.Values
            .Where(r => r.SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase) &&
                        r.EffectiveDate <= asOfDate &&
                        r.Status == ReclassificationStatus.Completed)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<Guid> CreateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default)
    {
        if (reclassification.ReclassificationId == Guid.Empty)
        {
            reclassification.ReclassificationId = Guid.NewGuid();
        }
        reclassification.CreatedDate = DateTime.UtcNow;
        reclassification.ModifiedDate = DateTime.UtcNow;

        _reclassifications.TryAdd(reclassification.ReclassificationId, reclassification);
        return Task.FromResult(reclassification.ReclassificationId);
    }

    public Task UpdateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default)
    {
        reclassification.ModifiedDate = DateTime.UtcNow;
        _reclassifications[reclassification.ReclassificationId] = reclassification;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomerImpactsAsync(Guid reclassificationId, CancellationToken cancellationToken = default)
    {
        var results = _impacts.Values
            .Where(i => i.ReclassificationId == reclassificationId)
            .ToList();
        return Task.FromResult<IEnumerable<ReclassificationCustomerImpact>>(results);
    }

    public Task<ReclassificationCustomerImpact?> GetCustomerImpactAsync(Guid reclassificationId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = _impacts.Values
            .FirstOrDefault(i => i.ReclassificationId == reclassificationId && i.CustomerId == customerId);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomersRequiringReQualificationAsync(CancellationToken cancellationToken = default)
    {
        var results = _impacts.Values
            .Where(i => i.RequiresReQualification && i.ReQualificationDate == null)
            .ToList();
        return Task.FromResult<IEnumerable<ReclassificationCustomerImpact>>(results);
    }

    public Task<Guid> CreateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default)
    {
        if (impact.ImpactId == Guid.Empty)
        {
            impact.ImpactId = Guid.NewGuid();
        }
        impact.CreatedDate = DateTime.UtcNow;

        _impacts.TryAdd(impact.ImpactId, impact);
        return Task.FromResult(impact.ImpactId);
    }

    public Task UpdateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default)
    {
        _impacts[impact.ImpactId] = impact;
        return Task.CompletedTask;
    }

    public Task CreateCustomerImpactsBatchAsync(IEnumerable<ReclassificationCustomerImpact> impacts, CancellationToken cancellationToken = default)
    {
        foreach (var impact in impacts)
        {
            if (impact.ImpactId == Guid.Empty)
            {
                impact.ImpactId = Guid.NewGuid();
            }
            impact.CreatedDate = DateTime.UtcNow;
            _impacts.TryAdd(impact.ImpactId, impact);
        }
        return Task.CompletedTask;
    }
}
