using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpDocumentRepository for local development and testing.
/// T234: Stores GDP document metadata in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpDocumentRepository : IGdpDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, GdpDocument> _documents = new();

    public Task<IEnumerable<GdpDocument>> GetDocumentsByEntityAsync(GdpDocumentEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var results = _documents.Values
            .Where(d => d.OwnerEntityType == entityType && d.OwnerEntityId == entityId)
            .Select(CloneDocument)
            .ToList();
        return Task.FromResult<IEnumerable<GdpDocument>>(results);
    }

    public Task<GdpDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(documentId, out var document);
        return Task.FromResult(document != null ? CloneDocument(document) : null);
    }

    public Task<Guid> CreateDocumentAsync(GdpDocument document, CancellationToken cancellationToken = default)
    {
        if (document.DocumentId == Guid.Empty)
            document.DocumentId = Guid.NewGuid();

        _documents[document.DocumentId] = CloneDocument(document);
        return Task.FromResult(document.DocumentId);
    }

    public Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _documents.TryRemove(documentId, out _);
        return Task.CompletedTask;
    }

    #region Seed Methods

    /// <summary>
    /// Seeds GDP document data for local development.
    /// </summary>
    internal void SeedDocuments(IEnumerable<GdpDocument> documents)
    {
        foreach (var document in documents)
        {
            _documents.TryAdd(document.DocumentId, document);
        }
    }

    #endregion

    #region Clone Helpers

    private static GdpDocument CloneDocument(GdpDocument source) => new()
    {
        DocumentId = source.DocumentId,
        OwnerEntityType = source.OwnerEntityType,
        OwnerEntityId = source.OwnerEntityId,
        DocumentType = source.DocumentType,
        FileName = source.FileName,
        BlobStorageUrl = source.BlobStorageUrl,
        UploadedDate = source.UploadedDate,
        UploadedBy = source.UploadedBy,
        ContentType = source.ContentType,
        FileSizeBytes = source.FileSizeBytes,
        Description = source.Description
    };

    #endregion
}
