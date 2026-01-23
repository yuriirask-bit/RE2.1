using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Customer domain model.
/// T081: Test-driven development for Customer per data-model.md entity 5.
/// </summary>
public class CustomerTests
{
    #region Property Initialization Tests

    [Fact]
    public void Customer_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var customerId = Guid.NewGuid();
        var customer = new Customer
        {
            CustomerId = customerId,
            BusinessName = "Ziekenhuis St. Elisabeth",
            RegistrationNumber = "KVK-12345678",
            BusinessCategory = BusinessCategory.HospitalPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2).AddMonths(6)),
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(customerId, customer.CustomerId);
        Assert.Equal("Ziekenhuis St. Elisabeth", customer.BusinessName);
        Assert.Equal("KVK-12345678", customer.RegistrationNumber);
        Assert.Equal(BusinessCategory.HospitalPharmacy, customer.BusinessCategory);
        Assert.Equal("NL", customer.Country);
        Assert.Equal(ApprovalStatus.Approved, customer.ApprovalStatus);
        Assert.False(customer.IsSuspended);
        Assert.Equal(GdpQualificationStatus.Approved, customer.GdpQualificationStatus);
    }

    [Fact]
    public void Customer_IsSuspended_DefaultsToFalse()
    {
        // Arrange & Act
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Test Pharmacy",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert
        Assert.False(customer.IsSuspended);
    }

    [Fact]
    public void Customer_RowVersion_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Test Customer",
            BusinessCategory = BusinessCategory.Veterinarian,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(customer.RowVersion);
        Assert.Empty(customer.RowVersion);
    }

    #endregion

    #region BusinessCategory Enum Tests

    [Theory]
    [InlineData(BusinessCategory.HospitalPharmacy, "HospitalPharmacy")]
    [InlineData(BusinessCategory.CommunityPharmacy, "CommunityPharmacy")]
    [InlineData(BusinessCategory.Veterinarian, "Veterinarian")]
    [InlineData(BusinessCategory.Manufacturer, "Manufacturer")]
    [InlineData(BusinessCategory.WholesalerEU, "WholesalerEU")]
    [InlineData(BusinessCategory.WholesalerNonEU, "WholesalerNonEU")]
    [InlineData(BusinessCategory.ResearchInstitution, "ResearchInstitution")]
    public void BusinessCategory_AllCategoriesAreDefined(BusinessCategory category, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, category.ToString());
    }

    [Fact]
    public void BusinessCategory_HasExpectedValues()
    {
        // Assert - all 7 business categories per data-model.md entity 5
        var values = Enum.GetValues<BusinessCategory>();
        Assert.Equal(7, values.Length);
    }

    #endregion

    #region ApprovalStatus Enum Tests

    [Theory]
    [InlineData(ApprovalStatus.Pending, "Pending")]
    [InlineData(ApprovalStatus.Approved, "Approved")]
    [InlineData(ApprovalStatus.ConditionallyApproved, "ConditionallyApproved")]
    [InlineData(ApprovalStatus.Rejected, "Rejected")]
    [InlineData(ApprovalStatus.Suspended, "Suspended")]
    public void ApprovalStatus_AllStatusesAreDefined(ApprovalStatus status, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void ApprovalStatus_HasExpectedValues()
    {
        // Assert - all 5 approval statuses per data-model.md entity 5
        var values = Enum.GetValues<ApprovalStatus>();
        Assert.Equal(5, values.Length);
    }

    #endregion

    #region GdpQualificationStatus Enum Tests

    [Theory]
    [InlineData(GdpQualificationStatus.NotRequired, "NotRequired")]
    [InlineData(GdpQualificationStatus.Pending, "Pending")]
    [InlineData(GdpQualificationStatus.Approved, "Approved")]
    [InlineData(GdpQualificationStatus.ConditionallyApproved, "ConditionallyApproved")]
    [InlineData(GdpQualificationStatus.Rejected, "Rejected")]
    [InlineData(GdpQualificationStatus.UnderReview, "UnderReview")]
    public void GdpQualificationStatus_AllStatusesAreDefined(GdpQualificationStatus status, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void GdpQualificationStatus_HasExpectedValues()
    {
        // Assert - all 6 GDP qualification statuses per data-model.md entity 5
        var values = Enum.GetValues<GdpQualificationStatus>();
        Assert.Equal(6, values.Length);
    }

    #endregion

    #region CanTransact Business Logic Tests

    [Theory]
    [InlineData(ApprovalStatus.Approved, false, true)]
    [InlineData(ApprovalStatus.ConditionallyApproved, false, true)]
    [InlineData(ApprovalStatus.Pending, false, false)]
    [InlineData(ApprovalStatus.Rejected, false, false)]
    [InlineData(ApprovalStatus.Suspended, false, false)]
    [InlineData(ApprovalStatus.Approved, true, false)] // Suspended overrides approval
    [InlineData(ApprovalStatus.ConditionallyApproved, true, false)] // Suspended overrides approval
    public void Customer_CanTransact_ReturnsExpectedResult(
        ApprovalStatus approvalStatus,
        bool isSuspended,
        bool expectedCanTransact)
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = approvalStatus,
            IsSuspended = isSuspended,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var canTransact = customer.CanTransact();

        // Assert
        Assert.Equal(expectedCanTransact, canTransact);
    }

    [Fact]
    public void Customer_CanTransact_SuspensionBlocksAllTransactions()
    {
        // Arrange - per data-model.md: IsSuspended = true blocks all transactions regardless of ApprovalStatus
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Suspended Pharmacy",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            IsSuspended = true,
            SuspensionReason = "Under investigation by IGJ",
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act & Assert
        Assert.False(customer.CanTransact());
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Customer_Validate_FailsWithMissingBusinessName()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = string.Empty,
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("BusinessName"));
    }

    [Fact]
    public void Customer_Validate_FailsWithMissingCountry()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = string.Empty,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Country"));
    }

    [Fact]
    public void Customer_Validate_FailsWithInvalidCountryCode()
    {
        // Arrange - country should be ISO 3166-1 alpha-2 (2 characters)
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "Netherlands", // Should be "NL"
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Country") && v.Message.Contains("ISO 3166-1"));
    }

    [Fact]
    public void Customer_Validate_SucceedsWithValidData()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Valid Pharmacy",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Customer_Validate_AcceptsNullOptionalFields()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Minimal Customer",
            RegistrationNumber = null,
            BusinessCategory = BusinessCategory.ResearchInstitution,
            Country = "DE",
            ApprovalStatus = ApprovalStatus.Pending,
            OnboardingDate = null,
            NextReVerificationDate = null,
            SuspensionReason = null,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Suspension Tests

    [Fact]
    public void Customer_Suspend_SetsSuspendedStatusWithReason()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Active Customer",
            BusinessCategory = BusinessCategory.Manufacturer,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            IsSuspended = false,
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        customer.Suspend("Multiple suspicious order reports");

        // Assert
        Assert.True(customer.IsSuspended);
        Assert.Equal("Multiple suspicious order reports", customer.SuspensionReason);
    }

    [Fact]
    public void Customer_Reinstate_ClearsSuspensionStatus()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Suspended Customer",
            BusinessCategory = BusinessCategory.WholesalerEU,
            Country = "BE",
            ApprovalStatus = ApprovalStatus.Approved,
            IsSuspended = true,
            SuspensionReason = "Investigation completed",
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        customer.Reinstate();

        // Assert
        Assert.False(customer.IsSuspended);
        Assert.Null(customer.SuspensionReason);
    }

    #endregion

    #region Re-Verification Date Tests

    [Fact]
    public void Customer_SetNextReVerificationDate_CalculatesFromOnboardingDate()
    {
        // Arrange - per FR-017: periodic re-verification
        var onboardingDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Customer for Reverification",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = onboardingDate,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act - 36 month re-verification period
        customer.SetNextReVerificationDate(36);

        // Assert
        var expectedDate = onboardingDate.AddMonths(36);
        Assert.Equal(expectedDate, customer.NextReVerificationDate);
    }

    [Fact]
    public void Customer_IsReVerificationDue_ReturnsTrue_WhenDatePassed()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Overdue Customer",
            BusinessCategory = BusinessCategory.HospitalPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-4)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var isDue = customer.IsReVerificationDue();

        // Assert
        Assert.True(isDue);
    }

    [Fact]
    public void Customer_IsReVerificationDue_ReturnsFalse_WhenDateInFuture()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = "Current Customer",
            BusinessCategory = BusinessCategory.HospitalPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2)),
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var isDue = customer.IsReVerificationDue();

        // Assert
        Assert.False(isDue);
    }

    #endregion
}
