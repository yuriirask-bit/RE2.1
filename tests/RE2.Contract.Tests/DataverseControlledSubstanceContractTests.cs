using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;
using RE2.Shared.Constants;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse SubstanceComplianceExtension entity.
/// T058: Verifies DTO-to-domain and domain-to-DTO mapping contracts.
/// </summary>
public class DataverseControlledSubstanceContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void SubstanceComplianceExtensionDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(SubstanceComplianceExtensionDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_complianceextensionid");
        properties.Should().Contain(p => p.Name == "phr_substancecode");
        properties.Should().Contain(p => p.Name == "phr_substancename");
        properties.Should().Contain(p => p.Name == "phr_regulatoryrestrictions");
        properties.Should().Contain(p => p.Name == "phr_isactive");
        properties.Should().Contain(p => p.Name == "phr_classificationeffectivedate");
        properties.Should().Contain(p => p.Name == "phr_createddate");
        properties.Should().Contain(p => p.Name == "phr_modifieddate");
        properties.Should().Contain(p => p.Name == "phr_rowversion");
    }

    [Fact]
    public void SubstanceComplianceExtensionDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(SubstanceComplianceExtensionDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_complianceextensionid");

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
        var dto = new SubstanceComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_substancecode = "Morphine",
            phr_substancename = "Morphine",
            phr_regulatoryrestrictions = "Schedule II controlled substance",
            phr_isactive = true,
            phr_classificationeffectivedate = new DateTime(2024, 1, 1),
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SubstanceCode.Should().Be(dto.phr_substancecode);
        domainModel.SubstanceName.Should().Be(dto.phr_substancename);
        domainModel.ComplianceExtensionId.Should().Be(dto.phr_complianceextensionid);
        domainModel.RegulatoryRestrictions.Should().Be(dto.phr_regulatoryrestrictions);
        domainModel.IsActive.Should().Be(dto.phr_isactive);
        domainModel.ClassificationEffectiveDate.Should().Be(new DateOnly(2024, 1, 1));
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new SubstanceComplianceExtensionDto
        {
            phr_complianceextensionid = Guid.NewGuid(),
            phr_substancecode = null,
            phr_substancename = null,
            phr_regulatoryrestrictions = null,
            phr_isactive = false,
            phr_classificationeffectivedate = null,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SubstanceCode.Should().BeEmpty();
        domainModel.SubstanceName.Should().BeEmpty();
        domainModel.RegulatoryRestrictions.Should().BeNull();
        domainModel.ClassificationEffectiveDate.Should().BeNull();
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var domainModel = new ControlledSubstance
        {
            SubstanceCode = "Fentanyl",
            SubstanceName = "Fentanyl",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            ComplianceExtensionId = Guid.NewGuid(),
            RegulatoryRestrictions = "Strict monitoring required",
            IsActive = true,
            ClassificationEffectiveDate = new DateOnly(2024, 1, 1),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_complianceextensionid.Should().Be(domainModel.ComplianceExtensionId);
        dto.phr_substancecode.Should().Be(domainModel.SubstanceCode);
        dto.phr_substancename.Should().Be(domainModel.SubstanceName);
        dto.phr_regulatoryrestrictions.Should().Be(domainModel.RegulatoryRestrictions);
        dto.phr_isactive.Should().Be(domainModel.IsActive);
        dto.phr_classificationeffectivedate.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullRegulatoryRestrictions()
    {
        // Arrange
        var domainModel = new ControlledSubstance
        {
            SubstanceCode = "Codeine",
            SubstanceName = "Codeine",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            RegulatoryRestrictions = null,
            IsActive = true
        };

        // Act
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_regulatoryrestrictions.Should().BeNull();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DomainToDto_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new ControlledSubstance
        {
            SubstanceCode = "Ephedrine",
            SubstanceName = "Ephedrine",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
            ComplianceExtensionId = Guid.NewGuid(),
            RegulatoryRestrictions = "Precursor chemical - requires special licence",
            IsActive = true,
            ClassificationEffectiveDate = new DateOnly(2024, 1, 1),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.SubstanceCode.Should().Be(original.SubstanceCode);
        roundTripped.SubstanceName.Should().Be(original.SubstanceName);
        roundTripped.ComplianceExtensionId.Should().Be(original.ComplianceExtensionId);
        roundTripped.RegulatoryRestrictions.Should().Be(original.RegulatoryRestrictions);
        roundTripped.IsActive.Should().Be(original.IsActive);
        roundTripped.ClassificationEffectiveDate.Should().Be(original.ClassificationEffectiveDate);
    }

    [Fact]
    public void RoundTrip_ShouldPreserve_OpiumActListI_Substance()
    {
        // Arrange - Dutch Opium Act List I substance
        var original = new ControlledSubstance
        {
            SubstanceCode = "Heroin",
            SubstanceName = "Heroin (Diacetylmorphine)",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            ComplianceExtensionId = Guid.NewGuid(),
            RegulatoryRestrictions = "List I - Prohibited for general use",
            IsActive = true
        };

        // Act
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.SubstanceName.Should().Be(original.SubstanceName);
        roundTripped.SubstanceCode.Should().Be(original.SubstanceCode);
    }

    [Fact]
    public void RoundTrip_ShouldPreserve_Category1Precursor()
    {
        // Arrange - EU Category 1 precursor
        var original = new ControlledSubstance
        {
            SubstanceCode = "AceticAnhydride",
            SubstanceName = "Acetic Anhydride",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
            ComplianceExtensionId = Guid.NewGuid(),
            RegulatoryRestrictions = "Category 1 precursor - strict controls",
            IsActive = true
        };

        // Act
        var dto = SubstanceComplianceExtensionDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.SubstanceCode.Should().Be(original.SubstanceCode);
        roundTripped.SubstanceName.Should().Be(original.SubstanceName);
    }

    #endregion
}
