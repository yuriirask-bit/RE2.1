using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.LicenceValidation;

/// <summary>
/// Decorator that adds caching to ILicenceService.
/// T281: Caches licence lookups with 30-minute TTL; invalidates on writes.
/// </summary>
public class CachedLicenceService : ILicenceService
{
    private readonly ILicenceService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedLicenceService> _logger;

    private const string KeyPrefix = "re2:licence:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    public CachedLicenceService(
        ILicenceService inner,
        ICacheService cache,
        ILogger<CachedLicenceService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}id:{licenceId}";
        var cached = await _cache.GetAsync<Licence>(key, cancellationToken);
        if (cached != null) return cached;

        var result = await _inner.GetByIdAsync(licenceId, cancellationToken);
        if (result != null)
        {
            await _cache.SetAsync(key, result, DefaultTtl, cancellationToken);
        }
        return result;
    }

    public async Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}num:{licenceNumber}";
        var cached = await _cache.GetAsync<Licence>(key, cancellationToken);
        if (cached != null) return cached;

        var result = await _inner.GetByLicenceNumberAsync(licenceNumber, cancellationToken);
        if (result != null)
        {
            await _cache.SetAsync(key, result, DefaultTtl, cancellationToken);
        }
        return result;
    }

    public async Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}holder:{holderId}:{holderType}";
        var cached = await _cache.GetAsync<List<Licence>>(key, cancellationToken);
        if (cached != null) return cached;

        var result = (await _inner.GetByHolderAsync(holderId, holderType, cancellationToken)).ToList();
        await _cache.SetAsync(key, result, DefaultTtl, cancellationToken);
        return result;
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateAsync(licence, cancellationToken);
        if (result.Result.IsValid)
        {
            await InvalidateLicenceCacheAsync(licence, cancellationToken);
        }
        return result;
    }

    public async Task<ValidationResult> UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(licence, cancellationToken);
        if (result.IsValid)
        {
            await InvalidateLicenceCacheAsync(licence, cancellationToken);
        }
        return result;
    }

    public async Task<ValidationResult> DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        // Get licence before delete to know holder for invalidation
        var licence = await _inner.GetByIdAsync(licenceId, cancellationToken);
        var result = await _inner.DeleteAsync(licenceId, cancellationToken);
        if (result.IsValid && licence != null)
        {
            await InvalidateLicenceCacheAsync(licence, cancellationToken);
        }
        return result;
    }

    // Pass-through methods (not cached)

    public Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
        => _inner.GetExpiringLicencesAsync(daysAhead, cancellationToken);

    public Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
        => _inner.GetAllAsync(cancellationToken);

    public Task<ValidationResult> ValidateLicenceAsync(Licence licence, CancellationToken cancellationToken = default)
        => _inner.ValidateLicenceAsync(licence, cancellationToken);

    public Task<ValidationResult> CheckHolderLicenceForActivityAsync(Guid holderId, string holderType, LicenceTypes.PermittedActivity requiredActivity, CancellationToken cancellationToken = default)
        => _inner.CheckHolderLicenceForActivityAsync(holderId, holderType, requiredActivity, cancellationToken);

    public Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default)
        => _inner.GetDocumentsAsync(licenceId, cancellationToken);

    public Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _inner.GetDocumentByIdAsync(documentId, cancellationToken);

    public Task<(Guid? Id, ValidationResult Result)> UploadDocumentAsync(Guid licenceId, LicenceDocument document, Stream content, CancellationToken cancellationToken = default)
        => _inner.UploadDocumentAsync(licenceId, document, content, cancellationToken);

    public Task<ValidationResult> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _inner.DeleteDocumentAsync(documentId, cancellationToken);

    public Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default)
        => _inner.GetVerificationHistoryAsync(licenceId, cancellationToken);

    public Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default)
        => _inner.GetLatestVerificationAsync(licenceId, cancellationToken);

    public Task<(Guid? Id, ValidationResult Result)> RecordVerificationAsync(LicenceVerification verification, CancellationToken cancellationToken = default)
        => _inner.RecordVerificationAsync(verification, cancellationToken);

    public Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default)
        => _inner.GetScopeChangesAsync(licenceId, cancellationToken);

    public Task<(Guid? Id, ValidationResult Result)> RecordScopeChangeAsync(LicenceScopeChange scopeChange, CancellationToken cancellationToken = default)
        => _inner.RecordScopeChangeAsync(scopeChange, cancellationToken);

    private async Task InvalidateLicenceCacheAsync(Licence licence, CancellationToken ct)
    {
        _logger.LogDebug("Invalidating licence cache for {LicenceId}", licence.LicenceId);

        await _cache.RemoveAsync($"{KeyPrefix}id:{licence.LicenceId}", ct);
        await _cache.RemoveAsync($"{KeyPrefix}num:{licence.LicenceNumber}", ct);
        await _cache.RemoveByPrefixAsync($"{KeyPrefix}holder:{licence.HolderId}", ct);
    }
}
