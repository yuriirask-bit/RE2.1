using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpchangerecord entity.
/// T284: Dataverse DTO for GDP change record.
/// </summary>
public class GdpChangeRecordDto
{
    public Guid phr_gdpchangerecordid { get; set; }
    public string? phr_changenumber { get; set; }
    public int phr_changetype { get; set; }
    public string? phr_description { get; set; }
    public string? phr_riskassessment { get; set; }
    public int phr_approvalstatus { get; set; }
    public Guid? phr_approvedby { get; set; }
    public DateTime? phr_approvaldate { get; set; }
    public DateTime? phr_implementationdate { get; set; }
    public string? phr_updateddocumentationrefs { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }

    public GdpChangeRecord ToDomainModel()
    {
        return new GdpChangeRecord
        {
            ChangeRecordId = phr_gdpchangerecordid,
            ChangeNumber = phr_changenumber ?? string.Empty,
            ChangeType = (GdpChangeType)phr_changetype,
            Description = phr_description ?? string.Empty,
            RiskAssessment = phr_riskassessment,
            ApprovalStatus = (ChangeApprovalStatus)phr_approvalstatus,
            ApprovedBy = phr_approvedby,
            ApprovalDate = phr_approvaldate.HasValue ? DateOnly.FromDateTime(phr_approvaldate.Value) : null,
            ImplementationDate = phr_implementationdate.HasValue ? DateOnly.FromDateTime(phr_implementationdate.Value) : null,
            UpdatedDocumentationRefs = phr_updateddocumentationrefs,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue
        };
    }
}
