using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IIntegrationSystemRepository for local development and testing.
/// T047d variant: Repository implementation for local development without Dataverse.
/// </summary>
public class InMemoryIntegrationSystemRepository : IIntegrationSystemRepository
{
    private readonly Dictionary<Guid, IntegrationSystem> _systems = new();
    private readonly object _lock = new();

    public Task<IntegrationSystem?> GetByIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _systems.TryGetValue(integrationSystemId, out var system);
            return Task.FromResult(system);
        }
    }

    public Task<IntegrationSystem?> GetBySystemNameAsync(string systemName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var system = _systems.Values.FirstOrDefault(s =>
                s.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(system);
        }
    }

    public Task<IntegrationSystem?> GetByOAuthClientIdAsync(string oauthClientId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var system = _systems.Values.FirstOrDefault(s =>
                s.OAuthClientId != null &&
                s.OAuthClientId.Equals(oauthClientId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(system);
        }
    }

    public Task<IEnumerable<IntegrationSystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<IntegrationSystem>>(_systems.Values.ToList());
        }
    }

    public Task<IEnumerable<IntegrationSystem>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _systems.Values.Where(s => s.IsActive).ToList();
            return Task.FromResult<IEnumerable<IntegrationSystem>>(result);
        }
    }

    public Task<IEnumerable<IntegrationSystem>> GetBySystemTypeAsync(IntegrationSystemType systemType, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _systems.Values.Where(s => s.SystemType == systemType).ToList();
            return Task.FromResult<IEnumerable<IntegrationSystem>>(result);
        }
    }

    public Task<Guid> CreateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            integrationSystem.IntegrationSystemId = Guid.NewGuid();
            integrationSystem.CreatedDate = DateTime.UtcNow;
            integrationSystem.ModifiedDate = DateTime.UtcNow;
            _systems[integrationSystem.IntegrationSystemId] = integrationSystem;
            return Task.FromResult(integrationSystem.IntegrationSystemId);
        }
    }

    public Task UpdateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            integrationSystem.ModifiedDate = DateTime.UtcNow;
            _systems[integrationSystem.IntegrationSystemId] = integrationSystem;
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _systems.Remove(integrationSystemId);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(Guid integrationSystemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_systems.ContainsKey(integrationSystemId));
        }
    }

    public Task<bool> SystemNameExistsAsync(string systemName, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var exists = _systems.Values.Any(s =>
                s.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase) &&
                (!excludeId.HasValue || s.IntegrationSystemId != excludeId.Value));
            return Task.FromResult(exists);
        }
    }

    public Task<bool> IsAuthorizedAsync(Guid integrationSystemId, string endpoint, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_systems.TryGetValue(integrationSystemId, out var system))
            {
                return Task.FromResult(false);
            }

            if (!system.IsActive)
            {
                return Task.FromResult(false);
            }

            if (!system.IsEndpointAuthorized(endpoint))
            {
                return Task.FromResult(false);
            }

            if (!string.IsNullOrEmpty(ipAddress) && !system.IsIpAllowed(ipAddress))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
