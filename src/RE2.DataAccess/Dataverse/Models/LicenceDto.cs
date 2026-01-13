using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licence virtual table.
/// T067: Dataverse DTO for Licence.
/// Maps to data-model.md entity 1.
/// </summary>
public class LicenceDto
{
    public Guid phr_licenceid { get; set; }
    public string? phr_licencenumber { get; set; }
    public Guid phr_licencetypeid { get; set; }
    public string? phr_holdertype { get; set; }
    public Guid phr_holderid { get; set; }
    public string? phr_issuingauthority { get; set; }
    public DateTime phr_issuedate { get; set; }
    public DateTime? phr_expirydate { get; set; }
    public string? phr_status { get; set; }
    public string? phr_scope { get; set; }
    public int phr_permittedactivities { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }

    public Licence ToDomainModel()
    {
        return new Licence
        {
            LicenceId = phr_licenceid,
            LicenceNumber = phr_licencenumber ?? string.Empty,
            LicenceTypeId = phr_licencetypeid,
            HolderType = phr_holdertype ?? string.Empty,
            HolderId = phr_holderid,
            IssuingAuthority = phr_issuingauthority ?? string.Empty,
            IssueDate = DateOnly.FromDateTime(phr_issuedate),
            ExpiryDate = phr_expirydate.HasValue ? DateOnly.FromDateTime(phr_expirydate.Value) : null,
            Status = phr_status ?? "Valid",
            Scope = phr_scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)phr_permittedactivities,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate
        };
    }

    public static LicenceDto FromDomainModel(Licence model)
    {
        return new LicenceDto
        {
            phr_licenceid = model.LicenceId,
            phr_licencenumber = model.LicenceNumber,
            phr_licencetypeid = model.LicenceTypeId,
            phr_holdertype = model.HolderType,
            phr_holderid = model.HolderId,
            phr_issuingauthority = model.IssuingAuthority,
            phr_issuedate = model.IssueDate.ToDateTime(TimeOnly.MinValue),
            phr_expirydate = model.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
            phr_status = model.Status,
            phr_scope = model.Scope,
            phr_permittedactivities = (int)model.PermittedActivities,
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate
        };
    }
}
