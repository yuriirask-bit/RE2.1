using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class ConfigureComplianceRequestDtoValidator : AbstractValidator<ConfigureComplianceRequestDto>
{
    public ConfigureComplianceRequestDtoValidator()
    {
        RuleFor(x => x.SubstanceCode)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.RegulatoryRestrictions)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);
    }
}
