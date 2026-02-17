using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Tests for GdpSop domain model.
/// T269: Validates SOP business rules per FR-049.
/// </summary>
public class GdpSopTests
{
    #region Validate

    [Fact]
    public void Validate_WithValidRecord_ShouldSucceed()
    {
        var sop = CreateValidSop();
        var result = sop.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMissingSopNumber_ShouldFail()
    {
        var sop = CreateValidSop();
        sop.SopNumber = string.Empty;
        var result = sop.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("SopNumber"));
    }

    [Fact]
    public void Validate_WithMissingTitle_ShouldFail()
    {
        var sop = CreateValidSop();
        sop.Title = string.Empty;
        var result = sop.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("Title"));
    }

    [Fact]
    public void Validate_WithMissingVersion_ShouldFail()
    {
        var sop = CreateValidSop();
        sop.Version = string.Empty;
        var result = sop.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("Version"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAll()
    {
        var sop = new GdpSop();
        var result = sop.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCountGreaterOrEqualTo(3);
    }

    #endregion

    #region Enums

    [Fact]
    public void GdpSopCategory_ShouldHaveExpectedValues()
    {
        Enum.GetValues<GdpSopCategory>().Should().HaveCount(6);
    }

    [Fact]
    public void GdpSopCategory_ShouldContainReturns()
    {
        Enum.IsDefined(GdpSopCategory.Returns).Should().BeTrue();
    }

    [Fact]
    public void GdpSopCategory_ShouldContainRecalls()
    {
        Enum.IsDefined(GdpSopCategory.Recalls).Should().BeTrue();
    }

    [Fact]
    public void GdpSopCategory_ShouldContainTemperatureExcursions()
    {
        Enum.IsDefined(GdpSopCategory.TemperatureExcursions).Should().BeTrue();
    }

    #endregion

    #region Properties

    [Fact]
    public void IsActive_ShouldDefaultToTrue()
    {
        var sop = new GdpSop();
        sop.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DocumentUrl_ShouldBeNullByDefault()
    {
        var sop = new GdpSop();
        sop.DocumentUrl.Should().BeNull();
    }

    #endregion

    private static GdpSop CreateValidSop() => new()
    {
        SopId = Guid.NewGuid(),
        SopNumber = "SOP-GDP-001",
        Title = "Returns Handling Procedure",
        Category = GdpSopCategory.Returns,
        Version = "1.0",
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
        DocumentUrl = "https://docs.example.com/sop-gdp-001",
        IsActive = true
    };
}
