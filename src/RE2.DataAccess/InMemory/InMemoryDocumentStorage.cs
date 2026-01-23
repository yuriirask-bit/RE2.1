using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IDocumentStorage for local development and testing.
/// Stores documents in memory - data is lost when the application restarts.
/// </summary>
public class InMemoryDocumentStorage : IDocumentStorage
{
    private readonly ConcurrentDictionary<string, InMemoryDocument> _documents = new();

    private static string GetKey(string containerName, string blobName) => $"{containerName}/{blobName}";

    public Task<Uri> UploadDocumentAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);

        var document = new InMemoryDocument
        {
            Content = memoryStream.ToArray(),
            ContentType = contentType,
            Metadata = metadata != null ? new Dictionary<string, string>(metadata) : new Dictionary<string, string>(),
            UploadedAt = DateTime.UtcNow
        };

        var key = GetKey(containerName, blobName);
        _documents[key] = document;

        // Return a fake URI for the document
        var uri = new Uri($"memory://localhost/{containerName}/{blobName}");
        return Task.FromResult(uri);
    }

    public Task<Stream> DownloadDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(containerName, blobName);

        if (!_documents.TryGetValue(key, out var document))
        {
            throw new FileNotFoundException($"Document not found: {key}");
        }

        Stream stream = new MemoryStream(document.Content);
        return Task.FromResult(stream);
    }

    public Task DeleteDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(containerName, blobName);
        _documents.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> DocumentExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(containerName, blobName);
        return Task.FromResult(_documents.ContainsKey(key));
    }

    public Task<Uri> GetDocumentSasUriAsync(
        string containerName,
        string blobName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(containerName, blobName);

        if (!_documents.ContainsKey(key))
        {
            throw new FileNotFoundException($"Document not found: {key}");
        }

        // Return a fake SAS URI for in-memory storage
        var expiry = DateTime.UtcNow.Add(expiresIn).ToString("o");
        var uri = new Uri($"memory://localhost/{containerName}/{blobName}?expiry={Uri.EscapeDataString(expiry)}");
        return Task.FromResult(uri);
    }

    public Task<IEnumerable<string>> ListDocumentsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var containerPrefix = $"{containerName}/";
        var fullPrefix = prefix != null ? $"{containerPrefix}{prefix}" : containerPrefix;

        var blobNames = _documents.Keys
            .Where(k => k.StartsWith(fullPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => k.Substring(containerPrefix.Length))
            .ToList();

        return Task.FromResult<IEnumerable<string>>(blobNames);
    }

    private class InMemoryDocument
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime UploadedAt { get; set; }
    }
}
