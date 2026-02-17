using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Controlled change record for GDP-impacting changes.
/// T275: GdpChangeRecord domain model per US12 data-model.md entity 26 (FR-051).
/// Manages changes requiring GDP risk assessment and approval before implementation.
/// Stored in Dataverse phr_gdpchangerecord table.
/// </summary>
public class GdpChangeRecord
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ChangeRecordId { get; set; }

    /// <summary>
    /// Change control reference number (e.g., "CHG-2026-001").
    /// Required. Unique.
    /// </summary>
    public string ChangeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of GDP-impacting change.
    /// Required.
    /// </summary>
    public GdpChangeType ChangeType { get; set; }

    /// <summary>
    /// Description of the change.
    /// Required.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Risk assessment results (free text).
    /// </summary>
    public string? RiskAssessment { get; set; }

    /// <summary>
    /// Current approval status.
    /// </summary>
    public ChangeApprovalStatus ApprovalStatus { get; set; } = ChangeApprovalStatus.Pending;

    /// <summary>
    /// Who approved or rejected the change (FK to User).
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// When the change was approved or rejected.
    /// </summary>
    public DateOnly? ApprovalDate { get; set; }

    /// <summary>
    /// When the change was implemented.
    /// </summary>
    public DateOnly? ImplementationDate { get; set; }

    /// <summary>
    /// References to updated SOPs, training records, etc.
    /// </summary>
    public string? UpdatedDocumentationRefs { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    #region Business Logic

    /// <summary>
    /// Validates the change record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(ChangeNumber))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ChangeNumber is required"
            });
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Description is required"
            });
        }

        if (ImplementationDate.HasValue && ApprovalStatus != ChangeApprovalStatus.Approved)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Cannot set ImplementationDate without approval"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if the change is still pending approval.
    /// </summary>
    public bool IsPending() => ApprovalStatus == ChangeApprovalStatus.Pending;

    /// <summary>
    /// Checks if the change has been approved.
    /// </summary>
    public bool IsApproved() => ApprovalStatus == ChangeApprovalStatus.Approved;

    /// <summary>
    /// Checks if the change can be implemented (approved but not yet implemented).
    /// </summary>
    public bool CanImplement() => IsApproved() && !ImplementationDate.HasValue;

    #endregion
}

/// <summary>
/// Types of GDP-impacting changes per FR-051.
/// </summary>
public enum GdpChangeType
{
    NewWarehouse,
    New3PL,
    NewProductType,
    StorageConditionChange,
    Other
}

/// <summary>
/// Approval status for GDP change records per FR-051.
/// </summary>
public enum ChangeApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
