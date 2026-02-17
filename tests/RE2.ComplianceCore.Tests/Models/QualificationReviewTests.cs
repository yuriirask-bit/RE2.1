using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for QualificationReview domain model.
/// T082: Test-driven development for QualificationReview per data-model.md entity 29.
/// </summary>
public class QualificationReviewTests
{
    #region Property Initialization Tests

    [Fact]
    public void QualificationReview_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var reviewId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var review = new QualificationReview
        {
            ReviewId = reviewId,
            EntityType = ReviewEntityType.Customer,
            EntityId = entityId,
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Maria Jansen",
            Notes = "All documentation verified against authority records",
            NextReviewDate = DateOnly.FromDateTime(DateTime.Now.AddYears(3))
        };

        // Assert
        Assert.Equal(reviewId, review.ReviewId);
        Assert.Equal(ReviewEntityType.Customer, review.EntityType);
        Assert.Equal(entityId, review.EntityId);
        Assert.Equal("Maria Jansen", review.ReviewerName);
        Assert.Equal(ReviewOutcome.Approved, review.ReviewOutcome);
        Assert.NotNull(review.Notes);
    }

    [Fact]
    public void QualificationReview_Notes_CanBeNull()
    {
        // Arrange & Act
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.Questionnaire,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Jan de Vries",
            Notes = null,
            NextReviewDate = null
        };

        // Assert
        Assert.Null(review.Notes);
        Assert.Null(review.NextReviewDate);
    }

    #endregion

    #region ReviewEntityType Enum Tests

    [Theory]
    [InlineData(ReviewEntityType.Customer, "Customer")]
    [InlineData(ReviewEntityType.ServiceProvider, "ServiceProvider")]
    public void ReviewEntityType_AllTypesAreDefined(ReviewEntityType entityType, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, entityType.ToString());
    }

    [Fact]
    public void ReviewEntityType_HasExpectedValues()
    {
        // Assert - per data-model.md entity 29
        var values = Enum.GetValues<ReviewEntityType>();
        Assert.Equal(2, values.Length);
    }

    #endregion

    #region ReviewMethod Enum Tests

    [Theory]
    [InlineData(ReviewMethod.OnSiteAudit, "OnSiteAudit")]
    [InlineData(ReviewMethod.Questionnaire, "Questionnaire")]
    [InlineData(ReviewMethod.DocumentReview, "DocumentReview")]
    public void ReviewMethod_AllMethodsAreDefined(ReviewMethod method, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, method.ToString());
    }

    [Fact]
    public void ReviewMethod_HasExpectedValues()
    {
        // Assert - per data-model.md entity 29
        var values = Enum.GetValues<ReviewMethod>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region ReviewOutcome Enum Tests

    [Theory]
    [InlineData(ReviewOutcome.Approved, "Approved")]
    [InlineData(ReviewOutcome.ConditionallyApproved, "ConditionallyApproved")]
    [InlineData(ReviewOutcome.Rejected, "Rejected")]
    public void ReviewOutcome_AllOutcomesAreDefined(ReviewOutcome outcome, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, outcome.ToString());
    }

    [Fact]
    public void ReviewOutcome_HasExpectedValues()
    {
        // Assert - per data-model.md entity 29
        var values = Enum.GetValues<ReviewOutcome>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void QualificationReview_Validate_FailsWithEmptyEntityId()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.Empty,
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Maria Jansen"
        };

        // Act
        var result = review.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("EntityId"));
    }

    [Fact]
    public void QualificationReview_Validate_FailsWithMissingReviewerName()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = string.Empty
        };

        // Act
        var result = review.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ReviewerName"));
    }

    [Fact]
    public void QualificationReview_Validate_FailsWithFutureReviewDate()
    {
        // Arrange - review date cannot be in the future
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            ReviewMethod = ReviewMethod.OnSiteAudit,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Jan de Vries"
        };

        // Act
        var result = review.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ReviewDate") && v.Message.Contains("future"));
    }

    [Fact]
    public void QualificationReview_Validate_SucceedsWithValidData()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Maria Jansen",
            Notes = "Complete review conducted",
            NextReviewDate = DateOnly.FromDateTime(DateTime.Now.AddYears(3))
        };

        // Act
        var result = review.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void QualificationReview_Validate_AcceptsNullOptionalFields()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.ServiceProvider,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7)),
            ReviewMethod = ReviewMethod.Questionnaire,
            ReviewOutcome = ReviewOutcome.ConditionallyApproved,
            ReviewerName = "Pieter van Dam",
            Notes = null,
            NextReviewDate = null
        };

        // Act
        var result = review.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void QualificationReview_ForCustomer_HasCorrectEntityType()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var review = QualificationReview.CreateForCustomer(
            customerId,
            DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod.DocumentReview,
            ReviewOutcome.Approved,
            "Maria Jansen");

        // Assert
        Assert.Equal(ReviewEntityType.Customer, review.EntityType);
        Assert.Equal(customerId, review.EntityId);
    }

    [Fact]
    public void QualificationReview_ForServiceProvider_HasCorrectEntityType()
    {
        // Arrange
        var serviceProviderId = Guid.NewGuid();

        // Act
        var review = QualificationReview.CreateForServiceProvider(
            serviceProviderId,
            DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod.OnSiteAudit,
            ReviewOutcome.Approved,
            "Jan de Vries");

        // Assert
        Assert.Equal(ReviewEntityType.ServiceProvider, review.EntityType);
        Assert.Equal(serviceProviderId, review.EntityId);
    }

    [Fact]
    public void QualificationReview_IsApproved_ReturnsTrueForApprovedOutcome()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Maria Jansen"
        };

        // Act & Assert
        Assert.True(review.IsApproved);
    }

    [Fact]
    public void QualificationReview_IsApproved_ReturnsTrueForConditionallyApproved()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.Questionnaire,
            ReviewOutcome = ReviewOutcome.ConditionallyApproved,
            ReviewerName = "Jan de Vries"
        };

        // Act & Assert
        Assert.True(review.IsApproved);
    }

    [Fact]
    public void QualificationReview_IsApproved_ReturnsFalseForRejected()
    {
        // Arrange
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now),
            ReviewMethod = ReviewMethod.OnSiteAudit,
            ReviewOutcome = ReviewOutcome.Rejected,
            ReviewerName = "Pieter van Dam"
        };

        // Act & Assert
        Assert.False(review.IsApproved);
    }

    [Fact]
    public void QualificationReview_SetNextReviewDate_CalculatesCorrectly()
    {
        // Arrange
        var reviewDate = DateOnly.FromDateTime(DateTime.Now);
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.Customer,
            EntityId = Guid.NewGuid(),
            ReviewDate = reviewDate,
            ReviewMethod = ReviewMethod.DocumentReview,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "Maria Jansen"
        };

        // Act
        review.SetNextReviewDate(36); // 3 year review period

        // Assert
        Assert.Equal(reviewDate.AddMonths(36), review.NextReviewDate);
    }

    #endregion

    #region Service Provider Review Tests

    [Fact]
    public void QualificationReview_ServiceProviderReview_SupportsOnSiteAudit()
    {
        // Arrange - GDP service providers typically require on-site audits
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = ReviewEntityType.ServiceProvider,
            EntityId = Guid.NewGuid(),
            ReviewDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-14)),
            ReviewMethod = ReviewMethod.OnSiteAudit,
            ReviewOutcome = ReviewOutcome.Approved,
            ReviewerName = "GDP Auditor",
            Notes = "On-site GDP audit completed satisfactorily"
        };

        // Assert
        Assert.Equal(ReviewMethod.OnSiteAudit, review.ReviewMethod);
        Assert.True(review.Validate().IsValid);
    }

    #endregion
}
