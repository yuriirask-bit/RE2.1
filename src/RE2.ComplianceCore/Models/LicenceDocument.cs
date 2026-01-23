using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Stores metadata and reference to supporting documentation for licences.
/// T104: LicenceDocument domain model per data-model.md entity 12.
/// Actual document files are stored in Azure Blob Storage, not in Dataverse/D365.
/// </summary>
public class LicenceDocument
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Which licence this document belongs to.
    /// Required.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Type of document.
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
    /// Who uploaded document.
    /// Required.
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Content type/MIME type of the document.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Validates the licence document according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (LicenceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceId is required"
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

        if (UploadedBy == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "UploadedBy is required"
            });
        }

        // Validate file extension matches document type
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
/// Type of licence document.
/// Per data-model.md entity 12 DocumentType enum.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Official certificate document.
    /// </summary>
    Certificate = 1,

    /// <summary>
    /// Letter from authority or customer.
    /// </summary>
    Letter = 2,

    /// <summary>
    /// Inspection report.
    /// </summary>
    InspectionReport = 3,

    /// <summary>
    /// Other supporting documentation.
    /// </summary>
    Other = 4
}
