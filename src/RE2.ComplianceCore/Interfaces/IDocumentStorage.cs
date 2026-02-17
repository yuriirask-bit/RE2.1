namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Abstraction for document storage operations using Azure Blob Storage.
/// Per plan.md: Stores document attachments (PDFs, scanned licences, certificates).
/// </summary>
public interface IDocumentStorage
{
    /// <summary>
    /// Uploads a document to blob storage.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="blobName">The blob name (file name).</param>
    /// <param name="content">The document content stream.</param>
    /// <param name="contentType">The MIME content type (e.g., "application/pdf").</param>
    /// <param name="metadata">Optional metadata key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URI of the uploaded blob.</returns>
    Task<Uri> UploadDocumentAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document from blob storage.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="blobName">The blob name (file name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document content stream.</returns>
    Task<Stream> DownloadDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from blob storage.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="blobName">The blob name (file name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in blob storage.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="blobName">The blob name (file name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document exists, false otherwise.</returns>
    Task<bool> DocumentExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a SAS (Shared Access Signature) URL for temporary document access.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="blobName">The blob name (file name).</param>
    /// <param name="expiresIn">Duration until the SAS token expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAS URL for temporary access.</returns>
    Task<Uri> GetDocumentSasUriAsync(
        string containerName,
        string blobName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists documents in a container with optional prefix filter.
    /// </summary>
    /// <param name="containerName">The blob container name.</param>
    /// <param name="prefix">Optional prefix to filter blob names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of blob names.</returns>
    Task<IEnumerable<string>> ListDocumentsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default);
}
