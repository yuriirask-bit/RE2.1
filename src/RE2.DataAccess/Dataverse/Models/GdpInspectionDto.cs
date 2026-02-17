using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpinspection entity.
/// T219: Dataverse DTO for GDP inspection.
/// </summary>
public class GdpInspectionDto
{
    public Guid phr_gdpinspectionid { get; set; }
    public DateTime? phr_inspectiondate { get; set; }
    public string? phr_inspectorname { get; set; }
    public int phr_inspectiontype { get; set; }
    public Guid phr_siteid { get; set; }
    public Guid? phr_wdalicenceid { get; set; }
    public string? phr_findingssummary { get; set; }
    public string? phr_reportreferenceurl { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }

    public GdpInspection ToDomainModel()
    {
        return new GdpInspection
        {
            InspectionId = phr_gdpinspectionid,
            InspectionDate = phr_inspectiondate.HasValue ? DateOnly.FromDateTime(phr_inspectiondate.Value) : default,
            InspectorName = phr_inspectorname ?? string.Empty,
            InspectionType = (GdpInspectionType)phr_inspectiontype,
            SiteId = phr_siteid,
            WdaLicenceId = phr_wdalicenceid,
            FindingsSummary = phr_findingssummary,
            ReportReferenceUrl = phr_reportreferenceurl,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_gdpinspectionfinding entity.
/// T219: Dataverse DTO for GDP inspection finding.
/// </summary>
public class GdpInspectionFindingDto
{
    public Guid phr_gdpinspectionfindingid { get; set; }
    public Guid phr_inspectionid { get; set; }
    public string? phr_findingdescription { get; set; }
    public int phr_classification { get; set; }
    public string? phr_findingnumber { get; set; }

    public GdpInspectionFinding ToDomainModel()
    {
        return new GdpInspectionFinding
        {
            FindingId = phr_gdpinspectionfindingid,
            InspectionId = phr_inspectionid,
            FindingDescription = phr_findingdescription ?? string.Empty,
            Classification = (FindingClassification)phr_classification,
            FindingNumber = phr_findingnumber
        };
    }
}
