using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.LicenceValidation;

/// <summary>
/// Service for licence management business logic.
/// T074: Business logic for licence management including validation, expiry checking, and CRUD operations.
/// T112-T113: Extended with document, verification, and scope change operations for US3.
/// </summary>
public class LicenceService : ILicenceService
{
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<LicenceService> _logger;

    private const string DocumentContainerName = "licence-documents";

    public LicenceService(
        ILicenceRepository licenceRepository,
        ILicenceTypeRepository licenceTypeRepository,
        IControlledSubstanceRepository substanceRepository,
        IDocumentStorage documentStorage,
        ILogger<LicenceService> logger)
    {
        _licenceRepository = licenceRepository;
        _licenceTypeRepository = licenceTypeRepository;
        _substanceRepository = substanceRepository;
        _documentStorage = documentStorage;
        _logger = logger;
    }

    /// <summary>
    /// Gets a licence by ID with LicenceType populated.
    /// </summary>
    public async Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceRepository.GetByIdAsync(licenceId, cancellationToken);
        if (licence != null)
        {
            licence.UpdateStatus(); // Auto-update expired status
            await PopulateLicenceTypeAsync(licence, cancellationToken);
        }
        return licence;
    }

    /// <summary>
    /// Populates the LicenceType navigation property for a licence.
    /// </summary>
    private async Task PopulateLicenceTypeAsync(Licence licence, CancellationToken cancellationToken)
    {
        if (licence.LicenceType == null && licence.LicenceTypeId != Guid.Empty)
        {
            licence.LicenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        }
    }

    /// <summary>
    /// Populates the LicenceType navigation property for multiple licences.
    /// </summary>
    private async Task PopulateLicenceTypesAsync(IEnumerable<Licence> licences, CancellationToken cancellationToken)
    {
        // Get all unique licence type IDs
        var typeIds = licences.Select(l => l.LicenceTypeId).Distinct().ToList();

        // Load all licence types in one batch
        var allTypes = await _licenceTypeRepository.GetAllAsync(cancellationToken);
        var typesDict = allTypes.ToDictionary(t => t.LicenceTypeId);

        // Populate navigation properties
        foreach (var licence in licences)
        {
            if (typesDict.TryGetValue(licence.LicenceTypeId, out var licenceType))
            {
                licence.LicenceType = licenceType;
            }
        }
    }

    /// <summary>
    /// Gets a licence by licence number with LicenceType populated.
    /// </summary>
    public async Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceRepository.GetByLicenceNumberAsync(licenceNumber, cancellationToken);
        if (licence != null)
        {
            licence.UpdateStatus();
            await PopulateLicenceTypeAsync(licence, cancellationToken);
        }
        return licence;
    }

    /// <summary>
    /// Gets all licences for a holder (Company or Customer) with LicenceType populated.
    /// </summary>
    public async Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetByHolderAsync(holderId, holderType, cancellationToken)).ToList();
        foreach (var licence in licences)
        {
            licence.UpdateStatus();
        }
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Gets all licences expiring within specified days with LicenceType populated.
    /// Per FR-007: Generate alerts for licences expiring within configurable period (default: 90 days).
    /// </summary>
    public async Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetExpiringLicencesAsync(daysAhead, cancellationToken)).ToList();
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Gets all licences with LicenceType populated.
    /// </summary>
    public async Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetAllAsync(cancellationToken)).ToList();
        foreach (var licence in licences)
        {
            licence.UpdateStatus();
        }
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Creates a new licence after validation.
    /// </summary>
    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        // Validate the licence
        var validationResult = await ValidateLicenceAsync(licence, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Check for duplicate licence number
        var existing = await _licenceRepository.GetByLicenceNumberAsync(licence.LicenceNumber, cancellationToken);
        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Licence with number '{licence.LicenceNumber}' already exists"
                }
            }));
        }

        // Set timestamps
        licence.CreatedDate = DateTime.UtcNow;
        licence.ModifiedDate = DateTime.UtcNow;

        var id = await _licenceRepository.CreateAsync(licence, cancellationToken);
        _logger.LogInformation("Created licence {LicenceNumber} with ID {Id}", licence.LicenceNumber, id);

        return (id, ValidationResult.Success());
    }

    /// <summary>
    /// Updates an existing licence after validation.
    /// </summary>
    public async Task<ValidationResult> UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        // Check licence exists
        var existing = await _licenceRepository.GetByIdAsync(licence.LicenceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licence.LicenceId}' not found"
                }
            });
        }

        // Validate the licence
        var validationResult = await ValidateLicenceAsync(licence, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Check for duplicate licence number (if changed)
        if (existing.LicenceNumber != licence.LicenceNumber)
        {
            var duplicate = await _licenceRepository.GetByLicenceNumberAsync(licence.LicenceNumber, cancellationToken);
            if (duplicate != null)
            {
                return ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"Licence with number '{licence.LicenceNumber}' already exists"
                    }
                });
            }
        }

        // Update timestamp
        licence.ModifiedDate = DateTime.UtcNow;

        await _licenceRepository.UpdateAsync(licence, cancellationToken);
        _logger.LogInformation("Updated licence {LicenceNumber} with ID {Id}", licence.LicenceNumber, licence.LicenceId);

        return ValidationResult.Success();
    }

    /// <summary>
    /// Deletes a licence.
    /// </summary>
    public async Task<ValidationResult> DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var existing = await _licenceRepository.GetByIdAsync(licenceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licenceId}' not found"
                }
            });
        }

        await _licenceRepository.DeleteAsync(licenceId, cancellationToken);
        _logger.LogInformation("Deleted licence {Id}", licenceId);

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a licence meets business rules.
    /// Per data-model.md validation rules.
    /// </summary>
    public async Task<ValidationResult> ValidateLicenceAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var violations = new List<ValidationViolation>();

        // Basic model validation
        var modelResult = licence.Validate();
        if (!modelResult.IsValid)
        {
            violations.AddRange(modelResult.Violations);
        }

        // Verify licence type exists
        var licenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        if (licenceType == null)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence type with ID '{licence.LicenceTypeId}' not found"
            });
        }
        else if (!licenceType.IsActive)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence type '{licenceType.Name}' is not active"
            });
        }
        else
        {
            // T078: Validate permitted activities against licence type
            var activitiesResult = licence.ValidatePermittedActivities(licenceType);
            if (!activitiesResult.IsValid)
            {
                violations.AddRange(activitiesResult.Violations);
            }
        }

        // Validate holder type
        if (!string.IsNullOrEmpty(licence.HolderType) &&
            licence.HolderType != "Company" && licence.HolderType != "Customer")
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "HolderType must be 'Company' or 'Customer'"
            });
        }

        // Validate status
        var validStatuses = new[] { "Valid", "Expired", "Suspended", "Revoked" };
        if (!string.IsNullOrEmpty(licence.Status) && !validStatuses.Contains(licence.Status))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Status must be one of: {string.Join(", ", validStatuses)}"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if a holder has a valid licence for a specific activity.
    /// Per FR-018: Verify customer holds all required valid licences for each controlled product.
    /// </summary>
    public async Task<ValidationResult> CheckHolderLicenceForActivityAsync(
        Guid holderId,
        string holderType,
        LicenceTypes.PermittedActivity requiredActivity,
        CancellationToken cancellationToken = default)
    {
        var licences = await GetByHolderAsync(holderId, holderType, cancellationToken);

        var validLicences = licences
            .Where(l => l.Status == "Valid" && !l.IsExpired())
            .Where(l => l.PermittedActivities.HasFlag(requiredActivity))
            .ToList();

        if (!validLicences.Any())
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_MISSING,
                    Message = $"No valid licence found for activity '{requiredActivity}'"
                }
            });
        }

        return ValidationResult.Success();
    }

    #region Document Operations (T112)

    /// <summary>
    /// Gets all documents for a licence.
    /// </summary>
    public async Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        return await _licenceRepository.GetDocumentsAsync(licenceId, cancellationToken);
    }

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    public async Task<LicenceDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _licenceRepository.GetDocumentByIdAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// Uploads a document for a licence.
    /// T112: Document upload with blob storage integration per FR-008.
    /// </summary>
    public async Task<(Guid? Id, ValidationResult Result)> UploadDocumentAsync(
        Guid licenceId,
        LicenceDocument document,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        // Verify licence exists
        var licence = await _licenceRepository.GetByIdAsync(licenceId, cancellationToken);
        if (licence == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licenceId}' not found"
                }
            }));
        }

        // Validate document
        var validationResult = document.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Generate blob name: licenceid/documentid/filename
        var blobName = $"{licenceId}/{document.DocumentId}/{document.FileName}";

        try
        {
            // Upload to blob storage
            var metadata = new Dictionary<string, string>
            {
                { "licenceId", licenceId.ToString() },
                { "documentType", document.DocumentType.ToString() },
                { "uploadedBy", document.UploadedBy.ToString() }
            };

            var blobUri = await _documentStorage.UploadDocumentAsync(
                DocumentContainerName,
                blobName,
                content,
                document.ContentType ?? "application/octet-stream",
                metadata,
                cancellationToken);

            // Update document with blob URL
            document.LicenceId = licenceId;
            document.BlobStorageUrl = blobUri.ToString();
            document.UploadedDate = DateTime.UtcNow;

            // Save document metadata to repository
            var id = await _licenceRepository.AddDocumentAsync(document, cancellationToken);
            _logger.LogInformation("Uploaded document {DocumentId} for licence {LicenceId}", id, licenceId);

            return (id, ValidationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for licence {LicenceId}", licenceId);
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

    /// <summary>
    /// Deletes a document from a licence.
    /// T112: Document removal including blob storage cleanup.
    /// </summary>
    public async Task<ValidationResult> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Get document to find blob URL
        var document = await _licenceRepository.GetDocumentByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Document with ID '{documentId}' not found"
                }
            });
        }

        try
        {
            // Delete from blob storage
            if (!string.IsNullOrEmpty(document.BlobStorageUrl))
            {
                var blobName = $"{document.LicenceId}/{document.DocumentId}/{document.FileName}";
                await _documentStorage.DeleteDocumentAsync(DocumentContainerName, blobName, cancellationToken);
            }

            // Delete metadata from repository
            await _licenceRepository.DeleteDocumentAsync(documentId, cancellationToken);
            _logger.LogInformation("Deleted document {DocumentId} from licence {LicenceId}", documentId, document.LicenceId);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
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

    #region Verification Operations (T112)

    /// <summary>
    /// Gets verification history for a licence.
    /// </summary>
    public async Task<IEnumerable<LicenceVerification>> GetVerificationHistoryAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        return await _licenceRepository.GetVerificationHistoryAsync(licenceId, cancellationToken);
    }

    /// <summary>
    /// Gets the most recent verification for a licence.
    /// </summary>
    public async Task<LicenceVerification?> GetLatestVerificationAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        return await _licenceRepository.GetLatestVerificationAsync(licenceId, cancellationToken);
    }

    /// <summary>
    /// Records a verification for a licence.
    /// T112: Verification recording per FR-009 (method, date, verifier, outcome).
    /// </summary>
    public async Task<(Guid? Id, ValidationResult Result)> RecordVerificationAsync(
        LicenceVerification verification,
        CancellationToken cancellationToken = default)
    {
        // Verify licence exists
        var licence = await _licenceRepository.GetByIdAsync(verification.LicenceId, cancellationToken);
        if (licence == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{verification.LicenceId}' not found"
                }
            }));
        }

        // Validate verification
        var validationResult = verification.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Set created date
        verification.CreatedDate = DateTime.UtcNow;

        // Save verification
        var id = await _licenceRepository.AddVerificationAsync(verification, cancellationToken);
        _logger.LogInformation("Recorded verification {VerificationId} for licence {LicenceId} with outcome {Outcome}",
            id, verification.LicenceId, verification.Outcome);

        return (id, ValidationResult.Success());
    }

    #endregion

    #region Scope Change Operations (T113)

    /// <summary>
    /// Gets scope change history for a licence.
    /// </summary>
    public async Task<IEnumerable<LicenceScopeChange>> GetScopeChangesAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        return await _licenceRepository.GetScopeChangesAsync(licenceId, cancellationToken);
    }

    /// <summary>
    /// Records a scope change for a licence.
    /// T113: Scope change recording per FR-010 with effective dates.
    /// </summary>
    public async Task<(Guid? Id, ValidationResult Result)> RecordScopeChangeAsync(
        LicenceScopeChange scopeChange,
        CancellationToken cancellationToken = default)
    {
        // Verify licence exists
        var licence = await _licenceRepository.GetByIdAsync(scopeChange.LicenceId, cancellationToken);
        if (licence == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{scopeChange.LicenceId}' not found"
                }
            }));
        }

        // Validate scope change
        var validationResult = scopeChange.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Set recorded date
        scopeChange.RecordedDate = DateTime.UtcNow;

        // Save scope change
        var id = await _licenceRepository.AddScopeChangeAsync(scopeChange, cancellationToken);
        _logger.LogInformation("Recorded scope change {ChangeId} for licence {LicenceId} with type {ChangeType}",
            id, scopeChange.LicenceId, scopeChange.ChangeType);

        return (id, ValidationResult.Success());
    }

    #endregion
}
