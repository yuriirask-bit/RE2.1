using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for GDP credential and service provider operations.
/// T203: Combined CRUD for GdpServiceProvider, GdpCredential, QualificationReview, GdpCredentialVerification.
/// Per User Story 8 (FR-036, FR-037, FR-038, FR-039).
/// </summary>
public interface IGdpCredentialRepository
{
    #region GdpServiceProvider Operations

    /// <summary>
    /// Gets all GDP service providers.
    /// </summary>
    Task<IEnumerable<GdpServiceProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP service provider by ID.
    /// </summary>
    Task<GdpServiceProvider?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP service provider.
    /// </summary>
    Task<Guid> CreateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP service provider.
    /// </summary>
    Task UpdateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP service provider.
    /// </summary>
    Task DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets providers that need re-qualification review (NextReviewDate before specified date).
    /// </summary>
    Task<IEnumerable<GdpServiceProvider>> GetProvidersRequiringReviewAsync(DateOnly beforeDate, CancellationToken cancellationToken = default);

    #endregion

    #region GdpCredential Operations

    /// <summary>
    /// Gets all GDP credentials for an entity (Customer or ServiceProvider).
    /// </summary>
    Task<IEnumerable<GdpCredential>> GetCredentialsByEntityAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP credential by ID.
    /// </summary>
    Task<GdpCredential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP credential.
    /// </summary>
    Task<Guid> CreateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP credential.
    /// </summary>
    Task UpdateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP credential.
    /// </summary>
    Task DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credentials expiring before specified date.
    /// </summary>
    Task<IEnumerable<GdpCredential>> GetCredentialsExpiringBeforeAsync(DateOnly beforeDate, CancellationToken cancellationToken = default);

    #endregion

    #region QualificationReview Operations

    /// <summary>
    /// Gets qualification reviews for an entity.
    /// </summary>
    Task<IEnumerable<QualificationReview>> GetReviewsByEntityAsync(ReviewEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new qualification review.
    /// </summary>
    Task<Guid> CreateReviewAsync(QualificationReview review, CancellationToken cancellationToken = default);

    #endregion

    #region GdpCredentialVerification Operations

    /// <summary>
    /// Gets verifications for a credential.
    /// </summary>
    Task<IEnumerable<GdpCredentialVerification>> GetVerificationsByCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new credential verification.
    /// </summary>
    Task<Guid> CreateVerificationAsync(GdpCredentialVerification verification, CancellationToken cancellationToken = default);

    #endregion
}
