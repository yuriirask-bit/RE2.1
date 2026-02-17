using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpDocumentRepository.
/// T235: CRUD for GDP document metadata via IDataverseClient.
/// Entity: phr_gdpdocument.
/// </summary>
public class DataverseGdpDocumentRepository : IGdpDocumentRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpDocumentRepository> _logger;

    private const string EntityName = "phr_gdpdocument";

    public DataverseGdpDocumentRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpDocumentRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<GdpDocument>> GetDocumentsByEntityAsync(GdpDocumentEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
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
                        new ConditionExpression("phr_ownerentitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_ownerentityid", ConditionOperator.Equal, entityId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToDocumentDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP documents for {EntityType} {EntityId}", entityType, entityId);
            return Enumerable.Empty<GdpDocument>();
        }
    }

    public async Task<GdpDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(EntityName, documentId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToDocumentDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<Guid> CreateDocumentAsync(GdpDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            if (document.DocumentId == Guid.Empty)
                document.DocumentId = Guid.NewGuid();

            var entity = MapDocumentToEntity(document);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP document {DocumentId} for {EntityType} {EntityId}",
                document.DocumentId, document.OwnerEntityType, document.OwnerEntityId);
            return document.DocumentId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP document for {EntityType} {EntityId}",
                document.OwnerEntityType, document.OwnerEntityId);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(EntityName, documentId, cancellationToken);
            _logger.LogInformation("Deleted GDP document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP document {DocumentId}", documentId);
            throw;
        }
    }

    #region Mapping Helpers

    private static GdpDocumentDto MapToDocumentDto(Entity entity)
    {
        return new GdpDocumentDto
        {
            phr_gdpdocumentid = entity.Id,
            phr_ownerentitytype = entity.GetAttributeValue<int>("phr_ownerentitytype"),
            phr_ownerentityid = entity.GetAttributeValue<Guid>("phr_ownerentityid"),
            phr_documenttype = entity.GetAttributeValue<int>("phr_documenttype"),
            phr_filename = entity.GetAttributeValue<string>("phr_filename"),
            phr_blobstorageurl = entity.GetAttributeValue<string>("phr_blobstorageurl"),
            phr_uploadeddate = entity.GetAttributeValue<DateTime>("phr_uploadeddate"),
            phr_uploadedby = entity.GetAttributeValue<string>("phr_uploadedby"),
            phr_contenttype = entity.GetAttributeValue<string>("phr_contenttype"),
            phr_filesizebytes = entity.Contains("phr_filesizebytes") ? entity.GetAttributeValue<long?>("phr_filesizebytes") : null,
            phr_description = entity.GetAttributeValue<string>("phr_description")
        };
    }

    private static Entity MapDocumentToEntity(GdpDocument document)
    {
        var entity = new Entity(EntityName, document.DocumentId);
        entity["phr_ownerentitytype"] = (int)document.OwnerEntityType;
        entity["phr_ownerentityid"] = document.OwnerEntityId;
        entity["phr_documenttype"] = (int)document.DocumentType;
        entity["phr_filename"] = document.FileName;
        entity["phr_blobstorageurl"] = document.BlobStorageUrl;
        entity["phr_uploadeddate"] = document.UploadedDate;
        entity["phr_uploadedby"] = document.UploadedBy;
        if (document.ContentType != null)
            entity["phr_contenttype"] = document.ContentType;
        if (document.FileSizeBytes.HasValue)
            entity["phr_filesizebytes"] = document.FileSizeBytes.Value;
        if (document.Description != null)
            entity["phr_description"] = document.Description;
        return entity;
    }

    #endregion
}
