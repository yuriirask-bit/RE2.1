using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class RecordScopeChangeRequestDtoValidator : AbstractValidator<RecordScopeChangeRequestDto>
{
    public RecordScopeChangeRequestDtoValidator()
    {
        RuleFor(x => x.ChangeDescription)
            .NotEmpty()
            .SafeText(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.RecordedBy)
            .NotEmpty()
            .WithMessage("'RecordedBy' is required.");

        RuleFor(x => x.RecorderName)
            .SafeTextNullable(SharedValidationRules.MaxShortTextLength);

        RuleFor(x => x.SubstancesAdded)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.SubstancesRemoved)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.ActivitiesAdded)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);

        RuleFor(x => x.ActivitiesRemoved)
            .SafeTextNullable(SharedValidationRules.MaxTextLength);
    }
}
