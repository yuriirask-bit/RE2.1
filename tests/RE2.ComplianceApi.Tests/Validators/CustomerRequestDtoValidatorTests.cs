using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class ConfigureCustomerComplianceRequestDtoValidatorTests
{
    private readonly ConfigureCustomerComplianceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
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
    public void Should_Fail_When_DataAreaId_Is_Empty()
    {
        var model = CreateValid();
        model.DataAreaId = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DataAreaId);
    }

    [Fact]
    public void Should_Fail_When_BusinessCategory_Is_Invalid()
    {
        var model = CreateValid();
        model.BusinessCategory = "InvalidCategory";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BusinessCategory);
    }

    [Fact]
    public void Should_Fail_When_ApprovalStatus_Is_Invalid()
    {
        var model = CreateValid();
        model.ApprovalStatus = "InvalidStatus";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ApprovalStatus);
    }

    [Fact]
    public void Should_Fail_When_OnboardingDate_Has_Invalid_Format()
    {
        var model = CreateValid();
        model.OnboardingDate = "not-a-date";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.OnboardingDate);
    }

    [Fact]
    public void Should_Pass_When_OnboardingDate_Has_Valid_Format()
    {
        var model = CreateValid();
        model.OnboardingDate = "2026-01-15";
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.OnboardingDate);
    }

    private static ConfigureCustomerComplianceRequestDto CreateValid() => new()
    {
        CustomerAccount = "CUST-001",
        DataAreaId = "dat",
        BusinessCategory = "HospitalPharmacy",
        ApprovalStatus = "Pending",
        OnboardingDate = null,
        NextReVerificationDate = null,
        GdpQualificationStatus = "NotRequired"
    };
}

public class UpdateCustomerComplianceRequestDtoValidatorTests
{
    private readonly UpdateCustomerComplianceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_BusinessCategory_Is_Invalid()
    {
        var model = CreateValid();
        model.BusinessCategory = "InvalidCategory";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BusinessCategory);
    }

    [Fact]
    public void Should_Fail_When_GdpQualificationStatus_Is_Invalid()
    {
        var model = CreateValid();
        model.GdpQualificationStatus = "InvalidStatus";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.GdpQualificationStatus);
    }

    private static UpdateCustomerComplianceRequestDto CreateValid() => new()
    {
        BusinessCategory = "CommunityPharmacy",
        ApprovalStatus = "Approved",
        OnboardingDate = "2026-01-15",
        NextReVerificationDate = "2027-01-15",
        GdpQualificationStatus = "Qualified"
    };
}

public class SuspendCustomerRequestDtoValidatorTests
{
    private readonly SuspendCustomerRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        var model = CreateValid();
        model.Reason = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Fail_When_Reason_Contains_Html()
    {
        var model = CreateValid();
        model.Reason = "<b>Suspended</b> due to <script>alert('xss')</script>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    private static SuspendCustomerRequestDto CreateValid() => new()
    {
        Reason = "Customer failed compliance re-verification audit"
    };
}
