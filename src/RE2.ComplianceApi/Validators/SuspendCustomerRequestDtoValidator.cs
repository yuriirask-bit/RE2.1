using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class SuspendCustomerRequestDtoValidator : AbstractValidator<SuspendCustomerRequestDto>
{
    public SuspendCustomerRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .SafeText(SharedValidationRules.MaxTextLength);
    }
}
