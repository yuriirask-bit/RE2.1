using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceType domain model.
/// T053: Test-driven development for LicenceType per data-model.md entity 2.
/// </summary>
public class LicenceTypeTests
{
    [Fact]
    public void LicenceType_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Wholesale Licence (WDA)",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = 60,
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                  LicenceTypes.PermittedActivity.Store |
                                  LicenceTypes.PermittedActivity.Distribute,
            IsActive = true
        };

        // Assert
        Assert.NotEqual(Guid.Empty, licenceType.LicenceTypeId);
        Assert.Equal("Wholesale Licence (WDA)", licenceType.Name);
        Assert.Equal("IGJ", licenceType.IssuingAuthority);
        Assert.Equal(60, licenceType.TypicalValidityMonths);
        Assert.True(licenceType.IsActive);
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Possess));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Store));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute));
    }

    [Fact]
    public void LicenceType_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Opium Act Exemption",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess
        };

        // Assert
        Assert.True(licenceType.IsActive);
    }

    [Fact]
    public void LicenceType_TypicalValidityMonths_CanBeNull()
    {
        // Arrange & Act
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Permanent Research Licence",
            IssuingAuthority = "Ministry of Health",
            TypicalValidityMonths = null,
            PermittedActivities = LicenceTypes.PermittedActivity.Possess
        };

        // Assert
        Assert.Null(licenceType.TypicalValidityMonths);
    }

    [Fact]
    public void LicenceType_PermittedActivities_SupportsMultipleFlags()
    {
        // Arrange
        var activities = LicenceTypes.PermittedActivity.Possess |
                        LicenceTypes.PermittedActivity.Store |
                        LicenceTypes.PermittedActivity.Distribute |
                        LicenceTypes.PermittedActivity.Import |
                        LicenceTypes.PermittedActivity.Export;

        // Act
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Comprehensive Wholesale Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = activities
        };

        // Assert
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Possess));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Store));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Import));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Export));
        Assert.False(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Manufacture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LicenceType_Validate_FailsWithInvalidName(string invalidName)
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = invalidName!,
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess
        };

        // Act
        var result = licenceType.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.ErrorCode == "VALIDATION_ERROR" && v.Message.Contains("Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LicenceType_Validate_FailsWithInvalidIssuingAuthority(string invalidAuthority)
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Wholesale Licence",
            IssuingAuthority = invalidAuthority!,
            PermittedActivities = LicenceTypes.PermittedActivity.Possess
        };

        // Act
        var result = licenceType.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.ErrorCode == "VALIDATION_ERROR" && v.Message.Contains("IssuingAuthority"));
    }

    [Fact]
    public void LicenceType_Validate_FailsWithNoPermittedActivities()
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Invalid Licence Type",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.None
        };

        // Act
        var result = licenceType.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.ErrorCode == "VALIDATION_ERROR" && v.Message.Contains("PermittedActivities"));
    }

    [Fact]
    public void LicenceType_Validate_SucceedsWithValidData()
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Opium Act Exemption",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = 12,
            PermittedActivities = LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Store,
            IsActive = true
        };

        // Act
        var result = licenceType.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void LicenceType_Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Obsolete Licence Type",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess,
            IsActive = true
        };

        // Act
        licenceType.Deactivate();

        // Assert
        Assert.False(licenceType.IsActive);
    }

    [Fact]
    public void LicenceType_Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Reinstated Licence Type",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess,
            IsActive = false
        };

        // Act
        licenceType.Activate();

        // Assert
        Assert.True(licenceType.IsActive);
    }

    #region T071f: PermittedActivities Flags Verification Tests

    /// <summary>
    /// T071f: Verifies all FR-002 activities are available as flags.
    /// FR-002 defines: possess, store, distribute, import, export, manufacture, handle precursors.
    /// </summary>
    [Theory]
    [InlineData(LicenceTypes.PermittedActivity.Possess, "Possess")]
    [InlineData(LicenceTypes.PermittedActivity.Store, "Store")]
    [InlineData(LicenceTypes.PermittedActivity.Distribute, "Distribute")]
    [InlineData(LicenceTypes.PermittedActivity.Import, "Import")]
    [InlineData(LicenceTypes.PermittedActivity.Export, "Export")]
    [InlineData(LicenceTypes.PermittedActivity.Manufacture, "Manufacture")]
    [InlineData(LicenceTypes.PermittedActivity.HandlePrecursors, "HandlePrecursors")]
    public void PermittedActivity_AllFR002ActivitiesAreDefined(LicenceTypes.PermittedActivity activity, string expectedName)
    {
        // Assert
        Assert.NotEqual(LicenceTypes.PermittedActivity.None, activity);
        Assert.Equal(expectedName, activity.ToString());
    }

    [Fact]
    public void PermittedActivity_FlagsAreUniquePowersOfTwo()
    {
        // Verify each flag is a unique power of 2 (required for proper flags enum behavior)
        var values = new[]
        {
            (int)LicenceTypes.PermittedActivity.Possess,
            (int)LicenceTypes.PermittedActivity.Store,
            (int)LicenceTypes.PermittedActivity.Distribute,
            (int)LicenceTypes.PermittedActivity.Import,
            (int)LicenceTypes.PermittedActivity.Export,
            (int)LicenceTypes.PermittedActivity.Manufacture,
            (int)LicenceTypes.PermittedActivity.HandlePrecursors
        };

        foreach (var value in values)
        {
            // Check power of 2: value & (value - 1) == 0 for power of 2
            Assert.True((value & (value - 1)) == 0, $"Value {value} is not a power of 2");
            Assert.True(value > 0, $"Value {value} should be positive");
        }

        // Check all values are unique
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void PermittedActivity_ManufactureLicence_SupportsManufactureActivity()
    {
        // Arrange - Manufacturing licence type
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Manufacturing Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                  LicenceTypes.PermittedActivity.Store |
                                  LicenceTypes.PermittedActivity.Manufacture
        };

        // Assert
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Manufacture));
        Assert.False(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Import));
        Assert.False(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Export));
    }

    [Fact]
    public void PermittedActivity_PrecursorRegistration_SupportsHandlePrecursors()
    {
        // Arrange - Precursor registration per EU Regulation 273/2004
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Precursor Registration",
            IssuingAuthority = "Farmatec/CIBG",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                  LicenceTypes.PermittedActivity.Store |
                                  LicenceTypes.PermittedActivity.HandlePrecursors
        };

        // Assert
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.HandlePrecursors));
        Assert.False(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute));
    }

    [Fact]
    public void PermittedActivity_CanConvertToAndFromInteger()
    {
        // Arrange - Combined activities
        var activities = LicenceTypes.PermittedActivity.Possess |
                        LicenceTypes.PermittedActivity.Store |
                        LicenceTypes.PermittedActivity.Distribute;

        // Act - Convert to int (for API/database storage)
        var intValue = (int)activities;

        // Convert back to enum
        var reconvertedActivities = (LicenceTypes.PermittedActivity)intValue;

        // Assert
        Assert.Equal(activities, reconvertedActivities);
        Assert.True(reconvertedActivities.HasFlag(LicenceTypes.PermittedActivity.Possess));
        Assert.True(reconvertedActivities.HasFlag(LicenceTypes.PermittedActivity.Store));
        Assert.True(reconvertedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute));
    }

    [Fact]
    public void PermittedActivity_AllFR002Activities_CanBeCombined()
    {
        // Arrange - All 7 FR-002 activities combined
        var allActivities = LicenceTypes.PermittedActivity.Possess |
                           LicenceTypes.PermittedActivity.Store |
                           LicenceTypes.PermittedActivity.Distribute |
                           LicenceTypes.PermittedActivity.Import |
                           LicenceTypes.PermittedActivity.Export |
                           LicenceTypes.PermittedActivity.Manufacture |
                           LicenceTypes.PermittedActivity.HandlePrecursors;

        // Act
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Comprehensive Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = allActivities
        };

        // Assert - All 7 activities present
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Possess));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Store));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Import));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Export));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Manufacture));
        Assert.True(licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.HandlePrecursors));

        // Validate the combined value (sum of all powers of 2 from 1 to 64)
        Assert.Equal(127, (int)allActivities);
    }

    #endregion
}
