using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpServiceProvider domain model.
/// T197: TDD tests for GdpServiceProvider per User Story 8 (FR-036, FR-037).
/// </summary>
public class GdpServiceProviderTests
{
    #region GdpServiceType Enum Tests

    [Theory]
    [InlineData(GdpServiceType.ThirdPartyLogistics, "ThirdPartyLogistics")]
    [InlineData(GdpServiceType.Transporter, "Transporter")]
    [InlineData(GdpServiceType.ExternalWarehouse, "ExternalWarehouse")]
    public void GdpServiceType_AllTypesAreDefined(GdpServiceType serviceType, string expectedName)
    {
        Assert.Equal(expectedName, serviceType.ToString());
    }

    [Fact]
    public void GdpServiceType_HasExpectedValues()
    {
        var values = Enum.GetValues<GdpServiceType>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region ReviewMethod Enum Tests

    [Theory]
    [InlineData(ReviewMethod.OnSiteAudit, "OnSiteAudit")]
    [InlineData(ReviewMethod.Questionnaire, "Questionnaire")]
    [InlineData(ReviewMethod.DocumentReview, "DocumentReview")]
    public void ReviewMethod_AllMethodsAreDefined(ReviewMethod method, string expectedName)
    {
        Assert.Equal(expectedName, method.ToString());
    }

    #endregion

    #region ReviewOutcome Enum Tests

    [Theory]
    [InlineData(ReviewOutcome.Approved, "Approved")]
    [InlineData(ReviewOutcome.ConditionallyApproved, "ConditionallyApproved")]
    [InlineData(ReviewOutcome.Rejected, "Rejected")]
    public void ReviewOutcome_AllOutcomesAreDefined(ReviewOutcome outcome, string expectedName)
    {
        Assert.Equal(expectedName, outcome.ToString());
    }

    [Fact]
    public void ReviewOutcome_HasExpectedValues()
    {
        var values = Enum.GetValues<ReviewOutcome>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region GdpVerificationMethod Enum Tests (in GdpCredentialVerification.cs)

    [Theory]
    [InlineData(GdpVerificationMethod.EudraGMDP, "EudraGMDP")]
    [InlineData(GdpVerificationMethod.NationalDatabase, "NationalDatabase")]
    [InlineData(GdpVerificationMethod.Other, "Other")]
    public void GdpVerificationMethod_AllMethodsAreDefined(GdpVerificationMethod method, string expectedName)
    {
        Assert.Equal(expectedName, method.ToString());
    }

    #endregion

    #region GdpVerificationOutcome Enum Tests (in GdpCredentialVerification.cs)

    [Theory]
    [InlineData(GdpVerificationOutcome.Valid, "Valid")]
    [InlineData(GdpVerificationOutcome.Invalid, "Invalid")]
    [InlineData(GdpVerificationOutcome.NotFound, "NotFound")]
    public void GdpVerificationOutcome_AllOutcomesAreDefined(GdpVerificationOutcome outcome, string expectedName)
    {
        Assert.Equal(expectedName, outcome.ToString());
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void GdpServiceProvider_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var providerId = Guid.NewGuid();
        var provider = new GdpServiceProvider
        {
            ProviderId = providerId,
            ProviderName = "MedTrans NL B.V.",
            ServiceType = GdpServiceType.Transporter,
            TemperatureControlledCapability = true,
            ApprovedRoutes = "Amsterdam-Rotterdam, Amsterdam-Utrecht",
            QualificationStatus = GdpQualificationStatus.Approved,
            ReviewFrequencyMonths = 24,
            LastReviewDate = new DateOnly(2025, 1, 15),
            NextReviewDate = new DateOnly(2027, 1, 15),
            IsActive = true
        };

        // Assert
        Assert.Equal(providerId, provider.ProviderId);
        Assert.Equal("MedTrans NL B.V.", provider.ProviderName);
        Assert.Equal(GdpServiceType.Transporter, provider.ServiceType);
        Assert.True(provider.TemperatureControlledCapability);
        Assert.Equal("Amsterdam-Rotterdam, Amsterdam-Utrecht", provider.ApprovedRoutes);
        Assert.Equal(GdpQualificationStatus.Approved, provider.QualificationStatus);
        Assert.Equal(24, provider.ReviewFrequencyMonths);
        Assert.Equal(new DateOnly(2025, 1, 15), provider.LastReviewDate);
        Assert.Equal(new DateOnly(2027, 1, 15), provider.NextReviewDate);
        Assert.True(provider.IsActive);
    }

    [Fact]
    public void GdpServiceProvider_IsActive_DefaultsToTrue()
    {
        var provider = new GdpServiceProvider();
        Assert.True(provider.IsActive);
    }

    [Fact]
    public void GdpServiceProvider_DefaultQualificationStatus_IsUnderReview()
    {
        var provider = new GdpServiceProvider();
        Assert.Equal(GdpQualificationStatus.UnderReview, provider.QualificationStatus);
    }

    #endregion

    #region IsApproved Tests

    [Fact]
    public void GdpServiceProvider_IsApproved_ReturnsTrueWhenApproved()
    {
        var provider = new GdpServiceProvider { QualificationStatus = GdpQualificationStatus.Approved };
        Assert.True(provider.IsApproved());
    }

    [Fact]
    public void GdpServiceProvider_IsApproved_ReturnsTrueWhenConditionallyApproved()
    {
        var provider = new GdpServiceProvider { QualificationStatus = GdpQualificationStatus.ConditionallyApproved };
        Assert.True(provider.IsApproved());
    }

    [Fact]
    public void GdpServiceProvider_IsApproved_ReturnsFalseWhenRejected()
    {
        var provider = new GdpServiceProvider { QualificationStatus = GdpQualificationStatus.Rejected };
        Assert.False(provider.IsApproved());
    }

    [Fact]
    public void GdpServiceProvider_IsApproved_ReturnsFalseWhenUnderReview()
    {
        var provider = new GdpServiceProvider { QualificationStatus = GdpQualificationStatus.UnderReview };
        Assert.False(provider.IsApproved());
    }

    #endregion

    #region CanBeSelected Tests

    [Fact]
    public void GdpServiceProvider_CanBeSelected_ReturnsTrueWhenActiveAndApproved()
    {
        var provider = new GdpServiceProvider
        {
            IsActive = true,
            QualificationStatus = GdpQualificationStatus.Approved
        };
        Assert.True(provider.CanBeSelected());
    }

    [Fact]
    public void GdpServiceProvider_CanBeSelected_ReturnsFalseWhenInactive()
    {
        var provider = new GdpServiceProvider
        {
            IsActive = false,
            QualificationStatus = GdpQualificationStatus.Approved
        };
        Assert.False(provider.CanBeSelected());
    }

    [Fact]
    public void GdpServiceProvider_CanBeSelected_ReturnsFalseWhenRejected()
    {
        var provider = new GdpServiceProvider
        {
            IsActive = true,
            QualificationStatus = GdpQualificationStatus.Rejected
        };
        Assert.False(provider.CanBeSelected());
    }

    [Fact]
    public void GdpServiceProvider_CanBeSelected_ReturnsTrueWhenConditionallyApproved()
    {
        var provider = new GdpServiceProvider
        {
            IsActive = true,
            QualificationStatus = GdpQualificationStatus.ConditionallyApproved
        };
        Assert.True(provider.CanBeSelected());
    }

    #endregion

    #region IsReviewDue Tests

    [Fact]
    public void GdpServiceProvider_IsReviewDue_ReturnsTrueWhenPastNextReviewDate()
    {
        var provider = new GdpServiceProvider
        {
            NextReviewDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
        };
        Assert.True(provider.IsReviewDue());
    }

    [Fact]
    public void GdpServiceProvider_IsReviewDue_ReturnsFalseWhenFutureNextReviewDate()
    {
        var provider = new GdpServiceProvider
        {
            NextReviewDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)
        };
        Assert.False(provider.IsReviewDue());
    }

    [Fact]
    public void GdpServiceProvider_IsReviewDue_ReturnsFalseWhenNoReviewDateSet()
    {
        var provider = new GdpServiceProvider { NextReviewDate = null };
        Assert.False(provider.IsReviewDue());
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void GdpServiceProvider_Validate_SucceedsWithValidData()
    {
        var provider = new GdpServiceProvider
        {
            ProviderId = Guid.NewGuid(),
            ProviderName = "MedTrans NL B.V.",
            ServiceType = GdpServiceType.Transporter,
            ReviewFrequencyMonths = 24
        };

        var result = provider.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void GdpServiceProvider_Validate_FailsWithEmptyProviderName()
    {
        var provider = new GdpServiceProvider
        {
            ProviderName = string.Empty,
            ReviewFrequencyMonths = 24
        };

        var result = provider.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ProviderName"));
    }

    [Fact]
    public void GdpServiceProvider_Validate_FailsWithZeroReviewFrequency()
    {
        var provider = new GdpServiceProvider
        {
            ProviderName = "MedTrans NL B.V.",
            ReviewFrequencyMonths = 0
        };

        var result = provider.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ReviewFrequencyMonths"));
    }

    [Fact]
    public void GdpServiceProvider_Validate_FailsWhenNextReviewDateBeforeLastReviewDate()
    {
        var provider = new GdpServiceProvider
        {
            ProviderName = "MedTrans NL B.V.",
            ReviewFrequencyMonths = 24,
            LastReviewDate = new DateOnly(2025, 6, 1),
            NextReviewDate = new DateOnly(2025, 1, 1)
        };

        var result = provider.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("NextReviewDate"));
    }

    [Fact]
    public void GdpServiceProvider_Validate_ReportsMultipleViolations()
    {
        var provider = new GdpServiceProvider
        {
            ProviderName = string.Empty,
            ReviewFrequencyMonths = 0
        };

        var result = provider.Validate();

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Violations.Count);
    }

    #endregion
}
