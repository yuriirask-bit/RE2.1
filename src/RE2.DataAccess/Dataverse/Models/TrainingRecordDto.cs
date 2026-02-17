using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_trainingrecord entity.
/// T281: Dataverse DTO for training record.
/// </summary>
public class TrainingRecordDto
{
    public Guid phr_trainingrecordid { get; set; }
    public Guid phr_staffmemberid { get; set; }
    public string? phr_staffmembername { get; set; }
    public string? phr_trainingcurriculum { get; set; }
    public Guid? phr_sopid { get; set; }
    public Guid? phr_siteid { get; set; }
    public DateTime? phr_completiondate { get; set; }
    public DateTime? phr_expirydate { get; set; }
    public string? phr_trainername { get; set; }
    public int phr_assessmentresult { get; set; }

    public TrainingRecord ToDomainModel()
    {
        return new TrainingRecord
        {
            TrainingRecordId = phr_trainingrecordid,
            StaffMemberId = phr_staffmemberid,
            StaffMemberName = phr_staffmembername ?? string.Empty,
            TrainingCurriculum = phr_trainingcurriculum ?? string.Empty,
            SopId = phr_sopid,
            SiteId = phr_siteid,
            CompletionDate = phr_completiondate.HasValue ? DateOnly.FromDateTime(phr_completiondate.Value) : default,
            ExpiryDate = phr_expirydate.HasValue ? DateOnly.FromDateTime(phr_expirydate.Value) : null,
            TrainerName = phr_trainername,
            AssessmentResult = (AssessmentResult)phr_assessmentresult
        };
    }
}
