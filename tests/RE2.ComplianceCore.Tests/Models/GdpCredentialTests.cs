using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpCredential domain model.
/// T196: TDD tests for GdpCredential per User Story 8 (FR-036, FR-037).
/// </summary>
public class GdpCredentialTests
{
    #region GdpCredentialEntityType Enum Tests

    [Theory]
    [InlineData(GdpCredentialEntityType.Supplier, "Supplier")]
    [InlineData(GdpCredentialEntityType.ServiceProvider, "ServiceProvider")]
    public void GdpCredentialEntityType_AllTypesAreDefined(GdpCredentialEntityType entityType, string expectedName)
    {
        Assert.Equal(expectedName, entityType.ToString());
    }

    [Fact]
    public void GdpCredentialEntityType_HasExpectedValues()
    {
        var values = Enum.GetValues<GdpCredentialEntityType>();
        Assert.Equal(2, values.Length);
    }

    #endregion

    #region GdpQualificationStatus Enum Tests (reused from Customer)

    [Theory]
    [InlineData(GdpQualificationStatus.Approved, "Approved")]
    [InlineData(GdpQualificationStatus.ConditionallyApproved, "ConditionallyApproved")]
    [InlineData(GdpQualificationStatus.Rejected, "Rejected")]
    [InlineData(GdpQualificationStatus.UnderReview, "UnderReview")]
    public void GdpQualificationStatus_ContainsQualificationValues(GdpQualificationStatus status, string expectedName)
    {
        Assert.Equal(expectedName, status.ToString());
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void GdpCredential_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var credentialId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var credential = new GdpCredential
        {
            CredentialId = credentialId,
            EntityType = GdpCredentialEntityType.ServiceProvider,
            EntityId = entityId,
            WdaNumber = "WDA-NL-2024-001",
            GdpCertificateNumber = "GDP-NL-2024-001",
            EudraGmdpEntryUrl = "https://eudragmdp.ema.europa.eu/entry/12345",
            ValidityStartDate = new DateOnly(2024, 1, 1),
            ValidityEndDate = new DateOnly(2029, 1, 1),
            QualificationStatus = GdpQualificationStatus.Approved,
            LastVerificationDate = new DateOnly(2025, 6, 15),
            NextReviewDate = new DateOnly(2027, 1, 1)
        };

        // Assert
        Assert.Equal(credentialId, credential.CredentialId);
        Assert.Equal(GdpCredentialEntityType.ServiceProvider, credential.EntityType);
        Assert.Equal(entityId, credential.EntityId);
        Assert.Equal("WDA-NL-2024-001", credential.WdaNumber);
        Assert.Equal("GDP-NL-2024-001", credential.GdpCertificateNumber);
        Assert.Equal("https://eudragmdp.ema.europa.eu/entry/12345", credential.EudraGmdpEntryUrl);
        Assert.Equal(new DateOnly(2024, 1, 1), credential.ValidityStartDate);
        Assert.Equal(new DateOnly(2029, 1, 1), credential.ValidityEndDate);
        Assert.Equal(GdpQualificationStatus.Approved, credential.QualificationStatus);
        Assert.Equal(new DateOnly(2025, 6, 15), credential.LastVerificationDate);
        Assert.Equal(new DateOnly(2027, 1, 1), credential.NextReviewDate);
    }

    [Fact]
    public void GdpCredential_DefaultQualificationStatus_IsUnderReview()
    {
        var credential = new GdpCredential();
        Assert.Equal(GdpQualificationStatus.UnderReview, credential.QualificationStatus);
    }

    [Fact]
    public void GdpCredential_RowVersion_DefaultsToEmptyArray()
    {
        var credential = new GdpCredential();
        Assert.NotNull(credential.RowVersion);
        Assert.Empty(credential.RowVersion);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void GdpCredential_IsValid_ReturnsTrueWhenNotExpired()
    {
        var credential = new GdpCredential
        {
            ValidityStartDate = new DateOnly(2024, 1, 1),
            ValidityEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)
        };

        Assert.True(credential.IsValid());
    }

    [Fact]
    public void GdpCredential_IsValid_ReturnsFalseWhenExpired()
    {
        var credential = new GdpCredential
        {
            ValidityStartDate = new DateOnly(2020, 1, 1),
            ValidityEndDate = new DateOnly(2023, 1, 1)
        };

        Assert.False(credential.IsValid());
    }

