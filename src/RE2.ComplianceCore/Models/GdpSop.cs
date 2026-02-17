using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// GDP-relevant Standard Operating Procedure (SOP) index entry.
/// T272: GdpSop domain model per US12 data-model.md entity 23 (FR-049).
/// Covers: returns, recalls, deviations, temperature excursions, outsourced activities.
/// Stored in Dataverse phr_gdpsop table.
/// </summary>
public class GdpSop
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid SopId { get; set; }

    /// <summary>
    /// SOP reference number (e.g., "SOP-GDP-001").
    /// Required. Unique.
    /// </summary>
    public string SopNumber { get; set; } = string.Empty;

    /// <summary>
    /// SOP title/description.
    /// Required.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Category of GDP activity this SOP covers.
    /// Required.
    /// </summary>
    public GdpSopCategory Category { get; set; }

    /// <summary>
    /// Document version identifier.
    /// Required.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// When this version became effective.
    /// Required.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// URL to the SOP document.
    /// </summary>
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Whether this SOP is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the SOP record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(SopNumber))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "SopNumber is required"
            });
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Title is required"
            });
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Version is required"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Categories of GDP-relevant SOPs per FR-049.
/// </summary>
public enum GdpSopCategory
{
    Returns,
    Recalls,
    Deviations,
    TemperatureExcursions,
    OutsourcedActivities,
    Other
}
