using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class OverrideApprovalRequestDtoValidator : AbstractValidator<OverrideApprovalRequestDto>
{
    public OverrideApprovalRequestDtoValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("'Justification' must be at least 20 characters to provide adequate reasoning.")
            .SafeText(SharedValidationRules.MaxTextLength);
    }
}
