using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpCredentialRepository for local development and testing.
/// T204: Stores GDP service providers, credentials, reviews, and verifications in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpCredentialRepository : IGdpCredentialRepository
{
    private readonly ConcurrentDictionary<Guid, GdpServiceProvider> _providers = new();
    private readonly ConcurrentDictionary<Guid, GdpCredential> _credentials = new();
    private readonly ConcurrentDictionary<Guid, QualificationReview> _reviews = new();
    private readonly ConcurrentDictionary<Guid, GdpCredentialVerification> _verifications = new();

    #region GdpServiceProvider Operations

    public Task<IEnumerable<GdpServiceProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GdpServiceProvider>>(
            _providers.Values.Select(CloneProvider).ToList());
    }

    public Task<GdpServiceProvider?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        _providers.TryGetValue(providerId, out var provider);
        return Task.FromResult(provider != null ? CloneProvider(provider) : null);
    }

    public Task<Guid> CreateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        if (provider.ProviderId == Guid.Empty)
        {
            provider.ProviderId = Guid.NewGuid();
        }

        provider.CreatedDate = DateTime.UtcNow;
        provider.ModifiedDate = DateTime.UtcNow;

        _providers[provider.ProviderId] = CloneProvider(provider);
        return Task.FromResult(provider.ProviderId);
    }

    public Task UpdateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        provider.ModifiedDate = DateTime.UtcNow;
        _providers[provider.ProviderId] = CloneProvider(provider);
        return Task.CompletedTask;
    }

    public Task DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        _providers.TryRemove(providerId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<GdpServiceProvider>> GetProvidersRequiringReviewAsync(DateOnly beforeDate, CancellationToken cancellationToken = default)
    {
        var providers = _providers.Values
            .Where(p => p.NextReviewDate.HasValue && p.NextReviewDate.Value <= beforeDate)
            .Select(CloneProvider)
            .ToList();

        return Task.FromResult<IEnumerable<GdpServiceProvider>>(providers);
    }

    #endregion

    #region GdpCredential Operations

    public Task<IEnumerable<GdpCredential>> GetCredentialsByEntityAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var credentials = _credentials.Values
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .Select(CloneCredential)
            .ToList();

        return Task.FromResult<IEnumerable<GdpCredential>>(credentials);
    }

    public Task<GdpCredential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        _credentials.TryGetValue(credentialId, out var credential);
        return Task.FromResult(credential != null ? CloneCredential(credential) : null);
    }

    public Task<Guid> CreateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        if (credential.CredentialId == Guid.Empty)
        {
            credential.CredentialId = Guid.NewGuid();
        }

        credential.CreatedDate = DateTime.UtcNow;
        credential.ModifiedDate = DateTime.UtcNow;

        _credentials[credential.CredentialId] = CloneCredential(credential);
        return Task.FromResult(credential.CredentialId);
    }

    public Task UpdateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        credential.ModifiedDate = DateTime.UtcNow;
        _credentials[credential.CredentialId] = CloneCredential(credential);
        return Task.CompletedTask;
    }

    public Task DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        _credentials.TryRemove(credentialId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<GdpCredential>> GetCredentialsExpiringBeforeAsync(DateOnly beforeDate, CancellationToken cancellationToken = default)
    {
        var credentials = _credentials.Values
            .Where(c => c.ValidityEndDate.HasValue && c.ValidityEndDate.Value <= beforeDate)
            .Select(CloneCredential)
            .ToList();

        return Task.FromResult<IEnumerable<GdpCredential>>(credentials);
    }

    #endregion

    #region QualificationReview Operations

    public Task<IEnumerable<QualificationReview>> GetReviewsByEntityAsync(ReviewEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var reviews = _reviews.Values
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .Select(CloneReview)
            .ToList();

        return Task.FromResult<IEnumerable<QualificationReview>>(reviews);
    }

    public Task<Guid> CreateReviewAsync(QualificationReview review, CancellationToken cancellationToken = default)
    {
        if (review.ReviewId == Guid.Empty)
        {
            review.ReviewId = Guid.NewGuid();
        }

        _reviews[review.ReviewId] = CloneReview(review);
        return Task.FromResult(review.ReviewId);
    }

    #endregion

    #region GdpCredentialVerification Operations

    public Task<IEnumerable<GdpCredentialVerification>> GetVerificationsByCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var verifications = _verifications.Values
            .Where(v => v.CredentialId == credentialId)
            .Select(CloneVerification)
            .ToList();

        return Task.FromResult<IEnumerable<GdpCredentialVerification>>(verifications);
    }

    public Task<Guid> CreateVerificationAsync(GdpCredentialVerification verification, CancellationToken cancellationToken = default)
    {
        if (verification.VerificationId == Guid.Empty)
        {
            verification.VerificationId = Guid.NewGuid();
        }

        _verifications[verification.VerificationId] = CloneVerification(verification);
        return Task.FromResult(verification.VerificationId);
    }

    #endregion

    #region Seed Methods

    /// <summary>
    /// Seeds GDP service provider data for local development.
    /// </summary>
    internal void SeedProviders(IEnumerable<GdpServiceProvider> providers)
    {
        foreach (var provider in providers)
        {
            _providers.TryAdd(provider.ProviderId, provider);
        }
    }

    /// <summary>
    /// Seeds GDP credential data for local development.
    /// </summary>
    internal void SeedCredentials(IEnumerable<GdpCredential> credentials)
    {
        foreach (var credential in credentials)
        {
            _credentials.TryAdd(credential.CredentialId, credential);
        }
    }

    /// <summary>
    /// Seeds qualification review data for local development.
    /// </summary>
    internal void SeedReviews(IEnumerable<QualificationReview> reviews)
    {
        foreach (var review in reviews)
        {
            _reviews.TryAdd(review.ReviewId, review);
        }
    }

    /// <summary>
    /// Seeds credential verification data for local development.
    /// </summary>
    internal void SeedVerifications(IEnumerable<GdpCredentialVerification> verifications)
    {
        foreach (var verification in verifications)
        {
            _verifications.TryAdd(verification.VerificationId, verification);
        }
    }

    #endregion

    #region Private Helpers

    private static GdpServiceProvider CloneProvider(GdpServiceProvider source) => new()
    {
        ProviderId = source.ProviderId,
        ProviderName = source.ProviderName,
        ServiceType = source.ServiceType,
        TemperatureControlledCapability = source.TemperatureControlledCapability,
        ApprovedRoutes = source.ApprovedRoutes,
        QualificationStatus = source.QualificationStatus,
        ReviewFrequencyMonths = source.ReviewFrequencyMonths,
        LastReviewDate = source.LastReviewDate,
        NextReviewDate = source.NextReviewDate,
        IsActive = source.IsActive,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate
    };

    private static GdpCredential CloneCredential(GdpCredential source) => new()
    {
        CredentialId = source.CredentialId,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        WdaNumber = source.WdaNumber,
        GdpCertificateNumber = source.GdpCertificateNumber,
        EudraGmdpEntryUrl = source.EudraGmdpEntryUrl,
        ValidityStartDate = source.ValidityStartDate,
        ValidityEndDate = source.ValidityEndDate,
        QualificationStatus = source.QualificationStatus,
        LastVerificationDate = source.LastVerificationDate,
        NextReviewDate = source.NextReviewDate,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate,
        RowVersion = source.RowVersion.ToArray()
    };

    private static QualificationReview CloneReview(QualificationReview source) => new()
    {
        ReviewId = source.ReviewId,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        ReviewDate = source.ReviewDate,
        ReviewMethod = source.ReviewMethod,
        ReviewOutcome = source.ReviewOutcome,
        ReviewerName = source.ReviewerName,
        Notes = source.Notes,
        NextReviewDate = source.NextReviewDate
    };

    private static GdpCredentialVerification CloneVerification(GdpCredentialVerification source) => new()
    {
        VerificationId = source.VerificationId,
        CredentialId = source.CredentialId,
        VerificationDate = source.VerificationDate,
        VerificationMethod = source.VerificationMethod,
        VerifiedBy = source.VerifiedBy,
        Outcome = source.Outcome,
        Notes = source.Notes
    };

    #endregion
}
