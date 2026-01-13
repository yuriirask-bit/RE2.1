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
}
