using RE2.Shared.Constants;
using RE2.Shared.Extensions;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a legal authorization (permit, exemption, certificate) held by the company or a customer.
/// Per data-model.md entity 1: Licence
/// T063: Domain model implementation.
/// </summary>
public class Licence
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Official licence/permit number from issuing authority.
    /// Required, indexed.
    /// </summary>
    public required string LicenceNumber { get; set; }

    /// <summary>
    /// Reference to LicenceType (category of authorization).
    /// Required.
    /// </summary>
    public Guid LicenceTypeId { get; set; }

    /// <summary>
    /// Who holds this licence ("Company" or "Customer").
    /// Required.
    /// </summary>
    public required string HolderType { get; set; }

    /// <summary>
    /// Reference to holder entity (Company or Customer ID).
    /// Required.
    /// </summary>
    public Guid HolderId { get; set; }

    /// <summary>
    /// Name of authority (e.g., "IGJ", "Farmatec", "CBG-MEB").
    /// Required.
    /// </summary>
    public required string IssuingAuthority { get; set; }

    /// <summary>
    /// Date licence was issued.
    /// Required.
    /// </summary>
    public DateOnly IssueDate { get; set; }

    /// <summary>
    /// Date licence expires (null = no expiry).
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// End date of grace period allowing continued operation during licence renewal.
    /// Per Assumption 16: When set and greater than today, licence is treated as valid
    /// even if ExpiryDate has passed. Grace periods must be configured manually based
    /// on regulatory authority guidance.
    /// </summary>
    public DateOnly? GracePeriodEndDate { get; set; }

    /// <summary>
    /// Current status ("Valid", "Expired", "Suspended", "Revoked").
    /// Required.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Textual description of restrictions or conditions.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// What activities this licence allows (flags).
    /// </summary>
    public LicenceTypes.PermittedActivity PermittedActivities { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optimistic concurrency token.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Navigation property to LicenceType.
    /// </summary>
    public LicenceType? LicenceType { get; set; }

    /// <summary>
    /// Navigation property to substance mappings.
    /// </summary>
    public List<LicenceSubstanceMapping>? SubstanceMappings { get; set; }

    /// <summary>
    /// Validates the licence according to business rules.
    /// Per data-model.md: ExpiryDate must be after IssueDate if specified.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(LicenceNumber))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceNumber is required"
            });
        }

        if (LicenceTypeId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceTypeId is required"
            });
        }

        if (HolderId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "HolderId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(IssuingAuthority))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "IssuingAuthority is required"
            });
        }

        // Per data-model.md: ExpiryDate must be after IssueDate if specified
        if (ExpiryDate.HasValue && ExpiryDate.Value <= IssueDate)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ExpiryDate must be after IssueDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if the licence is currently expired, accounting for grace periods.
    /// Per data-model.md: Status automatically set to Expired when ExpiryDate &lt; Today
    /// AND GracePeriodEndDate is null or also past.
    /// </summary>
    /// <returns>True if expired (including grace period), false otherwise.</returns>
    public bool IsExpired()
    {
        if (!ExpiryDate.HasValue)
        {
            return false;
        }

        if (!ExpiryDate.Value.IsExpired())
        {
            return false;
        }

        // Check if grace period extends validity (per Assumption 16)
        if (GracePeriodEndDate.HasValue && !GracePeriodEndDate.Value.IsExpired())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the licence is expiring within the specified number of days.
    /// Used for alert generation per FR-007 (90/60/30 day warnings).
    /// </summary>
    /// <param name="warningDays">Number of days before expiry.</param>
    /// <returns>True if expiring within the warning period.</returns>
    public bool IsExpiringWithin(int warningDays)
    {
        return ExpiryDate.HasValue && ExpiryDate.Value.IsExpiringWithin(warningDays);
    }

    /// <summary>
    /// Updates the status based on expiry date.
    /// Per data-model.md: Status automatically set to Expired when ExpiryDate < Today.
    /// </summary>
    public void UpdateStatus()
    {
        if (IsExpired() && Status == "Valid")
        {
            Status = "Expired";
            ModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validates the licence's permitted activities against its licence type.
    /// T078: Licence's PermittedActivities must be a subset of LicenceType's PermittedActivities.
    /// </summary>
    /// <param name="licenceType">The licence type to validate against.</param>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult ValidatePermittedActivities(LicenceType licenceType)
    {
        var violations = new List<ValidationViolation>();

        // Get activities that the licence has but the type doesn't allow
        var unauthorizedActivities = PermittedActivities & ~licenceType.PermittedActivities;

        if (unauthorizedActivities != LicenceTypes.PermittedActivity.None)
        {
            var unauthorizedList = GetActivityNames(unauthorizedActivities);
            var allowedList = GetActivityNames(licenceType.PermittedActivities);

            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence has activities not permitted by licence type '{licenceType.Name}'. " +
                         $"Unauthorized: {unauthorizedList}. Allowed: {allowedList}"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Converts permitted activity flags to a human-readable string.
    /// </summary>
    private static string GetActivityNames(LicenceTypes.PermittedActivity activities)
    {
        if (activities == LicenceTypes.PermittedActivity.None)
        {
            return "None";
        }

        var names = new List<string>();

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Possess))
        {
            names.Add("Possess");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Store))
        {
            names.Add("Store");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Distribute))
        {
            names.Add("Distribute");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Import))
        {
            names.Add("Import");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Export))
        {
            names.Add("Export");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.Manufacture))
        {
            names.Add("Manufacture");
        }

        if (activities.HasFlag(LicenceTypes.PermittedActivity.HandlePrecursors))
        {
            names.Add("HandlePrecursors");
        }

        return string.Join(", ", names);
    }
}
