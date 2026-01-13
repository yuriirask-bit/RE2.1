using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licencetype virtual table.
/// T064: Dataverse DTO for LicenceType.
/// Maps to data-model.md entity 2.
/// </summary>
public class LicenceTypeDto
{
    public Guid phr_licencetypeid { get; set; }
    public string? phr_name { get; set; }
    public string? phr_issuingauthority { get; set; }
    public int? phr_typicalvaliditymonths { get; set; }
    public int phr_permittedactivities { get; set; }
    public bool phr_isactive { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public LicenceType ToDomainModel()
    {
        return new LicenceType
        {
            LicenceTypeId = phr_licencetypeid,
            Name = phr_name ?? string.Empty,
            IssuingAuthority = phr_issuingauthority ?? string.Empty,
            TypicalValidityMonths = phr_typicalvaliditymonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)phr_permittedactivities,
            IsActive = phr_isactive
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    public static LicenceTypeDto FromDomainModel(LicenceType model)
    {
        return new LicenceTypeDto
        {
            phr_licencetypeid = model.LicenceTypeId,
            phr_name = model.Name,
            phr_issuingauthority = model.IssuingAuthority,
            phr_typicalvaliditymonths = model.TypicalValidityMonths,
            phr_permittedactivities = (int)model.PermittedActivities,
            phr_isactive = model.IsActive
        };
    }
}
