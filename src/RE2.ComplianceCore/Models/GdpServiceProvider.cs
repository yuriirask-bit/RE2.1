using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a 3PL, transporter, or external warehouse providing GDP services.
/// T200: GDP service provider domain model per User Story 8 (FR-036, FR-037).
/// Standalone Dataverse entity (no D365 F&O composite).
/// Stored in Dataverse phr_gdpserviceprovider table.
/// </summary>
public class GdpServiceProvider
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Provider name.
    /// Required, indexed.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Type of service provided.
    /// Required.
    /// </summary>
    public GdpServiceType ServiceType { get; set; }

    /// <summary>
    /// Whether temperature-controlled transport is available.
    /// Required.
    /// </summary>
    public bool TemperatureControlledCapability { get; set; }

    /// <summary>
    /// Approved routes/lanes (free text or JSON).
    /// </summary>
    public string? ApprovedRoutes { get; set; }

    /// <summary>
    /// Current GDP qualification status.
    /// Default: UnderReview for new providers.
    /// </summary>
    public GdpQualificationStatus QualificationStatus { get; set; } = GdpQualificationStatus.UnderReview;

    /// <summary>
    /// How often re-qualification is needed (e.g., 24 or 36 months).
    /// Required, must be > 0.
    /// </summary>
    public int ReviewFrequencyMonths { get; set; }

    /// <summary>
    /// Last qualification review date.
    /// </summary>
    public DateOnly? LastReviewDate { get; set; }

    /// <summary>
    /// Next review due date.
    /// </summary>
    public DateOnly? NextReviewDate { get; set; }

    /// <summary>
    /// Whether provider can be selected in transactions.
    /// Default: true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    #region Business Logic

    /// <summary>
    /// Checks if the provider is approved for GDP (Approved or ConditionallyApproved).
    /// Per FR-038.
    /// </summary>
    public bool IsApproved()
    {
        return QualificationStatus == GdpQualificationStatus.Approved ||
               QualificationStatus == GdpQualificationStatus.ConditionallyApproved;
    }

    /// <summary>
    /// Checks if the provider can be selected in transactions.
    /// Per FR-038: Must be active AND approved.
    /// </summary>
    public bool CanBeSelected()
    {
        return IsActive && IsApproved();
    }

    /// <summary>
    /// Checks if a re-qualification review is due.
    /// Per FR-039.
    /// </summary>
    public bool IsReviewDue()
    {
        if (!NextReviewDate.HasValue)
            return false;

        return NextReviewDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Validates the service provider according to business rules.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(ProviderName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ProviderName is required"
            });
        }

        if (ReviewFrequencyMonths <= 0)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ReviewFrequencyMonths must be greater than 0"
            });
        }

        if (NextReviewDate.HasValue && LastReviewDate.HasValue &&
            NextReviewDate.Value <= LastReviewDate.Value)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "NextReviewDate must be after LastReviewDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    #endregion
}

/// <summary>
/// Type of GDP service provided.
/// Per data-model.md entity 19 ServiceType enum.
/// </summary>
public enum GdpServiceType
{
    /// <summary>
    /// Third-party logistics provider.
    /// </summary>
    ThirdPartyLogistics,

    /// <summary>
    /// Transport provider.
    /// </summary>
    Transporter,

    /// <summary>
    /// External warehouse provider.
    /// </summary>
    ExternalWarehouse
}
