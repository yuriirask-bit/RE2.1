using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a GDP audit — regulatory authority inspection, internal audit, or self-inspection.
/// T216: GdpInspection domain model per data-model.md entity 20 (FR-040).
/// Stored in Dataverse phr_gdpinspection table.
/// </summary>
public class GdpInspection
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// When inspection occurred.
    /// Required.
    /// </summary>
    public DateOnly InspectionDate { get; set; }

    /// <summary>
    /// Inspector or authority name (e.g., "IGJ", "NVWA", "Internal QA").
    /// Required.
    /// </summary>
    public string InspectorName { get; set; } = string.Empty;

    /// <summary>
    /// Type of inspection.
    /// Required.
    /// </summary>
    public GdpInspectionType InspectionType { get; set; }

    /// <summary>
    /// Which GDP site was inspected.
    /// Required. FK → GdpSite.GdpExtensionId.
    /// </summary>
    public Guid SiteId { get; set; }

    /// <summary>
    /// Which WDA was inspected (if applicable).
    /// Nullable. FK → Licence.
    /// </summary>
    public Guid? WdaLicenceId { get; set; }

    /// <summary>
    /// Overall findings summary.
    /// </summary>
    public string? FindingsSummary { get; set; }

    /// <summary>
    /// Link to inspection report document.
    /// </summary>
    public string? ReportReferenceUrl { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Validates the inspection record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(InspectorName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "InspectorName is required"
            });
        }

        if (SiteId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SiteId is required"
            });
        }

        if (InspectionDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "InspectionDate cannot be in the future"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Type of GDP inspection.
/// Per data-model.md entity 20 InspectionType enum.
/// </summary>
public enum GdpInspectionType
{
    /// <summary>
    /// Inspection by a regulatory authority (e.g., IGJ, NVWA).
    /// </summary>
    RegulatoryAuthority,

    /// <summary>
    /// Internal audit conducted by the company.
    /// </summary>
    Internal,

    /// <summary>
    /// Self-inspection per GDP requirements.
    /// </summary>
    SelfInspection
}

/// <summary>
/// Represents an individual finding from a GDP inspection.
/// T217: GdpInspectionFinding domain model per data-model.md entity 21 (FR-040).
/// Stored in Dataverse phr_gdpinspectionfinding table.
/// </summary>
public class GdpInspectionFinding
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid FindingId { get; set; }

    /// <summary>
    /// Parent inspection.
    /// Required. FK → GdpInspection.
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// What deficiency was found.
    /// Required.
    /// </summary>
    public string FindingDescription { get; set; } = string.Empty;

    /// <summary>
    /// Severity classification.
    /// Required.
    /// </summary>
    public FindingClassification Classification { get; set; }

    /// <summary>
    /// Official finding reference number.
    /// </summary>
    public string? FindingNumber { get; set; }

    /// <summary>
    /// Checks if this is a critical finding.
    /// </summary>
    public bool IsCritical() => Classification == FindingClassification.Critical;

    /// <summary>
    /// Validates the finding.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(FindingDescription))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "FindingDescription is required"
            });
        }

        if (InspectionId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "InspectionId is required"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Classification severity of an inspection finding.
/// Per data-model.md entity 21 Classification enum.
/// </summary>
public enum FindingClassification
{
    /// <summary>
    /// Critical deficiency requiring immediate action.
    /// </summary>
    Critical,

    /// <summary>
    /// Major deficiency requiring corrective action.
    /// </summary>
    Major,

    /// <summary>
    /// Other (minor) observation.
    /// </summary>
    Other
}
