using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for GDP compliance operations.
/// T190: Business logic for GDP site management per User Story 7.
/// </summary>
public interface IGdpComplianceService
{
    #region D365FO Warehouse Browsing

    /// <summary>
    /// Gets all D365FO warehouses for browsing.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific D365FO warehouse.
    /// </summary>
    Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP-Configured Sites

    /// <summary>
    /// Gets all warehouses that have been configured for GDP.
    /// </summary>
    Task<IEnumerable<GdpSite>> GetAllGdpSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP-configured site.
    /// </summary>
    Task<GdpSite?> GetGdpSiteAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Configuration

    /// <summary>
    /// Configures GDP compliance for a D365FO warehouse.
    /// Validates GdpSiteType + PermittedActivities, verifies warehouse exists in D365FO.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> ConfigureGdpAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the GDP configuration for a warehouse.
    /// </summary>
    Task<ValidationResult> UpdateGdpConfigAsync(GdpSite site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the GDP configuration from a warehouse.
    /// </summary>
    Task<ValidationResult> RemoveGdpConfigAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Service Providers (T205)

    /// <summary>
    /// Gets all GDP service providers.
    /// </summary>
    Task<IEnumerable<GdpServiceProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP service provider by ID.
    /// </summary>
    Task<GdpServiceProvider?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP service provider with validation.
    /// Per FR-036.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP service provider.
    /// </summary>
    Task<ValidationResult> UpdateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP service provider.
    /// </summary>
    Task<ValidationResult> DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Credentials (T205)

    /// <summary>
    /// Gets GDP credentials for an entity (Customer or ServiceProvider).
    /// Per FR-037.
    /// </summary>
    Task<IEnumerable<GdpCredential>> GetCredentialsByEntityAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP credential by ID.
    /// </summary>
    Task<GdpCredential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP credential with validation.
    /// Per FR-037.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP credential.
    /// </summary>
    Task<ValidationResult> UpdateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP credential.
    /// </summary>
    Task<ValidationResult> DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    #endregion

    #region Qualification Reviews (T205)

    /// <summary>
    /// Gets qualification reviews for an entity.
    /// Per FR-038.
    /// </summary>
    Task<IEnumerable<QualificationReview>> GetReviewsByEntityAsync(ReviewEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a qualification review and updates the entity's qualification status.
    /// Per FR-038.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> RecordReviewAsync(QualificationReview review, CancellationToken cancellationToken = default);

    #endregion

    #region Credential Verifications (T205)

    /// <summary>
    /// Gets verifications for a credential.
    /// </summary>
    Task<IEnumerable<GdpCredentialVerification>> GetVerificationsByCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a credential verification (e.g., EudraGMDP check).
    /// Per FR-045.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> RecordVerificationAsync(GdpCredentialVerification verification, CancellationToken cancellationToken = default);

    #endregion

    #region Partner Qualification Checks (T205)

    /// <summary>
    /// Checks whether a partner (Customer or ServiceProvider) is GDP-qualified for transactions.
    /// Per FR-038: Only approved/conditionally-approved partners can be selected.
    /// </summary>
    Task<bool> IsPartnerQualifiedAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets providers needing re-qualification review.
    /// Per FR-039.
    /// </summary>
    Task<IEnumerable<GdpServiceProvider>> GetProvidersRequiringReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credentials expiring within the specified days.
    /// </summary>
    Task<IEnumerable<GdpCredential>> GetCredentialsExpiringAsync(int daysAhead = 90, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Inspections (T224)

    /// <summary>
    /// Gets all GDP inspections.
    /// Per FR-040.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetAllInspectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP inspection by ID.
    /// </summary>
    Task<GdpInspection?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inspections for a specific GDP site.
    /// </summary>
    Task<IEnumerable<GdpInspection>> GetInspectionsBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GDP inspection with validation.
    /// Per FR-040.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateInspectionAsync(GdpInspection inspection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing GDP inspection.
    /// </summary>
    Task<ValidationResult> UpdateInspectionAsync(GdpInspection inspection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets findings for an inspection.
    /// </summary>
    Task<IEnumerable<GdpInspectionFinding>> GetFindingsAsync(Guid inspectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific finding by ID.
    /// </summary>
    Task<GdpInspectionFinding?> GetFindingAsync(Guid findingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a finding for an inspection with validation.
    /// Per FR-040.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing finding.
    /// </summary>
    Task<ValidationResult> UpdateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a finding.
    /// </summary>
    Task<ValidationResult> DeleteFindingAsync(Guid findingId, CancellationToken cancellationToken = default);

    #endregion

    #region CAPAs (T224)

    /// <summary>
    /// Gets all CAPAs.
    /// Per FR-041.
    /// </summary>
    Task<IEnumerable<Capa>> GetAllCapasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific CAPA by ID.
    /// </summary>
    Task<Capa?> GetCapaAsync(Guid capaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets CAPAs for a specific finding.
    /// </summary>
    Task<IEnumerable<Capa>> GetCapasByFindingAsync(Guid findingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue CAPAs.
    /// Per FR-042.
    /// </summary>
    Task<IEnumerable<Capa>> GetOverdueCapasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new CAPA with validation.
    /// Per FR-041.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateCapaAsync(Capa capa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing CAPA.
    /// </summary>
    Task<ValidationResult> UpdateCapaAsync(Capa capa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a CAPA with verification notes.
    /// Per FR-041.
    /// </summary>
    Task<ValidationResult> CompleteCapaAsync(Guid capaId, DateOnly completionDate, string? verificationNotes = null, CancellationToken cancellationToken = default);

    #endregion

    #region GDP Documents (T238)

    /// <summary>
    /// Gets all documents for a specific GDP entity.
    /// Per FR-044.
    /// </summary>
    Task<IEnumerable<GdpDocument>> GetDocumentsByEntityAsync(GdpDocumentEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific GDP document by ID.
    /// </summary>
    Task<GdpDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a GDP document with blob storage integration.
    /// Per FR-044.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> UploadDocumentAsync(GdpDocument document, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary SAS download URL for a GDP document.
    /// </summary>
    Task<(Uri? Url, ValidationResult Result)> GetDocumentDownloadUrlAsync(Guid documentId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GDP document from blob storage and metadata store.
    /// </summary>
    Task<ValidationResult> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion

    #region WDA Coverage

    /// <summary>
    /// Gets WDA coverage records for a warehouse.
    /// </summary>
    Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds WDA coverage for a warehouse.
    /// FR-033: Validates that LicenceId references a WDA-type licence.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a WDA coverage record.
    /// </summary>
    Task<ValidationResult> RemoveWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default);

    #endregion
}
