using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class OverrideRequestDtoValidatorTests
{
    private readonly OverrideApprovalRequestDtoValidator _approvalValidator = new();
    private readonly OverrideRejectionRequestDtoValidator _rejectionValidator = new();

    #region OverrideApprovalRequestDto Tests

    [Fact]
    public void Approval_Should_Pass_When_Valid()
    {
        var model = CreateValidApproval();
        var result = _approvalValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Approval_Should_Fail_When_Justification_Is_Empty()
    {
        var model = CreateValidApproval();
        model.Justification = string.Empty;
        var result = _approvalValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Justification);
    }

    [Fact]
    public void Approval_Should_Fail_When_Justification_Is_Too_Short()
    {
        var model = CreateValidApproval();
        model.Justification = "Too short";
        var result = _approvalValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Justification);
    }

    [Fact]
    public void Approval_Should_Fail_When_Justification_Contains_Script_Tag()
    {
        var model = CreateValidApproval();
        model.Justification = "<script>alert('xss')</script> this is a long enough justification text";
        var result = _approvalValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Justification);
    }

    #endregion

    #region OverrideRejectionRequestDto Tests

    [Fact]
    public void Rejection_Should_Pass_When_Valid()
    {
        var model = CreateValidRejection();
        var result = _rejectionValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejection_Should_Fail_When_Reason_Is_Empty()
    {
        var model = CreateValidRejection();
        model.Reason = string.Empty;
        var result = _rejectionValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Rejection_Should_Fail_When_Reason_Is_Too_Short()
    {
        var model = CreateValidRejection();
        model.Reason = "Short";
        var result = _rejectionValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    #endregion

    private static OverrideApprovalRequestDto CreateValidApproval() => new()
    {
        Justification = "This override is justified because the customer has a valid exemption certificate on file."
    };

    private static OverrideRejectionRequestDto CreateValidRejection() => new()
    {
        Reason = "The customer does not have sufficient documentation to warrant an override."
    };
}
