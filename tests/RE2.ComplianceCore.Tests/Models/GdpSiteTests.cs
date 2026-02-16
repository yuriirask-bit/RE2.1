using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpSite domain model.
/// T181: Test-driven development for GdpSite per User Story 7 (FR-033, FR-034, FR-035).
/// </summary>
public class GdpSiteTests
{
    #region GdpSiteType Enum Tests

    [Theory]
    [InlineData(GdpSiteType.Warehouse, "Warehouse")]
    [InlineData(GdpSiteType.CrossDock, "CrossDock")]
    [InlineData(GdpSiteType.TransportHub, "TransportHub")]
    public void GdpSiteType_AllTypesAreDefined(GdpSiteType siteType, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, siteType.ToString());
    }

    [Fact]
    public void GdpSiteType_HasExpectedValues()
    {
        // Assert - 3 GDP site types
        var values = Enum.GetValues<GdpSiteType>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region GdpSiteActivity Flags Tests

    [Fact]
    public void GdpSiteActivity_None_HasValueZero()
    {
        Assert.Equal(0, (int)GdpSiteActivity.None);
    }

    [Fact]
    public void GdpSiteActivity_IsFlagsEnum()
    {
        // Assert - can combine activities
        var combined = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled;
        Assert.True(combined.HasFlag(GdpSiteActivity.StorageOver72h));
        Assert.True(combined.HasFlag(GdpSiteActivity.TemperatureControlled));
        Assert.False(combined.HasFlag(GdpSiteActivity.Outsourced));
        Assert.False(combined.HasFlag(GdpSiteActivity.TransportOnly));
    }

    [Fact]
    public void GdpSiteActivity_AllActivitiesCanBeCombined()
    {
        // Arrange & Act
        var allActivities = GdpSiteActivity.StorageOver72h |
                           GdpSiteActivity.TemperatureControlled |
                           GdpSiteActivity.Outsourced |
                           GdpSiteActivity.TransportOnly;

        // Assert
        Assert.True(allActivities.HasFlag(GdpSiteActivity.StorageOver72h));
        Assert.True(allActivities.HasFlag(GdpSiteActivity.TemperatureControlled));
        Assert.True(allActivities.HasFlag(GdpSiteActivity.Outsourced));
        Assert.True(allActivities.HasFlag(GdpSiteActivity.TransportOnly));
    }

    [Theory]
    [InlineData(GdpSiteActivity.StorageOver72h, 1)]
    [InlineData(GdpSiteActivity.TemperatureControlled, 2)]
    [InlineData(GdpSiteActivity.Outsourced, 4)]
    [InlineData(GdpSiteActivity.TransportOnly, 8)]
    public void GdpSiteActivity_HasCorrectFlagValues(GdpSiteActivity activity, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)activity);
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void GdpSite_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var extensionId = Guid.NewGuid();
        var site = new GdpSite
        {
            WarehouseId = "WH-001",
            WarehouseName = "Amsterdam Central Warehouse",
            OperationalSiteId = "SITE-AMS",
            OperationalSiteName = "Amsterdam Site",
            DataAreaId = "nlpd",
            WarehouseType = "Standard",
            City = "Amsterdam",
            CountryRegionId = "NL",
            GdpExtensionId = extensionId,
            GdpSiteType = GdpSiteType.Warehouse,
            PermittedActivities = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled,
            IsGdpActive = true
        };

        // Assert
        Assert.Equal("WH-001", site.WarehouseId);
        Assert.Equal("Amsterdam Central Warehouse", site.WarehouseName);
        Assert.Equal("SITE-AMS", site.OperationalSiteId);
        Assert.Equal("nlpd", site.DataAreaId);
        Assert.Equal(extensionId, site.GdpExtensionId);
        Assert.Equal(GdpSiteType.Warehouse, site.GdpSiteType);
        Assert.True(site.IsGdpActive);
    }

    [Fact]
    public void GdpSite_IsGdpActive_DefaultsToTrue()
    {
        var site = new GdpSite();
        Assert.True(site.IsGdpActive);
    }

    [Fact]
    public void GdpSite_RowVersion_DefaultsToEmptyArray()
    {
        var site = new GdpSite();
        Assert.NotNull(site.RowVersion);
        Assert.Empty(site.RowVersion);
    }

    #endregion

    #region IsConfiguredForGdp Tests

    [Fact]
    public void GdpSite_IsConfiguredForGdp_ReturnsTrueWhenExtensionIdSet()
    {
        // Arrange
        var site = new GdpSite { GdpExtensionId = Guid.NewGuid() };

        // Act & Assert
        Assert.True(site.IsConfiguredForGdp);
    }

    [Fact]
    public void GdpSite_IsConfiguredForGdp_ReturnsFalseWhenExtensionIdEmpty()
    {
        // Arrange
        var site = new GdpSite { GdpExtensionId = Guid.Empty };

        // Act & Assert
        Assert.False(site.IsConfiguredForGdp);
    }

    [Fact]
    public void GdpSite_IsConfiguredForGdp_ReturnsFalseByDefault()
    {
        // Arrange
        var site = new GdpSite();

        // Act & Assert
        Assert.False(site.IsConfiguredForGdp);
    }

    #endregion

    #region HasActivity Tests

    [Theory]
    [InlineData(GdpSiteActivity.StorageOver72h)]
    [InlineData(GdpSiteActivity.TemperatureControlled)]
    [InlineData(GdpSiteActivity.Outsourced)]
    [InlineData(GdpSiteActivity.TransportOnly)]
    public void GdpSite_HasActivity_ReturnsTrueWhenActivityPresent(GdpSiteActivity activity)
    {
        // Arrange
        var site = new GdpSite { PermittedActivities = activity };

        // Act & Assert
        Assert.True(site.HasActivity(activity));
    }

    [Fact]
    public void GdpSite_HasActivity_ReturnsFalseWhenActivityNotPresent()
    {
        // Arrange
        var site = new GdpSite { PermittedActivities = GdpSiteActivity.StorageOver72h };

        // Act & Assert
        Assert.False(site.HasActivity(GdpSiteActivity.TemperatureControlled));
    }

    [Fact]
    public void GdpSite_HasActivity_WorksWithCombinedFlags()
    {
        // Arrange
        var site = new GdpSite
        {
            PermittedActivities = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled
        };

        // Act & Assert
        Assert.True(site.HasActivity(GdpSiteActivity.StorageOver72h));
        Assert.True(site.HasActivity(GdpSiteActivity.TemperatureControlled));
        Assert.False(site.HasActivity(GdpSiteActivity.Outsourced));
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void GdpSite_Validate_SucceedsWithValidData()
    {
        // Arrange
        var site = new GdpSite
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            GdpSiteType = GdpSiteType.Warehouse,
            PermittedActivities = GdpSiteActivity.StorageOver72h
        };

        // Act
        var result = site.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void GdpSite_Validate_FailsWithMissingWarehouseId()
    {
        // Arrange
        var site = new GdpSite
        {
            WarehouseId = string.Empty,
            DataAreaId = "nlpd",
            PermittedActivities = GdpSiteActivity.StorageOver72h
        };

        // Act
        var result = site.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("WarehouseId"));
    }

    [Fact]
    public void GdpSite_Validate_FailsWithMissingDataAreaId()
    {
        // Arrange
        var site = new GdpSite
        {
            WarehouseId = "WH-001",
            DataAreaId = string.Empty,
            PermittedActivities = GdpSiteActivity.StorageOver72h
        };

        // Act
        var result = site.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("DataAreaId"));
    }

    [Fact]
    public void GdpSite_Validate_FailsWithNoPermittedActivities()
    {
        // Arrange
        var site = new GdpSite
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            PermittedActivities = GdpSiteActivity.None
        };

        // Act
        var result = site.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("permitted activity"));
    }

    [Fact]
    public void GdpSite_Validate_ReportsMultipleViolations()
    {
        // Arrange
        var site = new GdpSite
        {
            WarehouseId = string.Empty,
            DataAreaId = string.Empty,
            PermittedActivities = GdpSiteActivity.None
        };

        // Act
        var result = site.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Violations.Count);
    }

    #endregion
}
