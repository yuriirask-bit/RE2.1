using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Interface for licence management service.
/// T059: Enables unit testing of controllers through dependency injection.
/// </summary>
public interface ILicenceService
{
    /// <summary>
    /// Gets a licence by ID with LicenceType populated.
    /// </summary>
    Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a licence by licence number with LicenceType populated.
    /// </summary>
    Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all licences for a holder (Company or Customer) with LicenceType populated.
    /// </summary>
    Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all licences expiring within specified days with LicenceType populated.
    /// Per FR-007: Generate alerts for licences expiring within configurable period (default: 90 days).
    /// </summary>
    Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all licences with LicenceType populated.
    /// </summary>
    Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new licence after validation.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateAsync(Licence licence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing licence after validation.
    /// </summary>
    Task<ValidationResult> UpdateAsync(Licence licence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a licence.
    /// </summary>
    Task<ValidationResult> DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a licence meets business rules.
    /// Per data-model.md validation rules.
    /// </summary>
    Task<ValidationResult> ValidateLicenceAsync(Licence licence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a holder has a valid licence for a specific activity.
    /// Per FR-018: Verify customer holds all required valid licences for each controlled product.
    /// </summary>
    Task<ValidationResult> CheckHolderLicenceForActivityAsync(
        Guid holderId,
        string holderType,
        LicenceTypes.PermittedActivity requiredActivity,
        CancellationToken cancellationToken = default);

    #region Document Operations (T112)

    /// <summary>
    /// Gets all documents for a licence.
    /// T112: Document retrieval for licence evidence management per FR-008.
    /// </summary>
    Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID.
    /// T112: Document retrieval for download.
    /// </summary>
    Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a document for a licence.
    /// T112: Document upload with blob storage integration per FR-008.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> UploadDocumentAsync(
        Guid licenceId,
        LicenceDocument document,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from a licence.
    /// T112: Document removal including blob storage cleanup.
    /// </summary>
    Task<ValidationResult> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion

    #region Verification Operations (T112)

    /// <summary>
    /// Gets verification history for a licence.
    /// T112: Verification audit trail per FR-009.
    /// </summary>
    Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent verification for a licence.
    /// T112: Quick access to current verification status.
    /// </summary>
    Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a verification for a licence.
    /// T112: Verification recording per FR-009 (method, date, verifier, outcome).
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> RecordVerificationAsync(
        LicenceVerification verification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Scope Change Operations (T113)

    /// <summary>
    /// Gets scope change history for a licence.
    /// T113: Scope change audit trail per FR-010.
    /// </summary>
    Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a scope change for a licence.
    /// T113: Scope change recording per FR-010 with effective dates.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> RecordScopeChangeAsync(
        LicenceScopeChange scopeChange,
        CancellationToken cancellationToken = default);

    #endregion
}
