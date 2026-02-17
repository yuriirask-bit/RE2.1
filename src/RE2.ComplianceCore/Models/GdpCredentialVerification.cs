using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Records verification of a partner's GDP status via EudraGMDP or national databases.
/// T201: GDP credential verification domain model per User Story 8 (FR-045).
/// Stored in Dataverse phr_gdpcredentialverification table.
/// </summary>
public class GdpCredentialVerification
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid VerificationId { get; set; }

    /// <summary>
    /// Which credential was verified.
    /// Required.
    /// </summary>
    public Guid CredentialId { get; set; }

    /// <summary>
    /// When verified.
    /// Required.
    /// </summary>
    public DateOnly VerificationDate { get; set; }

    /// <summary>
    /// How the verification was performed.
    /// Required.
    /// </summary>
    public GdpVerificationMethod VerificationMethod { get; set; }

    /// <summary>
    /// Who performed verification.
    /// Required.
    /// </summary>
    public required string VerifiedBy { get; set; }

    /// <summary>
    /// Verification result.
    /// Required.
    /// </summary>
    public GdpVerificationOutcome Outcome { get; set; }

    /// <summary>
    /// Additional details.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Validates the verification record.
    /// </summary>
    public ValidationResult Validate()
    {
        var violations = new List<ValidationViolation>();

        if (CredentialId == Guid.Empty)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "CredentialId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(VerifiedBy))
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

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }
}

/// <summary>
/// How the GDP verification was performed.
/// Per data-model.md entity 30 VerificationMethod enum.
/// </summary>
public enum GdpVerificationMethod
{
    /// <summary>
    /// Verified via EudraGMDP database.
    /// </summary>
    EudraGMDP,

    /// <summary>
    /// Verified via national regulatory database.
    /// </summary>
    NationalDatabase,

    /// <summary>
    /// Other verification method.
    /// </summary>
    Other
}

/// <summary>
/// Outcome of a GDP credential verification.
/// Per data-model.md entity 30 Outcome enum.
/// </summary>
public enum GdpVerificationOutcome
{
    /// <summary>
    /// Credential is valid.
    /// </summary>
    Valid,

    /// <summary>
    /// Credential is invalid.
    /// </summary>
    Invalid,

    /// <summary>
    /// Credential was not found in the database.
    /// </summary>
    NotFound
}
