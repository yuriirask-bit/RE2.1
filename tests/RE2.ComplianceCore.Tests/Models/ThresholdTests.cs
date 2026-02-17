using FluentAssertions;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Threshold domain model.
/// T149a: Threshold model tests per FR-020/FR-022.
/// </summary>
public class ThresholdTests
{
    #region IsEffective Tests

    [Fact]
    public void IsEffective_ReturnsTrue_WhenActiveAndNoDateRange()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = null;
        threshold.EffectiveTo = null;

        // Act & Assert
        threshold.IsEffective().Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_WhenInactive()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.IsActive = false;

        // Act & Assert
        threshold.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_WhenBeforeEffectiveFrom()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        threshold.EffectiveTo = null;

        // Act - Check against today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Assert
        threshold.IsEffective(today).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_WhenAfterEffectiveTo()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = null;
        threshold.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

        // Act - Check against today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Assert
        threshold.IsEffective(today).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_ReturnsTrue_WhenWithinDateRange()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        threshold.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // Act - Check against today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Assert
        threshold.IsEffective(today).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsTrue_OnExactStartDate()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = startDate;
        threshold.EffectiveTo = null;

        // Act & Assert
        threshold.IsEffective(startDate).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsTrue_OnExactEndDate()
    {
        // Arrange
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = CreateThreshold();
        threshold.IsActive = true;
        threshold.EffectiveFrom = null;
        threshold.EffectiveTo = endDate;

        // Act & Assert
        threshold.IsEffective(endDate).Should().BeTrue();
    }

    #endregion

    #region IsExceeded Tests

    [Fact]
    public void IsExceeded_ReturnsTrue_WhenValueAboveLimit()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 100m;

