using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class UpdateLicenceRequestDtoValidator : AbstractValidator<UpdateLicenceRequestDto>
{
    private static readonly string[] AllowedHolderTypes = { "Company", "Customer" };
    private static readonly string[] AllowedStatuses = { "Valid", "Expired", "Suspended", "Revoked", "Pending" };

    public UpdateLicenceRequestDtoValidator()
    {
        RuleFor(x => x.LicenceNumber)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.LicenceTypeId)
            .NotEmpty()
            .WithMessage("'LicenceTypeId' is required.");

        RuleFor(x => x.HolderType)
            .NotEmpty()
            .Must(h => AllowedHolderTypes.Contains(h))
            .WithMessage($"'HolderType' must be one of: {string.Join(", ", AllowedHolderTypes)}.");

        RuleFor(x => x.HolderId)
            .NotEmpty()
            .WithMessage("'HolderId' is required.");

        RuleFor(x => x.IssuingAuthority)
            .NotEmpty()
            .SafeText(SharedValidationRules.MaxShortTextLength);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"'Status' must be one of: {string.Join(", ", AllowedStatuses)}.");

        RuleFor(x => x.IssueDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("'IssueDate' cannot be in the future.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.IssueDate)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("'ExpiryDate' must be after 'IssueDate'.");

        RuleFor(x => x.Scope)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);
    }
}
