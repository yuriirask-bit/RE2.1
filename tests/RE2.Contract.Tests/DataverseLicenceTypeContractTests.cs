using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;
using RE2.Shared.Constants;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse LicenceType entity.
/// T057: Verifies DTO-to-domain and domain-to-DTO mapping contracts.
/// </summary>
public class DataverseLicenceTypeContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void LicenceTypeDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(LicenceTypeDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_licencetypeid");
        properties.Should().Contain(p => p.Name == "phr_name");
        properties.Should().Contain(p => p.Name == "phr_issuingauthority");
        properties.Should().Contain(p => p.Name == "phr_typicalvaliditymonths");
        properties.Should().Contain(p => p.Name == "phr_permittedactivities");
        properties.Should().Contain(p => p.Name == "phr_isactive");
    }

    [Fact]
    public void LicenceTypeDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(LicenceTypeDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_licencetypeid");

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
        var dto = new LicenceTypeDto
        {
            phr_licencetypeid = Guid.NewGuid(),
            phr_name = "Wholesale Licence (WDA)",
            phr_issuingauthority = "IGJ",
            phr_typicalvaliditymonths = 60,
            phr_permittedactivities = (int)(LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Store | LicenceTypes.PermittedActivity.Distribute),
            phr_isactive = true
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.LicenceTypeId.Should().Be(dto.phr_licencetypeid);
        domainModel.Name.Should().Be(dto.phr_name);
        domainModel.IssuingAuthority.Should().Be(dto.phr_issuingauthority);
        domainModel.TypicalValidityMonths.Should().Be(dto.phr_typicalvaliditymonths);
        domainModel.PermittedActivities.Should().Be(LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Store | LicenceTypes.PermittedActivity.Distribute);
        domainModel.IsActive.Should().Be(dto.phr_isactive);
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new LicenceTypeDto
        {
            phr_licencetypeid = Guid.NewGuid(),
            phr_name = null,
            phr_issuingauthority = null,
            phr_typicalvaliditymonths = null,
            phr_permittedactivities = 0,
            phr_isactive = false
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.Name.Should().BeEmpty();
        domainModel.IssuingAuthority.Should().BeEmpty();
        domainModel.TypicalValidityMonths.Should().BeNull();
        domainModel.PermittedActivities.Should().Be(LicenceTypes.PermittedActivity.None);
    }

    [Theory]
    [InlineData(1, LicenceTypes.PermittedActivity.Possess)]
    [InlineData(2, LicenceTypes.PermittedActivity.Store)]
    [InlineData(4, LicenceTypes.PermittedActivity.Distribute)]
    [InlineData(8, LicenceTypes.PermittedActivity.Import)]
    [InlineData(16, LicenceTypes.PermittedActivity.Export)]
    [InlineData(32, LicenceTypes.PermittedActivity.Manufacture)]
    [InlineData(64, LicenceTypes.PermittedActivity.HandlePrecursors)]
    public void ToDomainModel_ShouldMapPermittedActivities_Correctly(int dtoValue, LicenceTypes.PermittedActivity expected)
    {
        // Arrange
        var dto = new LicenceTypeDto
        {
            phr_licencetypeid = Guid.NewGuid(),
            phr_name = "Test",
            phr_issuingauthority = "Test",
            phr_permittedactivities = dtoValue,
            phr_isactive = true
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.PermittedActivities.Should().Be(expected);
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var domainModel = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Opium Act Exemption",
            IssuingAuthority = "Farmatec",
            TypicalValidityMonths = 12,
            PermittedActivities = LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.HandlePrecursors,
            IsActive = true
        };

        // Act
        var dto = LicenceTypeDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_licencetypeid.Should().Be(domainModel.LicenceTypeId);
        dto.phr_name.Should().Be(domainModel.Name);
        dto.phr_issuingauthority.Should().Be(domainModel.IssuingAuthority);
        dto.phr_typicalvaliditymonths.Should().Be(domainModel.TypicalValidityMonths);
        dto.phr_permittedactivities.Should().Be((int)domainModel.PermittedActivities);
        dto.phr_isactive.Should().Be(domainModel.IsActive);
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullValidityMonths()
    {
        // Arrange
        var domainModel = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Permanent Licence",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = null,
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute,
            IsActive = true
        };

        // Act
        var dto = LicenceTypeDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_typicalvaliditymonths.Should().BeNull();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DomainToDto_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Import Permit",
            IssuingAuthority = "Farmatec",
            TypicalValidityMonths = 6,
            PermittedActivities = LicenceTypes.PermittedActivity.Import | LicenceTypes.PermittedActivity.Possess,
            IsActive = true
        };

        // Act
        var dto = LicenceTypeDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.LicenceTypeId.Should().Be(original.LicenceTypeId);
        roundTripped.Name.Should().Be(original.Name);
        roundTripped.IssuingAuthority.Should().Be(original.IssuingAuthority);
        roundTripped.TypicalValidityMonths.Should().Be(original.TypicalValidityMonths);
        roundTripped.PermittedActivities.Should().Be(original.PermittedActivities);
        roundTripped.IsActive.Should().Be(original.IsActive);
    }

    #endregion
}
