using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpserviceprovider entity.
/// T202: Dataverse DTO for GDP service provider.
/// </summary>
public class GdpServiceProviderDto
{
    public Guid phr_gdpserviceproviderid { get; set; }
    public string? phr_providername { get; set; }
    public int phr_servicetype { get; set; }
    public bool phr_temperaturecontrolledcapability { get; set; }
    public string? phr_approvedroutes { get; set; }
    public int phr_qualificationstatus { get; set; }
    public int phr_reviewfrequencymonths { get; set; }
    public DateTime? phr_lastreviewdate { get; set; }
    public DateTime? phr_nextreviewdate { get; set; }
    public bool phr_isactive { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }

    public GdpServiceProvider ToDomainModel()
    {
        return new GdpServiceProvider
        {
            ProviderId = phr_gdpserviceproviderid,
            ProviderName = phr_providername ?? string.Empty,
            ServiceType = (GdpServiceType)phr_servicetype,
            TemperatureControlledCapability = phr_temperaturecontrolledcapability,
            ApprovedRoutes = phr_approvedroutes,
            QualificationStatus = (GdpQualificationStatus)phr_qualificationstatus,
            ReviewFrequencyMonths = phr_reviewfrequencymonths,
            LastReviewDate = phr_lastreviewdate.HasValue ? DateOnly.FromDateTime(phr_lastreviewdate.Value) : null,
            NextReviewDate = phr_nextreviewdate.HasValue ? DateOnly.FromDateTime(phr_nextreviewdate.Value) : null,
            IsActive = phr_isactive,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_gdpcredential entity.
/// T202: Dataverse DTO for GDP credential.
/// </summary>
public class GdpCredentialDto
{
    public Guid phr_gdpcredentialid { get; set; }
    public int phr_entitytype { get; set; }
    public Guid phr_entityid { get; set; }
    public string? phr_wdanumber { get; set; }
    public string? phr_gdpcertificatenumber { get; set; }
    public string? phr_eudragmdpentryurl { get; set; }
    public DateTime? phr_validitystartdate { get; set; }
    public DateTime? phr_validityenddate { get; set; }
    public int phr_qualificationstatus { get; set; }
    public DateTime? phr_lastverificationdate { get; set; }
    public DateTime? phr_nextreviewdate { get; set; }
    public DateTime? createdon { get; set; }
    public DateTime? modifiedon { get; set; }
    public byte[]? versionnumber { get; set; }

    public GdpCredential ToDomainModel()
    {
        return new GdpCredential
        {
            CredentialId = phr_gdpcredentialid,
            EntityType = (GdpCredentialEntityType)phr_entitytype,
            EntityId = phr_entityid,
            WdaNumber = phr_wdanumber,
            GdpCertificateNumber = phr_gdpcertificatenumber,
            EudraGmdpEntryUrl = phr_eudragmdpentryurl,
            ValidityStartDate = phr_validitystartdate.HasValue ? DateOnly.FromDateTime(phr_validitystartdate.Value) : null,
            ValidityEndDate = phr_validityenddate.HasValue ? DateOnly.FromDateTime(phr_validityenddate.Value) : null,
            QualificationStatus = (GdpQualificationStatus)phr_qualificationstatus,
            LastVerificationDate = phr_lastverificationdate.HasValue ? DateOnly.FromDateTime(phr_lastverificationdate.Value) : null,
            NextReviewDate = phr_nextreviewdate.HasValue ? DateOnly.FromDateTime(phr_nextreviewdate.Value) : null,
            CreatedDate = createdon ?? DateTime.MinValue,
            ModifiedDate = modifiedon ?? DateTime.MinValue,
            RowVersion = versionnumber ?? Array.Empty<byte>()
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_qualificationreview entity.
/// T202: Dataverse DTO for qualification review.
/// </summary>
public class QualificationReviewDto
{
    public Guid phr_qualificationreviewid { get; set; }
    public int phr_entitytype { get; set; }
    public Guid phr_entityid { get; set; }
    public DateTime? phr_reviewdate { get; set; }
    public int phr_reviewmethod { get; set; }
    public int phr_reviewoutcome { get; set; }
    public string? phr_reviewername { get; set; }
    public string? phr_notes { get; set; }
    public DateTime? phr_nextreviewdate { get; set; }

    public QualificationReview ToDomainModel()
    {
        return new QualificationReview
        {
            ReviewId = phr_qualificationreviewid,
            EntityType = (ReviewEntityType)phr_entitytype,
            EntityId = phr_entityid,
            ReviewDate = phr_reviewdate.HasValue ? DateOnly.FromDateTime(phr_reviewdate.Value) : DateOnly.MinValue,
            ReviewMethod = (ReviewMethod)phr_reviewmethod,
            ReviewOutcome = (ReviewOutcome)phr_reviewoutcome,
            ReviewerName = phr_reviewername ?? string.Empty,
            Notes = phr_notes,
            NextReviewDate = phr_nextreviewdate.HasValue ? DateOnly.FromDateTime(phr_nextreviewdate.Value) : null
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_gdpcredentialverification entity.
/// T202: Dataverse DTO for credential verification.
/// </summary>
public class GdpCredentialVerificationDto
{
    public Guid phr_gdpcredentialverificationid { get; set; }
    public Guid phr_credentialid { get; set; }
    public DateTime? phr_verificationdate { get; set; }
    public int phr_verificationmethod { get; set; }
    public string? phr_verifiedby { get; set; }
    public int phr_outcome { get; set; }
    public string? phr_notes { get; set; }

    public GdpCredentialVerification ToDomainModel()
    {
        return new GdpCredentialVerification
        {
            VerificationId = phr_gdpcredentialverificationid,
            CredentialId = phr_credentialid,
            VerificationDate = phr_verificationdate.HasValue ? DateOnly.FromDateTime(phr_verificationdate.Value) : DateOnly.MinValue,
            VerificationMethod = (GdpVerificationMethod)phr_verificationmethod,
            VerifiedBy = phr_verifiedby ?? string.Empty,
            Outcome = (GdpVerificationOutcome)phr_outcome,
            Notes = phr_notes
        };
    }
}
