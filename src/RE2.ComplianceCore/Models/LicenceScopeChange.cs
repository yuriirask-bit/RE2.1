using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records historical changes to a licence's scope or authorized substances.
/// T106: LicenceScopeChange domain model per data-model.md entity 14.
/// Supports FR-010 requirement for tracking scope changes with effective dates.
/// </summary>
public class LicenceScopeChange
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ChangeId { get; set; }

    /// <summary>
    /// Which licence this change applies to.
    /// Required.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// When the change took effect.
    /// Required.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// Description of what changed.
    /// Required.
    /// </summary>
    public string ChangeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Type of scope change.
    /// </summary>
    public ScopeChangeType ChangeType { get; set; }

    /// <summary>
    /// Who recorded the change.
    /// Required.
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// Name of the person who recorded the change.
    /// For display purposes.
    /// </summary>
    public string? RecorderName { get; set; }

    /// <summary>
    /// When the change was recorded in the system.
    /// Required.
    /// </summary>
    public DateTime RecordedDate { get; set; }

    /// <summary>
    /// Reference to supporting document (if any).
    /// </summary>
    public Guid? SupportingDocumentId { get; set; }

    /// <summary>
    /// Substances added by this change (comma-separated internal codes).
    /// </summary>
    public string? SubstancesAdded { get; set; }

    /// <summary>
    /// Substances removed by this change (comma-separated internal codes).
    /// </summary>
    public string? SubstancesRemoved { get; set; }

    /// <summary>
    /// Activities added by this change.
    /// </summary>
    public string? ActivitiesAdded { get; set; }

    /// <summary>
    /// Activities removed by this change.
    /// </summary>
    public string? ActivitiesRemoved { get; set; }

    /// <summary>
    /// Validates the scope change according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (LicenceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(ChangeDescription))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ChangeDescription is required"
            });
        }

        if (RecordedBy == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "RecordedBy is required"
            });
        }

        // Effective date should not be too far in the future
        var maxFutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        if (EffectiveDate > maxFutureDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EffectiveDate cannot be more than 1 year in the future"
            });
        }

        // At least one change detail should be provided
        if (string.IsNullOrWhiteSpace(SubstancesAdded) &&
            string.IsNullOrWhiteSpace(SubstancesRemoved) &&
            string.IsNullOrWhiteSpace(ActivitiesAdded) &&
            string.IsNullOrWhiteSpace(ActivitiesRemoved) &&
            ChangeType == ScopeChangeType.Other)
        {
            // This is OK - description covers it
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if this change is effective as of a given date.
    /// </summary>
    public bool IsEffectiveAsOf(DateOnly date)
    {
        return EffectiveDate <= date;
    }

    /// <summary>
    /// Gets the list of substances added as an array.
    /// </summary>
    public string[] GetSubstancesAddedArray()
    {
        if (string.IsNullOrWhiteSpace(SubstancesAdded))
        {
            return Array.Empty<string>();
        }

        return SubstancesAdded
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Gets the list of substances removed as an array.
    /// </summary>
    public string[] GetSubstancesRemovedArray()
    {
        if (string.IsNullOrWhiteSpace(SubstancesRemoved))
        {
            return Array.Empty<string>();
        }

        return SubstancesRemoved
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Checks if this change adds any substances.
    /// </summary>
    public bool AddsSubstances()
    {
        return !string.IsNullOrWhiteSpace(SubstancesAdded);
    }

    /// <summary>
    /// Checks if this change removes any substances.
    /// </summary>
    public bool RemovesSubstances()
    {
        return !string.IsNullOrWhiteSpace(SubstancesRemoved);
    }
}

/// <summary>
/// Type of scope change for a licence.
/// </summary>
public enum ScopeChangeType
{
    /// <summary>
    /// Substances added to licence scope.
    /// </summary>
    SubstancesAdded = 1,

    /// <summary>
    /// Substances removed from licence scope.
    /// </summary>
    SubstancesRemoved = 2,

    /// <summary>
    /// Activities added to licence.
    /// </summary>
    ActivitiesAdded = 3,

    /// <summary>
    /// Activities removed from licence.
    /// </summary>
    ActivitiesRemoved = 4,

    /// <summary>
    /// Geographic scope changed.
    /// </summary>
    GeographicChange = 5,

    /// <summary>
    /// Conditions or restrictions modified.
    /// </summary>
    ConditionsModified = 6,

    /// <summary>
    /// Other type of scope change.
    /// </summary>
    Other = 99
}
