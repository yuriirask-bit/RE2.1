using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpequipmentqualification entity.
/// T258: Dataverse DTO for GDP equipment qualification.
/// </summary>
public class GdpEquipmentQualificationDto
{
    public Guid phr_gdpequipmentqualificationid { get; set; }
    public string? phr_equipmentname { get; set; }
    public int phr_equipmenttype { get; set; }
    public Guid? phr_providerid { get; set; }
    public Guid? phr_siteid { get; set; }
    public DateTime? phr_qualificationdate { get; set; }
    public DateTime? phr_requalificationduedate { get; set; }
    public int phr_qualificationstatus { get; set; }
    public string? phr_qualifiedby { get; set; }
    public string? phr_notes { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }

    public GdpEquipmentQualification ToDomainModel()
    {
        return new GdpEquipmentQualification
        {
            EquipmentQualificationId = phr_gdpequipmentqualificationid,
            EquipmentName = phr_equipmentname ?? string.Empty,
            EquipmentType = (GdpEquipmentType)phr_equipmenttype,
            ProviderId = phr_providerid,
            SiteId = phr_siteid,
            QualificationDate = phr_qualificationdate.HasValue ? DateOnly.FromDateTime(phr_qualificationdate.Value) : default,
            RequalificationDueDate = phr_requalificationduedate.HasValue ? DateOnly.FromDateTime(phr_requalificationduedate.Value) : null,
            QualificationStatus = (GdpQualificationStatusType)phr_qualificationstatus,
            QualifiedBy = phr_qualifiedby ?? string.Empty,
            Notes = phr_notes,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue
        };
    }
}
