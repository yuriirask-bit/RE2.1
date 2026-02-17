using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.GdpCompliance;

/// <summary>
/// Service for GDP compliance business logic.
/// T190, T194: GDP site management including WDA coverage validation per FR-033.
/// </summary>
public class GdpComplianceService : IGdpComplianceService
{
    private readonly IGdpSiteRepository _gdpSiteRepository;
    private readonly IGdpCredentialRepository _gdpCredentialRepository;
    private readonly IGdpInspectionRepository _gdpInspectionRepository;
    private readonly ICapaRepository _capaRepository;
    private readonly IGdpDocumentRepository _gdpDocumentRepository;
    private readonly IDocumentStorage _documentStorage;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILogger<GdpComplianceService> _logger;

    private const string WdaLicenceTypeName = "Wholesale Distribution Authorisation (WDA)";
    private const string GdpDocumentContainerName = "gdp-documents";
    private static readonly TimeSpan DefaultSasExpiry = TimeSpan.FromMinutes(15);

    public GdpComplianceService(
        IGdpSiteRepository gdpSiteRepository,
        IGdpCredentialRepository gdpCredentialRepository,
        IGdpInspectionRepository gdpInspectionRepository,
        ICapaRepository capaRepository,
        IGdpDocumentRepository gdpDocumentRepository,
        IDocumentStorage documentStorage,
        ILicenceRepository licenceRepository,
        ILicenceTypeRepository licenceTypeRepository,
        ILogger<GdpComplianceService> logger)
    {
        _gdpSiteRepository = gdpSiteRepository;
        _gdpCredentialRepository = gdpCredentialRepository;
        _gdpInspectionRepository = gdpInspectionRepository;
        _capaRepository = capaRepository;
        _gdpDocumentRepository = gdpDocumentRepository;
        _documentStorage = documentStorage;
        _licenceRepository = licenceRepository;
        _licenceTypeRepository = licenceTypeRepository;
        _logger = logger;
    }

    #region D365FO Warehouse Browsing

