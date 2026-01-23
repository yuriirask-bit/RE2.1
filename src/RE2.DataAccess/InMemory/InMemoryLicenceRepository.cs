using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ILicenceRepository for local development and testing.
/// Extended with document, verification, and scope change operations for US3.
/// </summary>
public class InMemoryLicenceRepository : ILicenceRepository
{
    private readonly ConcurrentDictionary<Guid, Licence> _licences = new();
    private readonly ConcurrentDictionary<Guid, LicenceDocument> _documents = new();
    private readonly ConcurrentDictionary<Guid, LicenceVerification> _verifications = new();
    private readonly ConcurrentDictionary<Guid, LicenceScopeChange> _scopeChanges = new();

    public Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        _licences.TryGetValue(licenceId, out var licence);
        return Task.FromResult(licence);
    }

    public Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = _licences.Values.FirstOrDefault(l =>
            l.LicenceNumber.Equals(licenceNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(licence);
    }

    public Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        var licences = _licences.Values
            .Where(l => l.HolderId == holderId &&
                        l.HolderType.Equals(holderType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    public Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead));
        var licences = _licences.Values
            .Where(l => l.ExpiryDate.HasValue &&
                        l.ExpiryDate.Value <= cutoffDate &&
                        l.ExpiryDate.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(l => l.ExpiryDate)
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    public Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Licence>>(_licences.Values.ToList());
    }

    public Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        if (licence.LicenceId == Guid.Empty)
        {
            licence.LicenceId = Guid.NewGuid();
        }
        licence.CreatedDate = DateTime.UtcNow;
        licence.ModifiedDate = DateTime.UtcNow;

        _licences.TryAdd(licence.LicenceId, licence);
        return Task.FromResult(licence.LicenceId);
    }

    public Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        licence.ModifiedDate = DateTime.UtcNow;
        _licences[licence.LicenceId] = licence;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        _licences.TryRemove(licenceId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Licence>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        // In-memory implementation: filter licences that have substance mappings for this substance
        var licences = _licences.Values
            .Where(l => l.Status == "Valid" &&
                        l.SubstanceMappings != null &&
                        l.SubstanceMappings.Any(m => m.SubstanceId == substanceId))
            .ToList();
        return Task.FromResult<IEnumerable<Licence>>(licences);
    }

    #region Document Operations

    public Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var documents = _documents.Values
            .Where(d => d.LicenceId == licenceId)
            .OrderByDescending(d => d.UploadedDate)
            .ToList();
        return Task.FromResult<IEnumerable<LicenceDocument>>(documents);
    }

    public Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(documentId, out var document);
        return Task.FromResult(document);
    }

    public Task<Guid> AddDocumentAsync(LicenceDocument document, CancellationToken cancellationToken = default)
    {
        if (document.DocumentId == Guid.Empty)
        {
            document.DocumentId = Guid.NewGuid();
        }
        _documents.TryAdd(document.DocumentId, document);
        return Task.FromResult(document.DocumentId);
    }

    public Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _documents.TryRemove(documentId, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region Verification Operations

    public Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var verifications = _verifications.Values
            .Where(v => v.LicenceId == licenceId)
            .OrderByDescending(v => v.VerificationDate)
            .ToList();
        return Task.FromResult<IEnumerable<LicenceVerification>>(verifications);
    }

    public Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var verification = _verifications.Values
            .Where(v => v.LicenceId == licenceId)
            .OrderByDescending(v => v.VerificationDate)
            .FirstOrDefault();
        return Task.FromResult(verification);
    }

    public Task<Guid> AddVerificationAsync(LicenceVerification verification, CancellationToken cancellationToken = default)
    {
        if (verification.VerificationId == Guid.Empty)
        {
            verification.VerificationId = Guid.NewGuid();
        }
        _verifications.TryAdd(verification.VerificationId, verification);
        return Task.FromResult(verification.VerificationId);
    }

    #endregion

    #region Scope Change Operations

    public Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var scopeChanges = _scopeChanges.Values
            .Where(s => s.LicenceId == licenceId)
            .OrderByDescending(s => s.EffectiveDate)
            .ToList();
        return Task.FromResult<IEnumerable<LicenceScopeChange>>(scopeChanges);
    }

    public Task<Guid> AddScopeChangeAsync(LicenceScopeChange scopeChange, CancellationToken cancellationToken = default)
    {
        if (scopeChange.ChangeId == Guid.Empty)
        {
            scopeChange.ChangeId = Guid.NewGuid();
        }
        _scopeChanges.TryAdd(scopeChange.ChangeId, scopeChange);
        return Task.FromResult(scopeChange.ChangeId);
    }

    #endregion

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Licence> licences)
    {
        foreach (var licence in licences)
        {
            _licences.TryAdd(licence.LicenceId, licence);
        }
    }
}
