using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpChangeRepository for local development and testing.
/// T283: Stores GDP change records in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpChangeRepository : IGdpChangeRepository
{
    private readonly ConcurrentDictionary<Guid, GdpChangeRecord> _records = new();

    public Task<IEnumerable<GdpChangeRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GdpChangeRecord>>(
            _records.Values.Select(Clone).ToList());
    }

    public Task<GdpChangeRecord?> GetByIdAsync(Guid changeRecordId, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(changeRecordId, out var record);
        return Task.FromResult(record != null ? Clone(record) : null);
    }

    public Task<IEnumerable<GdpChangeRecord>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var results = _records.Values
            .Where(r => r.IsPending())
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<GdpChangeRecord>>(results);
    }

    public Task<Guid> CreateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default)
    {
        if (record.ChangeRecordId == Guid.Empty)
            record.ChangeRecordId = Guid.NewGuid();

        record.CreatedDate = DateTime.UtcNow;
        record.ModifiedDate = DateTime.UtcNow;

        _records[record.ChangeRecordId] = Clone(record);
        return Task.FromResult(record.ChangeRecordId);
    }

    public Task UpdateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default)
    {
        record.ModifiedDate = DateTime.UtcNow;
        _records[record.ChangeRecordId] = Clone(record);
        return Task.CompletedTask;
    }

    public Task ApproveAsync(Guid changeRecordId, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        if (_records.TryGetValue(changeRecordId, out var record))
        {
            record.ApprovalStatus = ChangeApprovalStatus.Approved;
            record.ApprovedBy = approvedBy;
            record.ApprovalDate = DateOnly.FromDateTime(DateTime.UtcNow);
            record.ModifiedDate = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task RejectAsync(Guid changeRecordId, Guid rejectedBy, CancellationToken cancellationToken = default)
    {
        if (_records.TryGetValue(changeRecordId, out var record))
        {
            record.ApprovalStatus = ChangeApprovalStatus.Rejected;
            record.ApprovedBy = rejectedBy;
            record.ApprovalDate = DateOnly.FromDateTime(DateTime.UtcNow);
            record.ModifiedDate = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    #region Seed Methods

    internal void SeedChangeRecords(IEnumerable<GdpChangeRecord> records)
    {
        foreach (var record in records)
        {
            _records.TryAdd(record.ChangeRecordId, record);
        }
    }

    #endregion

    #region Clone Helpers

    private static GdpChangeRecord Clone(GdpChangeRecord source) => new()
    {
        ChangeRecordId = source.ChangeRecordId,
        ChangeNumber = source.ChangeNumber,
        ChangeType = source.ChangeType,
        Description = source.Description,
        RiskAssessment = source.RiskAssessment,
        ApprovalStatus = source.ApprovalStatus,
        ApprovedBy = source.ApprovedBy,
        ApprovalDate = source.ApprovalDate,
        ImplementationDate = source.ImplementationDate,
        UpdatedDocumentationRefs = source.UpdatedDocumentationRefs,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate,
        RowVersion = (byte[])source.RowVersion.Clone()
    };

    #endregion
}
