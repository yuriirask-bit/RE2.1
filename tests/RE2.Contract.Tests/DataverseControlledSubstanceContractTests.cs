using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;
using RE2.Shared.Constants;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse ControlledSubstance entity.
/// T058: Verifies DTO-to-domain and domain-to-DTO mapping contracts.
/// </summary>
public class DataverseControlledSubstanceContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void ControlledSubstanceDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(ControlledSubstanceDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_controlledsubstanceid");
        properties.Should().Contain(p => p.Name == "phr_substancename");
        properties.Should().Contain(p => p.Name == "phr_opiumactlist");
        properties.Should().Contain(p => p.Name == "phr_precursorcategory");
        properties.Should().Contain(p => p.Name == "phr_internalcode");
        properties.Should().Contain(p => p.Name == "phr_regulatoryrestrictions");
        properties.Should().Contain(p => p.Name == "phr_isactive");
    }

    [Fact]
    public void ControlledSubstanceDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(ControlledSubstanceDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_controlledsubstanceid");

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
        var dto = new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = Guid.NewGuid(),
            phr_substancename = "Morphine",
            phr_opiumactlist = (int)SubstanceCategories.OpiumActList.ListI,
            phr_precursorcategory = (int)SubstanceCategories.PrecursorCategory.None,
            phr_internalcode = "MORPH-001",
            phr_regulatoryrestrictions = "Schedule II controlled substance",
            phr_isactive = true
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SubstanceId.Should().Be(dto.phr_controlledsubstanceid);
        domainModel.SubstanceName.Should().Be(dto.phr_substancename);
        domainModel.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListI);
        domainModel.PrecursorCategory.Should().Be(SubstanceCategories.PrecursorCategory.None);
        domainModel.InternalCode.Should().Be(dto.phr_internalcode);
        domainModel.RegulatoryRestrictions.Should().Be(dto.phr_regulatoryrestrictions);
        domainModel.IsActive.Should().Be(dto.phr_isactive);
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = Guid.NewGuid(),
            phr_substancename = null,
            phr_opiumactlist = null,
            phr_precursorcategory = null,
            phr_internalcode = null,
            phr_regulatoryrestrictions = null,
            phr_isactive = false
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SubstanceName.Should().BeEmpty();
        domainModel.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.None);
        domainModel.PrecursorCategory.Should().Be(SubstanceCategories.PrecursorCategory.None);
        domainModel.InternalCode.Should().BeEmpty();
        domainModel.RegulatoryRestrictions.Should().BeNull();
    }

    [Theory]
    [InlineData(0, SubstanceCategories.OpiumActList.None)]
    [InlineData(1, SubstanceCategories.OpiumActList.ListI)]
    [InlineData(2, SubstanceCategories.OpiumActList.ListII)]
    public void ToDomainModel_ShouldMapOpiumActList_Correctly(int dtoValue, SubstanceCategories.OpiumActList expected)
    {
        // Arrange
        var dto = new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = Guid.NewGuid(),
            phr_substancename = "Test Substance",
            phr_opiumactlist = dtoValue,
            phr_internalcode = "TEST-001",
            phr_isactive = true
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.OpiumActList.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, SubstanceCategories.PrecursorCategory.None)]
    [InlineData(1, SubstanceCategories.PrecursorCategory.Category1)]
    [InlineData(2, SubstanceCategories.PrecursorCategory.Category2)]
    [InlineData(3, SubstanceCategories.PrecursorCategory.Category3)]
    public void ToDomainModel_ShouldMapPrecursorCategory_Correctly(int dtoValue, SubstanceCategories.PrecursorCategory expected)
    {
        // Arrange
        var dto = new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = Guid.NewGuid(),
            phr_substancename = "Test Precursor",
            phr_precursorcategory = dtoValue,
            phr_internalcode = "PREC-001",
            phr_isactive = true
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.PrecursorCategory.Should().Be(expected);
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var domainModel = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Fentanyl",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            InternalCode = "FENT-001",
            RegulatoryRestrictions = "Strict monitoring required",
            IsActive = true
        };

        // Act
        var dto = ControlledSubstanceDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_controlledsubstanceid.Should().Be(domainModel.SubstanceId);
        dto.phr_substancename.Should().Be(domainModel.SubstanceName);
        dto.phr_opiumactlist.Should().Be((int)domainModel.OpiumActList);
        dto.phr_precursorcategory.Should().Be((int)domainModel.PrecursorCategory);
        dto.phr_internalcode.Should().Be(domainModel.InternalCode);
        dto.phr_regulatoryrestrictions.Should().Be(domainModel.RegulatoryRestrictions);
        dto.phr_isactive.Should().Be(domainModel.IsActive);
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullRegulatoryRestrictions()
    {
        // Arrange
        var domainModel = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Codeine",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            InternalCode = "COD-001",
            RegulatoryRestrictions = null,
            IsActive = true
        };

        // Act
        var dto = ControlledSubstanceDto.FromDomainModel(domainModel);

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
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Ephedrine",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
            InternalCode = "EPH-001",
            RegulatoryRestrictions = "Precursor chemical - requires special licence",
            IsActive = true
        };

        // Act
        var dto = ControlledSubstanceDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.SubstanceId.Should().Be(original.SubstanceId);
        roundTripped.SubstanceName.Should().Be(original.SubstanceName);
        roundTripped.OpiumActList.Should().Be(original.OpiumActList);
        roundTripped.PrecursorCategory.Should().Be(original.PrecursorCategory);
        roundTripped.InternalCode.Should().Be(original.InternalCode);
        roundTripped.RegulatoryRestrictions.Should().Be(original.RegulatoryRestrictions);
        roundTripped.IsActive.Should().Be(original.IsActive);
    }

    [Fact]
    public void RoundTrip_ShouldPreserve_OpiumActListI_Substance()
    {
        // Arrange - Dutch Opium Act List I substance
        var original = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Heroin (Diacetylmorphine)",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            InternalCode = "DAM-001",
            RegulatoryRestrictions = "List I - Prohibited for general use",
            IsActive = true
        };

        // Act
        var dto = ControlledSubstanceDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListI);
        roundTripped.SubstanceName.Should().Be(original.SubstanceName);
    }

    [Fact]
    public void RoundTrip_ShouldPreserve_Category1Precursor()
    {
        // Arrange - EU Category 1 precursor
        var original = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Acetic Anhydride",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
            InternalCode = "AA-001",
            RegulatoryRestrictions = "Category 1 precursor - strict controls",
            IsActive = true
        };

        // Act
        var dto = ControlledSubstanceDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.PrecursorCategory.Should().Be(SubstanceCategories.PrecursorCategory.Category1);
    }

    #endregion
}
