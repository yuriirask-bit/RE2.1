using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
            throw new ArgumentException("Dataverse URL is required", nameof(dataverseUrl));

        // Per research.md: Use DefaultAzureCredential for Managed Identity
        var credential = new DefaultAzureCredential();

        _serviceClient = new ServiceClient(
            instanceUrl: new Uri(dataverseUrl),
            tokenProviderFunction: async (uri) =>
            {
                try
                {
                    var token = await credential.GetTokenAsync(
                        new TokenRequestContext(new[] { $"{uri}/.default" }),
                        default);
                    return token.Token;
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
    /// Disposes the ServiceClient connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _serviceClient?.Dispose();
        _disposed = true;

        _logger.LogInformation("DataverseClient disposed");
    }
}
