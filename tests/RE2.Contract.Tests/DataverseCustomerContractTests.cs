using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse CustomerComplianceExtension entity.
/// T083: Verifies DTO-to-domain and domain-to-DTO mapping contracts.
/// Uses CustomerComplianceExtensionDto with composite key: phr_customeraccount + phr_dataareaid.
/// </summary>
public class DataverseCustomerContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void CustomerComplianceExtensionDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(CustomerComplianceExtensionDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_complianceextensionid");
        properties.Should().Contain(p => p.Name == "phr_customeraccount");
        properties.Should().Contain(p => p.Name == "phr_dataareaid");
        properties.Should().Contain(p => p.Name == "phr_businesscategory");
        properties.Should().Contain(p => p.Name == "phr_approvalstatus");
        properties.Should().Contain(p => p.Name == "phr_onboardingdate");
        properties.Should().Contain(p => p.Name == "phr_nextreverificationdate");
        properties.Should().Contain(p => p.Name == "phr_gdpqualificationstatus");
        properties.Should().Contain(p => p.Name == "phr_issuspended");
        properties.Should().Contain(p => p.Name == "phr_suspensionreason");
        properties.Should().Contain(p => p.Name == "phr_createddate");
        properties.Should().Contain(p => p.Name == "phr_modifieddate");
        properties.Should().Contain(p => p.Name == "phr_rowversion");
    }

    [Fact]
    public void CustomerComplianceExtensionDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(CustomerComplianceExtensionDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_complianceextensionid");

        // Assert
        primaryKeyProperty.Should().NotBeNull();
        primaryKeyProperty!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void CustomerComplianceExtensionDto_CompositeKey_ShouldBeStrings()
    {
        // Arrange
        var dtoType = typeof(CustomerComplianceExtensionDto);
        var accountProperty = dtoType.GetProperty("phr_customeraccount");
        var dataAreaProperty = dtoType.GetProperty("phr_dataareaid");

        // Assert
        accountProperty.Should().NotBeNull();
        accountProperty!.PropertyType.Should().Be(typeof(string));
        dataAreaProperty.Should().NotBeNull();
        dataAreaProperty!.PropertyType.Should().Be(typeof(string));
    }

    #endregion

    #region DTO to Domain Model Mapping Tests

    [Fact]
    public void ToDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var complianceExtensionId = Guid.NewGuid();
        var onboardingDate = DateTime.Now.AddMonths(-6);
        var reVerificationDate = DateTime.Now.AddYears(2).AddMonths(6);
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = complianceExtensionId,
            phr_customeraccount = "CUST-001",
            phr_dataareaid = "nlpd",
            phr_businesscategory = (int)BusinessCategory.HospitalPharmacy,
            phr_approvalstatus = (int)ApprovalStatus.Approved,
            phr_onboardingdate = onboardingDate,
            phr_nextreverificationdate = reVerificationDate,
            phr_gdpqualificationstatus = (int)GdpQualificationStatus.Approved,
            phr_issuspended = false,
            phr_suspensionreason = null,
            phr_createddate = createdDate,
            phr_modifieddate = modifiedDate,
            phr_rowversion = rowVersion
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.ComplianceExtensionId.Should().Be(complianceExtensionId);
        domainModel.CustomerAccount.Should().Be("CUST-001");
        domainModel.DataAreaId.Should().Be("nlpd");
        domainModel.BusinessCategory.Should().Be(BusinessCategory.HospitalPharmacy);
        domainModel.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        domainModel.OnboardingDate.Should().Be(DateOnly.FromDateTime(onboardingDate));
        domainModel.NextReVerificationDate.Should().Be(DateOnly.FromDateTime(reVerificationDate));
        domainModel.GdpQualificationStatus.Should().Be(GdpQualificationStatus.Approved);
        domainModel.IsSuspended.Should().BeFalse();
        domainModel.SuspensionReason.Should().BeNull();
        domainModel.CreatedDate.Should().Be(createdDate);
        domainModel.ModifiedDate.Should().Be(modifiedDate);
        domainModel.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = null,
            phr_dataareaid = null,
            phr_businesscategory = 0,
            phr_approvalstatus = 0,
            phr_onboardingdate = null,
            phr_nextreverificationdate = null,
            phr_gdpqualificationstatus = 0,
            phr_issuspended = false,
            phr_suspensionreason = null,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow,
            phr_rowversion = null
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.CustomerAccount.Should().BeEmpty();
        domainModel.DataAreaId.Should().BeEmpty();
        domainModel.OnboardingDate.Should().BeNull();
        domainModel.NextReVerificationDate.Should().BeNull();
        domainModel.SuspensionReason.Should().BeNull();
        domainModel.RowVersion.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, BusinessCategory.HospitalPharmacy)]
    [InlineData(1, BusinessCategory.CommunityPharmacy)]
    [InlineData(2, BusinessCategory.Veterinarian)]
    [InlineData(3, BusinessCategory.Manufacturer)]
    [InlineData(4, BusinessCategory.WholesalerEU)]
    [InlineData(5, BusinessCategory.WholesalerNonEU)]
    [InlineData(6, BusinessCategory.ResearchInstitution)]
    public void ToDomainModel_ShouldMapBusinessCategory_Correctly(int dtoValue, BusinessCategory expected)
    {
        // Arrange
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = "CUST-001",
            phr_dataareaid = "nlpd",
            phr_businesscategory = dtoValue,
            phr_approvalstatus = 0,
            phr_gdpqualificationstatus = 0,
            phr_issuspended = false,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.BusinessCategory.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, ApprovalStatus.Pending)]
    [InlineData(1, ApprovalStatus.Approved)]
    [InlineData(2, ApprovalStatus.ConditionallyApproved)]
    [InlineData(3, ApprovalStatus.Rejected)]
    [InlineData(4, ApprovalStatus.Suspended)]
    public void ToDomainModel_ShouldMapApprovalStatus_Correctly(int dtoValue, ApprovalStatus expected)
    {
        // Arrange
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = "CUST-001",
            phr_dataareaid = "nlpd",
            phr_businesscategory = 0,
            phr_approvalstatus = dtoValue,
            phr_gdpqualificationstatus = 0,
            phr_issuspended = false,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.ApprovalStatus.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, GdpQualificationStatus.NotRequired)]
    [InlineData(1, GdpQualificationStatus.Pending)]
    [InlineData(2, GdpQualificationStatus.Approved)]
    [InlineData(3, GdpQualificationStatus.ConditionallyApproved)]
    [InlineData(4, GdpQualificationStatus.Rejected)]
    [InlineData(5, GdpQualificationStatus.UnderReview)]
    public void ToDomainModel_ShouldMapGdpQualificationStatus_Correctly(int dtoValue, GdpQualificationStatus expected)
    {
        // Arrange
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = "CUST-001",
            phr_dataareaid = "nlpd",
            phr_businesscategory = 0,
            phr_approvalstatus = 0,
            phr_gdpqualificationstatus = dtoValue,
            phr_issuspended = false,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.GdpQualificationStatus.Should().Be(expected);
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var complianceExtensionId = Guid.NewGuid();
        var onboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));
        var reVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2).AddMonths(6));
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var domainModel = new Customer
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            OrganizationName = "Apotheek Van der Berg",
            AddressCountryRegionId = "NL",
            ComplianceExtensionId = complianceExtensionId,
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = onboardingDate,
            NextReVerificationDate = reVerificationDate,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            SuspensionReason = null,
            CreatedDate = createdDate,
            ModifiedDate = modifiedDate,
            RowVersion = rowVersion
        };

        // Act
        var dto = CustomerComplianceExtensionDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_complianceextensionid.Should().Be(complianceExtensionId);
        dto.phr_customeraccount.Should().Be("CUST-001");
        dto.phr_dataareaid.Should().Be("nlpd");
        dto.phr_businesscategory.Should().Be((int)BusinessCategory.CommunityPharmacy);
        dto.phr_approvalstatus.Should().Be((int)ApprovalStatus.Approved);
        dto.phr_onboardingdate.Should().Be(onboardingDate.ToDateTime(TimeOnly.MinValue));
        dto.phr_nextreverificationdate.Should().Be(reVerificationDate.ToDateTime(TimeOnly.MinValue));
        dto.phr_gdpqualificationstatus.Should().Be((int)GdpQualificationStatus.NotRequired);
        dto.phr_issuspended.Should().BeFalse();
        dto.phr_suspensionreason.Should().BeNull();
        dto.phr_createddate.Should().Be(createdDate);
        dto.phr_modifieddate.Should().Be(modifiedDate);
        dto.phr_rowversion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullOptionalDates()
    {
        // Arrange
        var domainModel = new Customer
        {
            CustomerAccount = "CUST-002",
            DataAreaId = "nlpd",
            OrganizationName = "Minimal Customer",
            AddressCountryRegionId = "DE",
            BusinessCategory = BusinessCategory.ResearchInstitution,
            ApprovalStatus = ApprovalStatus.Pending,
            OnboardingDate = null,
            NextReVerificationDate = null,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        };

        // Act
        var dto = CustomerComplianceExtensionDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_onboardingdate.Should().BeNull();
        dto.phr_nextreverificationdate.Should().BeNull();
        dto.phr_rowversion.Should().BeNull();
    }

    [Fact]
    public void FromDomainModel_ShouldMapSuspendedCustomer()
    {
        // Arrange
        var domainModel = new Customer
        {
            CustomerAccount = "CUST-003",
            DataAreaId = "nlpd",
            OrganizationName = "Suspended Veterinary Clinic",
            AddressCountryRegionId = "NL",
            ComplianceExtensionId = Guid.NewGuid(),
            BusinessCategory = BusinessCategory.Veterinarian,
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = true,
            SuspensionReason = "Under investigation by IGJ",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = CustomerComplianceExtensionDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_issuspended.Should().BeTrue();
        dto.phr_suspensionreason.Should().Be("Under investigation by IGJ");
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DomainToDto_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new Customer
        {
            CustomerAccount = "CUST-004",
            DataAreaId = "nlpd",
            OrganizationName = "Wholesaler EU Partner",
            AddressCountryRegionId = "BE",
            ComplianceExtensionId = Guid.NewGuid(),
            BusinessCategory = BusinessCategory.WholesalerEU,
            ApprovalStatus = ApprovalStatus.ConditionallyApproved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(3).AddMonths(-3)),
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow.AddMonths(-3),
            ModifiedDate = DateTime.UtcNow,
            RowVersion = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 }
        };

        // Act
        var dto = CustomerComplianceExtensionDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.ComplianceExtensionId.Should().Be(original.ComplianceExtensionId);
        roundTripped.CustomerAccount.Should().Be(original.CustomerAccount);
        roundTripped.DataAreaId.Should().Be(original.DataAreaId);
        roundTripped.BusinessCategory.Should().Be(original.BusinessCategory);
        roundTripped.ApprovalStatus.Should().Be(original.ApprovalStatus);
        roundTripped.OnboardingDate.Should().Be(original.OnboardingDate);
        roundTripped.NextReVerificationDate.Should().Be(original.NextReVerificationDate);
        roundTripped.GdpQualificationStatus.Should().Be(original.GdpQualificationStatus);
        roundTripped.IsSuspended.Should().Be(original.IsSuspended);
        roundTripped.SuspensionReason.Should().Be(original.SuspensionReason);
        roundTripped.CreatedDate.Should().Be(original.CreatedDate);
        roundTripped.ModifiedDate.Should().Be(original.ModifiedDate);
        roundTripped.RowVersion.Should().BeEquivalentTo(original.RowVersion);
    }

    [Fact]
    public void RoundTrip_SuspendedCustomer_ShouldPreserveSuspensionData()
    {
        // Arrange
        var original = new Customer
        {
            CustomerAccount = "CUST-005",
            DataAreaId = "nlpd",
            OrganizationName = "Previously Active Customer",
            AddressCountryRegionId = "FR",
            ComplianceExtensionId = Guid.NewGuid(),
            BusinessCategory = BusinessCategory.Manufacturer,
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = true,
            SuspensionReason = "Multiple suspicious order reports under investigation",
            CreatedDate = DateTime.UtcNow.AddYears(-2),
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = CustomerComplianceExtensionDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.IsSuspended.Should().BeTrue();
        roundTripped.SuspensionReason.Should().Be(original.SuspensionReason);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void ToDomainModel_CanTransact_ReturnsCorrectValue()
    {
        // Arrange - Approved, not suspended customer
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = "CUST-001",
            phr_dataareaid = "nlpd",
            phr_businesscategory = (int)BusinessCategory.CommunityPharmacy,
            phr_approvalstatus = (int)ApprovalStatus.Approved,
            phr_gdpqualificationstatus = (int)GdpQualificationStatus.NotRequired,
            phr_issuspended = false,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.CanTransact().Should().BeTrue();
    }

    [Fact]
    public void ToDomainModel_CanTransact_ReturnsFalseForSuspended()
    {
        // Arrange - Approved but suspended customer
        var dto = new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_customeraccount = "CUST-002",
            phr_dataareaid = "nlpd",
            phr_businesscategory = (int)BusinessCategory.CommunityPharmacy,
            phr_approvalstatus = (int)ApprovalStatus.Approved,
            phr_gdpqualificationstatus = (int)GdpQualificationStatus.NotRequired,
            phr_issuspended = true,
            phr_suspensionreason = "Under investigation",
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.CanTransact().Should().BeFalse();
    }

    #endregion
}
