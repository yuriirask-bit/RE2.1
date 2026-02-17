using FluentValidation;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Validators;

public class UpdateCustomerComplianceRequestDtoValidator : AbstractValidator<UpdateCustomerComplianceRequestDto>
{
    private static readonly string[] AllowedBusinessCategories =
    {
        "HospitalPharmacy", "CommunityPharmacy", "Veterinarian",
        "Manufacturer", "WholesalerEU", "WholesalerNonEU", "ResearchInstitution"
    };

    private static readonly string[] AllowedApprovalStatuses =
    {
        "Pending", "Approved", "Rejected", "Suspended", "UnderReview"
    };

    private static readonly string[] AllowedGdpStatuses =
    {
        "NotRequired", "Pending", "Qualified", "Disqualified", "UnderReview"
    };

    public UpdateCustomerComplianceRequestDtoValidator()
    {
        RuleFor(x => x.BusinessCategory)
            .NotEmpty()
            .Must(b => AllowedBusinessCategories.Contains(b))
            .WithMessage($"'BusinessCategory' must be one of: {string.Join(", ", AllowedBusinessCategories)}.");

        RuleFor(x => x.ApprovalStatus)
            .NotEmpty()
            .Must(s => AllowedApprovalStatuses.Contains(s))
            .WithMessage($"'ApprovalStatus' must be one of: {string.Join(", ", AllowedApprovalStatuses)}.");

        RuleFor(x => x.GdpQualificationStatus)
            .NotEmpty()
            .Must(s => AllowedGdpStatuses.Contains(s))
            .WithMessage($"'GdpQualificationStatus' must be one of: {string.Join(", ", AllowedGdpStatuses)}.");

        RuleFor(x => x.OnboardingDate)
            .Must(BeValidDateOrNull)
            .When(x => !string.IsNullOrEmpty(x.OnboardingDate))
            .WithMessage("'OnboardingDate' must be a valid date (yyyy-MM-dd).");

        RuleFor(x => x.NextReVerificationDate)
            .Must(BeValidDateOrNull)
            .When(x => !string.IsNullOrEmpty(x.NextReVerificationDate))
            .WithMessage("'NextReVerificationDate' must be a valid date (yyyy-MM-dd).");
    }

    private static bool BeValidDateOrNull(string? value)
    {
        return string.IsNullOrEmpty(value) || DateOnly.TryParse(value, out _);
    }
}
