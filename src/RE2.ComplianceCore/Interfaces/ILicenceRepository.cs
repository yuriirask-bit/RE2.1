using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Licence entity operations.
/// T068: Repository interface for licence CRUD operations.
/// T108b, T110: Extended with document, verification, and scope change methods for US3.
/// </summary>
public interface ILicenceRepository
{
    #region Core Licence Operations

    Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default);
    Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default);
    Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid licences that cover a specific substance.
    /// T080g: Used for reclassification customer impact analysis per FR-066.
    /// </summary>
    Task<IEnumerable<Licence>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default);

    #endregion

    #region Document Operations (T110)

    /// <summary>
    /// Gets all documents associated with a licence.
    /// T110: Document retrieval for licence evidence management per FR-008.
    /// </summary>
    Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific document by ID.
    /// T110: Document retrieval for download and viewing.
    /// </summary>
    Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a document record to a licence.
    /// T110: Document upload metadata storage (file stored via IDocumentStorage).
    /// </summary>
    Task<Guid> AddDocumentAsync(LicenceDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document record from a licence.
    /// T110: Document removal (file deletion handled separately via IDocumentStorage).
    /// </summary>
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion

    #region Verification Operations (T108b)

    /// <summary>
    /// Gets verification history for a licence.
    /// T108b: Verification history retrieval per FR-009 for audit trail.
    /// </summary>
    Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent verification for a licence.
    /// T108b: Quick access to current verification status.
    /// </summary>
    Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a verification record to a licence.
    /// T108b: Record verification per FR-009 (method, date, verifier, outcome).
    /// </summary>
    Task<Guid> AddVerificationAsync(LicenceVerification verification, CancellationToken cancellationToken = default);

    #endregion

    #region Scope Change Operations

    /// <summary>
    /// Gets scope change history for a licence.
    /// Per data-model.md entity 14: Track changes to authorized substances and activities.
    /// </summary>
    Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a scope change record to a licence.
    /// Per FR-010: Record scope changes with effective dates for audit trail.
    /// </summary>
    Task<Guid> AddScopeChangeAsync(LicenceScopeChange scopeChange, CancellationToken cancellationToken = default);

    #endregion
}
