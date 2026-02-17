using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class WarehouseOperationValidationRequestDtoValidator : AbstractValidator<WarehouseOperationValidationRequestDto>
{
    private static readonly string[] AllowedOperationTypes = { "Pick", "Pack", "PrintLabels", "ShipmentRelease" };

    public WarehouseOperationValidationRequestDtoValidator()
    {
        RuleFor(x => x.ExternalTransactionId)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.OperationType)
            .NotEmpty()
            .Must(t => AllowedOperationTypes.Contains(t))
            .WithMessage($"'OperationType' must be one of: {string.Join(", ", AllowedOperationTypes)}.");

        RuleFor(x => x.WarehouseSiteId)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.WarehouseSiteId != null);

        RuleFor(x => x.IntegrationSystemId)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode()
            .When(x => x.IntegrationSystemId != null);
    }
}
