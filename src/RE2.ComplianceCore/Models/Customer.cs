using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a trading partner qualified to purchase controlled drugs or provide services.
/// T085: Customer domain model per data-model.md entity 5.
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Legal entity name.
    /// Required, indexed.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Company registration number (KVK, VAT, etc.).
    /// Nullable, indexed.
    /// </summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Type of entity (Hospital, Pharmacy, Wholesaler, etc.).
    /// Required.
    /// </summary>
    public BusinessCategory BusinessCategory { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code.
    /// Required.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Current qualification status.
    /// Required.
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// When customer was first qualified.
    /// Nullable.
    /// </summary>
    public DateOnly? OnboardingDate { get; set; }

    /// <summary>
    /// When next periodic review is due.
    /// Nullable.
    /// </summary>
    public DateOnly? NextReVerificationDate { get; set; }

    /// <summary>
    /// GDP qualification status.
    /// Required.
    /// </summary>
    public GdpQualificationStatus GdpQualificationStatus { get; set; } = GdpQualificationStatus.NotRequired;

    /// <summary>
    /// Whether sales are currently blocked.
    /// Required, default: false.
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    /// Why customer is suspended.
    /// Nullable.
    /// </summary>
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// Required.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// Required.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token.
    /// Required.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Determines whether transactions are allowed for this customer.
    /// Per data-model.md: ApprovalStatus must be Approved or ConditionallyApproved,
    /// and IsSuspended = true blocks all transactions regardless of ApprovalStatus.
    /// </summary>
    public bool CanTransact()
    {
        if (IsSuspended)
        {
            return false;
        }

        return ApprovalStatus == ApprovalStatus.Approved ||
               ApprovalStatus == ApprovalStatus.ConditionallyApproved;
    }

    /// <summary>
    /// Validates the customer according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "BusinessName is required"
            });
        }

        if (string.IsNullOrWhiteSpace(Country))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Country is required"
            });
        }
        else if (Country.Length != 2)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Country must be a valid ISO 3166-1 alpha-2 code (2 characters)"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Suspends the customer with a reason.
    /// </summary>
    /// <param name="reason">The reason for suspension.</param>
    public void Suspend(string reason)
    {
        IsSuspended = true;
        SuspensionReason = reason;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    public void Reinstate()
    {
        IsSuspended = false;
        SuspensionReason = null;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the next re-verification date based on the onboarding date.
    /// Per FR-017: periodic re-verification tracking.
    /// </summary>
    /// <param name="monthsFromOnboarding">Number of months from onboarding date.</param>
    public void SetNextReVerificationDate(int monthsFromOnboarding)
    {
        if (OnboardingDate.HasValue)
        {
            NextReVerificationDate = OnboardingDate.Value.AddMonths(monthsFromOnboarding);
            ModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Checks if re-verification is due.
    /// </summary>
    /// <returns>True if re-verification date has passed.</returns>
    public bool IsReVerificationDue()
    {
        if (!NextReVerificationDate.HasValue)
        {
            return false;
        }

        return NextReVerificationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

/// <summary>
/// Type of business entity.
/// Per data-model.md entity 5 BusinessCategory enum.
/// </summary>
public enum BusinessCategory
{
    HospitalPharmacy,
    CommunityPharmacy,
    Veterinarian,
    Manufacturer,
    WholesalerEU,
    WholesalerNonEU,
    ResearchInstitution
}

/// <summary>
/// Customer approval status.
/// Per data-model.md entity 5 ApprovalStatus enum.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    ConditionallyApproved,
    Rejected,
    Suspended
}

/// <summary>
/// GDP qualification status.
/// Per data-model.md entity 5 GdpQualificationStatus enum.
/// </summary>
public enum GdpQualificationStatus
{
    NotRequired,
    Pending,
    Approved,
    ConditionallyApproved,
    Rejected,
    UnderReview
}
