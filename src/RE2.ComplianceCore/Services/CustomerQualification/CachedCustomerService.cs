using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Services.CustomerQualification;

/// <summary>
/// Decorator that adds caching to ICustomerService.
/// T282: Caches customer lookups (30 min) and compliance status (5 min per FR-032 max 15 min freshness).
/// Invalidates on write operations.
/// </summary>
public class CachedCustomerService : ICustomerService
{
    private readonly ICustomerService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedCustomerService> _logger;

    private const string KeyPrefix = "re2:customer:";
    private static readonly TimeSpan CustomerTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ComplianceStatusTtl = TimeSpan.FromMinutes(5);

    public CachedCustomerService(
        ICustomerService inner,
        ICacheService cache,
        ILogger<CachedCustomerService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}acct:{customerAccount}:{dataAreaId}";
        var cached = await _cache.GetAsync<Customer>(key, cancellationToken);
        if (cached != null) return cached;

        var result = await _inner.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (result != null)
        {
            await _cache.SetAsync(key, result, CustomerTtl, cancellationToken);
        }
        return result;
    }

    public async Task<CustomerComplianceStatus> GetComplianceStatusAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}status:{customerAccount}:{dataAreaId}";
        var cached = await _cache.GetAsync<CustomerComplianceStatus>(key, cancellationToken);
        if (cached != null) return cached;

        var result = await _inner.GetComplianceStatusAsync(customerAccount, dataAreaId, cancellationToken);
        await _cache.SetAsync(key, result, ComplianceStatusTtl, cancellationToken);
        return result;
    }

    public async Task<(Guid? Id, ValidationResult Result)> ConfigureComplianceAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ConfigureComplianceAsync(customer, cancellationToken);
        if (result.Result.IsValid)
        {
            await InvalidateCustomerCacheAsync(customer.CustomerAccount, customer.DataAreaId, cancellationToken);
        }
        return result;
    }

    public async Task<ValidationResult> UpdateComplianceAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateComplianceAsync(customer, cancellationToken);
        if (result.IsValid)
        {
            await InvalidateCustomerCacheAsync(customer.CustomerAccount, customer.DataAreaId, cancellationToken);
        }
        return result;
    }

    public async Task<ValidationResult> SuspendCustomerAsync(string customerAccount, string dataAreaId, string reason, CancellationToken cancellationToken = default)
    {
        var result = await _inner.SuspendCustomerAsync(customerAccount, dataAreaId, reason, cancellationToken);
        if (result.IsValid)
        {
            await InvalidateCustomerCacheAsync(customerAccount, dataAreaId, cancellationToken);
        }
        return result;
    }

    public async Task<ValidationResult> ReinstateCustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ReinstateCustomerAsync(customerAccount, dataAreaId, cancellationToken);
        if (result.IsValid)
        {
            await InvalidateCustomerCacheAsync(customerAccount, dataAreaId, cancellationToken);
        }
        return result;
    }

    // Pass-through methods (not cached - list operations)

    public Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => _inner.GetAllAsync(cancellationToken);

    public Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default)
        => _inner.GetAllD365CustomersAsync(cancellationToken);

    public Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
        => _inner.GetByApprovalStatusAsync(status, cancellationToken);

    public Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
        => _inner.GetByBusinessCategoryAsync(category, cancellationToken);

    public Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
        => _inner.GetReVerificationDueAsync(daysAhead, cancellationToken);

    public Task<ValidationResult> RemoveComplianceAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
        => _inner.RemoveComplianceAsync(customerAccount, dataAreaId, cancellationToken);

    public Task<ValidationResult> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        => _inner.ValidateCustomerAsync(customer, cancellationToken);

    public Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        => _inner.SearchByNameAsync(searchTerm, cancellationToken);

    private async Task InvalidateCustomerCacheAsync(string customerAccount, string dataAreaId, CancellationToken ct)
    {
        _logger.LogDebug("Invalidating customer cache for {Account}:{DataArea}", customerAccount, dataAreaId);

        await _cache.RemoveAsync($"{KeyPrefix}acct:{customerAccount}:{dataAreaId}", ct);
        await _cache.RemoveAsync($"{KeyPrefix}status:{customerAccount}:{dataAreaId}", ct);
    }
}
