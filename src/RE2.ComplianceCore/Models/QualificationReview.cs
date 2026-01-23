using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Represents a qualification or re-qualification review of a customer/partner.
/// T086: QualificationReview domain model per data-model.md entity 29.
/// </summary>
public class QualificationReview
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// What is being reviewed (Customer or ServiceProvider).
    /// Required.
    /// </summary>
    public ReviewEntityType EntityType { get; set; }

    /// <summary>
    /// Which entity (Customer or GdpServiceProvider ID).
    /// Required, FK → Customer or GdpServiceProvider.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// When review occurred.
    /// Required.
    /// </summary>
    public DateOnly ReviewDate { get; set; }

    /// <summary>
    /// How reviewed (OnSiteAudit, Questionnaire, DocumentReview).
    /// Required.
    /// </summary>
    public ReviewMethod ReviewMethod { get; set; }

    /// <summary>
    /// Result of the review.
    /// Required.
    /// </summary>
    public ReviewOutcome ReviewOutcome { get; set; }

    /// <summary>
    /// Who performed the review.
    /// Required.
    /// </summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>
    /// Review notes.
    /// Nullable.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When next review is due.
    /// Nullable.
    /// </summary>
    public DateOnly? NextReviewDate { get; set; }

    /// <summary>
    /// Indicates whether the review outcome is an approval (Approved or ConditionallyApproved).
    /// </summary>
    public bool IsApproved =>
        ReviewOutcome == ReviewOutcome.Approved ||
        ReviewOutcome == ReviewOutcome.ConditionallyApproved;

    /// <summary>
    /// Creates a qualification review for a customer.
    /// </summary>
    public static QualificationReview CreateForCustomer(
        Guid customerId,
        DateOnly reviewDate,
        ReviewMethod method,
        ReviewOutcome outcome,
        string reviewerName,
        string? notes = null)
    {
        return new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = customerId,
            ReviewDate = reviewDate,
            ReviewMethod = method,
            ReviewOutcome = outcome,
            ReviewerName = reviewerName,
            Notes = notes
        };
    }

    /// <summary>
    /// Creates a qualification review for a service provider.
    /// </summary>
    public static QualificationReview CreateForServiceProvider(
        Guid serviceProviderId,
        DateOnly reviewDate,
        ReviewMethod method,
        ReviewOutcome outcome,
        string reviewerName,
        string? notes = null)
    {
        return new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.ServiceProvider,
            EntityId = serviceProviderId,
            ReviewDate = reviewDate,
            ReviewMethod = method,
            ReviewOutcome = outcome,
            ReviewerName = reviewerName,
            Notes = notes
        };
    }

    /// <summary>
    /// Sets the next review date based on the review date.
    /// </summary>
    /// <param name="monthsFromReview">Number of months from review date.</param>
    public void SetNextReviewDate(int monthsFromReview)
    {
        NextReviewDate = ReviewDate.AddMonths(monthsFromReview);
    }

    /// <summary>
    /// Validates the qualification review according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
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

        if (string.IsNullOrWhiteSpace(ReviewerName))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ReviewerName is required"
            });
        }

        if (ReviewDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "ReviewDate cannot be in the future"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Type of entity being reviewed.
/// Per data-model.md entity 29 EntityType enum.
/// </summary>
public enum ReviewEntityType
{
    Customer,
    ServiceProvider
}

/// <summary>
/// Method used for the review.
/// Per data-model.md entity 29 ReviewMethod enum.
/// </summary>
public enum ReviewMethod
{
    OnSiteAudit,
    Questionnaire,
    DocumentReview
}

/// <summary>
/// Outcome of the review.
/// Per data-model.md entity 29 ReviewOutcome enum.
/// </summary>
public enum ReviewOutcome
{
    Approved,
    ConditionallyApproved,
    Rejected
}
