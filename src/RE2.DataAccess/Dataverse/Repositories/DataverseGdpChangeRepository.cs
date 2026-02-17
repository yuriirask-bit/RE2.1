using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpChangeRepository.
/// T284: CRUD for GDP change records via IDataverseClient.
/// </summary>
public class DataverseGdpChangeRepository : IGdpChangeRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpChangeRepository> _logger;

    private const string EntityName = "phr_gdpchangerecord";

    public DataverseGdpChangeRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpChangeRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<GdpChangeRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP change records");
            return Enumerable.Empty<GdpChangeRecord>();
        }
    }

    public async Task<GdpChangeRecord?> GetByIdAsync(Guid changeRecordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(EntityName, changeRecordId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP change record {Id}", changeRecordId);
            return null;
        }
    }

    public async Task<IEnumerable<GdpChangeRecord>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("phr_approvalstatus", ConditionOperator.Equal, (int)ChangeApprovalStatus.Pending) }
                }
            };
            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending GDP change records");
            return Enumerable.Empty<GdpChangeRecord>();
        }
    }

    public async Task<Guid> CreateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            if (record.ChangeRecordId == Guid.Empty)
                record.ChangeRecordId = Guid.NewGuid();

            record.CreatedDate = DateTime.UtcNow;
            record.ModifiedDate = DateTime.UtcNow;

            var entity = MapToEntity(record);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP change record {Id} ({Number})", record.ChangeRecordId, record.ChangeNumber);
            return record.ChangeRecordId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP change record {Number}", record.ChangeNumber);
            throw;
        }
    }

    public async Task UpdateAsync(GdpChangeRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            record.ModifiedDate = DateTime.UtcNow;
            var entity = MapToEntity(record);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP change record {Id}", record.ChangeRecordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP change record {Id}", record.ChangeRecordId);
            throw;
        }
    }

    public async Task ApproveAsync(Guid changeRecordId, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new Entity(EntityName, changeRecordId);
            entity["phr_approvalstatus"] = (int)ChangeApprovalStatus.Approved;
            entity["phr_approvedby"] = approvedBy;
            entity["phr_approvaldate"] = DateTime.UtcNow;
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Approved GDP change record {Id} by {ApprovedBy}", changeRecordId, approvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving GDP change record {Id}", changeRecordId);
            throw;
        }
    }

    public async Task RejectAsync(Guid changeRecordId, Guid rejectedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new Entity(EntityName, changeRecordId);
            entity["phr_approvalstatus"] = (int)ChangeApprovalStatus.Rejected;
            entity["phr_approvedby"] = rejectedBy;
            entity["phr_approvaldate"] = DateTime.UtcNow;
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Rejected GDP change record {Id} by {RejectedBy}", changeRecordId, rejectedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting GDP change record {Id}", changeRecordId);
            throw;
        }
    }

    #region Mapping Helpers

    private static GdpChangeRecordDto MapToDto(Entity entity)
    {
        return new GdpChangeRecordDto
        {
            phr_gdpchangerecordid = entity.Id,
            phr_changenumber = entity.GetAttributeValue<string>("phr_changenumber"),
            phr_changetype = entity.GetAttributeValue<int>("phr_changetype"),
            phr_description = entity.GetAttributeValue<string>("phr_description"),
            phr_riskassessment = entity.GetAttributeValue<string>("phr_riskassessment"),
            phr_approvalstatus = entity.GetAttributeValue<int>("phr_approvalstatus"),
            phr_approvedby = entity.Contains("phr_approvedby") ? entity.GetAttributeValue<Guid?>("phr_approvedby") : null,
            phr_approvaldate = entity.Contains("phr_approvaldate") ? entity.GetAttributeValue<DateTime?>("phr_approvaldate") : null,
            phr_implementationdate = entity.Contains("phr_implementationdate") ? entity.GetAttributeValue<DateTime?>("phr_implementationdate") : null,
            phr_updateddocumentationrefs = entity.GetAttributeValue<string>("phr_updateddocumentationrefs"),
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon")
        };
    }

    private static Entity MapToEntity(GdpChangeRecord record)
    {
        var entity = new Entity(EntityName, record.ChangeRecordId);
        entity["phr_changenumber"] = record.ChangeNumber;
        entity["phr_changetype"] = (int)record.ChangeType;
        entity["phr_description"] = record.Description;
        entity["phr_riskassessment"] = record.RiskAssessment;
        entity["phr_approvalstatus"] = (int)record.ApprovalStatus;
        if (record.ApprovedBy.HasValue)
            entity["phr_approvedby"] = record.ApprovedBy.Value;
        if (record.ApprovalDate.HasValue)
            entity["phr_approvaldate"] = record.ApprovalDate.Value.ToDateTime(TimeOnly.MinValue);
        if (record.ImplementationDate.HasValue)
            entity["phr_implementationdate"] = record.ImplementationDate.Value.ToDateTime(TimeOnly.MinValue);
        entity["phr_updateddocumentationrefs"] = record.UpdatedDocumentationRefs;
        return entity;
    }

    #endregion
}
