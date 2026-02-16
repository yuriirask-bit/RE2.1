using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ILicenceRepository.
/// T069: Repository implementation for Licence CRUD operations.
/// T108c, T111: Extended with document, verification, and scope change operations for US3.
/// </summary>
public class DataverseLicenceRepository : ILicenceRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseLicenceRepository> _logger;
    private const string EntityName = "phr_licence";
    private const string DocumentEntityName = "phr_licencedocument";
    private const string VerificationEntityName = "phr_licenceverification";
    private const string ScopeChangeEntityName = "phr_licencescopechange";

    public DataverseLicenceRepository(IDataverseClient client, ILogger<DataverseLicenceRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, licenceId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licence {Id}", licenceId);
            return null;
        }
    }

    public async Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_licencenumber", ConditionOperator.Equal, licenceNumber)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licence by number {LicenceNumber}", licenceNumber);
            return null;
        }
    }

    public async Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_holderid", ConditionOperator.Equal, holderId),
                        new ConditionExpression("phr_holdertype", ConditionOperator.Equal, holderType)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licences for holder {HolderId} of type {HolderType}", holderId, holderType);
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        try
        {
            var futureDate = DateTime.UtcNow.AddDays(daysAhead);
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_expirydate", ConditionOperator.NotNull),
                        new ConditionExpression("phr_expirydate", ConditionOperator.LessEqual, futureDate),
                        new ConditionExpression("phr_expirydate", ConditionOperator.GreaterEqual, DateTime.UtcNow),
                        new ConditionExpression("phr_status", ConditionOperator.Equal, "Valid")
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring licences within {DaysAhead} days", daysAhead);
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all licences");
            return Enumerable.Empty<Licence>();
        }
    }

    public async Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var dto = LicenceDto.FromDomainModel(licence);
        var entity = MapToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created licence {Id} with number {LicenceNumber}", id, licence.LicenceNumber);
        return id;
    }

    public async Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var dto = LicenceDto.FromDomainModel(licence);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated licence {Id}", licence.LicenceId);
    }

    public async Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, licenceId, cancellationToken);
        _logger.LogInformation("Deleted licence {Id}", licenceId);
    }

    public async Task<IEnumerable<Licence>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query licences through LicenceSubstanceMapping join
            var query = new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(true),
                Distinct = true,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_status", ConditionOperator.Equal, "Valid")
                    }
                }
            };

            // Add link entity to join with phr_licencesubstancemapping
            var linkEntity = new LinkEntity(
                EntityName,
                "phr_licencesubstancemapping",
                "phr_licenceid",
                "phr_licenceid",
                JoinOperator.Inner);

            linkEntity.LinkCriteria.AddCondition(
                "phr_substancecode",
                ConditionOperator.Equal,
                substanceCode);

            query.LinkEntities.Add(linkEntity);

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licences for substance {SubstanceCode}", substanceCode);
            return Enumerable.Empty<Licence>();
        }
    }

    private LicenceDto MapToDto(Entity entity)
    {
        return new LicenceDto
        {
            phr_licenceid = entity.Id,
            phr_licencenumber = entity.GetAttributeValue<string>("phr_licencenumber"),
            phr_licencetypeid = entity.GetAttributeValue<Guid>("phr_licencetypeid"),
            phr_holdertype = entity.GetAttributeValue<string>("phr_holdertype"),
            phr_holderid = entity.GetAttributeValue<Guid>("phr_holderid"),
            phr_issuingauthority = entity.GetAttributeValue<string>("phr_issuingauthority"),
            phr_issuedate = entity.GetAttributeValue<DateTime>("phr_issuedate"),
            phr_expirydate = entity.GetAttributeValue<DateTime?>("phr_expirydate"),
            phr_status = entity.GetAttributeValue<string>("phr_status"),
            phr_scope = entity.GetAttributeValue<string>("phr_scope"),
            phr_permittedactivities = entity.GetAttributeValue<int>("phr_permittedactivities"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate")
        };
    }

    private Entity MapToEntity(LicenceDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_licenceid };
        entity["phr_licencenumber"] = dto.phr_licencenumber;
        entity["phr_licencetypeid"] = dto.phr_licencetypeid;
        entity["phr_holdertype"] = dto.phr_holdertype;
        entity["phr_holderid"] = dto.phr_holderid;
        entity["phr_issuingauthority"] = dto.phr_issuingauthority;
        entity["phr_issuedate"] = dto.phr_issuedate;
        entity["phr_expirydate"] = dto.phr_expirydate;
        entity["phr_status"] = dto.phr_status;
        entity["phr_scope"] = dto.phr_scope;
        entity["phr_permittedactivities"] = dto.phr_permittedactivities;
        entity["phr_modifieddate"] = DateTime.UtcNow;
        return entity;
    }

    #region Document Operations (T111)

    public async Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(DocumentEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId)
                    }
                },
                Orders = { new OrderExpression("phr_uploadeddate", OrderType.Descending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDocumentDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for licence {LicenceId}", licenceId);
            return Enumerable.Empty<LicenceDocument>();
        }
    }

    public async Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(DocumentEntityName, documentId, new ColumnSet(true), cancellationToken);
            return MapToDocumentDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<Guid> AddDocumentAsync(LicenceDocument document, CancellationToken cancellationToken = default)
    {
        var dto = LicenceDocumentDto.FromDomainModel(document);
        var entity = MapDocumentToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Added document {Id} to licence {LicenceId}", id, document.LicenceId);
        return id;
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(DocumentEntityName, documentId, cancellationToken);
        _logger.LogInformation("Deleted document {DocumentId}", documentId);
    }

    private LicenceDocumentDto MapToDocumentDto(Entity entity)
    {
        return new LicenceDocumentDto
        {
            phr_documentid = entity.Id,
            phr_licenceid = entity.GetAttributeValue<Guid>("phr_licenceid"),
            phr_documenttype = entity.GetAttributeValue<int>("phr_documenttype"),
            phr_filename = entity.GetAttributeValue<string>("phr_filename"),
            phr_blobstorageurl = entity.GetAttributeValue<string>("phr_blobstorageurl"),
            phr_uploadeddate = entity.GetAttributeValue<DateTime>("phr_uploadeddate"),
            phr_uploadedby = entity.GetAttributeValue<Guid>("phr_uploadedby"),
            phr_contenttype = entity.GetAttributeValue<string>("phr_contenttype"),
            phr_filesizebytes = entity.GetAttributeValue<long?>("phr_filesizebytes")
        };
    }

    private Entity MapDocumentToEntity(LicenceDocumentDto dto)
    {
        var entity = new Entity(DocumentEntityName) { Id = dto.phr_documentid };
        entity["phr_licenceid"] = dto.phr_licenceid;
        entity["phr_documenttype"] = dto.phr_documenttype;
        entity["phr_filename"] = dto.phr_filename;
        entity["phr_blobstorageurl"] = dto.phr_blobstorageurl;
        entity["phr_uploadeddate"] = dto.phr_uploadeddate;
        entity["phr_uploadedby"] = dto.phr_uploadedby;
        entity["phr_contenttype"] = dto.phr_contenttype;
        entity["phr_filesizebytes"] = dto.phr_filesizebytes;
        return entity;
    }

    #endregion

    #region Verification Operations (T108c)

    public async Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(VerificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId)
                    }
                },
                Orders = { new OrderExpression("phr_verificationdate", OrderType.Descending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToVerificationDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification history for licence {LicenceId}", licenceId);
            return Enumerable.Empty<LicenceVerification>();
        }
    }

    public async Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(VerificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId)
                    }
                },
                Orders = { new OrderExpression("phr_verificationdate", OrderType.Descending) },
                TopCount = 1
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null
                ? MapToVerificationDto(result.Entities.First()).ToDomainModel()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest verification for licence {LicenceId}", licenceId);
            return null;
        }
    }

    public async Task<Guid> AddVerificationAsync(LicenceVerification verification, CancellationToken cancellationToken = default)
    {
        var dto = LicenceVerificationDto.FromDomainModel(verification);
        var entity = MapVerificationToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Added verification {Id} to licence {LicenceId} with outcome {Outcome}",
            id, verification.LicenceId, verification.Outcome);
        return id;
    }

    private LicenceVerificationDto MapToVerificationDto(Entity entity)
    {
        return new LicenceVerificationDto
        {
            phr_verificationid = entity.Id,
            phr_licenceid = entity.GetAttributeValue<Guid>("phr_licenceid"),
            phr_verificationmethod = entity.GetAttributeValue<int>("phr_verificationmethod"),
            phr_verificationdate = entity.GetAttributeValue<DateTime>("phr_verificationdate"),
            phr_verifiedby = entity.GetAttributeValue<Guid>("phr_verifiedby"),
            phr_verifiername = entity.GetAttributeValue<string>("phr_verifiername"),
            phr_outcome = entity.GetAttributeValue<int>("phr_outcome"),
            phr_notes = entity.GetAttributeValue<string>("phr_notes"),
            phr_authorityreferencenumber = entity.GetAttributeValue<string>("phr_authorityreferencenumber"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate")
        };
    }

    private Entity MapVerificationToEntity(LicenceVerificationDto dto)
    {
        var entity = new Entity(VerificationEntityName) { Id = dto.phr_verificationid };
        entity["phr_licenceid"] = dto.phr_licenceid;
        entity["phr_verificationmethod"] = dto.phr_verificationmethod;
        entity["phr_verificationdate"] = dto.phr_verificationdate;
        entity["phr_verifiedby"] = dto.phr_verifiedby;
        entity["phr_verifiername"] = dto.phr_verifiername;
        entity["phr_outcome"] = dto.phr_outcome;
        entity["phr_notes"] = dto.phr_notes;
        entity["phr_authorityreferencenumber"] = dto.phr_authorityreferencenumber;
        entity["phr_createddate"] = dto.phr_createddate;
        return entity;
    }

    #endregion

    #region Scope Change Operations

    public async Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ScopeChangeEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_licenceid", ConditionOperator.Equal, licenceId)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Descending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToScopeChangeDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scope changes for licence {LicenceId}", licenceId);
            return Enumerable.Empty<LicenceScopeChange>();
        }
    }

    public async Task<Guid> AddScopeChangeAsync(LicenceScopeChange scopeChange, CancellationToken cancellationToken = default)
    {
        var dto = LicenceScopeChangeDto.FromDomainModel(scopeChange);
        var entity = MapScopeChangeToEntity(dto);
        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Added scope change {Id} to licence {LicenceId} with type {ChangeType}",
            id, scopeChange.LicenceId, scopeChange.ChangeType);
        return id;
    }

    private LicenceScopeChangeDto MapToScopeChangeDto(Entity entity)
    {
        return new LicenceScopeChangeDto
        {
            phr_changeid = entity.Id,
            phr_licenceid = entity.GetAttributeValue<Guid>("phr_licenceid"),
            phr_effectivedate = entity.GetAttributeValue<DateTime>("phr_effectivedate"),
            phr_changedescription = entity.GetAttributeValue<string>("phr_changedescription"),
            phr_changetype = entity.GetAttributeValue<int>("phr_changetype"),
            phr_recordedby = entity.GetAttributeValue<Guid>("phr_recordedby"),
            phr_recordername = entity.GetAttributeValue<string>("phr_recordername"),
            phr_recordeddate = entity.GetAttributeValue<DateTime>("phr_recordeddate"),
            phr_supportingdocumentid = entity.GetAttributeValue<Guid?>("phr_supportingdocumentid"),
            phr_substancesadded = entity.GetAttributeValue<string>("phr_substancesadded"),
            phr_substancesremoved = entity.GetAttributeValue<string>("phr_substancesremoved"),
            phr_activitiesadded = entity.GetAttributeValue<string>("phr_activitiesadded"),
            phr_activitiesremoved = entity.GetAttributeValue<string>("phr_activitiesremoved")
        };
    }

    private Entity MapScopeChangeToEntity(LicenceScopeChangeDto dto)
    {
        var entity = new Entity(ScopeChangeEntityName) { Id = dto.phr_changeid };
        entity["phr_licenceid"] = dto.phr_licenceid;
        entity["phr_effectivedate"] = dto.phr_effectivedate;
        entity["phr_changedescription"] = dto.phr_changedescription;
        entity["phr_changetype"] = dto.phr_changetype;
        entity["phr_recordedby"] = dto.phr_recordedby;
        entity["phr_recordername"] = dto.phr_recordername;
        entity["phr_recordeddate"] = dto.phr_recordeddate;
        if (dto.phr_supportingdocumentid.HasValue)
            entity["phr_supportingdocumentid"] = dto.phr_supportingdocumentid.Value;
        entity["phr_substancesadded"] = dto.phr_substancesadded;
        entity["phr_substancesremoved"] = dto.phr_substancesremoved;
        entity["phr_activitiesadded"] = dto.phr_activitiesadded;
        entity["phr_activitiesremoved"] = dto.phr_activitiesremoved;
        return entity;
    }

    #endregion
}
