using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ICapaRepository.
/// T223: CRUD for CAPAs via IDataverseClient.
/// </summary>
public class DataverseCapaRepository : ICapaRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseCapaRepository> _logger;

    private const string CapaEntityName = "phr_capa";

    public DataverseCapaRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseCapaRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Capa>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CapaEntityName)
            {
                ColumnSet = new ColumnSet(true)
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCapaDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CAPAs");
            return Enumerable.Empty<Capa>();
        }
    }

    public async Task<Capa?> GetByIdAsync(Guid capaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(CapaEntityName, capaId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToCapaDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CAPA {CapaId}", capaId);
            return null;
        }
    }

    public async Task<IEnumerable<Capa>> GetByStatusAsync(CapaStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CapaEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_status", ConditionOperator.Equal, (int)status)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCapaDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CAPAs with status {Status}", status);
            return Enumerable.Empty<Capa>();
        }
    }

    public async Task<IEnumerable<Capa>> GetByFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CapaEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_findingid", ConditionOperator.Equal, findingId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCapaDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CAPAs for finding {FindingId}", findingId);
            return Enumerable.Empty<Capa>();
        }
    }

    public async Task<IEnumerable<Capa>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get non-completed CAPAs with due date in the past
            var query = new QueryExpression(CapaEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_duedate", ConditionOperator.LessThan, DateTime.UtcNow),
                        new ConditionExpression("phr_completiondate", ConditionOperator.Null)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCapaDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue CAPAs");
            return Enumerable.Empty<Capa>();
        }
    }

    public async Task<IEnumerable<Capa>> GetByOwnerAsync(string ownerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CapaEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_ownername", ConditionOperator.Equal, ownerName)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCapaDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CAPAs for owner {OwnerName}", ownerName);
            return Enumerable.Empty<Capa>();
        }
    }

    public async Task<Guid> CreateAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        try
        {
            if (capa.CapaId == Guid.Empty)
                capa.CapaId = Guid.NewGuid();

            capa.CreatedDate = DateTime.UtcNow;
            capa.ModifiedDate = DateTime.UtcNow;

            var entity = MapCapaToEntity(capa);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created CAPA {CapaId} ({CapaNumber})", capa.CapaId, capa.CapaNumber);
            return capa.CapaId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CAPA {CapaNumber}", capa.CapaNumber);
            throw;
        }
    }

    public async Task UpdateAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        try
        {
            capa.ModifiedDate = DateTime.UtcNow;
            var entity = MapCapaToEntity(capa);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated CAPA {CapaId}", capa.CapaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CAPA {CapaId}", capa.CapaId);
            throw;
        }
    }

    #region Mapping Helpers

    private static CapaDto MapToCapaDto(Entity entity)
    {
        return new CapaDto
        {
            phr_capaid = entity.Id,
            phr_capanumber = entity.GetAttributeValue<string>("phr_capanumber"),
            phr_findingid = entity.GetAttributeValue<Guid>("phr_findingid"),
            phr_description = entity.GetAttributeValue<string>("phr_description"),
            phr_ownername = entity.GetAttributeValue<string>("phr_ownername"),
            phr_duedate = entity.Contains("phr_duedate") ? entity.GetAttributeValue<DateTime?>("phr_duedate") : null,
            phr_completiondate = entity.Contains("phr_completiondate") ? entity.GetAttributeValue<DateTime?>("phr_completiondate") : null,
            phr_status = entity.GetAttributeValue<int>("phr_status"),
            phr_verificationnotes = entity.GetAttributeValue<string>("phr_verificationnotes"),
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon"),
            phr_rowversion = entity.Contains("phr_rowversion") ? entity.GetAttributeValue<byte[]>("phr_rowversion") : null
        };
    }

    private static Entity MapCapaToEntity(Capa capa)
    {
        var entity = new Entity(CapaEntityName, capa.CapaId);
        entity["phr_capanumber"] = capa.CapaNumber;
        entity["phr_findingid"] = capa.FindingId;
        entity["phr_description"] = capa.Description;
        entity["phr_ownername"] = capa.OwnerName;
        entity["phr_duedate"] = capa.DueDate.ToDateTime(TimeOnly.MinValue);
        if (capa.CompletionDate.HasValue)
            entity["phr_completiondate"] = capa.CompletionDate.Value.ToDateTime(TimeOnly.MinValue);
        entity["phr_status"] = (int)capa.Status;
        entity["phr_verificationnotes"] = capa.VerificationNotes;
        return entity;
    }

    #endregion
}
