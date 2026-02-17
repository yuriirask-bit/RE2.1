using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class TransactionLineDtoValidator : AbstractValidator<TransactionLineDto>
{
    public TransactionLineDtoValidator()
    {
        RuleFor(x => x.ItemNumber)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.DataAreaId)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("'Quantity' must be greater than zero.");

        RuleFor(x => x.ProductDescription)
            .SafeTextNullable();

        RuleFor(x => x.BatchNumber)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.BatchNumber != null);

        RuleFor(x => x.UnitOfMeasure)
            .MaximumLength(20)
            .When(x => x.UnitOfMeasure != null);

        RuleFor(x => x.BaseUnit)
            .MaximumLength(20)
            .When(x => x.BaseUnit != null);
    }
}
