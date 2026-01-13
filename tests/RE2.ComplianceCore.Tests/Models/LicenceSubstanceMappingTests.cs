using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceSubstanceMapping domain model.
/// T055: Test-driven development for LicenceSubstanceMapping per data-model.md entity 4.
/// </summary>
public class LicenceSubstanceMappingTests
{
    [Fact]
    public void LicenceSubstanceMapping_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            MaxQuantityPerTransaction = 1000.0m,
            MaxQuantityPerPeriod = 5000.0m,
            PeriodType = "Monthly",
            Restrictions = "Prescription required"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, mapping.MappingId);
        Assert.NotEqual(Guid.Empty, mapping.LicenceId);
        Assert.NotEqual(Guid.Empty, mapping.SubstanceId);
        Assert.Equal(1000.0m, mapping.MaxQuantityPerTransaction);
        Assert.Equal(5000.0m, mapping.MaxQuantityPerPeriod);
        Assert.Equal("Monthly", mapping.PeriodType);
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_FailsWithEmptyLicenceId()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.Empty,
            SubstanceId = Guid.NewGuid()
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("LicenceId"));
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_SucceedsWithValidData()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            MaxQuantityPerTransaction = 500.0m
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.True(result.IsValid);
    }
}
