using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents the mapping between a GDP-configured warehouse and a WDA licence.
/// T186: WDA coverage model per User Story 7 (FR-033).
/// Stored in Dataverse phr_gdpsitewdacoverage table.
/// </summary>
public class GdpSiteWdaCoverage
{
    /// <summary>
    /// Unique identifier for the coverage record.
    /// </summary>
    public Guid CoverageId { get; set; }

    /// <summary>
    /// D365FO warehouse identifier.
    /// </summary>
    public string WarehouseId { get; set; } = string.Empty;

    /// <summary>
    /// Legal entity (data area) context.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the WDA licence covering this site.
    /// Must be a licence with LicenceType.Name == "Wholesale Distribution Authorisation (WDA)".
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Date from which the WDA coverage is effective.
    /// </summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>
    /// Date when the WDA coverage expires (null = no expiry).
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// Navigation property to the associated licence.
    /// </summary>
    public Licence? Licence { get; set; }

    /// <summary>
    /// Checks whether this coverage is currently active.
    /// Coverage is active when EffectiveDate is in the past and ExpiryDate is null or in the future.
    /// </summary>
    /// <returns>True if coverage is currently active.</returns>
    public bool IsActive()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (EffectiveDate > today)
        {
            return false;
        }

        if (ExpiryDate.HasValue && ExpiryDate.Value < today)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the WDA coverage according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(WarehouseId))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "WarehouseId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(DataAreaId))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "DataAreaId is required"
            });
        }

        if (LicenceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceId is required"
            });
        }

        if (ExpiryDate.HasValue && ExpiryDate.Value <= EffectiveDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ExpiryDate must be after EffectiveDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}
