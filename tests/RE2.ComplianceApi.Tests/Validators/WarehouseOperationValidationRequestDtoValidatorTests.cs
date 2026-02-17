using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class WarehouseOperationValidationRequestDtoValidatorTests
{
    private readonly WarehouseOperationValidationRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ExternalTransactionId_Is_Empty()
    {
        var model = CreateValid();
        model.ExternalTransactionId = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExternalTransactionId);
    }

    [Fact]
    public void Should_Fail_When_OperationType_Is_Invalid()
    {
        var model = CreateValid();
        model.OperationType = "InvalidOperation";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.OperationType);
    }

    [Fact]
    public void Should_Fail_When_OperationType_Is_Empty()
    {
        var model = CreateValid();
        model.OperationType = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.OperationType);
    }

    private static WarehouseOperationValidationRequestDto CreateValid() => new()
    {
        ExternalTransactionId = "ORD-2026-001",
        OperationType = "Pick"
    };
}
