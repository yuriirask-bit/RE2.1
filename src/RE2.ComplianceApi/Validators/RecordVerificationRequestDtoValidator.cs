using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class RecordVerificationRequestDtoValidator : AbstractValidator<RecordVerificationRequestDto>
{
    public RecordVerificationRequestDtoValidator()
    {
        RuleFor(x => x.VerificationDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("'VerificationDate' cannot be in the future.");

        RuleFor(x => x.VerifiedBy)
            .NotEmpty()
            .WithMessage("'VerifiedBy' is required.");

        RuleFor(x => x.Notes)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.AuthorityReferenceNumber)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.AuthorityReferenceNumber != null);

        RuleFor(x => x.VerifierName)
            .SafeTextNullable(SharedValidationRules.MaxShortTextLength);
    }
}
