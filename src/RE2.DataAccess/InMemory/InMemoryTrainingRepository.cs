using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ITrainingRepository for local development and testing.
/// T280: Stores training records in ConcurrentDictionary.
/// </summary>
public class InMemoryTrainingRepository : ITrainingRepository
{
    private readonly ConcurrentDictionary<Guid, TrainingRecord> _records = new();

    public Task<IEnumerable<TrainingRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TrainingRecord>>(
            _records.Values.Select(Clone).ToList());
    }

    public Task<TrainingRecord?> GetByIdAsync(Guid trainingRecordId, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(trainingRecordId, out var record);
        return Task.FromResult(record != null ? Clone(record) : null);
    }

    public Task<IEnumerable<TrainingRecord>> GetByStaffAsync(Guid staffMemberId, CancellationToken cancellationToken = default)
    {
        var results = _records.Values
            .Where(r => r.StaffMemberId == staffMemberId)
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<TrainingRecord>>(results);
    }

    public Task<IEnumerable<TrainingRecord>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var results = _records.Values
            .Where(r => r.SiteId == siteId)
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<TrainingRecord>>(results);
    }

    public Task<IEnumerable<TrainingRecord>> GetBySopAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        var results = _records.Values
            .Where(r => r.SopId == sopId)
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<TrainingRecord>>(results);
    }

    public Task<IEnumerable<TrainingRecord>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var results = _records.Values
            .Where(r => r.IsExpired())
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<TrainingRecord>>(results);
    }

    public Task<Guid> CreateAsync(TrainingRecord record, CancellationToken cancellationToken = default)
    {
        if (record.TrainingRecordId == Guid.Empty)
        {
            record.TrainingRecordId = Guid.NewGuid();
        }

        _records[record.TrainingRecordId] = Clone(record);
        return Task.FromResult(record.TrainingRecordId);
    }

    public Task UpdateAsync(TrainingRecord record, CancellationToken cancellationToken = default)
    {
        _records[record.TrainingRecordId] = Clone(record);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid trainingRecordId, CancellationToken cancellationToken = default)
    {
        _records.TryRemove(trainingRecordId, out _);
        return Task.CompletedTask;
    }

    #region Seed Methods

    internal void SeedTrainingRecords(IEnumerable<TrainingRecord> records)
    {
        foreach (var record in records)
        {
            _records.TryAdd(record.TrainingRecordId, record);
        }
    }

    #endregion

    #region Clone Helpers

    private static TrainingRecord Clone(TrainingRecord source) => new()
    {
        TrainingRecordId = source.TrainingRecordId,
        StaffMemberId = source.StaffMemberId,
        StaffMemberName = source.StaffMemberName,
        TrainingCurriculum = source.TrainingCurriculum,
        SopId = source.SopId,
        SiteId = source.SiteId,
        CompletionDate = source.CompletionDate,
        ExpiryDate = source.ExpiryDate,
        TrainerName = source.TrainerName,
        AssessmentResult = source.AssessmentResult
    };

    #endregion
}
