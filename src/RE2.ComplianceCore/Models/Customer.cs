using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Composite domain model combining D365FO customer master data with Dataverse compliance extensions.
/// D365FO CustomersV3 provides read-only master data (CustomerAccount, OrganizationName, AddressCountryRegionId).
/// Dataverse phr_customercomplianceextension stores compliance-specific extensions.
/// Composite key: CustomerAccount (string) + DataAreaId (string).
/// </summary>
public class Customer
{
    #region D365FO Read-Only Fields (from CustomersV3 OData entity)

    /// <summary>
    /// Customer account number from D365FO. Composite key part 1.
    /// </summary>
    public string CustomerAccount { get; set; } = string.Empty;

    /// <summary>
    /// Legal entity (data area) in D365FO. Composite key part 2.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Organization name from D365FO.
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Country/region ID from D365FO (ISO code).
    /// </summary>
    public string AddressCountryRegionId { get; set; } = string.Empty;

    #endregion

    #region Dataverse Compliance Extension Fields (phr_customercomplianceextension)

    /// <summary>
    /// Unique identifier for the compliance extension record in Dataverse.
    /// </summary>
    public Guid ComplianceExtensionId { get; set; }

    /// <summary>
    /// Type of entity (Hospital, Pharmacy, Wholesaler, etc.).
    /// Required.
    /// </summary>
    public BusinessCategory BusinessCategory { get; set; }

    /// <summary>
    /// Current qualification status.
    /// Required.
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// GDP qualification status.
    /// Required.
    /// </summary>
    public GdpQualificationStatus GdpQualificationStatus { get; set; } = GdpQualificationStatus.NotRequired;

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

    #endregion

    #region Convenience Aliases (backward compat for views/services)

    /// <summary>
    /// Alias for OrganizationName (backward compatibility).
    /// </summary>
    public string BusinessName => OrganizationName;

    /// <summary>
    /// Alias for AddressCountryRegionId (backward compatibility).
    /// </summary>
    public string Country => AddressCountryRegionId;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this customer has a compliance extension configured in Dataverse.
    /// </summary>
    public bool IsComplianceConfigured => ComplianceExtensionId != Guid.Empty;

    #endregion

    #region Business Logic

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

        if (string.IsNullOrWhiteSpace(CustomerAccount))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CustomerAccount is required"
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

    #endregion
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
