using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_gdpdocument virtual table.
/// T235: DTO for GdpDocument mapping to US10 data-model.md.
/// Stored in Dataverse, actual document files stored in Azure Blob Storage.
/// </summary>
public class GdpDocumentDto
{
    /// <summary>
    /// Primary key in Dataverse.
    /// </summary>
    public Guid phr_gdpdocumentid { get; set; }

    /// <summary>
    /// Owner entity type as integer per GdpDocumentEntityType enum.
    /// </summary>
    public int phr_ownerentitytype { get; set; }

    /// <summary>
    /// Foreign key to owning entity (polymorphic).
    /// </summary>
    public Guid phr_ownerentityid { get; set; }

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
    /// Who uploaded the document (display name).
    /// </summary>
    public string? phr_uploadedby { get; set; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    public string? phr_contenttype { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long? phr_filesizebytes { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? phr_description { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public GdpDocument ToDomainModel()
    {
        return new GdpDocument
        {
            DocumentId = phr_gdpdocumentid,
            OwnerEntityType = (GdpDocumentEntityType)phr_ownerentitytype,
            OwnerEntityId = phr_ownerentityid,
            DocumentType = (DocumentType)phr_documenttype,
            FileName = phr_filename ?? string.Empty,
            BlobStorageUrl = phr_blobstorageurl ?? string.Empty,
            UploadedDate = phr_uploadeddate,
            UploadedBy = phr_uploadedby ?? string.Empty,
            ContentType = phr_contenttype,
            FileSizeBytes = phr_filesizebytes,
            Description = phr_description
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    public static GdpDocumentDto FromDomainModel(GdpDocument model)
    {
        return new GdpDocumentDto
        {
            phr_gdpdocumentid = model.DocumentId,
            phr_ownerentitytype = (int)model.OwnerEntityType,
            phr_ownerentityid = model.OwnerEntityId,
            phr_documenttype = (int)model.DocumentType,
            phr_filename = model.FileName,
            phr_blobstorageurl = model.BlobStorageUrl,
            phr_uploadeddate = model.UploadedDate,
            phr_uploadedby = model.UploadedBy,
            phr_contenttype = model.ContentType,
            phr_filesizebytes = model.FileSizeBytes,
            phr_description = model.Description
        };
    }
}
