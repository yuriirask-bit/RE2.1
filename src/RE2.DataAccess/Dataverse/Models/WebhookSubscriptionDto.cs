using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_webhooksubscription virtual table.
/// T149d: Dataverse DTO for WebhookSubscription per FR-059.
/// Maps to data-model.md webhook subscription entity for async notifications.
/// </summary>
public class WebhookSubscriptionDto
{
    public Guid phr_webhooksubscriptionid { get; set; }
    public Guid phr_integrationsystemid { get; set; }
    public int phr_eventtypes { get; set; }
    public string? phr_callbackurl { get; set; }
    public string? phr_secretkey { get; set; }
    public bool phr_isactive { get; set; }
    public string? phr_description { get; set; }
    public int phr_failedattempts { get; set; }
    public DateTime? phr_lastsuccessfuldelivery { get; set; }
    public DateTime? phr_lastfaileddelivery { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }

    /// <summary>
    /// Converts this DTO to a domain model.
    /// </summary>
    /// <returns>The WebhookSubscription domain model.</returns>
    public WebhookSubscription ToDomainModel()
    {
        return new WebhookSubscription
        {
            SubscriptionId = phr_webhooksubscriptionid,
            IntegrationSystemId = phr_integrationsystemid,
            EventTypes = (WebhookEventType)phr_eventtypes,
            CallbackUrl = phr_callbackurl ?? string.Empty,
            SecretKey = phr_secretkey ?? string.Empty,
            IsActive = phr_isactive,
            Description = phr_description,
            FailedAttempts = phr_failedattempts,
            LastSuccessfulDelivery = phr_lastsuccessfuldelivery,
            LastFailedDelivery = phr_lastfaileddelivery,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate
        };
    }

    /// <summary>
    /// Creates a DTO from a domain model.
    /// </summary>
    /// <param name="model">The WebhookSubscription domain model.</param>
    /// <returns>The DTO for Dataverse.</returns>
    public static WebhookSubscriptionDto FromDomainModel(WebhookSubscription model)
    {
        return new WebhookSubscriptionDto
        {
            phr_webhooksubscriptionid = model.SubscriptionId,
            phr_integrationsystemid = model.IntegrationSystemId,
            phr_eventtypes = (int)model.EventTypes,
            phr_callbackurl = model.CallbackUrl,
            phr_secretkey = model.SecretKey,
            phr_isactive = model.IsActive,
            phr_description = model.Description,
            phr_failedattempts = model.FailedAttempts,
            phr_lastsuccessfuldelivery = model.LastSuccessfulDelivery,
            phr_lastfaileddelivery = model.LastFailedDelivery,
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate
        };
    }
}
