namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Abstraction for distributed caching operations.
/// T280: Provides cache get/set/remove with prefix-based invalidation.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Sets a cached value with optional TTL.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Removes a cached value by exact key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes all cached values matching a key prefix.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
