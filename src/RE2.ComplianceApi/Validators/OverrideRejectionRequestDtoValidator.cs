using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class OverrideRejectionRequestDtoValidator : AbstractValidator<OverrideRejectionRequestDto>
{
    public OverrideRejectionRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("'Reason' must be at least 10 characters to provide adequate explanation.")
            .SafeText(SharedValidationRules.MaxTextLength);
    }
}
