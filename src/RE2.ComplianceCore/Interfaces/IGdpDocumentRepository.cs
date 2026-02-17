using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP document attachment operations.
/// T233: CRUD for GdpDocument per US10 data-model.md.
/// Polymorphic ownership via OwnerEntityType/OwnerEntityId.
/// </summary>
public interface IGdpDocumentRepository
{
    /// <summary>
    /// Gets all documents for a specific entity.
    /// </summary>
    Task<IEnumerable<GdpDocument>> GetDocumentsByEntityAsync(GdpDocumentEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific document by ID.
    /// </summary>
    Task<GdpDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP document metadata record.
    /// </summary>
    Task<Guid> CreateDocumentAsync(GdpDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP document metadata record.
    /// </summary>
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
