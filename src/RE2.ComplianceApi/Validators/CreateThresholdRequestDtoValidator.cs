using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class CreateThresholdRequestDtoValidator : AbstractValidator<CreateThresholdRequestDto>
{
    public CreateThresholdRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .SafeText(SharedValidationRules.MaxShortTextLength);

        RuleFor(x => x.Description)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.LimitValue)
            .GreaterThan(0)
            .WithMessage("'LimitValue' must be greater than zero.");

        RuleFor(x => x.LimitUnit)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.WarningThresholdPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("'WarningThresholdPercent' must be between 0 and 100.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue)
            .WithMessage("'EffectiveTo' must be after 'EffectiveFrom'.");

        RuleFor(x => x.SubstanceCode)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode()
            .When(x => x.SubstanceCode != null);

        RuleFor(x => x.OpiumActList)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.OpiumActList != null);

        RuleFor(x => x.RegulatoryReference)
            .SafeTextNullable(SharedValidationRules.MaxShortTextLength);

        RuleFor(x => x.MaxOverridePercent)
            .InclusiveBetween(0, 1000)
            .When(x => x.MaxOverridePercent.HasValue)
            .WithMessage("'MaxOverridePercent' must be between 0 and 1000.");
    }
}
