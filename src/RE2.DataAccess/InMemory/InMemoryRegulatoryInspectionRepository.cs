using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of the regulatory inspection repository.
/// T167: For development and testing; production will use Dataverse.
/// </summary>
public class InMemoryRegulatoryInspectionRepository : IRegulatoryInspectionRepository
{
    private readonly ConcurrentDictionary<Guid, RegulatoryInspection> _inspections = new();

    /// <inheritdoc />
    public Task<IEnumerable<RegulatoryInspection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var inspections = _inspections.Values
            .OrderByDescending(i => i.InspectionDate)
            .ToList();

        return Task.FromResult<IEnumerable<RegulatoryInspection>>(inspections);
    }

    /// <inheritdoc />
    public Task<RegulatoryInspection?> GetByIdAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        _inspections.TryGetValue(inspectionId, out var inspection);
        return Task.FromResult(inspection);
    }

    /// <inheritdoc />
    public Task<IEnumerable<RegulatoryInspection>> GetByDateRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var inspections = _inspections.Values
            .Where(i => i.InspectionDate >= fromDate && i.InspectionDate <= toDate)
            .OrderByDescending(i => i.InspectionDate)
            .ToList();

        return Task.FromResult<IEnumerable<RegulatoryInspection>>(inspections);
    }

    /// <inheritdoc />
    public Task<IEnumerable<RegulatoryInspection>> GetByAuthorityAsync(
        InspectingAuthority authority,
        CancellationToken cancellationToken = default)
    {
        var inspections = _inspections.Values
            .Where(i => i.Authority == authority)
            .OrderByDescending(i => i.InspectionDate)
            .ToList();

        return Task.FromResult<IEnumerable<RegulatoryInspection>>(inspections);
    }

    /// <inheritdoc />
    public Task<IEnumerable<RegulatoryInspection>> GetWithOverdueCorrectiveActionsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var inspections = _inspections.Values
            .Where(i => i.CorrectiveActionsDueDate.HasValue &&
                       !i.CorrectiveActionsCompletedDate.HasValue &&
                       i.CorrectiveActionsDueDate.Value < today)
            .OrderBy(i => i.CorrectiveActionsDueDate)
            .ToList();

        return Task.FromResult<IEnumerable<RegulatoryInspection>>(inspections);
    }

    /// <inheritdoc />
    public Task<RegulatoryInspection> CreateAsync(
        RegulatoryInspection inspection,
        CancellationToken cancellationToken = default)
    {
        if (inspection.InspectionId == Guid.Empty)
        {
            inspection.InspectionId = Guid.NewGuid();
        }

        inspection.CreatedDate = DateTime.UtcNow;
        inspection.ModifiedDate = DateTime.UtcNow;

        _inspections[inspection.InspectionId] = inspection;

        return Task.FromResult(inspection);
    }

    /// <inheritdoc />
    public Task<RegulatoryInspection> UpdateAsync(
        RegulatoryInspection inspection,
        CancellationToken cancellationToken = default)
    {
        if (!_inspections.ContainsKey(inspection.InspectionId))
        {
            throw new KeyNotFoundException($"Inspection with ID {inspection.InspectionId} not found");
        }

        inspection.ModifiedDate = DateTime.UtcNow;
        _inspections[inspection.InspectionId] = inspection;

        return Task.FromResult(inspection);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_inspections.TryRemove(inspectionId, out _));
    }

    /// <summary>
    /// Seeds sample inspection data for development/testing.
    /// </summary>
    public void SeedData()
    {
        var inspection1 = new RegulatoryInspection
        {
            InspectionId = Guid.NewGuid(),
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            Authority = InspectingAuthority.IGJ,
            InspectorName = "Dr. A. van der Berg",
            ReferenceNumber = "IGJ-2025-1234",
            Outcome = InspectionOutcome.MinorFindings,
            FindingsSummary = "Minor documentation gaps in licence record keeping",
            CorrectiveActions = "Update licence filing procedures and implement quarterly review",
            CorrectiveActionsDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            CorrectiveActionsCompletedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
            RecordedBy = Guid.NewGuid()
        };

        var inspection2 = new RegulatoryInspection
        {
            InspectionId = Guid.NewGuid(),
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            Authority = InspectingAuthority.Customs,
            InspectorName = "J. Bakker",
            ReferenceNumber = "DOUANE-2025-5678",
            Outcome = InspectionOutcome.NoFindings,
            FindingsSummary = "No deficiencies found during import documentation review",
            RecordedBy = Guid.NewGuid()
        };

        _inspections[inspection1.InspectionId] = inspection1;
        _inspections[inspection2.InspectionId] = inspection2;
    }
}
