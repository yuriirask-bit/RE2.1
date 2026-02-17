using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class TransactionLineDtoValidatorTests
{
    private readonly TransactionLineDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ItemNumber_Is_Empty()
    {
        var model = CreateValid();
        model.ItemNumber = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ItemNumber);
    }

    [Fact]
    public void Should_Fail_When_DataAreaId_Is_Empty()
    {
        var model = CreateValid();
        model.DataAreaId = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DataAreaId);
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Zero()
    {
        var model = CreateValid();
        model.Quantity = 0;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        var model = CreateValid();
        model.Quantity = -5.0m;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    private static TransactionLineDto CreateValid() => new()
    {
        ItemNumber = "ITEM-001",
        DataAreaId = "dat",
        Quantity = 10.0m
    };
}
