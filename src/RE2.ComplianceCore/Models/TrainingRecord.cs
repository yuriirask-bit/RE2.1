using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records GDP-specific training completion for distribution staff.
/// T274: TrainingRecord domain model per US12 data-model.md entity 25 (FR-050).
/// Links training curricula to staff functions and tracks competency evidence.
/// Stored in Dataverse phr_trainingrecord table.
/// </summary>
public class TrainingRecord
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid TrainingRecordId { get; set; }

    /// <summary>
    /// Staff member who was trained (FK to User).
    /// Required.
    /// </summary>
    public Guid StaffMemberId { get; set; }

    /// <summary>
    /// Display name of the staff member.
    /// Required.
    /// </summary>
    public string StaffMemberName { get; set; } = string.Empty;

    /// <summary>
    /// Training topic/curriculum name.
    /// Required.
    /// </summary>
    public string TrainingCurriculum { get; set; } = string.Empty;

    /// <summary>
    /// Optional FK to the SOP this training covers.
    /// </summary>
    public Guid? SopId { get; set; }

    /// <summary>
    /// Optional FK to the site this training applies to (site-specific training).
    /// </summary>
    public Guid? SiteId { get; set; }

    /// <summary>
    /// When training was completed.
    /// Required.
    /// </summary>
    public DateOnly CompletionDate { get; set; }

    /// <summary>
    /// When retraining is required. Null if no expiry.
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// Who delivered the training.
    /// </summary>
    public string? TrainerName { get; set; }

    /// <summary>
    /// Training assessment result.
    /// </summary>
    public AssessmentResult AssessmentResult { get; set; } = AssessmentResult.NotAssessed;

    /// <summary>
    /// Validates the training record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (StaffMemberId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "StaffMemberId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(TrainingCurriculum))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "TrainingCurriculum is required"
            });
        }

        if (CompletionDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CompletionDate cannot be in the future"
            });
        }

        if (ExpiryDate.HasValue && ExpiryDate.Value <= CompletionDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ExpiryDate must be after CompletionDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if the training has expired.
    /// </summary>
    public bool IsExpired()
    {
        if (!ExpiryDate.HasValue)
            return false;

        return ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

/// <summary>
/// Training assessment outcome per FR-050.
/// </summary>
public enum AssessmentResult
{
    Pass,
    Fail,
    NotAssessed
}
