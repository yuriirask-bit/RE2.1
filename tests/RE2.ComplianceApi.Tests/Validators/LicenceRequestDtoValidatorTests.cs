using FluentAssertions;
using FluentValidation.TestHelper;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceApi.Validators;

namespace RE2.ComplianceApi.Tests.Validators;

public class CreateLicenceRequestDtoValidatorTests
{
    private readonly CreateLicenceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_LicenceNumber_Is_Empty()
    {
        var model = CreateValid();
        model.LicenceNumber = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LicenceNumber);
    }

    [Fact]
    public void Should_Fail_When_LicenceTypeId_Is_Empty()
    {
        var model = CreateValid();
        model.LicenceTypeId = Guid.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LicenceTypeId);
    }

    [Fact]
    public void Should_Fail_When_HolderType_Is_Invalid()
    {
        var model = CreateValid();
        model.HolderType = "InvalidType";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.HolderType);
    }

    [Fact]
    public void Should_Fail_When_HolderId_Is_Empty()
    {
        var model = CreateValid();
        model.HolderId = Guid.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.HolderId);
    }

    [Fact]
    public void Should_Fail_When_IssuingAuthority_Is_Empty()
    {
        var model = CreateValid();
        model.IssuingAuthority = string.Empty;
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.IssuingAuthority);
    }

    [Fact]
    public void Should_Fail_When_IssueDate_Is_In_The_Future()
    {
        var model = CreateValid();
        model.IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.IssueDate);
    }

    [Fact]
    public void Should_Fail_When_ExpiryDate_Is_Before_IssueDate()
    {
        var model = CreateValid();
        model.IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        model.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20));
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryDate);
    }

    [Fact]
    public void Should_Fail_When_Scope_Contains_Script_Tags()
    {
        var model = CreateValid();
        model.Scope = "<script>alert('xss')</script>";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Scope);
    }

    private static CreateLicenceRequestDto CreateValid() => new()
    {
        LicenceNumber = "LIC-2026-001",
        LicenceTypeId = Guid.NewGuid(),
        HolderType = "Company",
        HolderId = Guid.NewGuid(),
        IssuingAuthority = "Dutch Healthcare Authority",
        IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
        Scope = "Distribution of controlled substances"
    };
}

public class UpdateLicenceRequestDtoValidatorTests
{
    private readonly UpdateLicenceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var model = CreateValid();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Status_Is_Invalid()
    {
        var model = CreateValid();
        model.Status = "InvalidStatus";
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    private static UpdateLicenceRequestDto CreateValid() => new()
    {
        LicenceNumber = "LIC-2026-001",
        LicenceTypeId = Guid.NewGuid(),
        HolderType = "Customer",
        HolderId = Guid.NewGuid(),
        IssuingAuthority = "Dutch Healthcare Authority",
        IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
        Status = "Valid",
        Scope = "Distribution of controlled substances"
    };
}
