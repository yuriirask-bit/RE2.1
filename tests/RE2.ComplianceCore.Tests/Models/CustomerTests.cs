using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Customer domain model.
/// T081: Test-driven development for Customer per data-model.md entity 5.
/// Composite key: CustomerAccount (string) + DataAreaId (string).
/// </summary>
public class CustomerTests
{
    #region Property Initialization Tests

    [Fact]
    public void Customer_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var complianceExtensionId = Guid.NewGuid();
        var customer = new Customer
        {
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Ziekenhuis St. Elisabeth",
            AddressCountryRegionId = "NL",
            ComplianceExtensionId = complianceExtensionId,
            BusinessCategory = BusinessCategory.HospitalPharmacy,
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2).AddMonths(6)),
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("TEST-001", customer.CustomerAccount);
        Assert.Equal("nlpd", customer.DataAreaId);
        Assert.Equal("Ziekenhuis St. Elisabeth", customer.OrganizationName);
        Assert.Equal("Ziekenhuis St. Elisabeth", customer.BusinessName); // computed alias
        Assert.Equal("NL", customer.AddressCountryRegionId);
        Assert.Equal("NL", customer.Country); // computed alias
        Assert.Equal(complianceExtensionId, customer.ComplianceExtensionId);
        Assert.Equal(BusinessCategory.HospitalPharmacy, customer.BusinessCategory);
        Assert.Equal(ApprovalStatus.Approved, customer.ApprovalStatus);
        Assert.False(customer.IsSuspended);
        Assert.Equal(GdpQualificationStatus.Approved, customer.GdpQualificationStatus);
        Assert.True(customer.IsComplianceConfigured);
    }

    [Fact]
    public void Customer_IsSuspended_DefaultsToFalse()
    {
        // Arrange & Act
        var customer = new Customer
        {
            CustomerAccount = "TEST-002",
            DataAreaId = "nlpd",
            OrganizationName = "Test Pharmacy",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
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
            CustomerAccount = "TEST-003",
            DataAreaId = "nlpd",
            OrganizationName = "Test Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.Veterinarian,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(customer.RowVersion);
        Assert.Empty(customer.RowVersion);
    }

    [Fact]
    public void Customer_IsComplianceConfigured_ReturnsFalse_WhenNoExtensionId()
    {
        // Arrange & Act
        var customer = new Customer
        {
            CustomerAccount = "TEST-004",
            DataAreaId = "nlpd",
            OrganizationName = "Not Configured Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Assert - ComplianceExtensionId defaults to Guid.Empty
        Assert.False(customer.IsComplianceConfigured);
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Test Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Suspended Pharmacy",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
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
    public void Customer_Validate_FailsWithMissingCustomerAccount()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerAccount = string.Empty,
            DataAreaId = "nlpd",
            OrganizationName = "Test Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("CustomerAccount"));
    }

    [Fact]
    public void Customer_Validate_FailsWithMissingDataAreaId()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerAccount = "TEST-001",
            DataAreaId = string.Empty,
            OrganizationName = "Test Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("DataAreaId"));
    }

    [Fact]
    public void Customer_Validate_SucceedsWithValidData()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Valid Pharmacy",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Minimal Customer",
            AddressCountryRegionId = "DE",
            BusinessCategory = BusinessCategory.ResearchInstitution,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Active Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.Manufacturer,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Suspended Customer",
            AddressCountryRegionId = "BE",
            BusinessCategory = BusinessCategory.WholesalerEU,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Customer for Reverification",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Overdue Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.HospitalPharmacy,
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
            CustomerAccount = "TEST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Current Customer",
            AddressCountryRegionId = "NL",
            BusinessCategory = BusinessCategory.HospitalPharmacy,
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
