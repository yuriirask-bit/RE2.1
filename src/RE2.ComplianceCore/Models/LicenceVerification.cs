using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records verification of a licence's authenticity and validity.
/// T105: LicenceVerification domain model per data-model.md entity 13.
/// Tracks when and how a licence was verified, supporting FR-009 verification requirements.
/// </summary>
public class LicenceVerification
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid VerificationId { get; set; }

    /// <summary>
    /// Which licence was verified.
    /// Required.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// How the licence was verified.
    /// Required.
    /// </summary>
    public VerificationMethod VerificationMethod { get; set; }

    /// <summary>
    /// When verification was performed.
    /// Required.
    /// </summary>
    public DateOnly VerificationDate { get; set; }

    /// <summary>
    /// Who performed the verification.
    /// Required.
    /// </summary>
    public Guid VerifiedBy { get; set; }

    /// <summary>
    /// Name of the person who performed verification.
    /// For display purposes.
    /// </summary>
    public string? VerifierName { get; set; }

    /// <summary>
    /// Verification result.
    /// Required.
    /// </summary>
    public VerificationOutcome Outcome { get; set; }

    /// <summary>
    /// Additional details or notes about the verification.
    /// Nullable.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Reference number from authority (if applicable).
    /// </summary>
    public string? AuthorityReferenceNumber { get; set; }

    /// <summary>
    /// Timestamp when the record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Validates the licence verification according to business rules.
    /// </summary>
    /// <returns>Validation result with any violations.</returns>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (LicenceId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "LicenceId is required"
            });
        }

        if (VerifiedBy == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "VerifiedBy is required"
            });
        }

        if (VerificationDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "VerificationDate cannot be in the future"
            });
        }

        // Authority reference recommended for certain methods
        if (VerificationMethod == VerificationMethod.AuthorityWebsite &&
            Outcome == VerificationOutcome.Valid &&
            string.IsNullOrWhiteSpace(AuthorityReferenceNumber))
        {
            // This is a warning, not blocking
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_WARNING,
                Message = "AuthorityReferenceNumber is recommended when verification method is AuthorityWebsite"
            });
        }

        return violations.Any(v => v.ErrorCode != ErrorCodes.VALIDATION_WARNING)
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if this verification confirms the licence is valid.
    /// </summary>
    public bool IsValid()
    {
        return Outcome == VerificationOutcome.Valid;
    }

    /// <summary>
    /// Checks if verification is still considered current (within last year).
    /// </summary>
    public bool IsCurrent()
    {
        var oneYearAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
        return VerificationDate >= oneYearAgo;
    }

    /// <summary>
    /// Gets the age of this verification in days.
    /// </summary>
    public int GetAgeDays()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - VerificationDate.DayNumber;
    }
}

/// <summary>
/// Method used to verify a licence.
/// Per data-model.md entity 13 VerificationMethod enum.
/// </summary>
public enum VerificationMethod
{
    /// <summary>
    /// Verified via official authority website (e.g., IGJ register).
    /// </summary>
    AuthorityWebsite = 1,

    /// <summary>
    /// Verified via email confirmation from authority.
    /// </summary>
    EmailConfirmation = 2,

    /// <summary>
    /// Verified via phone confirmation with authority.
    /// </summary>
    PhoneConfirmation = 3,

    /// <summary>
    /// Verified via physical inspection of documents.
    /// </summary>
    PhysicalInspection = 4,

    /// <summary>
    /// Verified via Farmatec database lookup.
    /// </summary>
    FarmatecDatabase = 5,

    /// <summary>
    /// Verified via EudraGMDP database.
    /// </summary>
    EudraGMDP = 6
}

/// <summary>
/// Outcome of a licence verification.
/// Per data-model.md entity 13 Outcome enum.
/// </summary>
public enum VerificationOutcome
{
    /// <summary>
    /// Licence verified as valid and authentic.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// Licence could not be verified or is invalid.
    /// </summary>
    Invalid = 2,

    /// <summary>
    /// Verification is pending/in progress.
    /// </summary>
    Pending = 3,

    /// <summary>
    /// Verification was inconclusive, requires follow-up.
    /// </summary>
    Inconclusive = 4
}
