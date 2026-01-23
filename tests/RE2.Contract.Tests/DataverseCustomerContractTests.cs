using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse Customer entity.
/// T083: Verifies DTO-to-domain and domain-to-DTO mapping contracts.
/// </summary>
public class DataverseCustomerContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void CustomerDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(CustomerDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_customerid");
        properties.Should().Contain(p => p.Name == "phr_businessname");
        properties.Should().Contain(p => p.Name == "phr_registrationnumber");
        properties.Should().Contain(p => p.Name == "phr_businesscategory");
        properties.Should().Contain(p => p.Name == "phr_country");
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
    public void CustomerDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(CustomerDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_customerid");

        // Assert
        primaryKeyProperty.Should().NotBeNull();
        primaryKeyProperty!.PropertyType.Should().Be(typeof(Guid));
    }

    #endregion

    #region DTO to Domain Model Mapping Tests

    [Fact]
    public void ToDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var onboardingDate = DateTime.Now.AddMonths(-6);
        var reVerificationDate = DateTime.Now.AddYears(2).AddMonths(6);
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var dto = new CustomerDto
        {
            phr_customerid = customerId,
            phr_businessname = "Ziekenhuis St. Elisabeth",
            phr_registrationnumber = "KVK-12345678",
            phr_businesscategory = (int)BusinessCategory.HospitalPharmacy,
            phr_country = "NL",
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
        domainModel.CustomerId.Should().Be(customerId);
        domainModel.BusinessName.Should().Be("Ziekenhuis St. Elisabeth");
        domainModel.RegistrationNumber.Should().Be("KVK-12345678");
        domainModel.BusinessCategory.Should().Be(BusinessCategory.HospitalPharmacy);
        domainModel.Country.Should().Be("NL");
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = null,
            phr_registrationnumber = null,
            phr_businesscategory = 0,
            phr_country = null,
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
        domainModel.BusinessName.Should().BeEmpty();
        domainModel.RegistrationNumber.Should().BeNull();
        domainModel.Country.Should().BeEmpty();
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = "Test",
            phr_businesscategory = dtoValue,
            phr_country = "NL",
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = "Test",
            phr_businesscategory = 0,
            phr_country = "NL",
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = "Test",
            phr_businesscategory = 0,
            phr_country = "NL",
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
        var customerId = Guid.NewGuid();
        var onboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));
        var reVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2).AddMonths(6));
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var domainModel = new Customer
        {
            CustomerId = customerId,
            BusinessName = "Apotheek Van der Berg",
            RegistrationNumber = "KVK-87654321",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
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
        var dto = CustomerDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_customerid.Should().Be(customerId);
        dto.phr_businessname.Should().Be("Apotheek Van der Berg");
        dto.phr_registrationnumber.Should().Be("KVK-87654321");
        dto.phr_businesscategory.Should().Be((int)BusinessCategory.CommunityPharmacy);
        dto.phr_country.Should().Be("NL");
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
            CustomerId = Guid.NewGuid(),
            BusinessName = "Minimal Customer",
            BusinessCategory = BusinessCategory.ResearchInstitution,
            Country = "DE",
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
        var dto = CustomerDto.FromDomainModel(domainModel);

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
            CustomerId = Guid.NewGuid(),
            BusinessName = "Suspended Veterinary Clinic",
            BusinessCategory = BusinessCategory.Veterinarian,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = true,
            SuspensionReason = "Under investigation by IGJ",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = CustomerDto.FromDomainModel(domainModel);

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
            CustomerId = Guid.NewGuid(),
            BusinessName = "Wholesaler EU Partner",
            RegistrationNumber = "VAT-EU123456789",
            BusinessCategory = BusinessCategory.WholesalerEU,
            Country = "BE",
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
        var dto = CustomerDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.CustomerId.Should().Be(original.CustomerId);
        roundTripped.BusinessName.Should().Be(original.BusinessName);
        roundTripped.RegistrationNumber.Should().Be(original.RegistrationNumber);
        roundTripped.BusinessCategory.Should().Be(original.BusinessCategory);
        roundTripped.Country.Should().Be(original.Country);
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
            CustomerId = Guid.NewGuid(),
            BusinessName = "Previously Active Customer",
            BusinessCategory = BusinessCategory.Manufacturer,
            Country = "FR",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = true,
            SuspensionReason = "Multiple suspicious order reports under investigation",
            CreatedDate = DateTime.UtcNow.AddYears(-2),
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = CustomerDto.FromDomainModel(original);
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = "Active Pharmacy",
            phr_businesscategory = (int)BusinessCategory.CommunityPharmacy,
            phr_country = "NL",
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
        var dto = new CustomerDto
        {
            phr_customerid = Guid.NewGuid(),
            phr_businessname = "Suspended Pharmacy",
            phr_businesscategory = (int)BusinessCategory.CommunityPharmacy,
            phr_country = "NL",
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
