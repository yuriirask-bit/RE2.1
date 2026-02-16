using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpSiteWdaCoverage domain model.
/// T182: Test-driven development for WDA coverage per User Story 7 (FR-033).
/// </summary>
public class GdpSiteWdaCoverageTests
{
    #region Property Initialization Tests

    [Fact]
    public void GdpSiteWdaCoverage_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var coverageId = Guid.NewGuid();
        var licenceId = Guid.NewGuid();
        var coverage = new GdpSiteWdaCoverage
        {
            CoverageId = coverageId,
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = licenceId,
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31)
        };

        // Assert
        Assert.Equal(coverageId, coverage.CoverageId);
        Assert.Equal("WH-001", coverage.WarehouseId);
        Assert.Equal("nlpd", coverage.DataAreaId);
        Assert.Equal(licenceId, coverage.LicenceId);
        Assert.Equal(new DateOnly(2024, 1, 1), coverage.EffectiveDate);
        Assert.Equal(new DateOnly(2026, 12, 31), coverage.ExpiryDate);
    }

    [Fact]
    public void GdpSiteWdaCoverage_ExpiryDate_CanBeNull()
    {
        // Arrange & Act
        var coverage = new GdpSiteWdaCoverage
        {
            CoverageId = Guid.NewGuid(),
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = null
        };

        // Assert
        Assert.Null(coverage.ExpiryDate);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_ReturnsTrue_WhenEffectiveDateInPastAndNoExpiry()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = null
        };

        // Act & Assert
        Assert.True(coverage.IsActive());
    }

    [Fact]
    public void IsActive_ReturnsTrue_WhenEffectiveDateInPastAndExpiryInFuture()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };

        // Act & Assert
        Assert.True(coverage.IsActive());
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenEffectiveDateInFuture()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            ExpiryDate = null
        };

        // Act & Assert
        Assert.False(coverage.IsActive());
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenExpiryDateInPast()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        // Act & Assert
        Assert.False(coverage.IsActive());
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_Succeeds_WithValidData()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = new DateOnly(2026, 12, 31)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Validate_Succeeds_WithNullExpiryDate()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2024, 1, 1),
            ExpiryDate = null
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WithMissingWarehouseId()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = string.Empty,
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2024, 1, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("WarehouseId"));
    }

    [Fact]
    public void Validate_Fails_WithMissingDataAreaId()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = string.Empty,
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2024, 1, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("DataAreaId"));
    }

    [Fact]
    public void Validate_Fails_WithEmptyLicenceId()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.Empty,
            EffectiveDate = new DateOnly(2024, 1, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("LicenceId"));
    }

    [Fact]
    public void Validate_Fails_WhenExpiryDateBeforeEffectiveDate()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2025, 1, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ExpiryDate") && v.Message.Contains("EffectiveDate"));
    }

    [Fact]
    public void Validate_Fails_WhenExpiryDateEqualsEffectiveDate()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2025, 6, 1),
            ExpiryDate = new DateOnly(2025, 6, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ExpiryDate"));
    }

    [Fact]
    public void Validate_ReportsMultipleViolations()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage
        {
            WarehouseId = string.Empty,
            DataAreaId = string.Empty,
            LicenceId = Guid.Empty,
            EffectiveDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2025, 1, 1)
        };

        // Act
        var result = coverage.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Violations.Count);
    }

    #endregion
}
