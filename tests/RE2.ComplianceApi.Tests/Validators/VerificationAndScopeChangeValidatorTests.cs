using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class RecordVerificationRequestDtoValidatorTests
{
    private readonly RecordVerificationRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_VerificationDate_Is_In_The_Future()
    {
        var model = CreateValid();
        model.VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.VerificationDate);
    }

    [Fact]
    public void Should_Fail_When_VerifiedBy_Is_Empty()
    {
        var model = CreateValid();
        model.VerifiedBy = Guid.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.VerifiedBy);
    }

    [Fact]
    public void Should_Fail_When_Notes_Contains_Script_Tag()
    {
        var model = CreateValid();
        model.Notes = "<script>alert('xss')</script>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    private static RecordVerificationRequestDto CreateValid() => new()
    {
        VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
        VerifiedBy = Guid.NewGuid(),
        Notes = "Verification completed successfully",
        AuthorityReferenceNumber = "REF-2026-001",
        VerifierName = "John Smith"
    };
}

public class RecordScopeChangeRequestDtoValidatorTests
{
    private readonly RecordScopeChangeRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ChangeDescription_Is_Empty()
    {
        var model = CreateValid();
        model.ChangeDescription = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ChangeDescription);
    }

    [Fact]
    public void Should_Fail_When_RecordedBy_Is_Empty()
    {
        var model = CreateValid();
        model.RecordedBy = Guid.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.RecordedBy);
    }

    [Fact]
    public void Should_Fail_When_ChangeDescription_Contains_Html()
    {
        var model = CreateValid();
        model.ChangeDescription = "<div>Scope changed</div>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ChangeDescription);
    }

    private static RecordScopeChangeRequestDto CreateValid() => new()
    {
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
        ChangeDescription = "Added morphine to permitted substances",
        ChangeType = 0,
        RecordedBy = Guid.NewGuid(),
        RecorderName = "Jane Doe",
        SubstancesAdded = "Morphine",
        SubstancesRemoved = null,
        ActivitiesAdded = "Storage",
        ActivitiesRemoved = null
    };
}
