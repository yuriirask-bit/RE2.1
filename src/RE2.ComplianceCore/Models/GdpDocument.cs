using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Stores metadata and Azure Blob Storage reference for documents attached to GDP entity records.
/// T232: GdpDocument domain model per US10 data-model.md.
/// Polymorphic ownership via OwnerEntityType/OwnerEntityId supports credentials, sites, inspections, providers, customers.
/// Reuses existing DocumentType enum from LicenceDocument.
/// </summary>
public class GdpDocument
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Which entity type this document belongs to.
    /// Required.
    /// </summary>
    public GdpDocumentEntityType OwnerEntityType { get; set; }

    /// <summary>
    /// FK to owning entity (polymorphic based on OwnerEntityType).
    /// Required.
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Type of document (reuses existing DocumentType enum).
    /// Required.
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Original filename.
    /// Required.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage URL (SAS token not stored here).
    /// Required.
    /// </summary>
    public string BlobStorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// When document was uploaded.
    /// Required.
    /// </summary>
    public DateTime UploadedDate { get; set; }

    /// <summary>
    /// Who uploaded the document (display name).
    /// Required.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Content type/MIME type of the document.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Optional description of the document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Validates the GDP document according to business rules.
    /// Same validation rules as LicenceDocument.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (OwnerEntityId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "OwnerEntityId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(FileName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FileName is required"
            });
        }

        if (string.IsNullOrWhiteSpace(BlobStorageUrl))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "BlobStorageUrl is required"
            });
        }

        if (string.IsNullOrWhiteSpace(UploadedBy))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "UploadedBy is required"
            });
        }

        // Validate file extension
        if (!string.IsNullOrWhiteSpace(FileName))
        {
            var extension = Path.GetExtension(FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".tiff" };
            if (!allowedExtensions.Contains(extension))
            {
                violations.Add(new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtensions)}"
                });
            }
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Gets the file extension from the filename.
    /// </summary>
    public string GetFileExtension()
    {
        return Path.GetExtension(FileName).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if this is a PDF document.
    /// </summary>
    public bool IsPdf()
    {
        return GetFileExtension() == ".pdf";
    }

    /// <summary>
    /// Checks if this is an image document.
    /// </summary>
    public bool IsImage()
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".tiff" };
        return imageExtensions.Contains(GetFileExtension());
    }
}

/// <summary>
/// Entity types that can own GDP documents.
/// Per US10 data-model.md GdpDocumentEntityType enum.
/// </summary>
public enum GdpDocumentEntityType
{
    /// <summary>
    /// GdpCredential — GDP certificates, WDA copies.
    /// </summary>
    Credential,

    /// <summary>
    /// GdpSite — warehouse documentation.
    /// </summary>
    Site,

    /// <summary>
    /// GdpInspection — inspection reports.
    /// </summary>
    Inspection,

    /// <summary>
    /// GdpServiceProvider — provider qualification docs.
    /// </summary>
    Provider,

    /// <summary>
    /// Customer — supplier GDP certificates.
    /// </summary>
    Customer
}
