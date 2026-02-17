using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class TransactionValidationRequestDtoValidator : AbstractValidator<TransactionValidationRequestDto>
{
    private static readonly string[] AllowedTransactionTypes = { "Order", "Shipment", "Return", "Transfer" };
    private static readonly string[] AllowedDirections = { "Internal", "Inbound", "Outbound" };

    public TransactionValidationRequestDtoValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.TransactionType)
            .NotEmpty()
            .Must(t => AllowedTransactionTypes.Contains(t))
            .WithMessage($"'TransactionType' must be one of: {string.Join(", ", AllowedTransactionTypes)}.");

        RuleFor(x => x.Direction)
            .NotEmpty()
            .Must(d => AllowedDirections.Contains(d))
            .WithMessage($"'Direction' must be one of: {string.Join(", ", AllowedDirections)}.");

        RuleFor(x => x.CustomerAccount)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.CustomerDataAreaId)
            .NotEmpty()
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode();

        RuleFor(x => x.OriginCountry)
            .NotEmpty()
            .IsoCountryCode();

        RuleFor(x => x.DestinationCountry)
            .IsoCountryCodeNullable();

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one transaction line is required.");

        RuleForEach(x => x.Lines)
            .SetValidator(new TransactionLineDtoValidator());

        RuleFor(x => x.IntegrationSystemId)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .SafeCode()
            .When(x => x.IntegrationSystemId != null);

        RuleFor(x => x.ExternalUserId)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.ExternalUserId != null);

        RuleFor(x => x.WarehouseSiteId)
            .MaximumLength(SharedValidationRules.MaxCodeLength)
            .When(x => x.WarehouseSiteId != null);
    }
}
