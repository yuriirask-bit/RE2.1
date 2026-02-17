using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licenceverification virtual table.
/// T108a: DTO for LicenceVerification mapping to data-model.md entity 13.
/// Tracks verification history of licences.
/// </summary>
public class LicenceVerificationDto
{
    /// <summary>
    /// Primary key in Dataverse.
    /// </summary>
    public Guid phr_verificationid { get; set; }

    /// <summary>
    /// Foreign key to licence.
    /// </summary>
    public Guid phr_licenceid { get; set; }

    /// <summary>
    /// Verification method as integer per VerificationMethod enum.
    /// </summary>
    public int phr_verificationmethod { get; set; }

    /// <summary>
    /// When verification was performed.
    /// </summary>
    public DateTime phr_verificationdate { get; set; }

    /// <summary>
    /// Who performed the verification.
    /// </summary>
    public Guid phr_verifiedby { get; set; }

    /// <summary>
    /// Name of the verifier (for display).
    /// </summary>
    public string? phr_verifiername { get; set; }

    /// <summary>
    /// Verification outcome as integer per VerificationOutcome enum.
    /// </summary>
    public int phr_outcome { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    public string? phr_notes { get; set; }

    /// <summary>
    /// Reference number from authority.
    /// </summary>
    public string? phr_authorityreferencenumber { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime phr_createddate { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>LicenceVerification domain model.</returns>
    public LicenceVerification ToDomainModel()
    {
        return new LicenceVerification
        {
            VerificationId = phr_verificationid,
            LicenceId = phr_licenceid,
            VerificationMethod = (VerificationMethod)phr_verificationmethod,
            VerificationDate = DateOnly.FromDateTime(phr_verificationdate),
            VerifiedBy = phr_verifiedby,
            VerifierName = phr_verifiername,
            Outcome = (VerificationOutcome)phr_outcome,
            Notes = phr_notes,
            AuthorityReferenceNumber = phr_authorityreferencenumber,
            CreatedDate = phr_createddate
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>LicenceVerificationDto for Dataverse persistence.</returns>
    public static LicenceVerificationDto FromDomainModel(LicenceVerification model)
    {
        return new LicenceVerificationDto
        {
            phr_verificationid = model.VerificationId,
            phr_licenceid = model.LicenceId,
            phr_verificationmethod = (int)model.VerificationMethod,
            phr_verificationdate = model.VerificationDate.ToDateTime(TimeOnly.MinValue),
            phr_verifiedby = model.VerifiedBy,
            phr_verifiername = model.VerifierName,
            phr_outcome = (int)model.Outcome,
            phr_notes = model.Notes,
            phr_authorityreferencenumber = model.AuthorityReferenceNumber,
            phr_createddate = model.CreatedDate
        };
    }
}

/// <summary>
/// OData response wrapper for LicenceVerification queries.
/// </summary>
public class LicenceVerificationODataResponse
{
    /// <summary>
    /// Collection of licence verification DTOs.
    /// </summary>
    public List<LicenceVerificationDto> value { get; set; } = new();
}
