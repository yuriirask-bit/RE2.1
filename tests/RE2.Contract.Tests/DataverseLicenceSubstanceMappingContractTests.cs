using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.Contract.Tests;

/// <summary>
/// T079i: Contract tests for Dataverse LicenceSubstanceMapping entity.
/// Verifies DTO-to-domain and domain-to-DTO mapping contracts per FR-004.
/// </summary>
public class DataverseLicenceSubstanceMappingContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void LicenceSubstanceMappingDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(LicenceSubstanceMappingDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_licencesubstancemappingid");
        properties.Should().Contain(p => p.Name == "phr_licenceid");
        properties.Should().Contain(p => p.Name == "phr_substancecode");
        properties.Should().Contain(p => p.Name == "phr_maxquantitypertransaction");
        properties.Should().Contain(p => p.Name == "phr_maxquantityperperiod");
        properties.Should().Contain(p => p.Name == "phr_periodtype");
        properties.Should().Contain(p => p.Name == "phr_restrictions");
        properties.Should().Contain(p => p.Name == "phr_effectivedate");
        properties.Should().Contain(p => p.Name == "phr_expirydate");
    }

    [Fact]
    public void LicenceSubstanceMappingDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(LicenceSubstanceMappingDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_licencesubstancemappingid");

        // Assert
        primaryKeyProperty.Should().NotBeNull();
        primaryKeyProperty!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void LicenceSubstanceMappingDto_ForeignKeys_ShouldHaveCorrectTypes()
    {
        // Arrange
        var dtoType = typeof(LicenceSubstanceMappingDto);

        // Assert
        dtoType.GetProperty("phr_licenceid")!.PropertyType.Should().Be(typeof(Guid));
        dtoType.GetProperty("phr_substancecode")!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void LicenceSubstanceMappingDto_DateFields_ShouldBeDateTime()
    {
        // Arrange
        var dtoType = typeof(LicenceSubstanceMappingDto);

        // Assert - Dataverse uses DateTime, domain model uses DateOnly
        dtoType.GetProperty("phr_effectivedate")!.PropertyType.Should().Be(typeof(DateTime));
        dtoType.GetProperty("phr_expirydate")!.PropertyType.Should().Be(typeof(DateTime?));
    }

    [Fact]
    public void LicenceSubstanceMappingDto_QuantityFields_ShouldBeNullableDecimal()
    {
        // Arrange
        var dtoType = typeof(LicenceSubstanceMappingDto);

        // Assert
        dtoType.GetProperty("phr_maxquantitypertransaction")!.PropertyType.Should().Be(typeof(decimal?));
        dtoType.GetProperty("phr_maxquantityperperiod")!.PropertyType.Should().Be(typeof(decimal?));
    }

    #endregion

    #region DTO to Domain Model Mapping Tests

    [Fact]
    public void ToDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var dto = new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = Guid.NewGuid(),
            phr_licenceid = Guid.NewGuid(),
            phr_substancecode = "Morphine",
            phr_maxquantitypertransaction = 500.50m,
            phr_maxquantityperperiod = 10000.00m,
            phr_periodtype = "Monthly",
            phr_restrictions = "Storage at controlled temperature only",
            phr_effectivedate = new DateTime(2024, 1, 1),
            phr_expirydate = new DateTime(2025, 12, 31)
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.MappingId.Should().Be(dto.phr_licencesubstancemappingid);
        domainModel.LicenceId.Should().Be(dto.phr_licenceid);
        domainModel.SubstanceCode.Should().Be(dto.phr_substancecode);
        domainModel.MaxQuantityPerTransaction.Should().Be(dto.phr_maxquantitypertransaction);
        domainModel.MaxQuantityPerPeriod.Should().Be(dto.phr_maxquantityperperiod);
        domainModel.PeriodType.Should().Be(dto.phr_periodtype);
        domainModel.Restrictions.Should().Be(dto.phr_restrictions);
        domainModel.EffectiveDate.Should().Be(new DateOnly(2024, 1, 1));
        domainModel.ExpiryDate.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = Guid.NewGuid(),
            phr_licenceid = Guid.NewGuid(),
            phr_substancecode = "Fentanyl",
            phr_maxquantitypertransaction = null,
            phr_maxquantityperperiod = null,
            phr_periodtype = null,
            phr_restrictions = null,
            phr_effectivedate = new DateTime(2024, 6, 1),
            phr_expirydate = null
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.MaxQuantityPerTransaction.Should().BeNull();
        domainModel.MaxQuantityPerPeriod.Should().BeNull();
        domainModel.PeriodType.Should().BeNull();
        domainModel.Restrictions.Should().BeNull();
        domainModel.ExpiryDate.Should().BeNull();
    }

    [Fact]
    public void ToDomainModel_ShouldConvertDateTime_ToDateOnly()
    {
        // Arrange
        var dto = new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = Guid.NewGuid(),
            phr_licenceid = Guid.NewGuid(),
            phr_substancecode = "Codeine",
            phr_effectivedate = new DateTime(2024, 3, 15, 10, 30, 0), // Time component should be ignored
            phr_expirydate = new DateTime(2025, 6, 30, 23, 59, 59)    // Time component should be ignored
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.EffectiveDate.Should().Be(new DateOnly(2024, 3, 15));
        domainModel.ExpiryDate.Should().Be(new DateOnly(2025, 6, 30));
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("Quarterly")]
    [InlineData("Annual")]
    public void ToDomainModel_ShouldPreserve_PeriodTypeValues(string periodType)
    {
        // Arrange
        var dto = new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = Guid.NewGuid(),
            phr_licenceid = Guid.NewGuid(),
            phr_substancecode = "Ephedrine",
            phr_effectivedate = DateTime.Today,
            phr_periodtype = periodType
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.PeriodType.Should().Be(periodType);
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var domainModel = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Morphine",
            MaxQuantityPerTransaction = 250.75m,
            MaxQuantityPerPeriod = 5000.00m,
            PeriodType = "Quarterly",
            Restrictions = "Special handling required",
            EffectiveDate = new DateOnly(2024, 2, 1),
            ExpiryDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var dto = LicenceSubstanceMappingDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_licencesubstancemappingid.Should().Be(domainModel.MappingId);
        dto.phr_licenceid.Should().Be(domainModel.LicenceId);
        dto.phr_substancecode.Should().Be(domainModel.SubstanceCode);
        dto.phr_maxquantitypertransaction.Should().Be(domainModel.MaxQuantityPerTransaction);
        dto.phr_maxquantityperperiod.Should().Be(domainModel.MaxQuantityPerPeriod);
        dto.phr_periodtype.Should().Be(domainModel.PeriodType);
        dto.phr_restrictions.Should().Be(domainModel.Restrictions);
        dto.phr_effectivedate.Should().Be(new DateTime(2024, 2, 1));
        dto.phr_expirydate.Should().Be(new DateTime(2026, 1, 31));
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullFields()
    {
        // Arrange
        var domainModel = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Fentanyl",
            MaxQuantityPerTransaction = null,
            MaxQuantityPerPeriod = null,
            PeriodType = null,
            Restrictions = null,
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = null
        };

        // Act
        var dto = LicenceSubstanceMappingDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_maxquantitypertransaction.Should().BeNull();
        dto.phr_maxquantityperperiod.Should().BeNull();
        dto.phr_periodtype.Should().BeNull();
        dto.phr_restrictions.Should().BeNull();
        dto.phr_expirydate.Should().BeNull();
    }

    [Fact]
    public void FromDomainModel_ShouldConvertDateOnly_ToDateTime()
    {
        // Arrange
        var domainModel = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Codeine",
            EffectiveDate = new DateOnly(2024, 7, 15),
            ExpiryDate = new DateOnly(2025, 12, 31)
        };

        // Act
        var dto = LicenceSubstanceMappingDto.FromDomainModel(domainModel);

        // Assert - DateTime should be at midnight (TimeOnly.MinValue)
        dto.phr_effectivedate.Should().Be(new DateTime(2024, 7, 15, 0, 0, 0));
        dto.phr_expirydate.Should().Be(new DateTime(2025, 12, 31, 0, 0, 0));
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DomainToDto_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Morphine",
            MaxQuantityPerTransaction = 100.00m,
            MaxQuantityPerPeriod = 1000.00m,
            PeriodType = "Monthly",
            Restrictions = "Audit trail required",
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = new DateOnly(2024, 12, 31)
        };

        // Act
        var dto = LicenceSubstanceMappingDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.MappingId.Should().Be(original.MappingId);
        roundTripped.LicenceId.Should().Be(original.LicenceId);
        roundTripped.SubstanceCode.Should().Be(original.SubstanceCode);
        roundTripped.MaxQuantityPerTransaction.Should().Be(original.MaxQuantityPerTransaction);
        roundTripped.MaxQuantityPerPeriod.Should().Be(original.MaxQuantityPerPeriod);
        roundTripped.PeriodType.Should().Be(original.PeriodType);
        roundTripped.Restrictions.Should().Be(original.Restrictions);
        roundTripped.EffectiveDate.Should().Be(original.EffectiveDate);
        roundTripped.ExpiryDate.Should().Be(original.ExpiryDate);
    }

    [Fact]
    public void RoundTrip_DtoToDomain_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = Guid.NewGuid(),
            phr_licenceid = Guid.NewGuid(),
            phr_substancecode = "Ephedrine",
            phr_maxquantitypertransaction = 200.50m,
            phr_maxquantityperperiod = 2500.00m,
            phr_periodtype = "Annual",
            phr_restrictions = "Climate controlled storage",
            phr_effectivedate = new DateTime(2024, 6, 1),
            phr_expirydate = new DateTime(2027, 5, 31)
        };

        // Act
        var domainModel = original.ToDomainModel();
        var roundTripped = LicenceSubstanceMappingDto.FromDomainModel(domainModel);

        // Assert
        roundTripped.phr_licencesubstancemappingid.Should().Be(original.phr_licencesubstancemappingid);
        roundTripped.phr_licenceid.Should().Be(original.phr_licenceid);
        roundTripped.phr_substancecode.Should().Be(original.phr_substancecode);
        roundTripped.phr_maxquantitypertransaction.Should().Be(original.phr_maxquantitypertransaction);
        roundTripped.phr_maxquantityperperiod.Should().Be(original.phr_maxquantityperperiod);
        roundTripped.phr_periodtype.Should().Be(original.phr_periodtype);
        roundTripped.phr_restrictions.Should().Be(original.phr_restrictions);
        // Date comparison - time components are normalized during conversion
        roundTripped.phr_effectivedate.Date.Should().Be(original.phr_effectivedate.Date);
        roundTripped.phr_expirydate!.Value.Date.Should().Be(original.phr_expirydate!.Value.Date);
    }

    [Fact]
    public void RoundTrip_WithNullOptionalFields_ShouldPreserveNulls()
    {
        // Arrange
        var original = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "AceticAnhydride",
            MaxQuantityPerTransaction = null,
            MaxQuantityPerPeriod = null,
            PeriodType = null,
            Restrictions = null,
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = null
        };

        // Act
        var dto = LicenceSubstanceMappingDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.MaxQuantityPerTransaction.Should().BeNull();
        roundTripped.MaxQuantityPerPeriod.Should().BeNull();
        roundTripped.PeriodType.Should().BeNull();
        roundTripped.Restrictions.Should().BeNull();
        roundTripped.ExpiryDate.Should().BeNull();
    }

    #endregion

    #region Business Rule Contract Tests

    [Fact]
    public void LicenceSubstanceMapping_ShouldHaveCompositeKeyFields()
    {
        // This test verifies the composite key structure per data-model.md:
        // LicenceId + SubstanceCode + EffectiveDate must be unique

        // Arrange
        var domainType = typeof(LicenceSubstanceMapping);

        // Assert - all composite key fields exist
        domainType.GetProperty("LicenceId").Should().NotBeNull();
        domainType.GetProperty("SubstanceCode").Should().NotBeNull();
        domainType.GetProperty("EffectiveDate").Should().NotBeNull();
    }

    [Fact]
    public void LicenceSubstanceMapping_DateFields_ShouldUseDateOnly()
    {
        // Domain model should use DateOnly for date-only fields (no time component)

        // Arrange
        var domainType = typeof(LicenceSubstanceMapping);

        // Assert
        domainType.GetProperty("EffectiveDate")!.PropertyType.Should().Be(typeof(DateOnly));
        domainType.GetProperty("ExpiryDate")!.PropertyType.Should().Be(typeof(DateOnly?));
    }

    [Fact]
    public void LicenceSubstanceMapping_ShouldHaveNavigationProperties()
    {
        // Per data-model.md, mapping should reference Licence and ControlledSubstance

        // Arrange
        var domainType = typeof(LicenceSubstanceMapping);

        // Assert
        domainType.GetProperty("Licence").Should().NotBeNull();
        domainType.GetProperty("Licence")!.PropertyType.Should().Be(typeof(Licence));

        domainType.GetProperty("Substance").Should().NotBeNull();
        domainType.GetProperty("Substance")!.PropertyType.Should().Be(typeof(ControlledSubstance));
    }

    #endregion
}