    public async Task<IEnumerable<GdpSite>> GetAllWarehousesAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetAllWarehousesAsync(cancellationToken);
    }

    public async Task<GdpSite?> GetWarehouseAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetWarehouseAsync(warehouseId, dataAreaId, cancellationToken);
    }

    #endregion

    #region GDP-Configured Sites

    public async Task<IEnumerable<GdpSite>> GetAllGdpSitesAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetAllGdpConfiguredSitesAsync(cancellationToken);
    }

    public async Task<GdpSite?> GetGdpSiteAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
    }

    #endregion

    #region GDP Configuration

    public async Task<(Guid? Id, ValidationResult Result)> ConfigureGdpAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        // Validate the site configuration
        var validationResult = site.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify warehouse exists in D365FO
        var warehouse = await _gdpSiteRepository.GetWarehouseAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (warehouse == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Warehouse '{site.WarehouseId}' not found in D365 F&O for data area '{site.DataAreaId}'"
                }
            }));
        }

        // Check if already configured
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Warehouse '{site.WarehouseId}' is already configured for GDP"
                }
            }));
        }

        var id = await _gdpSiteRepository.SaveGdpExtensionAsync(site, cancellationToken);
        _logger.LogInformation("Configured GDP for warehouse {WarehouseId} with type {GdpSiteType}", site.WarehouseId, site.GdpSiteType);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateGdpConfigAsync(GdpSite site, CancellationToken cancellationToken = default)
    {
        // Validate the site configuration
        var validationResult = site.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Verify GDP extension exists
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(site.WarehouseId, site.DataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP configuration not found for warehouse '{site.WarehouseId}'"
                }
            });
        }

        // Preserve the extension ID
        site.GdpExtensionId = existing.GdpExtensionId;

        await _gdpSiteRepository.UpdateGdpExtensionAsync(site, cancellationToken);
        _logger.LogInformation("Updated GDP configuration for warehouse {WarehouseId}", site.WarehouseId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> RemoveGdpConfigAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        // Verify GDP extension exists
        var existing = await _gdpSiteRepository.GetGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP configuration not found for warehouse '{warehouseId}'"
                }
            });
        }

        await _gdpSiteRepository.DeleteGdpExtensionAsync(warehouseId, dataAreaId, cancellationToken);
        _logger.LogInformation("Removed GDP configuration for warehouse {WarehouseId}", warehouseId);

        return ValidationResult.Success();
    }

    #endregion

    #region GDP Service Providers (T205)

    public async Task<IEnumerable<GdpServiceProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetAllProvidersAsync(cancellationToken);
    }

    public async Task<GdpServiceProvider?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetProviderAsync(providerId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var validationResult = provider.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        var id = await _gdpCredentialRepository.CreateProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Created GDP service provider {ProviderId} ({Name})", id, provider.ProviderName);
        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var validationResult = provider.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var existing = await _gdpCredentialRepository.GetProviderAsync(provider.ProviderId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP service provider '{provider.ProviderId}' not found"
                }
            });
        }

        await _gdpCredentialRepository.UpdateProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Updated GDP service provider {ProviderId}", provider.ProviderId);
        return ValidationResult.Success();
    }

    public async Task<ValidationResult> DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var existing = await _gdpCredentialRepository.GetProviderAsync(providerId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP service provider '{providerId}' not found"
                }
            });
        }

        await _gdpCredentialRepository.DeleteProviderAsync(providerId, cancellationToken);
        _logger.LogInformation("Deleted GDP service provider {ProviderId}", providerId);
        return ValidationResult.Success();
    }

    #endregion

    #region GDP Credentials (T205)

    public async Task<IEnumerable<GdpCredential>> GetCredentialsByEntityAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetCredentialsByEntityAsync(entityType, entityId, cancellationToken);
    }

    public async Task<GdpCredential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetCredentialAsync(credentialId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        var validationResult = credential.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        var id = await _gdpCredentialRepository.CreateCredentialAsync(credential, cancellationToken);
        _logger.LogInformation("Created GDP credential {CredentialId} for {EntityType} {EntityId}", id, credential.EntityType, credential.EntityId);
        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        var validationResult = credential.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var existing = await _gdpCredentialRepository.GetCredentialAsync(credential.CredentialId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP credential '{credential.CredentialId}' not found"
                }
            });
        }

        await _gdpCredentialRepository.UpdateCredentialAsync(credential, cancellationToken);
        _logger.LogInformation("Updated GDP credential {CredentialId}", credential.CredentialId);
        return ValidationResult.Success();
    }

    public async Task<ValidationResult> DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var existing = await _gdpCredentialRepository.GetCredentialAsync(credentialId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP credential '{credentialId}' not found"
                }
            });
        }

        await _gdpCredentialRepository.DeleteCredentialAsync(credentialId, cancellationToken);
        _logger.LogInformation("Deleted GDP credential {CredentialId}", credentialId);
        return ValidationResult.Success();
    }

    #endregion

    #region Qualification Reviews (T205)

    public async Task<IEnumerable<QualificationReview>> GetReviewsByEntityAsync(ReviewEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetReviewsByEntityAsync(entityType, entityId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> RecordReviewAsync(QualificationReview review, CancellationToken cancellationToken = default)
    {
        var validationResult = review.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Update the reviewed entity's qualification status based on outcome
        if (review.EntityType == ReviewEntityType.ServiceProvider)
        {
            var provider = await _gdpCredentialRepository.GetProviderAsync(review.EntityId, cancellationToken);
            if (provider == null)
            {
                return (null, ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.NOT_FOUND,
                        Message = $"Service provider '{review.EntityId}' not found"
                    }
                }));
            }

            // Map review outcome to qualification status
            provider.QualificationStatus = review.ReviewOutcome switch
            {
                ReviewOutcome.Approved => GdpQualificationStatus.Approved,
                ReviewOutcome.ConditionallyApproved => GdpQualificationStatus.ConditionallyApproved,
                ReviewOutcome.Rejected => GdpQualificationStatus.Rejected,
                _ => provider.QualificationStatus
            };
            provider.LastReviewDate = review.ReviewDate;
            if (review.NextReviewDate.HasValue)
            {
                provider.NextReviewDate = review.NextReviewDate;
            }

            await _gdpCredentialRepository.UpdateProviderAsync(provider, cancellationToken);
        }

        var id = await _gdpCredentialRepository.CreateReviewAsync(review, cancellationToken);
        _logger.LogInformation("Recorded qualification review {ReviewId} for {EntityType} {EntityId} with outcome {Outcome}",
            id, review.EntityType, review.EntityId, review.ReviewOutcome);
        return (id, ValidationResult.Success());
    }

    #endregion

    #region Credential Verifications (T205)

    public async Task<IEnumerable<GdpCredentialVerification>> GetVerificationsByCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        return await _gdpCredentialRepository.GetVerificationsByCredentialAsync(credentialId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> RecordVerificationAsync(GdpCredentialVerification verification, CancellationToken cancellationToken = default)
    {
        var validationResult = verification.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify the credential exists
        var credential = await _gdpCredentialRepository.GetCredentialAsync(verification.CredentialId, cancellationToken);
        if (credential == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP credential '{verification.CredentialId}' not found"
                }
            }));
        }

        // Update the credential's last verification date
        credential.LastVerificationDate = verification.VerificationDate;
        await _gdpCredentialRepository.UpdateCredentialAsync(credential, cancellationToken);

        var id = await _gdpCredentialRepository.CreateVerificationAsync(verification, cancellationToken);
        _logger.LogInformation("Recorded verification {VerificationId} for credential {CredentialId} with outcome {Outcome}",
            id, verification.CredentialId, verification.Outcome);
        return (id, ValidationResult.Success());
    }

    #endregion

    #region Partner Qualification Checks (T205)

    public async Task<bool> IsPartnerQualifiedAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var credentials = await _gdpCredentialRepository.GetCredentialsByEntityAsync(entityType, entityId, cancellationToken);
        return credentials.Any(c => c.IsApproved() && c.IsValid());
    }

    public async Task<IEnumerable<GdpServiceProvider>> GetProvidersRequiringReviewAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _gdpCredentialRepository.GetProvidersRequiringReviewAsync(today, cancellationToken);
    }

    public async Task<IEnumerable<GdpCredential>> GetCredentialsExpiringAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var beforeDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysAhead);
        return await _gdpCredentialRepository.GetCredentialsExpiringBeforeAsync(beforeDate, cancellationToken);
    }

    #endregion

    #region GDP Inspections (T224)

    public async Task<IEnumerable<GdpInspection>> GetAllInspectionsAsync(CancellationToken cancellationToken = default)
    {
        return await _gdpInspectionRepository.GetAllAsync(cancellationToken);
    }

    public async Task<GdpInspection?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        return await _gdpInspectionRepository.GetByIdAsync(inspectionId, cancellationToken);
    }

    public async Task<IEnumerable<GdpInspection>> GetInspectionsBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        return await _gdpInspectionRepository.GetBySiteAsync(siteId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateInspectionAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        var validationResult = inspection.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        var id = await _gdpInspectionRepository.CreateAsync(inspection, cancellationToken);
        _logger.LogInformation("Created GDP inspection {InspectionId} for site {SiteId}", id, inspection.SiteId);
        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateInspectionAsync(GdpInspection inspection, CancellationToken cancellationToken = default)
    {
        var validationResult = inspection.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var existing = await _gdpInspectionRepository.GetByIdAsync(inspection.InspectionId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP inspection '{inspection.InspectionId}' not found"
                }
            });
        }

        await _gdpInspectionRepository.UpdateAsync(inspection, cancellationToken);
        _logger.LogInformation("Updated GDP inspection {InspectionId}", inspection.InspectionId);
        return ValidationResult.Success();
    }

    public async Task<IEnumerable<GdpInspectionFinding>> GetFindingsAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        return await _gdpInspectionRepository.GetFindingsAsync(inspectionId, cancellationToken);
    }

    public async Task<GdpInspectionFinding?> GetFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        return await _gdpInspectionRepository.GetFindingByIdAsync(findingId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        var validationResult = finding.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify the parent inspection exists
        var inspection = await _gdpInspectionRepository.GetByIdAsync(finding.InspectionId, cancellationToken);
        if (inspection == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP inspection '{finding.InspectionId}' not found"
                }
            }));
        }

        var id = await _gdpInspectionRepository.CreateFindingAsync(finding, cancellationToken);
        _logger.LogInformation("Created finding {FindingId} for inspection {InspectionId}", id, finding.InspectionId);
        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateFindingAsync(GdpInspectionFinding finding, CancellationToken cancellationToken = default)
    {
        var validationResult = finding.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var existing = await _gdpInspectionRepository.GetFindingByIdAsync(finding.FindingId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Finding '{finding.FindingId}' not found"
                }
            });
        }

        await _gdpInspectionRepository.UpdateFindingAsync(finding, cancellationToken);
        _logger.LogInformation("Updated finding {FindingId}", finding.FindingId);
        return ValidationResult.Success();
    }

    public async Task<ValidationResult> DeleteFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        var existing = await _gdpInspectionRepository.GetFindingByIdAsync(findingId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Finding '{findingId}' not found"
                }
            });
        }

        await _gdpInspectionRepository.DeleteFindingAsync(findingId, cancellationToken);
        _logger.LogInformation("Deleted finding {FindingId}", findingId);
        return ValidationResult.Success();
    }

    #endregion

    #region CAPAs (T224)

    public async Task<IEnumerable<Capa>> GetAllCapasAsync(CancellationToken cancellationToken = default)
    {
        return await _capaRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Capa?> GetCapaAsync(Guid capaId, CancellationToken cancellationToken = default)
    {
        return await _capaRepository.GetByIdAsync(capaId, cancellationToken);
    }

    public async Task<IEnumerable<Capa>> GetCapasByFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        return await _capaRepository.GetByFindingAsync(findingId, cancellationToken);
    }

    public async Task<IEnumerable<Capa>> GetOverdueCapasAsync(CancellationToken cancellationToken = default)
    {
        return await _capaRepository.GetOverdueAsync(cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateCapaAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        var validationResult = capa.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify the linked finding exists
        var finding = await _gdpInspectionRepository.GetFindingByIdAsync(capa.FindingId, cancellationToken);
        if (finding == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Finding '{capa.FindingId}' not found"
                }
            }));
        }

        var id = await _capaRepository.CreateAsync(capa, cancellationToken);
        _logger.LogInformation("Created CAPA {CapaId} ({CapaNumber}) for finding {FindingId}", id, capa.CapaNumber, capa.FindingId);
        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateCapaAsync(Capa capa, CancellationToken cancellationToken = default)
    {
        var validationResult = capa.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var existing = await _capaRepository.GetByIdAsync(capa.CapaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"CAPA '{capa.CapaId}' not found"
                }
            });
        }

        await _capaRepository.UpdateAsync(capa, cancellationToken);
        _logger.LogInformation("Updated CAPA {CapaId}", capa.CapaId);
        return ValidationResult.Success();
    }

    public async Task<ValidationResult> CompleteCapaAsync(Guid capaId, DateOnly completionDate, string? verificationNotes = null, CancellationToken cancellationToken = default)
    {
        var capa = await _capaRepository.GetByIdAsync(capaId, cancellationToken);
        if (capa == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"CAPA '{capaId}' not found"
                }
            });
        }

        capa.Complete(completionDate, verificationNotes);
        await _capaRepository.UpdateAsync(capa, cancellationToken);
        _logger.LogInformation("Completed CAPA {CapaId} ({CapaNumber}) on {CompletionDate}", capaId, capa.CapaNumber, completionDate);
        return ValidationResult.Success();
    }

    #endregion

    #region GDP Documents (T239)

    public async Task<IEnumerable<GdpDocument>> GetDocumentsByEntityAsync(GdpDocumentEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _gdpDocumentRepository.GetDocumentsByEntityAsync(entityType, entityId, cancellationToken);
    }

    public async Task<GdpDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _gdpDocumentRepository.GetDocumentAsync(documentId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> UploadDocumentAsync(GdpDocument document, Stream content, CancellationToken cancellationToken = default)
    {
        // Validate document
        var validationResult = document.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Generate blob name: {entityType}/{entityId}/{documentId}/{filename}
        if (document.DocumentId == Guid.Empty)
        {
            document.DocumentId = Guid.NewGuid();
        }

        var entityTypeName = document.OwnerEntityType.ToString().ToLowerInvariant();
        var blobName = $"{entityTypeName}/{document.OwnerEntityId}/{document.DocumentId}/{document.FileName}";

        try
        {
            // Upload to blob storage
            var metadata = new Dictionary<string, string>
            {
                { "ownerEntityType", document.OwnerEntityType.ToString() },
                { "ownerEntityId", document.OwnerEntityId.ToString() },
                { "documentType", document.DocumentType.ToString() },
                { "uploadedBy", document.UploadedBy }
            };

            var blobUri = await _documentStorage.UploadDocumentAsync(
                GdpDocumentContainerName,
                blobName,
                content,
                document.ContentType ?? "application/octet-stream",
                metadata,
                cancellationToken);

            // Update document with blob URL and upload timestamp
            document.BlobStorageUrl = blobUri.ToString();
            document.UploadedDate = DateTime.UtcNow;

            // Save document metadata to repository
            var id = await _gdpDocumentRepository.CreateDocumentAsync(document, cancellationToken);
            _logger.LogInformation("Uploaded GDP document {DocumentId} for {EntityType} {EntityId}",
                id, document.OwnerEntityType, document.OwnerEntityId);

            return (id, ValidationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading GDP document for {EntityType} {EntityId}",
                document.OwnerEntityType, document.OwnerEntityId);
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to upload document"
                }
            }));
        }
    }

    public async Task<(Uri? Url, ValidationResult Result)> GetDocumentDownloadUrlAsync(Guid documentId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        var document = await _gdpDocumentRepository.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP document '{documentId}' not found"
                }
            }));
        }

        try
        {
            var entityTypeName = document.OwnerEntityType.ToString().ToLowerInvariant();
            var blobName = $"{entityTypeName}/{document.OwnerEntityId}/{document.DocumentId}/{document.FileName}";
            var sasUri = await _documentStorage.GetDocumentSasUriAsync(
                GdpDocumentContainerName,
                blobName,
                expiresIn ?? DefaultSasExpiry,
                cancellationToken);

            return (sasUri, ValidationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for GDP document {DocumentId}", documentId);
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to generate download URL"
                }
            }));
        }
    }

    public async Task<ValidationResult> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _gdpDocumentRepository.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"GDP document '{documentId}' not found"
                }
            });
        }

        try
        {
            // Delete from blob storage
            if (!string.IsNullOrEmpty(document.BlobStorageUrl))
            {
                var entityTypeName = document.OwnerEntityType.ToString().ToLowerInvariant();
                var blobName = $"{entityTypeName}/{document.OwnerEntityId}/{document.DocumentId}/{document.FileName}";
                await _documentStorage.DeleteDocumentAsync(GdpDocumentContainerName, blobName, cancellationToken);
            }

            // Delete metadata from repository
            await _gdpDocumentRepository.DeleteDocumentAsync(documentId, cancellationToken);
            _logger.LogInformation("Deleted GDP document {DocumentId} from {EntityType} {EntityId}",
                documentId, document.OwnerEntityType, document.OwnerEntityId);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP document {DocumentId}", documentId);
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = "Failed to delete document"
                }
            });
        }
    }

    #endregion

    #region WDA Coverage

    public async Task<IEnumerable<GdpSiteWdaCoverage>> GetWdaCoverageAsync(string warehouseId, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _gdpSiteRepository.GetWdaCoverageAsync(warehouseId, dataAreaId, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> AddWdaCoverageAsync(GdpSiteWdaCoverage coverage, CancellationToken cancellationToken = default)
    {
        // Validate coverage model
        var validationResult = coverage.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify the licence exists
        var licence = await _licenceRepository.GetByIdAsync(coverage.LicenceId, cancellationToken);
        if (licence == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{coverage.LicenceId}' not found"
                }
            }));
        }

        // FR-033: Verify licence is WDA type
        var licenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        if (licenceType == null || licenceType.Name != WdaLicenceTypeName)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Licence '{licence.LicenceNumber}' is not a Wholesale Distribution Authorisation (WDA). " +
                             $"Only WDA licences can be used for GDP site coverage."
                }
            }));
        }

        // Verify GDP configuration exists for this warehouse
        var gdpSite = await _gdpSiteRepository.GetGdpExtensionAsync(coverage.WarehouseId, coverage.DataAreaId, cancellationToken);
        if (gdpSite == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Warehouse '{coverage.WarehouseId}' is not configured for GDP. Configure GDP first."
                }
            }));
        }

        var id = await _gdpSiteRepository.AddWdaCoverageAsync(coverage, cancellationToken);
        _logger.LogInformation("Added WDA coverage {CoverageId} for warehouse {WarehouseId} with licence {LicenceNumber}",
            id, coverage.WarehouseId, licence.LicenceNumber);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> RemoveWdaCoverageAsync(Guid coverageId, CancellationToken cancellationToken = default)
    {
        await _gdpSiteRepository.DeleteWdaCoverageAsync(coverageId, cancellationToken);
        _logger.LogInformation("Removed WDA coverage {CoverageId}", coverageId);

        return ValidationResult.Success();
    }

    #endregion
}
