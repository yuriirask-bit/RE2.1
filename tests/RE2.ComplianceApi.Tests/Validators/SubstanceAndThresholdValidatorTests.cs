using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceApi.Tests.Validators;

public class ConfigureComplianceRequestDtoValidatorTests
{
    private readonly ConfigureComplianceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_SubstanceCode_Is_Empty()
    {
        var model = CreateValid();
        model.SubstanceCode = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SubstanceCode);
    }

    [Fact]
    public void Should_Fail_When_SubstanceCode_Contains_Special_Chars()
    {
        var model = CreateValid();
        model.SubstanceCode = "<script>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SubstanceCode);
    }

    private static ConfigureComplianceRequestDto CreateValid() => new()
    {
        SubstanceCode = "MORPH-001",
        RegulatoryRestrictions = null,
        IsActive = true,
        ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
    };
}

public class CreateThresholdRequestDtoValidatorTests
{
    private readonly CreateThresholdRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var model = CreateValid();
        model.Name = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_LimitValue_Is_Zero()
    {
        var model = CreateValid();
        model.LimitValue = 0;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LimitValue);
    }

    [Fact]
    public void Should_Fail_When_LimitValue_Is_Negative()
    {
        var model = CreateValid();
        model.LimitValue = -5m;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LimitValue);
    }

    [Fact]
    public void Should_Fail_When_WarningThresholdPercent_Is_Greater_Than_100()
    {
        var model = CreateValid();
        model.WarningThresholdPercent = 101m;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.WarningThresholdPercent);
    }

    [Fact]
    public void Should_Fail_When_WarningThresholdPercent_Is_Less_Than_Zero()
    {
        var model = CreateValid();
        model.WarningThresholdPercent = -1m;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.WarningThresholdPercent);
    }

    [Fact]
    public void Should_Fail_When_EffectiveTo_Is_Before_EffectiveFrom()
    {
        var model = CreateValid();
        model.EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        model.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.EffectiveTo);
    }

    private static CreateThresholdRequestDto CreateValid() => new()
    {
        Name = "Monthly Morphine Limit",
        Description = "Maximum monthly morphine distribution threshold",
        ThresholdType = ThresholdType.Quantity,
        Period = ThresholdPeriod.Monthly,
        SubstanceCode = "MORPH-001",
        LimitValue = 500m,
        LimitUnit = "g",
        WarningThresholdPercent = 80m,
        AllowOverride = true,
        MaxOverridePercent = 120m,
        IsActive = true,
        EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
        RegulatoryReference = "Opium Act Section 3.2"
    };
}

public class UpdateThresholdRequestDtoValidatorTests
{
    private readonly UpdateThresholdRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Name_Contains_Html()
    {
        var model = CreateValid();
        model.Name = "<b>Threshold</b>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    private static UpdateThresholdRequestDto CreateValid() => new()
    {
        Name = "Updated Morphine Limit",
        Description = "Revised maximum monthly morphine threshold",
        ThresholdType = ThresholdType.Quantity,
        Period = ThresholdPeriod.Monthly,
        SubstanceCode = "MORPH-001",
        LimitValue = 600m,
        LimitUnit = "g",
        WarningThresholdPercent = 85m,
        AllowOverride = true,
        MaxOverridePercent = 150m,
        IsActive = true,
        EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
        RegulatoryReference = "Opium Act Section 3.2 (revised)"
    };
}
