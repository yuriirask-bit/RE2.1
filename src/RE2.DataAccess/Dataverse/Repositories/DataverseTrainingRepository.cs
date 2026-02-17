using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ITrainingRepository.
/// T281: CRUD for training records via IDataverseClient.
/// </summary>
public class DataverseTrainingRepository : ITrainingRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseTrainingRepository> _logger;

    private const string EntityName = "phr_trainingrecord";

    public DataverseTrainingRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseTrainingRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<TrainingRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training records");
            return Enumerable.Empty<TrainingRecord>();
        }
    }

    public async Task<TrainingRecord?> GetByIdAsync(Guid trainingRecordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(EntityName, trainingRecordId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training record {Id}", trainingRecordId);
            return null;
        }
    }

    public async Task<IEnumerable<TrainingRecord>> GetByStaffAsync(Guid staffMemberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_staffmemberid", ConditionOperator.Equal, staffMemberId) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training records for staff {StaffId}", staffMemberId);
            return Enumerable.Empty<TrainingRecord>();
        }
    }

    public async Task<IEnumerable<TrainingRecord>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_siteid", ConditionOperator.Equal, siteId) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training records for site {SiteId}", siteId);
            return Enumerable.Empty<TrainingRecord>();
        }
    }

    public async Task<IEnumerable<TrainingRecord>> GetBySopAsync(Guid sopId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_sopid", ConditionOperator.Equal, sopId) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training records for SOP {SopId}", sopId);
            return Enumerable.Empty<TrainingRecord>();
        }
    }

    public async Task<IEnumerable<TrainingRecord>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_expirydate", ConditionOperator.LessThan, DateTime.UtcNow) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired training records");
            return Enumerable.Empty<TrainingRecord>();
        }
    }

    public async Task<Guid> CreateAsync(TrainingRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            if (record.TrainingRecordId == Guid.Empty)
                record.TrainingRecordId = Guid.NewGuid();

            var entity = MapToEntity(record);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created training record {Id} for staff {StaffId}", record.TrainingRecordId, record.StaffMemberId);
            return record.TrainingRecordId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training record for staff {StaffId}", record.StaffMemberId);
            throw;
        }
    }

    public async Task UpdateAsync(TrainingRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = MapToEntity(record);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated training record {Id}", record.TrainingRecordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training record {Id}", record.TrainingRecordId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid trainingRecordId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(EntityName, trainingRecordId, cancellationToken);
            _logger.LogInformation("Deleted training record {Id}", trainingRecordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training record {Id}", trainingRecordId);
            throw;
        }
    }

    #region Mapping Helpers

    private static TrainingRecordDto MapToDto(Entity entity)
    {
        return new TrainingRecordDto
        {
            phr_trainingrecordid = entity.Id,
            phr_staffmemberid = entity.GetAttributeValue<Guid>("phr_staffmemberid"),
            phr_staffmembername = entity.GetAttributeValue<string>("phr_staffmembername"),
            phr_trainingcurriculum = entity.GetAttributeValue<string>("phr_trainingcurriculum"),
            phr_sopid = entity.Contains("phr_sopid") ? entity.GetAttributeValue<Guid?>("phr_sopid") : null,
            phr_siteid = entity.Contains("phr_siteid") ? entity.GetAttributeValue<Guid?>("phr_siteid") : null,
            phr_completiondate = entity.Contains("phr_completiondate") ? entity.GetAttributeValue<DateTime?>("phr_completiondate") : null,
            phr_expirydate = entity.Contains("phr_expirydate") ? entity.GetAttributeValue<DateTime?>("phr_expirydate") : null,
            phr_trainername = entity.GetAttributeValue<string>("phr_trainername"),
            phr_assessmentresult = entity.GetAttributeValue<int>("phr_assessmentresult")
        };
    }

    private static Entity MapToEntity(TrainingRecord record)
    {
        var entity = new Entity(EntityName, record.TrainingRecordId);
        entity["phr_staffmemberid"] = record.StaffMemberId;
        entity["phr_staffmembername"] = record.StaffMemberName;
        entity["phr_trainingcurriculum"] = record.TrainingCurriculum;
        if (record.SopId.HasValue)
            entity["phr_sopid"] = record.SopId.Value;
        if (record.SiteId.HasValue)
            entity["phr_siteid"] = record.SiteId.Value;
        entity["phr_completiondate"] = record.CompletionDate.ToDateTime(TimeOnly.MinValue);
        if (record.ExpiryDate.HasValue)
            entity["phr_expirydate"] = record.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue);
        entity["phr_trainername"] = record.TrainerName;
        entity["phr_assessmentresult"] = (int)record.AssessmentResult;
        return entity;
    }

    #endregion
}
