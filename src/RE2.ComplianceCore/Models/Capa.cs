using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a Corrective and Preventive Action (CAPA) linked to a GDP inspection finding.
/// T218: Capa domain model per data-model.md entity 22 (FR-041, FR-042).
/// Stored in Dataverse phr_capa table.
/// </summary>
public class Capa
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid CapaId { get; set; }

    /// <summary>
    /// Unique CAPA reference number (e.g., "CAPA-2026-001").
    /// Required, unique, indexed.
    /// </summary>
    public string CapaNumber { get; set; } = string.Empty;

    /// <summary>
    /// Which finding this CAPA addresses.
    /// Required. FK → GdpInspectionFinding.
    /// </summary>
    public Guid FindingId { get; set; }

    /// <summary>
    /// Description of the corrective/preventive action.
    /// Required.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Person responsible for completing the CAPA.
    /// Required.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// When the CAPA must be completed.
    /// Required.
    /// </summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// When the CAPA was actually completed.
    /// Nullable — set when CAPA is completed.
    /// </summary>
    public DateOnly? CompletionDate { get; set; }

    /// <summary>
    /// Current status.
    /// Default: Open.
    /// </summary>
    public CapaStatus Status { get; set; } = CapaStatus.Open;

    /// <summary>
    /// Verification notes for CAPA effectiveness.
    /// </summary>
    public string? VerificationNotes { get; set; }

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

    /// <summary>
    /// Checks if the CAPA is overdue (past due date and not completed).
    /// </summary>
    public bool IsOverdue()
        => DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && !CompletionDate.HasValue;

    /// <summary>
    /// Marks the CAPA as completed.
    /// </summary>
    public void Complete(DateOnly completionDate, string? verificationNotes = null)
    {
        CompletionDate = completionDate;
        VerificationNotes = verificationNotes;
        Status = CapaStatus.Completed;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the CAPA according to business rules.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(CapaNumber))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CapaNumber is required"
            });
        }

        if (FindingId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FindingId is required"
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

        if (string.IsNullOrWhiteSpace(OwnerName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "OwnerName is required"
            });
        }

        if (Status == CapaStatus.Completed && !CompletionDate.HasValue)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CompletionDate is required when Status is Completed"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Status of a CAPA.
/// Per data-model.md entity 22 Status enum.
/// </summary>
public enum CapaStatus
{
    /// <summary>
    /// CAPA is open and in progress.
    /// </summary>
    Open,

    /// <summary>
    /// CAPA is past its due date and not completed.
    /// </summary>
    Overdue,

    /// <summary>
    /// CAPA has been completed.
    /// </summary>
    Completed
}
