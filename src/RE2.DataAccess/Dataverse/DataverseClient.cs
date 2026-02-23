using System.Net.Http.Json;
using System.ServiceModel;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Exceptions;
using RE2.ComplianceCore.Interfaces;

namespace RE2.DataAccess.Dataverse;

/// <summary>
/// Implementation of IDataverseClient using Microsoft.PowerPlatform.Dataverse.Client.
/// Per research.md section 1: Uses ServiceClient with Managed Identity authentication.
/// Includes built-in retry logic and throttling handling per Dataverse service protection limits.
/// </summary>
public class DataverseClient : IDataverseClient, IDisposable
{
    private readonly ServiceClient _serviceClient;
    private readonly ILogger<DataverseClient> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of DataverseClient.
    /// </summary>
    /// <param name="dataverseUrl">Dataverse instance URL (e.g., "https://yourorg.crm.dynamics.com").</param>
    /// <param name="logger">Logger instance.</param>
    public DataverseClient(string dataverseUrl, ILogger<DataverseClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(dataverseUrl))
        {
            throw new ArgumentException("Dataverse URL is required", nameof(dataverseUrl));
        }

        // Acquire tokens by calling the App Service managed identity endpoint directly.
        // Using DefaultAzureCredential here causes MSAL conflicts with the ServiceClient SDK's
        // internal MSAL ConfidentialClientApplication — the SDK's MSAL flow passes the full
        // XRM endpoint URI as the resource (e.g. .../XRMServices/2011/Organization.svc/web?...)
        // which Azure AD rejects with AADSTS500011.
        // Bypassing MSAL entirely by calling the identity endpoint over HTTP avoids this.
        var dataverseResource = dataverseUrl.TrimEnd('/');

        _serviceClient = new ServiceClient(
            instanceUrl: new Uri(dataverseUrl),
            tokenProviderFunction: async (string instanceUri) =>
            {
                try
                {
                    return await AcquireManagedIdentityTokenAsync(dataverseResource);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to acquire token for Dataverse");
                    throw;
                }
            },
            useUniqueInstance: true,
            logger: logger);

        // Configure retry settings per research.md section 1
        _serviceClient.RetryPauseTime = TimeSpan.FromSeconds(2);
        _serviceClient.MaxRetryCount = 3;

        if (!_serviceClient.IsReady)
        {
            var error = _serviceClient.LastError;
            _logger.LogError("Failed to connect to Dataverse: {Error}", error);
            throw new InvalidOperationException($"Failed to connect to Dataverse: {error}");
        }

        _logger.LogInformation("Successfully connected to Dataverse instance: {Url}", dataverseUrl);
    }

    /// <inheritdoc/>
    public bool IsConnected => _serviceClient?.IsReady ?? false;

    /// <inheritdoc/>
    public async Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columnSet,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving entity {EntityName} with ID {Id}", entityName, id);

            var entity = await _serviceClient.RetrieveAsync(
                entityName,
                id,
                columnSet,
                cancellationToken);

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity {EntityName} with ID {Id}", entityName, id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<EntityCollection> RetrieveMultipleAsync(
        QueryExpression query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving multiple entities for {EntityName}", query.EntityName);

            var results = await _serviceClient.RetrieveMultipleAsync(query, cancellationToken);

            _logger.LogDebug("Retrieved {Count} entities for {EntityName}",
                results.Entities.Count, query.EntityName);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple entities for {EntityName}", query.EntityName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(
        Entity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating entity {EntityName}", entity.LogicalName);

            var id = await _serviceClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created entity {EntityName} with ID {Id}",
                entity.LogicalName, id);

            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity {EntityName}", entity.LogicalName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        Entity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating entity {EntityName} with ID {Id}",
                entity.LogicalName, entity.Id);

            await _serviceClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated entity {EntityName} with ID {Id}",
                entity.LogicalName, entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity {EntityName} with ID {Id}",
                entity.LogicalName, entity.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateWithConcurrencyAsync(
        Entity entity,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        const int ConcurrencyVersionMismatchErrorCode = -2147088254;

        try
        {
            _logger.LogDebug("Updating entity {EntityName} with ID {Id} with concurrency check (RowVersion: {RowVersion})",
                entity.LogicalName, entity.Id, rowVersion);

            // Set the row version on the entity for optimistic concurrency
            entity.RowVersion = rowVersion;

            await _serviceClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated entity {EntityName} with ID {Id} with concurrency check",
                entity.LogicalName, entity.Id);
        }
        catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail?.ErrorCode == ConcurrencyVersionMismatchErrorCode)
        {
            _logger.LogWarning("Concurrency conflict detected for entity {EntityName} with ID {Id}. " +
                              "Local version: {LocalVersion}",
                entity.LogicalName, entity.Id, rowVersion);

            throw new ConcurrencyException(
                entity.LogicalName,
                entity.Id,
                rowVersion,
                null, // Remote version not available from exception
                $"The {entity.LogicalName} with ID {entity.Id} has been modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity {EntityName} with ID {Id}",
                entity.LogicalName, entity.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string entityName,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting entity {EntityName} with ID {Id}", entityName, id);

            await _serviceClient.DeleteAsync(entityName, id, cancellationToken);

            _logger.LogInformation("Deleted entity {EntityName} with ID {Id}", entityName, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity {EntityName} with ID {Id}", entityName, id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<OrganizationResponse> ExecuteAsync(
        OrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing organization request {RequestName}",
                request.RequestName);

            var response = await _serviceClient.ExecuteAsync(request, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing organization request {RequestName}",
                request.RequestName);
            throw;
        }
    }

    /// <summary>
    /// Acquires a token from the App Service managed identity endpoint directly,
    /// bypassing MSAL to avoid conflicts with the ServiceClient SDK's internal MSAL flow.
    /// Falls back to DefaultAzureCredential for local development.
    /// </summary>
    private async Task<string> AcquireManagedIdentityTokenAsync(string resource)
    {
        var identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
        var identityHeader = Environment.GetEnvironmentVariable("IDENTITY_HEADER");

        if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(identityHeader))
        {
            // Running on App Service — call the identity endpoint directly
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{identityEndpoint}?resource={Uri.EscapeDataString(resource)}&api-version=2019-08-01");
            request.Headers.Add("X-IDENTITY-HEADER", identityHeader);

            using var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return json.RootElement.GetProperty("access_token").GetString()!;
        }

        // Fallback for local development / non-App-Service environments
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { $"{resource}/.default" }));
        return token.Token;
    }

    /// <summary>
    /// Disposes the ServiceClient connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _serviceClient?.Dispose();
        _disposed = true;

        _logger.LogInformation("DataverseClient disposed");
    }
}