    [Fact]
    public void GdpCredential_IsValid_ReturnsTrueWhenNoExpiryDate()
    {
        var credential = new GdpCredential
        {
            ValidityStartDate = new DateOnly(2024, 1, 1),
            ValidityEndDate = null
        };

        Assert.True(credential.IsValid());
    }

    [Fact]
    public void GdpCredential_IsValid_ReturnsFalseWhenNotYetStarted()
    {
        var credential = new GdpCredential
        {
            ValidityStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            ValidityEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2)
        };

        Assert.False(credential.IsValid());
    }

    [Fact]
    public void GdpCredential_IsValid_ReturnsTrueWhenNoDatesSet()
    {
        var credential = new GdpCredential();
        Assert.True(credential.IsValid());
    }

    #endregion

    #region IsApproved Tests

    [Fact]
    public void GdpCredential_IsApproved_ReturnsTrueWhenApproved()
    {
        var credential = new GdpCredential { QualificationStatus = GdpQualificationStatus.Approved };
        Assert.True(credential.IsApproved());
    }

    [Fact]
    public void GdpCredential_IsApproved_ReturnsTrueWhenConditionallyApproved()
    {
        var credential = new GdpCredential { QualificationStatus = GdpQualificationStatus.ConditionallyApproved };
        Assert.True(credential.IsApproved());
    }

    [Fact]
    public void GdpCredential_IsApproved_ReturnsFalseWhenRejected()
    {
        var credential = new GdpCredential { QualificationStatus = GdpQualificationStatus.Rejected };
        Assert.False(credential.IsApproved());
    }

    [Fact]
    public void GdpCredential_IsApproved_ReturnsFalseWhenUnderReview()
    {
        var credential = new GdpCredential { QualificationStatus = GdpQualificationStatus.UnderReview };
        Assert.False(credential.IsApproved());
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void GdpCredential_Validate_SucceedsWithValidData()
    {
        var credential = new GdpCredential
        {
            CredentialId = Guid.NewGuid(),
            EntityType = GdpCredentialEntityType.ServiceProvider,
            EntityId = Guid.NewGuid(),
            WdaNumber = "WDA-NL-2024-001",
            ValidityStartDate = new DateOnly(2024, 1, 1),
            ValidityEndDate = new DateOnly(2029, 1, 1)
        };

        var result = credential.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void GdpCredential_Validate_SucceedsWithGdpCertificateNumberOnly()
    {
        var credential = new GdpCredential
        {
            CredentialId = Guid.NewGuid(),
            EntityType = GdpCredentialEntityType.Supplier,
            EntityId = Guid.NewGuid(),
            GdpCertificateNumber = "GDP-NL-2024-001"
        };

        var result = credential.Validate();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GdpCredential_Validate_FailsWithEmptyEntityId()
    {
        var credential = new GdpCredential
        {
            EntityType = GdpCredentialEntityType.ServiceProvider,
            EntityId = Guid.Empty,
            WdaNumber = "WDA-NL-2024-001"
        };

        var result = credential.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("EntityId"));
    }

    [Fact]
    public void GdpCredential_Validate_FailsWithNoCredentialNumbers()
    {
        var credential = new GdpCredential
        {
            EntityType = GdpCredentialEntityType.ServiceProvider,
            EntityId = Guid.NewGuid(),
            WdaNumber = null,
            GdpCertificateNumber = null
        };

        var result = credential.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("WdaNumber") || v.Message.Contains("GdpCertificateNumber"));
    }

    [Fact]
    public void GdpCredential_Validate_FailsWhenValidityEndDateBeforeStartDate()
    {
        var credential = new GdpCredential
        {
            EntityType = GdpCredentialEntityType.ServiceProvider,
            EntityId = Guid.NewGuid(),
            WdaNumber = "WDA-NL-2024-001",
            ValidityStartDate = new DateOnly(2025, 1, 1),
            ValidityEndDate = new DateOnly(2024, 1, 1)
        };

        var result = credential.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ValidityEndDate"));
    }

    [Fact]
    public void GdpCredential_Validate_ReportsMultipleViolations()
    {
        var credential = new GdpCredential
        {
            EntityId = Guid.Empty,
            WdaNumber = null,
            GdpCertificateNumber = null,
            ValidityStartDate = new DateOnly(2025, 1, 1),
            ValidityEndDate = new DateOnly(2024, 1, 1)
        };

        var result = credential.Validate();

        Assert.False(result.IsValid);
        Assert.True(result.Violations.Count >= 3);
    }

    #endregion
}
