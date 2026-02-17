using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Tests for GdpEquipmentQualification domain model.
/// T254: Validates equipment qualification business rules per FR-048.
/// </summary>
public class GdpEquipmentQualificationTests
{
    #region Validate

    [Fact]
    public void Validate_WithValidRecord_ShouldSucceed()
    {
        var equipment = CreateValidEquipment();
        var result = equipment.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMissingEquipmentName_ShouldFail()
    {
        var equipment = CreateValidEquipment();
        equipment.EquipmentName = string.Empty;
        var result = equipment.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("EquipmentName"));
    }

    [Fact]
    public void Validate_WithMissingQualifiedBy_ShouldFail()
    {
        var equipment = CreateValidEquipment();
        equipment.QualifiedBy = string.Empty;
        var result = equipment.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("QualifiedBy"));
    }

    [Fact]
    public void Validate_WithFutureQualificationDate_ShouldFail()
    {
        var equipment = CreateValidEquipment();
        equipment.QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var result = equipment.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("future"));
    }

    [Fact]
    public void Validate_WithRequalificationBeforeQualification_ShouldFail()
    {
        var equipment = CreateValidEquipment();
        equipment.QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var result = equipment.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("RequalificationDueDate"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAll()
    {
        var equipment = new GdpEquipmentQualification();
        var result = equipment.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_WithPastRequalificationDate_ShouldReturnTrue()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        equipment.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithFutureRequalificationDate_ShouldReturnFalse()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
        equipment.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithNoRequalificationDate_ShouldReturnFalse()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = null;
        equipment.IsExpired().Should().BeFalse();
    }

    #endregion

    #region IsDueForRequalification

    [Fact]
    public void IsDueForRequalification_WithinWindow_ShouldReturnTrue()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);
        equipment.IsDueForRequalification(30).Should().BeTrue();
    }

    [Fact]
    public void IsDueForRequalification_OutsideWindow_ShouldReturnFalse()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
        equipment.IsDueForRequalification(30).Should().BeFalse();
    }

    [Fact]
    public void IsDueForRequalification_AlreadyExpired_ShouldReturnFalse()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        equipment.IsDueForRequalification(30).Should().BeFalse();
    }

    [Fact]
    public void IsDueForRequalification_NoRequalificationDate_ShouldReturnFalse()
    {
        var equipment = CreateValidEquipment();
        equipment.RequalificationDueDate = null;
        equipment.IsDueForRequalification().Should().BeFalse();
    }

    #endregion

    #region Enums

    [Fact]
    public void GdpEquipmentType_ShouldHaveExpectedValues()
    {
        Enum.GetValues<GdpEquipmentType>().Should().HaveCount(4);
    }

    [Fact]
    public void GdpQualificationStatusType_ShouldHaveExpectedValues()
    {
        Enum.GetValues<GdpQualificationStatusType>().Should().HaveCount(4);
    }

    #endregion

    private static GdpEquipmentQualification CreateValidEquipment() => new()
    {
        EquipmentQualificationId = Guid.NewGuid(),
        EquipmentName = "Temperature Vehicle TRK-001",
        EquipmentType = GdpEquipmentType.TemperatureControlledVehicle,
        ProviderId = Guid.NewGuid(),
        QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
        RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(11),
        QualificationStatus = GdpQualificationStatusType.Qualified,
        QualifiedBy = "Test Engineer",
        Notes = "Annual qualification - all tests passed",
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };
}
