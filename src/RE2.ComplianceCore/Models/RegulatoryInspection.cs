namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a regulatory inspection for Opium Act compliance (IGJ, Customs).
/// Per FR-028: System MUST record regulatory inspections including date, inspector,
/// findings, and corrective actions.
/// T167: Inspection recording for audit trail and reporting.
/// </summary>
public class RegulatoryInspection
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid InspectionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date when the inspection was conducted.
    /// Required per FR-028.
    /// </summary>
    public DateOnly InspectionDate { get; set; }

    /// <summary>
    /// Regulatory authority conducting the inspection.
    /// </summary>
    public InspectingAuthority Authority { get; set; }

    /// <summary>
    /// Name of the inspector or inspection team.
    /// Required per FR-028.
    /// </summary>
    public required string InspectorName { get; set; }

    /// <summary>
    /// Reference number assigned by the authority (if any).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Overall outcome of the inspection.
    /// </summary>
    public InspectionOutcome Outcome { get; set; }

    /// <summary>
    /// Summary of findings from the inspection.
    /// Required per FR-028.
    /// </summary>
    public string? FindingsSummary { get; set; }

    /// <summary>
    /// Corrective actions required or taken.
    /// Required per FR-028.
    /// </summary>
    public string? CorrectiveActions { get; set; }

    /// <summary>
    /// Due date for completing corrective actions (if applicable).
    /// </summary>
    public DateOnly? CorrectiveActionsDueDate { get; set; }

    /// <summary>
    /// Date when corrective actions were completed.
    /// </summary>
    public DateOnly? CorrectiveActionsCompletedDate { get; set; }

    /// <summary>
    /// Reference to any uploaded inspection report document.
    /// </summary>
    public string? ReportDocumentUrl { get; set; }

    /// <summary>
    /// Additional notes about the inspection.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who recorded this inspection in the system.
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When the inspection was recorded in the system.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the regulatory inspection record.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (InspectionDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "Inspection date cannot be in the future"
            });
        }

        if (string.IsNullOrWhiteSpace(InspectorName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "Inspector name is required"
            });
        }

        if (CorrectiveActionsCompletedDate.HasValue && CorrectiveActionsDueDate.HasValue &&
            CorrectiveActionsCompletedDate.Value < InspectionDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "Corrective actions completion date cannot be before inspection date"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if corrective actions are overdue.
    /// </summary>
    /// <returns>True if corrective actions are due and not completed.</returns>
    public bool AreCorrectiveActionsOverdue()
    {
        if (!CorrectiveActionsDueDate.HasValue)
        {
            return false;
        }

        if (CorrectiveActionsCompletedDate.HasValue)
        {
            return false;
        }

        return CorrectiveActionsDueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

/// <summary>
/// Regulatory authority conducting inspections for Opium Act compliance.
/// </summary>
public enum InspectingAuthority
{
    /// <summary>
    /// Inspectorate for Health and Youth Care (Inspectie Gezondheidszorg en Jeugd).
    /// Primary authority for Opium Act inspections.
    /// </summary>
    IGJ,

    /// <summary>
    /// Dutch Customs (Douane).
    /// Inspects import/export of controlled substances.
    /// </summary>
    Customs,

    /// <summary>
    /// Netherlands Food and Consumer Product Safety Authority.
    /// May inspect precursor chemicals.
    /// </summary>
    NVWA,

    /// <summary>
    /// Internal compliance audit.
    /// </summary>
    Internal,

    /// <summary>
    /// Other regulatory body.
    /// </summary>
    Other
}

/// <summary>
/// Outcome of a regulatory inspection.
/// </summary>
public enum InspectionOutcome
{
    /// <summary>
    /// No deficiencies found.
    /// </summary>
    NoFindings,

    /// <summary>
    /// Minor deficiencies that require attention.
    /// </summary>
    MinorFindings,

    /// <summary>
    /// Major deficiencies requiring corrective action.
    /// </summary>
    MajorFindings,

    /// <summary>
    /// Critical deficiencies requiring immediate action.
    /// </summary>
    CriticalFindings,

    /// <summary>
    /// Inspection is ongoing or pending final report.
    /// </summary>
    Pending
}
