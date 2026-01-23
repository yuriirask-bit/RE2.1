using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licencedocument virtual table.
/// T108: DTO for LicenceDocument mapping to data-model.md entity 12.
/// Stored in Dataverse, actual document files stored in Azure Blob Storage.
/// </summary>
public class LicenceDocumentDto
{
    /// <summary>
    /// Primary key in Dataverse.
    /// </summary>
    public Guid phr_documentid { get; set; }

    /// <summary>
    /// Foreign key to licence.
    /// </summary>
    public Guid phr_licenceid { get; set; }

    /// <summary>
    /// Document type as integer per DocumentType enum.
    /// </summary>
    public int phr_documenttype { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public string? phr_filename { get; set; }

    /// <summary>
    /// Azure Blob Storage URL.
    /// </summary>
    public string? phr_blobstorageurl { get; set; }

    /// <summary>
    /// When document was uploaded.
    /// </summary>
    public DateTime phr_uploadeddate { get; set; }

    /// <summary>
    /// Who uploaded the document.
    /// </summary>
    public Guid phr_uploadedby { get; set; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    public string? phr_contenttype { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long? phr_filesizebytes { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>LicenceDocument domain model.</returns>
    public LicenceDocument ToDomainModel()
    {
        return new LicenceDocument
        {
            DocumentId = phr_documentid,
            LicenceId = phr_licenceid,
            DocumentType = (DocumentType)phr_documenttype,
            FileName = phr_filename ?? string.Empty,
            BlobStorageUrl = phr_blobstorageurl ?? string.Empty,
            UploadedDate = phr_uploadeddate,
            UploadedBy = phr_uploadedby,
            ContentType = phr_contenttype,
            FileSizeBytes = phr_filesizebytes
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>LicenceDocumentDto for Dataverse persistence.</returns>
    public static LicenceDocumentDto FromDomainModel(LicenceDocument model)
    {
        return new LicenceDocumentDto
        {
            phr_documentid = model.DocumentId,
            phr_licenceid = model.LicenceId,
            phr_documenttype = (int)model.DocumentType,
            phr_filename = model.FileName,
            phr_blobstorageurl = model.BlobStorageUrl,
            phr_uploadeddate = model.UploadedDate,
            phr_uploadedby = model.UploadedBy,
            phr_contenttype = model.ContentType,
            phr_filesizebytes = model.FileSizeBytes
        };
    }
}

/// <summary>
/// OData response wrapper for LicenceDocument queries.
/// </summary>
public class LicenceDocumentODataResponse
{
    /// <summary>
    /// Collection of licence document DTOs.
    /// </summary>
    public List<LicenceDocumentDto> value { get; set; } = new();
}
