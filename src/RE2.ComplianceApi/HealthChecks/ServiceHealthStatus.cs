namespace RE2.ComplianceApi.HealthChecks;

/// <summary>
/// Tracks the aggregate health status of external services.
/// T047g: Used by GracefulDegradationMiddleware to determine service availability per FR-054.
/// Thread-safe singleton that is updated by health check results.
/// </summary>
public class ServiceHealthStatus
{
    private volatile bool _isDataverseHealthy = true;
    private volatile bool _isD365FoHealthy = true;
    private volatile bool _isBlobStorageHealthy = true;
    private DateTime _lastUpdated = DateTime.UtcNow;

    /// <summary>
    /// Whether the Dataverse service is healthy.
    /// </summary>
    public bool IsDataverseHealthy => _isDataverseHealthy;

    /// <summary>
    /// Whether the D365 F&O service is healthy.
    /// </summary>
    public bool IsD365FoHealthy => _isD365FoHealthy;

    /// <summary>
    /// Whether the Azure Blob Storage service is healthy.
    /// </summary>
    public bool IsBlobStorageHealthy => _isBlobStorageHealthy;

    /// <summary>
    /// When the status was last updated.
    /// </summary>
    public DateTime LastUpdated => _lastUpdated;

    /// <summary>
    /// Whether all external services are healthy.
    /// </summary>
    public bool AllServicesHealthy => _isDataverseHealthy && _isD365FoHealthy && _isBlobStorageHealthy;

    /// <summary>
    /// Whether core data services (Dataverse + D365 F&O) are healthy.
    /// Critical path endpoints require these services.
    /// </summary>
    public bool CoreServicesHealthy => _isDataverseHealthy && _isD365FoHealthy;

    /// <summary>
    /// Updates the health status for Dataverse.
    /// </summary>
    public void UpdateDataverseStatus(bool isHealthy)
    {
        _isDataverseHealthy = isHealthy;
        _lastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the health status for D365 F&O.
    /// </summary>
    public void UpdateD365FoStatus(bool isHealthy)
    {
        _isD365FoHealthy = isHealthy;
        _lastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the health status for Blob Storage.
    /// </summary>
    public void UpdateBlobStorageStatus(bool isHealthy)
    {
        _isBlobStorageHealthy = isHealthy;
        _lastUpdated = DateTime.UtcNow;
    }
}
