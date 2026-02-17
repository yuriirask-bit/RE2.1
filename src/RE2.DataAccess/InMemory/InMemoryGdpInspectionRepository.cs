using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpInspectionRepository for local development and testing.
/// T221: Stores GDP inspections and findings in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpInspectionRepository : IGdpInspectionRepository
{
    private readonly ConcurrentDictionary<Guid, GdpInspection> _inspections = new();
    private readonly ConcurrentDictionary<Guid, GdpInspectionFinding> _findings = new();

    #region GdpInspection Operations

    public Task<IEnumerable<GdpInspection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GdpInspection>>(
            _inspections.Values.Select(CloneInspection).ToList());
    }

    public Task<GdpInspection?> GetByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        _inspections.TryGetValue(inspectionId, out var inspection);
        return Task.FromResult(inspection != null ? CloneInspection(inspection) : null);
    }

    public Task<IEnumerable<GdpInspection>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var results = _inspections.Values
            .Where(i => i.SiteId == siteId)
            .Select(CloneInspection)
            .ToList();
        return Task.FromResult<IEnumerable<GdpInspection>>(results);
    }

    public Task<IEnumerable<GdpInspection>> GetByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var results = _inspections.Values
            .Where(i => i.InspectionDate >= fromDate && i.InspectionDate <= toDate)
            .Select(CloneInspection)
            .ToList();
        return Task.FromResult<IEnumerable<GdpInspection>>(results);
    }

    public Task<IEnumerable<GdpInspection>> GetByTypeAsync(GdpInspectionType inspectionType, CancellationToken cancellationToken = default)
    {
        var results = _inspections.Values
            .Where(i => i.InspectionType == inspectionType)
            .Select(CloneInspection)
            .ToList();
        return Task.FromResult<IEnumerable<GdpInspection>>(results);
    }

    public Task<Guid> CreateAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        if (inspection.InspectionId == Guid.Empty)
        {
            inspection.InspectionId = Guid.NewGuid();
        }

        inspection.CreatedDate = DateTime.UtcNow;
        inspection.ModifiedDate = DateTime.UtcNow;

        _inspections[inspection.InspectionId] = CloneInspection(inspection);
        return Task.FromResult(inspection.InspectionId);
    }

    public Task UpdateAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        inspection.ModifiedDate = DateTime.UtcNow;
        _inspections[inspection.InspectionId] = CloneInspection(inspection);
        return Task.CompletedTask;
    }

    #endregion

    #region GdpInspectionFinding Operations

    public Task<IEnumerable<GdpInspectionFinding>> GetFindingsAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var results = _findings.Values
            .Where(f => f.InspectionId == inspectionId)
            .Select(CloneFinding)
            .ToList();
        return Task.FromResult<IEnumerable<GdpInspectionFinding>>(results);
    }

    public Task<GdpInspectionFinding?> GetFindingByIdAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        _findings.TryGetValue(findingId, out var finding);
        return Task.FromResult(finding != null ? CloneFinding(finding) : null);
    }

    public Task<Guid> CreateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        if (finding.FindingId == Guid.Empty)
        {
            finding.FindingId = Guid.NewGuid();
        }

        _findings[finding.FindingId] = CloneFinding(finding);
        return Task.FromResult(finding.FindingId);
    }

    public Task UpdateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        _findings[finding.FindingId] = CloneFinding(finding);
        return Task.CompletedTask;
    }

    public Task DeleteFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        _findings.TryRemove(findingId, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region Seed Methods

    /// <summary>
    /// Seeds GDP inspection data for local development.
    /// </summary>
    internal void SeedInspections(IEnumerable<GdpInspection> inspections)
    {
        foreach (var inspection in inspections)
        {
            _inspections.TryAdd(inspection.InspectionId, inspection);
        }
    }

    /// <summary>
    /// Seeds GDP inspection finding data for local development.
    /// </summary>
    internal void SeedFindings(IEnumerable<GdpInspectionFinding> findings)
    {
        foreach (var finding in findings)
        {
            _findings.TryAdd(finding.FindingId, finding);
        }
    }

    #endregion

    #region Clone Helpers

    private static GdpInspection CloneInspection(GdpInspection source) => new()
    {
        InspectionId = source.InspectionId,
        InspectionDate = source.InspectionDate,
        InspectorName = source.InspectorName,
        InspectionType = source.InspectionType,
        SiteId = source.SiteId,
        WdaLicenceId = source.WdaLicenceId,
        FindingsSummary = source.FindingsSummary,
        ReportReferenceUrl = source.ReportReferenceUrl,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate
    };

    private static GdpInspectionFinding CloneFinding(GdpInspectionFinding source) => new()
    {
        FindingId = source.FindingId,
        InspectionId = source.InspectionId,
        FindingDescription = source.FindingDescription,
        Classification = source.Classification,
        FindingNumber = source.FindingNumber
    };

    #endregion
}
