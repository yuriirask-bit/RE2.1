using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class TransactionValidationRequestDtoValidatorTests
{
    private readonly TransactionValidationRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ExternalId_Is_Empty()
    {
        var model = CreateValid();
        model.ExternalId = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void Should_Fail_When_ExternalId_Contains_Html_Tags()
    {
        var model = CreateValid();
        model.ExternalId = "<script>alert('xss')</script>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void Should_Fail_When_TransactionType_Is_Invalid()
    {
        var model = CreateValid();
        model.TransactionType = "InvalidType";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Should_Fail_When_Direction_Is_Invalid()
    {
        var model = CreateValid();
        model.Direction = "InvalidDirection";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Fail_When_CustomerAccount_Is_Empty()
    {
        var model = CreateValid();
        model.CustomerAccount = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CustomerAccount);
    }

    [Fact]
    public void Should_Fail_When_OriginCountry_Is_Invalid()
    {
        var model = CreateValid();
        model.OriginCountry = "INVALID";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.OriginCountry);
    }

    [Fact]
    public void Should_Fail_When_Lines_Is_Empty()
    {
        var model = CreateValid();
        model.Lines = new List<TransactionLineDto>();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public void Should_Fail_When_Line_Has_Invalid_Quantity()
    {
        var model = CreateValid();
        model.Lines = new List<TransactionLineDto>
        {
            new()
            {
                ItemNumber = "ITEM-001",
                DataAreaId = "dat",
                Quantity = 0
            }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Lines[0].Quantity");
    }

    private static TransactionValidationRequestDto CreateValid() => new()
    {
        ExternalId = "ORD-2026-001",
        TransactionType = "Order",
        Direction = "Internal",
        CustomerAccount = "CUST-001",
        CustomerDataAreaId = "dat",
        OriginCountry = "NL",
        DestinationCountry = "DE",
        Lines = new List<TransactionLineDto>
        {
            new()
            {
                ItemNumber = "ITEM-001",
                DataAreaId = "dat",
                Quantity = 10.0m
            }
        }
    };
}
