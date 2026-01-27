using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RE2.ComplianceApi.HealthChecks;

/// <summary>
/// Publishes health check results to <see cref="ServiceHealthStatus"/> singleton.
/// T047g: Bridges ASP.NET Core health check system with the degradation middleware per FR-054/FR-056.
/// Runs periodically (every 30 seconds) via the health check publisher infrastructure.
/// </summary>
public class HealthCheckPublisher : IHealthCheckPublisher
{
    private readonly ServiceHealthStatus _serviceHealthStatus;
    private readonly ILogger<HealthCheckPublisher> _logger;

    public HealthCheckPublisher(ServiceHealthStatus serviceHealthStatus, ILogger<HealthCheckPublisher> logger)
    {
        _serviceHealthStatus = serviceHealthStatus;
        _logger = logger;
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            var isHealthy = entry.Value.Status == HealthStatus.Healthy;

            switch (entry.Key)
            {
                case "dataverse":
                    _serviceHealthStatus.UpdateDataverseStatus(isHealthy);
                    break;
                case "d365fo":
                    _serviceHealthStatus.UpdateD365FoStatus(isHealthy);
                    break;
                case "blobstorage":
                    _serviceHealthStatus.UpdateBlobStorageStatus(isHealthy);
                    break;
            }

            if (!isHealthy)
            {
                _logger.LogWarning("Health check {Name} reported {Status}: {Description}",
                    entry.Key, entry.Value.Status, entry.Value.Description);
            }
        }

        _logger.LogDebug("Health check publisher updated service status. All healthy: {AllHealthy}",
            _serviceHealthStatus.AllServicesHealthy);

        return Task.CompletedTask;
    }
}