        // Act & Assert
        threshold.IsExceeded(101m).Should().BeTrue();
        threshold.IsExceeded(150m).Should().BeTrue();
    }

    [Fact]
    public void IsExceeded_ReturnsFalse_WhenValueBelowOrEqualLimit()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 100m;

        // Act & Assert
        threshold.IsExceeded(99m).Should().BeFalse();
        threshold.IsExceeded(100m).Should().BeFalse();
    }

    [Fact]
    public void IsExceeded_HandlesZeroLimit()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 0m;

        // Act & Assert
        threshold.IsExceeded(0.01m).Should().BeTrue();
        threshold.IsExceeded(0m).Should().BeFalse();
    }

    #endregion

    #region IsWarning Tests

    [Fact]
    public void IsWarning_ReturnsTrue_WhenValueAtWarningLevel()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 100m;
        threshold.WarningThresholdPercent = 80m; // Warning at 80 or above

        // Act & Assert - 80% of 100 = 80
        threshold.IsWarning(80m).Should().BeTrue();
        threshold.IsWarning(90m).Should().BeTrue();
        threshold.IsWarning(100m).Should().BeTrue();
    }

    [Fact]
    public void IsWarning_ReturnsFalse_WhenValueBelowWarningLevel()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 100m;
        threshold.WarningThresholdPercent = 80m;

        // Act & Assert
        threshold.IsWarning(79m).Should().BeFalse();
        threshold.IsWarning(50m).Should().BeFalse();
    }

    [Fact]
    public void IsWarning_ReturnsFalse_WhenValueExceedsLimit()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 100m;
        threshold.WarningThresholdPercent = 80m;

        // Act & Assert - Above 100 is exceeded, not warning
        threshold.IsWarning(101m).Should().BeFalse();
    }

    #endregion

    #region ExceedsMaxOverride Tests

    [Fact]
    public void ExceedsMaxOverride_ReturnsFalse_WhenOverrideNotAllowed()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.AllowOverride = false;
        threshold.MaxOverridePercent = 120m;
        threshold.LimitValue = 100m;

        // Act & Assert - Even high values return false when override not allowed
        threshold.ExceedsMaxOverride(150m).Should().BeFalse();
    }

    [Fact]
    public void ExceedsMaxOverride_ReturnsFalse_WhenNoMaxOverrideSet()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.AllowOverride = true;
        threshold.MaxOverridePercent = null;
        threshold.LimitValue = 100m;

        // Act & Assert - No max means unlimited override
        threshold.ExceedsMaxOverride(500m).Should().BeFalse();
    }

    [Fact]
    public void ExceedsMaxOverride_ReturnsTrue_WhenValueExceedsMaxOverride()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.AllowOverride = true;
        threshold.MaxOverridePercent = 120m; // Max 120% of limit
        threshold.LimitValue = 100m;

        // Act & Assert - 120% of 100 = 120, so 121 exceeds
        threshold.ExceedsMaxOverride(121m).Should().BeTrue();
    }

    [Fact]
    public void ExceedsMaxOverride_ReturnsFalse_WhenValueWithinMaxOverride()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.AllowOverride = true;
        threshold.MaxOverridePercent = 120m;
        threshold.LimitValue = 100m;

        // Act & Assert
        threshold.ExceedsMaxOverride(110m).Should().BeFalse();
        threshold.ExceedsMaxOverride(120m).Should().BeFalse();
    }

    #endregion

    #region GetUsagePercent Tests

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(50, 100, 50)]
    [InlineData(150, 100, 150)]
    [InlineData(0, 100, 0)]
    public void GetUsagePercent_CalculatesCorrectly(decimal value, decimal limit, decimal expected)
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = limit;

        // Act & Assert
        threshold.GetUsagePercent(value).Should().Be(expected);
    }

    [Fact]
    public void GetUsagePercent_Returns100_WhenLimitIsZero()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.LimitValue = 0m;

        // Act & Assert - Avoid division by zero
        threshold.GetUsagePercent(50m).Should().Be(100m);
    }

    #endregion

    #region AppliesToSubstance Tests

    [Fact]
    public void AppliesToSubstance_ReturnsTrue_WhenNoSubstanceCodeSet()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.SubstanceCode = null;

        // Act & Assert - Applies to all substances
        threshold.AppliesToSubstance("Morphine").Should().BeTrue();
    }

    [Fact]
    public void AppliesToSubstance_ReturnsTrue_WhenSubstanceCodeMatches()
    {
        // Arrange
        var substanceCode = "Morphine";
        var threshold = CreateThreshold();
        threshold.SubstanceCode = substanceCode;

        // Act & Assert
        threshold.AppliesToSubstance(substanceCode).Should().BeTrue();
    }

    [Fact]
    public void AppliesToSubstance_ReturnsFalse_WhenSubstanceCodeDoesNotMatch()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.SubstanceCode = "Fentanyl";

        // Act & Assert
        threshold.AppliesToSubstance("Morphine").Should().BeFalse();
    }

    #endregion

    #region AppliesToCustomer Tests

    [Fact]
    public void AppliesToCustomer_ReturnsTrue_WhenSpecificCustomerMatches()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var threshold = CreateThreshold();
        threshold.CustomerId = customerId;
        threshold.CustomerCategory = null;

        // Act & Assert
        threshold.AppliesToCustomer(customerId, BusinessCategory.WholesalerEU).Should().BeTrue();
    }

    [Fact]
    public void AppliesToCustomer_ReturnsFalse_WhenSpecificCustomerDoesNotMatch()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerId = Guid.NewGuid();
        threshold.CustomerCategory = null;

        // Act & Assert
        threshold.AppliesToCustomer(Guid.NewGuid(), BusinessCategory.WholesalerEU).Should().BeFalse();
    }

    [Fact]
    public void AppliesToCustomer_ReturnsTrue_WhenCategoryMatches()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerId = null;
        threshold.CustomerCategory = BusinessCategory.WholesalerEU;

        // Act & Assert
        threshold.AppliesToCustomer(Guid.NewGuid(), BusinessCategory.WholesalerEU).Should().BeTrue();
    }

    [Fact]
    public void AppliesToCustomer_ReturnsFalse_WhenCategoryDoesNotMatch()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerId = null;
        threshold.CustomerCategory = BusinessCategory.WholesalerEU;

        // Act & Assert
        threshold.AppliesToCustomer(Guid.NewGuid(), BusinessCategory.CommunityPharmacy).Should().BeFalse();
    }

    [Fact]
    public void AppliesToCustomer_ReturnsTrue_WhenNoCategoryOrCustomerSet()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerId = null;
        threshold.CustomerCategory = null;

        // Act & Assert - Applies to all customers
        threshold.AppliesToCustomer(Guid.NewGuid(), BusinessCategory.HospitalPharmacy).Should().BeTrue();
    }

    #endregion

    #region AppliesToCustomerCategory Tests

    [Fact]
    public void AppliesToCustomerCategory_ReturnsTrue_WhenNoCategorySet()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerCategory = null;

        // Act & Assert
        threshold.AppliesToCustomerCategory(BusinessCategory.WholesalerEU).Should().BeTrue();
        threshold.AppliesToCustomerCategory(BusinessCategory.CommunityPharmacy).Should().BeTrue();
    }

    [Fact]
    public void AppliesToCustomerCategory_ReturnsTrue_WhenCategoryMatches()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerCategory = BusinessCategory.HospitalPharmacy;

        // Act & Assert
        threshold.AppliesToCustomerCategory(BusinessCategory.HospitalPharmacy).Should().BeTrue();
    }

    [Fact]
    public void AppliesToCustomerCategory_ReturnsFalse_WhenCategoryDoesNotMatch()
    {
        // Arrange
        var threshold = CreateThreshold();
        threshold.CustomerCategory = BusinessCategory.HospitalPharmacy;

        // Act & Assert
        threshold.AppliesToCustomerCategory(BusinessCategory.Veterinarian).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Threshold CreateThreshold()
    {
        return new Threshold
        {
            Id = Guid.NewGuid(),
            Name = "Test Threshold",
            ThresholdType = ThresholdType.Quantity,
            Period = ThresholdPeriod.Monthly,
            LimitValue = 1000m,
            LimitUnit = "g",
            WarningThresholdPercent = 80m,
            AllowOverride = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }

    #endregion
}
