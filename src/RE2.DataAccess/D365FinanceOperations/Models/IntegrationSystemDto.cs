using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// Data transfer object for Dataverse phr_integrationsystem virtual table.
/// T047b: DTO for IntegrationSystem mapping to data-model.md entity 27.
/// Stored in Dataverse and accessed via virtual table API.
/// </summary>
public class IntegrationSystemDto
{
    /// <summary>
    /// Primary key in Dataverse.
    /// </summary>
    public Guid phr_integrationsystemid { get; set; }

    /// <summary>
    /// System name (unique, indexed).
    /// </summary>
    public string? phr_systemname { get; set; }

    /// <summary>
    /// System type as integer per IntegrationSystemType enum.
    /// </summary>
    public int phr_systemtype { get; set; }

    /// <summary>
    /// Hashed API key for API key authentication.
    /// </summary>
    public string? phr_apikeyhash { get; set; }

    /// <summary>
    /// OAuth client ID for OAuth authentication.
    /// </summary>
    public string? phr_oauthclientid { get; set; }

    /// <summary>
    /// Comma-separated list of authorized API endpoints.
    /// </summary>
    public string? phr_authorizedendpoints { get; set; }

    /// <summary>
    /// Comma-separated IP addresses allowed.
    /// </summary>
    public string? phr_ipwhitelist { get; set; }

    /// <summary>
    /// Whether the system is active and can call APIs.
    /// </summary>
    public bool phr_isactive { get; set; }

    /// <summary>
    /// Technical contact person for this integration.
    /// </summary>
    public string? phr_contactperson { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime phr_createddate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime phr_modifieddate { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>IntegrationSystem domain model.</returns>
    public IntegrationSystem ToDomainModel()
    {
        return new IntegrationSystem
        {
            IntegrationSystemId = phr_integrationsystemid,
            SystemName = phr_systemname ?? string.Empty,
            SystemType = (IntegrationSystemType)phr_systemtype,
            ApiKeyHash = phr_apikeyhash,
            OAuthClientId = phr_oauthclientid,
            AuthorizedEndpoints = phr_authorizedendpoints,
            IpWhitelist = phr_ipwhitelist,
            IsActive = phr_isactive,
            ContactPerson = phr_contactperson,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>IntegrationSystemDto for Dataverse persistence.</returns>
    public static IntegrationSystemDto FromDomainModel(IntegrationSystem model)
    {
        return new IntegrationSystemDto
        {
            phr_integrationsystemid = model.IntegrationSystemId,
            phr_systemname = model.SystemName,
            phr_systemtype = (int)model.SystemType,
            phr_apikeyhash = model.ApiKeyHash,
            phr_oauthclientid = model.OAuthClientId,
            phr_authorizedendpoints = model.AuthorizedEndpoints,
            phr_ipwhitelist = model.IpWhitelist,
            phr_isactive = model.IsActive,
            phr_contactperson = model.ContactPerson,
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate
        };
    }
}

/// <summary>
/// OData response wrapper for IntegrationSystem queries.
/// </summary>
public class IntegrationSystemODataResponse
{
    /// <summary>
    /// Collection of integration system DTOs.
    /// </summary>
    public List<IntegrationSystemDto> value { get; set; } = new();
}
