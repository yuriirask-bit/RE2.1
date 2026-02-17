using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;

namespace RE2.DataAccess.BlobStorage;

/// <summary>
/// Implementation of IDocumentStorage using Azure Blob Storage.
/// Uses Managed Identity authentication via DefaultAzureCredential.
/// Stores document attachments (PDFs, scanned licences, certificates).
/// </summary>
public class DocumentStorageClient : IDocumentStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<DocumentStorageClient> _logger;

    /// <summary>
    /// Initializes a new instance of DocumentStorageClient.
    /// </summary>
    /// <param name="storageAccountUrl">Azure Storage account URL (e.g., "https://youraccount.blob.core.windows.net").</param>
    /// <param name="logger">Logger instance.</param>
    public DocumentStorageClient(string storageAccountUrl, ILogger<DocumentStorageClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(storageAccountUrl))
        {
            throw new ArgumentException("Storage account URL is required", nameof(storageAccountUrl));
        }

        // Use Managed Identity for authentication
        var credential = new DefaultAzureCredential();
        _blobServiceClient = new BlobServiceClient(new Uri(storageAccountUrl), credential);

        _logger.LogInformation("DocumentStorageClient initialized with storage account: {Url}", storageAccountUrl);
    }

    /// <inheritdoc/>
    public async Task<Uri> UploadDocumentAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Uploading document {BlobName} to container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata
            };

            await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

            _logger.LogInformation("Uploaded document {BlobName} to container {ContainerName}", blobName, containerName);

            return blobClient.Uri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {BlobName} to container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Downloading document {BlobName} from container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Downloaded document {BlobName} from container {ContainerName}", blobName, containerName);

            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteDocumentAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting document {BlobName} from container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted document {BlobName} from container {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DocumentExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var exists = await blobClient.ExistsAsync(cancellationToken);

            return exists.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document {BlobName} exists in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Uri> GetDocumentSasUriAsync(
        string containerName,
        string blobName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating SAS URI for document {BlobName} in container {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
            }

            // Generate SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b", // Blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start 5 minutes ago to account for clock skew
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogInformation("Generated SAS URI for document {BlobName} expiring in {Duration}",
                blobName, expiresIn);

            return sasUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URI for document {BlobName} in container {ContainerName}",
                blobName, containerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> ListDocumentsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing documents in container {ContainerName} with prefix {Prefix}", containerName, prefix ?? "none");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var blobNames = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobNames.Add(blobItem.Name);
            }

            _logger.LogInformation("Listed {Count} documents in container {ContainerName}", blobNames.Count, containerName);

            return blobNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents in container {ContainerName}", containerName);
            throw;
        }
    }
}
