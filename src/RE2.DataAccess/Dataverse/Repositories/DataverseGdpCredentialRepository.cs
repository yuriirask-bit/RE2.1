using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IGdpCredentialRepository.
/// T204: CRUD for GDP service providers, credentials, reviews, and verifications via IDataverseClient.
/// </summary>
public class DataverseGdpCredentialRepository : IGdpCredentialRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseGdpCredentialRepository> _logger;

    private const string ProviderEntityName = "phr_gdpserviceprovider";
    private const string CredentialEntityName = "phr_gdpcredential";
    private const string ReviewEntityName = "phr_qualificationreview";
    private const string VerificationEntityName = "phr_gdpcredentialverification";

    public DataverseGdpCredentialRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseGdpCredentialRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    #region GdpServiceProvider Operations

    public async Task<IEnumerable<GdpServiceProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ProviderEntityName)
            {
                ColumnSet = new ColumnSet(true)
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToProviderDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP service providers");
            return Enumerable.Empty<GdpServiceProvider>();
        }
    }

    public async Task<GdpServiceProvider?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(ProviderEntityName, providerId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToProviderDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP service provider {ProviderId}", providerId);
            return null;
        }
    }

    public async Task<Guid> CreateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        try
        {
            if (provider.ProviderId == Guid.Empty)
                provider.ProviderId = Guid.NewGuid();

            provider.CreatedDate = DateTime.UtcNow;
            provider.ModifiedDate = DateTime.UtcNow;

            var entity = MapProviderToEntity(provider);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP service provider {ProviderId} ({Name})", provider.ProviderId, provider.ProviderName);
            return provider.ProviderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP service provider {Name}", provider.ProviderName);
            throw;
        }
    }

    public async Task UpdateProviderAsync(GdpServiceProvider provider, CancellationToken cancellationToken = default)
    {
        try
        {
            provider.ModifiedDate = DateTime.UtcNow;
            var entity = MapProviderToEntity(provider);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP service provider {ProviderId}", provider.ProviderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP service provider {ProviderId}", provider.ProviderId);
            throw;
        }
    }

    public async Task DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(ProviderEntityName, providerId, cancellationToken);
            _logger.LogInformation("Deleted GDP service provider {ProviderId}", providerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP service provider {ProviderId}", providerId);
            throw;
        }
    }

    public async Task<IEnumerable<GdpServiceProvider>> GetProvidersRequiringReviewAsync(DateOnly beforeDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ProviderEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_nextreviewdate", ConditionOperator.OnOrBefore, beforeDate.ToDateTime(TimeOnly.MinValue))
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToProviderDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving providers requiring review before {BeforeDate}", beforeDate);
            return Enumerable.Empty<GdpServiceProvider>();
        }
    }

    #endregion

    #region GdpCredential Operations

    public async Task<IEnumerable<GdpCredential>> GetCredentialsByEntityAsync(GdpCredentialEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CredentialEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_entityid", ConditionOperator.Equal, entityId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCredentialDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for {EntityType} {EntityId}", entityType, entityId);
            return Enumerable.Empty<GdpCredential>();
        }
    }

    public async Task<GdpCredential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(CredentialEntityName, credentialId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToCredentialDto(entity).ToDomainModel() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDP credential {CredentialId}", credentialId);
            return null;
        }
    }

    public async Task<Guid> CreateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        try
        {
            if (credential.CredentialId == Guid.Empty)
                credential.CredentialId = Guid.NewGuid();

            credential.CreatedDate = DateTime.UtcNow;
            credential.ModifiedDate = DateTime.UtcNow;

            var entity = MapCredentialToEntity(credential);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created GDP credential {CredentialId} for {EntityType} {EntityId}", credential.CredentialId, credential.EntityType, credential.EntityId);
            return credential.CredentialId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDP credential for {EntityType} {EntityId}", credential.EntityType, credential.EntityId);
            throw;
        }
    }

    public async Task UpdateCredentialAsync(GdpCredential credential, CancellationToken cancellationToken = default)
    {
        try
        {
            credential.ModifiedDate = DateTime.UtcNow;
            var entity = MapCredentialToEntity(credential);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

            _logger.LogInformation("Updated GDP credential {CredentialId}", credential.CredentialId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GDP credential {CredentialId}", credential.CredentialId);
            throw;
        }
    }

    public async Task DeleteCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataverseClient.DeleteAsync(CredentialEntityName, credentialId, cancellationToken);
            _logger.LogInformation("Deleted GDP credential {CredentialId}", credentialId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDP credential {CredentialId}", credentialId);
            throw;
        }
    }

    public async Task<IEnumerable<GdpCredential>> GetCredentialsExpiringBeforeAsync(DateOnly beforeDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(CredentialEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_validityenddate", ConditionOperator.OnOrBefore, beforeDate.ToDateTime(TimeOnly.MinValue))
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToCredentialDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials expiring before {BeforeDate}", beforeDate);
            return Enumerable.Empty<GdpCredential>();
        }
    }

    #endregion

    #region QualificationReview Operations

    public async Task<IEnumerable<QualificationReview>> GetReviewsByEntityAsync(ReviewEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ReviewEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_entityid", ConditionOperator.Equal, entityId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToReviewDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for {EntityType} {EntityId}", entityType, entityId);
            return Enumerable.Empty<QualificationReview>();
        }
    }

    public async Task<Guid> CreateReviewAsync(QualificationReview review, CancellationToken cancellationToken = default)
    {
        try
        {
            if (review.ReviewId == Guid.Empty)
                review.ReviewId = Guid.NewGuid();

            var entity = MapReviewToEntity(review);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created qualification review {ReviewId} for {EntityType} {EntityId}", review.ReviewId, review.EntityType, review.EntityId);
            return review.ReviewId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating qualification review for {EntityType} {EntityId}", review.EntityType, review.EntityId);
            throw;
        }
    }

    #endregion

    #region GdpCredentialVerification Operations

    public async Task<IEnumerable<GdpCredentialVerification>> GetVerificationsByCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(VerificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_credentialid", ConditionOperator.Equal, credentialId)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapToVerificationDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verifications for credential {CredentialId}", credentialId);
            return Enumerable.Empty<GdpCredentialVerification>();
        }
    }

    public async Task<Guid> CreateVerificationAsync(GdpCredentialVerification verification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (verification.VerificationId == Guid.Empty)
                verification.VerificationId = Guid.NewGuid();

            var entity = MapVerificationToEntity(verification);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created credential verification {VerificationId} for credential {CredentialId}", verification.VerificationId, verification.CredentialId);
            return verification.VerificationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating verification for credential {CredentialId}", verification.CredentialId);
            throw;
        }
    }

    #endregion

    #region Mapping Helpers

    private static GdpServiceProviderDto MapToProviderDto(Entity entity)
    {
        return new GdpServiceProviderDto
        {
            phr_gdpserviceproviderid = entity.Id,
            phr_providername = entity.GetAttributeValue<string>("phr_providername"),
            phr_servicetype = entity.GetAttributeValue<int>("phr_servicetype"),
            phr_temperaturecontrolledcapability = entity.GetAttributeValue<bool>("phr_temperaturecontrolledcapability"),
            phr_approvedroutes = entity.GetAttributeValue<string>("phr_approvedroutes"),
            phr_qualificationstatus = entity.GetAttributeValue<int>("phr_qualificationstatus"),
            phr_reviewfrequencymonths = entity.GetAttributeValue<int>("phr_reviewfrequencymonths"),
            phr_lastreviewdate = entity.Contains("phr_lastreviewdate") ? entity.GetAttributeValue<DateTime?>("phr_lastreviewdate") : null,
            phr_nextreviewdate = entity.Contains("phr_nextreviewdate") ? entity.GetAttributeValue<DateTime?>("phr_nextreviewdate") : null,
            phr_isactive = entity.GetAttributeValue<bool>("phr_isactive"),
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon")
        };
    }

    private static Entity MapProviderToEntity(GdpServiceProvider provider)
    {
        var entity = new Entity(ProviderEntityName, provider.ProviderId);
        entity["phr_providername"] = provider.ProviderName;
        entity["phr_servicetype"] = (int)provider.ServiceType;
        entity["phr_temperaturecontrolledcapability"] = provider.TemperatureControlledCapability;
        entity["phr_approvedroutes"] = provider.ApprovedRoutes;
        entity["phr_qualificationstatus"] = (int)provider.QualificationStatus;
        entity["phr_reviewfrequencymonths"] = provider.ReviewFrequencyMonths;
        if (provider.LastReviewDate.HasValue)
            entity["phr_lastreviewdate"] = provider.LastReviewDate.Value.ToDateTime(TimeOnly.MinValue);
        if (provider.NextReviewDate.HasValue)
            entity["phr_nextreviewdate"] = provider.NextReviewDate.Value.ToDateTime(TimeOnly.MinValue);
        entity["phr_isactive"] = provider.IsActive;
        return entity;
    }

    private static GdpCredentialDto MapToCredentialDto(Entity entity)
    {
        return new GdpCredentialDto
        {
            phr_gdpcredentialid = entity.Id,
            phr_entitytype = entity.GetAttributeValue<int>("phr_entitytype"),
            phr_entityid = entity.GetAttributeValue<Guid>("phr_entityid"),
            phr_wdanumber = entity.GetAttributeValue<string>("phr_wdanumber"),
            phr_gdpcertificatenumber = entity.GetAttributeValue<string>("phr_gdpcertificatenumber"),
            phr_eudragmdpentryurl = entity.GetAttributeValue<string>("phr_eudragmdpentryurl"),
            phr_validitystartdate = entity.Contains("phr_validitystartdate") ? entity.GetAttributeValue<DateTime?>("phr_validitystartdate") : null,
            phr_validityenddate = entity.Contains("phr_validityenddate") ? entity.GetAttributeValue<DateTime?>("phr_validityenddate") : null,
            phr_qualificationstatus = entity.GetAttributeValue<int>("phr_qualificationstatus"),
            phr_lastverificationdate = entity.Contains("phr_lastverificationdate") ? entity.GetAttributeValue<DateTime?>("phr_lastverificationdate") : null,
            phr_nextreviewdate = entity.Contains("phr_nextreviewdate") ? entity.GetAttributeValue<DateTime?>("phr_nextreviewdate") : null,
            createdon = entity.GetAttributeValue<DateTime?>("createdon"),
            modifiedon = entity.GetAttributeValue<DateTime?>("modifiedon"),
            versionnumber = entity.Contains("versionnumber") ? entity.GetAttributeValue<byte[]>("versionnumber") : null
        };
    }

    private static Entity MapCredentialToEntity(GdpCredential credential)
    {
        var entity = new Entity(CredentialEntityName, credential.CredentialId);
        entity["phr_entitytype"] = (int)credential.EntityType;
        entity["phr_entityid"] = credential.EntityId;
        entity["phr_wdanumber"] = credential.WdaNumber;
        entity["phr_gdpcertificatenumber"] = credential.GdpCertificateNumber;
        entity["phr_eudragmdpentryurl"] = credential.EudraGmdpEntryUrl;
        if (credential.ValidityStartDate.HasValue)
            entity["phr_validitystartdate"] = credential.ValidityStartDate.Value.ToDateTime(TimeOnly.MinValue);
        if (credential.ValidityEndDate.HasValue)
            entity["phr_validityenddate"] = credential.ValidityEndDate.Value.ToDateTime(TimeOnly.MinValue);
        entity["phr_qualificationstatus"] = (int)credential.QualificationStatus;
        if (credential.LastVerificationDate.HasValue)
            entity["phr_lastverificationdate"] = credential.LastVerificationDate.Value.ToDateTime(TimeOnly.MinValue);
        if (credential.NextReviewDate.HasValue)
            entity["phr_nextreviewdate"] = credential.NextReviewDate.Value.ToDateTime(TimeOnly.MinValue);
        return entity;
    }

    private static QualificationReviewDto MapToReviewDto(Entity entity)
    {
        return new QualificationReviewDto
        {
            phr_qualificationreviewid = entity.Id,
            phr_entitytype = entity.GetAttributeValue<int>("phr_entitytype"),
            phr_entityid = entity.GetAttributeValue<Guid>("phr_entityid"),
            phr_reviewdate = entity.Contains("phr_reviewdate") ? entity.GetAttributeValue<DateTime?>("phr_reviewdate") : null,
            phr_reviewmethod = entity.GetAttributeValue<int>("phr_reviewmethod"),
            phr_reviewoutcome = entity.GetAttributeValue<int>("phr_reviewoutcome"),
            phr_reviewername = entity.GetAttributeValue<string>("phr_reviewername"),
            phr_notes = entity.GetAttributeValue<string>("phr_notes"),
            phr_nextreviewdate = entity.Contains("phr_nextreviewdate") ? entity.GetAttributeValue<DateTime?>("phr_nextreviewdate") : null
        };
    }

    private static Entity MapReviewToEntity(QualificationReview review)
    {
        var entity = new Entity(ReviewEntityName, review.ReviewId);
        entity["phr_entitytype"] = (int)review.EntityType;
        entity["phr_entityid"] = review.EntityId;
        entity["phr_reviewdate"] = review.ReviewDate.ToDateTime(TimeOnly.MinValue);
        entity["phr_reviewmethod"] = (int)review.ReviewMethod;
        entity["phr_reviewoutcome"] = (int)review.ReviewOutcome;
        entity["phr_reviewername"] = review.ReviewerName;
        entity["phr_notes"] = review.Notes;
        if (review.NextReviewDate.HasValue)
            entity["phr_nextreviewdate"] = review.NextReviewDate.Value.ToDateTime(TimeOnly.MinValue);
        return entity;
    }

    private static GdpCredentialVerificationDto MapToVerificationDto(Entity entity)
    {
        return new GdpCredentialVerificationDto
        {
            phr_gdpcredentialverificationid = entity.Id,
            phr_credentialid = entity.GetAttributeValue<Guid>("phr_credentialid"),
            phr_verificationdate = entity.Contains("phr_verificationdate") ? entity.GetAttributeValue<DateTime?>("phr_verificationdate") : null,
            phr_verificationmethod = entity.GetAttributeValue<int>("phr_verificationmethod"),
            phr_verifiedby = entity.GetAttributeValue<string>("phr_verifiedby"),
            phr_outcome = entity.GetAttributeValue<int>("phr_outcome"),
            phr_notes = entity.GetAttributeValue<string>("phr_notes")
        };
    }

    private static Entity MapVerificationToEntity(GdpCredentialVerification verification)
    {
        var entity = new Entity(VerificationEntityName, verification.VerificationId);
        entity["phr_credentialid"] = verification.CredentialId;
        entity["phr_verificationdate"] = verification.VerificationDate.ToDateTime(TimeOnly.MinValue);
        entity["phr_verificationmethod"] = (int)verification.VerificationMethod;
        entity["phr_verifiedby"] = verification.VerifiedBy;
        entity["phr_outcome"] = (int)verification.Outcome;
        entity["phr_notes"] = verification.Notes;
        return entity;
    }

    #endregion
}
