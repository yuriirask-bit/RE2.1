using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for ControlledSubstance domain model.
/// T054: Test-driven development for ControlledSubstance per data-model.md entity 3.
/// </summary>
public class ControlledSubstanceTests
{
    [Fact]
    public void ControlledSubstance_Constructor_InitializesWithOpiumActSubstance()
    {
        // Arrange & Act
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Morphine Sulfate 10mg",
            InternalCode = "MOR-10MG-AMP",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            RegulatoryRestrictions = "Prescription required",
            IsActive = true
        };

        // Assert
        Assert.NotEqual(Guid.Empty, substance.SubstanceId);
        Assert.Equal("Morphine Sulfate 10mg", substance.SubstanceName);
        Assert.Equal("MOR-10MG-AMP", substance.InternalCode);
        Assert.Equal(SubstanceCategories.OpiumActList.ListII, substance.OpiumActList);
        Assert.Equal(SubstanceCategories.PrecursorCategory.None, substance.PrecursorCategory);
        Assert.True(substance.IsActive);
    }

    [Fact]
    public void ControlledSubstance_Constructor_InitializesWithPrecursor()
    {
        // Arrange & Act
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Ephedrine",
            InternalCode = "EPHED-RAW",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
            RegulatoryRestrictions = "EU Regulation 273/2004 - Pre-export notification required",
            IsActive = true
        };

        // Assert
        Assert.Equal("Ephedrine", substance.SubstanceName);
        Assert.Equal(SubstanceCategories.PrecursorCategory.Category1, substance.PrecursorCategory);
        Assert.Equal(SubstanceCategories.OpiumActList.None, substance.OpiumActList);
    }

    [Fact]
    public void ControlledSubstance_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Fentanyl",
            InternalCode = "FENT-100MCG",
            OpiumActList = SubstanceCategories.OpiumActList.ListI
        };

        // Assert
        Assert.True(substance.IsActive);
    }

    [Fact]
    public void ControlledSubstance_Validate_FailsWhenBothOpiumActAndPrecursorAreNone()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Invalid Substance",
            InternalCode = "INVALID",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v =>
            v.ErrorCode == "VALIDATION_ERROR" &&
            v.Message.Contains("OpiumActList") &&
            v.Message.Contains("PrecursorCategory"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ControlledSubstance_Validate_FailsWithInvalidSubstanceName(string invalidName)
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = invalidName!,
            InternalCode = "MOR-10MG",
            OpiumActList = SubstanceCategories.OpiumActList.ListII
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.ErrorCode == "VALIDATION_ERROR" && v.Message.Contains("SubstanceName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ControlledSubstance_Validate_FailsWithInvalidInternalCode(string invalidCode)
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Morphine",
            InternalCode = invalidCode!,
            OpiumActList = SubstanceCategories.OpiumActList.ListII
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.ErrorCode == "VALIDATION_ERROR" && v.Message.Contains("InternalCode"));
    }

    [Fact]
    public void ControlledSubstance_Validate_SucceedsWithOpiumActOnly()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Cocaine",
            InternalCode = "COC-POWDER",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void ControlledSubstance_Validate_SucceedsWithPrecursorOnly()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Acetone",
            InternalCode = "ACET-SOLV",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category2
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void ControlledSubstance_Validate_SucceedsWithBothClassifications()
    {
        // Arrange (some substances can be both Opium Act and Precursor)
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Special Controlled Substance",
            InternalCode = "SPEC-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category3
        };

        // Act
        var result = substance.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void ControlledSubstance_IsOpiumActControlled_ReturnsTrueForListIorII()
    {
        // Arrange
        var substanceListI = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Heroin",
            InternalCode = "HER-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListI
        };

        var substanceListII = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Cannabis",
            InternalCode = "CANN-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListII
        };

        // Act & Assert
        Assert.True(substanceListI.IsOpiumActControlled());
        Assert.True(substanceListII.IsOpiumActControlled());
    }

    [Fact]
    public void ControlledSubstance_IsOpiumActControlled_ReturnsFalseForNone()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Acetone",
            InternalCode = "ACET-001",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category2
        };

        // Act & Assert
        Assert.False(substance.IsOpiumActControlled());
    }

    [Fact]
    public void ControlledSubstance_IsPrecursor_ReturnsTrueForAnyCategory()
    {
        // Arrange
        var substanceCat1 = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Ephedrine",
            InternalCode = "EPHED-001",
            PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1
        };

        // Act & Assert
        Assert.True(substanceCat1.IsPrecursor());
    }

    [Fact]
    public void ControlledSubstance_IsPrecursor_ReturnsFalseForNone()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Morphine",
            InternalCode = "MOR-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        // Act & Assert
        Assert.False(substance.IsPrecursor());
    }
}
