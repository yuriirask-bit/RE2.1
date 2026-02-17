using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;

namespace RE2.DataAccess.Caching;

/// <summary>
/// Implements ICacheService using IDistributedCache with JSON serialization.
/// T280: Supports Redis in production and in-memory cache for development.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    // Track keys by prefix for prefix-based invalidation with in-memory cache.
    // For production Redis, SCAN-based invalidation would be used instead.
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            if (data == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key: {Key}. Returning null.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var data = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            await _cache.SetStringAsync(key, data, options, ct);
            _knownKeys.TryAdd(key, 0);

            _logger.LogDebug("Cache set for key: {Key}, TTL: {Ttl}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key: {Key}. Continuing without cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _knownKeys.TryRemove(key, out _);

            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key: {Key}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var keysToRemove = _knownKeys.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                await _cache.RemoveAsync(key, ct);
                _knownKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Cache prefix invalidation: removed {Count} keys with prefix: {Prefix}",
                keysToRemove.Count, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache prefix removal failed for prefix: {Prefix}.", prefix);
        }
    }
}
