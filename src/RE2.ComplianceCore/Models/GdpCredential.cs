using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a partner's GDP compliance status and credentials.
/// T199: GDP credential domain model per User Story 8 (FR-036, FR-037).
/// Polymorphic: belongs to either a Customer (supplier) or GdpServiceProvider.
/// Stored in Dataverse phr_gdpcredential table.
/// </summary>
public class GdpCredential
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid CredentialId { get; set; }

    /// <summary>
    /// Type of partner entity.
    /// Required.
    /// </summary>
    public GdpCredentialEntityType EntityType { get; set; }

    /// <summary>
    /// Reference to Customer ComplianceExtensionId or GdpServiceProvider ProviderId.
    /// Required.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// WDA number if applicable.
    /// </summary>
    public string? WdaNumber { get; set; }

    /// <summary>
    /// GDP certificate number.
    /// </summary>
    public string? GdpCertificateNumber { get; set; }

    /// <summary>
    /// Link to EudraGMDP entry for verification.
    /// </summary>
    public string? EudraGmdpEntryUrl { get; set; }

    /// <summary>
    /// When credentials became valid.
    /// </summary>
    public DateOnly? ValidityStartDate { get; set; }

    /// <summary>
    /// When credentials expire.
    /// </summary>
    public DateOnly? ValidityEndDate { get; set; }

    /// <summary>
    /// Current qualification status.
    /// Default: UnderReview for new credentials.
    /// </summary>
    public GdpQualificationStatus QualificationStatus { get; set; } = GdpQualificationStatus.UnderReview;

    /// <summary>
    /// Last verification via EudraGMDP or national database.
    /// </summary>
    public DateOnly? LastVerificationDate { get; set; }

    /// <summary>
    /// Next periodic review due date.
    /// </summary>
    public DateOnly? NextReviewDate { get; set; }

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

    #region Business Logic

    /// <summary>
    /// Checks if the credential is currently valid based on dates.
    /// Valid when: no dates set, or start date is in the past and end date is null or in the future.
    /// </summary>
    public bool IsValid()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (ValidityStartDate.HasValue && ValidityStartDate.Value > today)
        {
            return false;
        }

        if (ValidityEndDate.HasValue && ValidityEndDate.Value < today)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the credential allows partner selection in transactions.
    /// Per FR-038: Approved and ConditionallyApproved allow selection.
    /// </summary>
    public bool IsApproved()
    {
        return QualificationStatus == GdpQualificationStatus.Approved ||
               QualificationStatus == GdpQualificationStatus.ConditionallyApproved;
    }

    /// <summary>
    /// Validates the credential according to business rules.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (EntityId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "EntityId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(WdaNumber) && string.IsNullOrWhiteSpace(GdpCertificateNumber))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "At least one of WdaNumber or GdpCertificateNumber must be provided"
            });
        }

        if (ValidityEndDate.HasValue && ValidityStartDate.HasValue &&
            ValidityEndDate.Value <= ValidityStartDate.Value)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ValidityEndDate must be after ValidityStartDate"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    #endregion
}

/// <summary>
/// Type of entity that a GDP credential belongs to.
/// Per data-model.md entity 18 EntityType enum.
/// </summary>
public enum GdpCredentialEntityType
{
    /// <summary>
    /// Credential belongs to a customer (supplier).
    /// EntityId references Customer ComplianceExtensionId.
    /// </summary>
    Supplier,

    /// <summary>
    /// Credential belongs to a service provider (3PL, transporter, external warehouse).
    /// EntityId references GdpServiceProvider ProviderId.
    /// </summary>
    ServiceProvider
}
