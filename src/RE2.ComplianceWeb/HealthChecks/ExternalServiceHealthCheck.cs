using Microsoft.Extensions.Diagnostics.HealthChecks;
using RE2.ComplianceCore.Interfaces;

namespace RE2.ComplianceWeb.HealthChecks;

/// <summary>
/// Health check for Dataverse connectivity.
/// </summary>
public class DataverseHealthCheck : IHealthCheck
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseHealthCheck> _logger;

    public DataverseHealthCheck(IDataverseClient dataverseClient, ILogger<DataverseHealthCheck> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_dataverseClient.IsConnected)
                return Task.FromResult(HealthCheckResult.Healthy("Dataverse connection is active."));

            _logger.LogWarning("Dataverse health check failed: client reports not connected");
            return Task.FromResult(HealthCheckResult.Unhealthy("Dataverse client is not connected."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dataverse health check threw an exception");
            return Task.FromResult(HealthCheckResult.Unhealthy("Dataverse health check failed.", ex));
        }
    }
}

/// <summary>
/// Health check for D365 Finance & Operations OData API connectivity.
/// </summary>
public class D365FoHealthCheck : IHealthCheck
{
    private readonly ID365FoClient _d365FoClient;
    private readonly ILogger<D365FoHealthCheck> _logger;

    public D365FoHealthCheck(ID365FoClient d365FoClient, ILogger<D365FoHealthCheck> logger)
    {
        _d365FoClient = d365FoClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _d365FoClient.GetAsync<object>(
                "$metadata",
                query: null,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("D365 F&O OData API is reachable.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "D365 F&O health check failed: connection error");
            return HealthCheckResult.Unhealthy("D365 F&O OData API is unreachable.", ex);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("D365 F&O health check timed out");
            return HealthCheckResult.Unhealthy("D365 F&O OData API request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "D365 F&O health check threw an unexpected exception");
            return HealthCheckResult.Unhealthy("D365 F&O health check failed.", ex);
        }
    }
}

/// <summary>
/// Health check for Azure Blob Storage document storage connectivity.
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<BlobStorageHealthCheck> _logger;

    public BlobStorageHealthCheck(IDocumentStorage documentStorage, ILogger<BlobStorageHealthCheck> logger)
    {
        _documentStorage = documentStorage;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentStorage.ListDocumentsAsync(
                "health-check",
                prefix: null,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Azure Blob Storage is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob Storage health check failed");
            return HealthCheckResult.Unhealthy("Azure Blob Storage is unreachable.", ex);
        }
    }
}
