using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_capa entity.
/// T219: Dataverse DTO for CAPA (Corrective and Preventive Action).
/// </summary>
public class CapaDto
{
    public Guid phr_capaid { get; set; }
    public string? phr_capanumber { get; set; }
    public Guid phr_findingid { get; set; }
    public string? phr_description { get; set; }
    public string? phr_ownername { get; set; }
    public DateTime? phr_duedate { get; set; }
    public DateTime? phr_completiondate { get; set; }
    public int phr_status { get; set; }
    public string? phr_verificationnotes { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }
    public byte[]? phr_rowversion { get; set; }

    public Capa ToDomainModel()
    {
        return new Capa
        {
            CapaId = phr_capaid,
            CapaNumber = phr_capanumber ?? string.Empty,
            FindingId = phr_findingid,
            Description = phr_description ?? string.Empty,
            OwnerName = phr_ownername ?? string.Empty,
            DueDate = phr_duedate.HasValue ? DateOnly.FromDateTime(phr_duedate.Value) : default,
            CompletionDate = phr_completiondate.HasValue ? DateOnly.FromDateTime(phr_completiondate.Value) : null,
            Status = (CapaStatus)phr_status,
            VerificationNotes = phr_verificationnotes,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue,
            RowVersion = phr_rowversion ?? Array.Empty<byte>()
        };
    }
}
