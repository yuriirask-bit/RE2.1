using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ISubstanceReclassificationRepository.
/// T080e: Repository for substance reclassification per FR-066.
/// </summary>
public class DataverseSubstanceReclassificationRepository : ISubstanceReclassificationRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseSubstanceReclassificationRepository> _logger;

    private const string ReclassificationEntityName = "phr_substancereclassification";
    private const string ImpactEntityName = "phr_reclassificationcustomerimpact";

    public DataverseSubstanceReclassificationRepository(
        IDataverseClient client,
        ILogger<DataverseSubstanceReclassificationRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SubstanceReclassification?> GetByIdAsync(Guid reclassificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(ReclassificationEntityName, reclassificationId, new ColumnSet(true), cancellationToken);
            return MapReclassificationToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reclassification {Id}", reclassificationId);
            return null;
        }
    }

    public async Task<IEnumerable<SubstanceReclassification>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ReclassificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_substancecode", ConditionOperator.Equal, substanceCode)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Descending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapReclassificationToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reclassifications for substance {SubstanceCode}", substanceCode);
            return Enumerable.Empty<SubstanceReclassification>();
        }
    }

    public async Task<IEnumerable<SubstanceReclassification>> GetByStatusAsync(ReclassificationStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ReclassificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_status", ConditionOperator.Equal, (int)status)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Descending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapReclassificationToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reclassifications with status {Status}", status);
            return Enumerable.Empty<SubstanceReclassification>();
        }
    }

    public async Task<IEnumerable<SubstanceReclassification>> GetByEffectiveDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
            var toDateTime = to.ToDateTime(TimeOnly.MaxValue);

            var query = new QueryExpression(ReclassificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_effectivedate", ConditionOperator.GreaterEqual, fromDateTime),
                        new ConditionExpression("phr_effectivedate", ConditionOperator.LessEqual, toDateTime)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Ascending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapReclassificationToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reclassifications for date range {From} to {To}", from, to);
            return Enumerable.Empty<SubstanceReclassification>();
        }
    }

    public async Task<IEnumerable<SubstanceReclassification>> GetPendingEffectiveReclassificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow;

            var query = new QueryExpression(ReclassificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_status", ConditionOperator.Equal, (int)ReclassificationStatus.Pending),
                        new ConditionExpression("phr_effectivedate", ConditionOperator.LessEqual, today)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Ascending) }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapReclassificationToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending effective reclassifications");
            return Enumerable.Empty<SubstanceReclassification>();
        }
    }

    public async Task<SubstanceReclassification?> GetEffectiveReclassificationAsync(string substanceCode, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var dateTime = asOfDate.ToDateTime(TimeOnly.MaxValue);

            var query = new QueryExpression(ReclassificationEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_substancecode", ConditionOperator.Equal, substanceCode),
                        new ConditionExpression("phr_effectivedate", ConditionOperator.LessEqual, dateTime),
                        new ConditionExpression("phr_status", ConditionOperator.Equal, (int)ReclassificationStatus.Completed)
                    }
                },
                Orders = { new OrderExpression("phr_effectivedate", OrderType.Descending) },
                TopCount = 1
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null
                ? MapReclassificationToDto(result.Entities.First()).ToDomainModel()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving effective reclassification for substance {SubstanceCode} as of {AsOfDate}", substanceCode, asOfDate);
            return null;
        }
    }

    public async Task<Guid> CreateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default)
    {
        reclassification.ReclassificationId = Guid.NewGuid();
        reclassification.CreatedDate = DateTime.UtcNow;
        reclassification.ModifiedDate = DateTime.UtcNow;

        var dto = SubstanceReclassificationDto.FromDomainModel(reclassification);
        var entity = MapReclassificationToEntity(dto);

        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created reclassification {Id} for substance {SubstanceCode}", id, reclassification.SubstanceCode);
        return id;
    }

    public async Task UpdateAsync(SubstanceReclassification reclassification, CancellationToken cancellationToken = default)
    {
        reclassification.ModifiedDate = DateTime.UtcNow;

        var dto = SubstanceReclassificationDto.FromDomainModel(reclassification);
        var entity = MapReclassificationToEntity(dto);

        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated reclassification {Id}", reclassification.ReclassificationId);
    }

    public async Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomerImpactsAsync(Guid reclassificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ImpactEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_reclassificationid", ConditionOperator.Equal, reclassificationId)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapImpactToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer impacts for reclassification {ReclassificationId}", reclassificationId);
            return Enumerable.Empty<ReclassificationCustomerImpact>();
        }
    }

    public async Task<ReclassificationCustomerImpact?> GetCustomerImpactAsync(Guid reclassificationId, Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ImpactEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_reclassificationid", ConditionOperator.Equal, reclassificationId),
                        new ConditionExpression("phr_customerid", ConditionOperator.Equal, customerId)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.FirstOrDefault() != null
                ? MapImpactToDto(result.Entities.First()).ToDomainModel()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer impact for reclassification {ReclassificationId} and customer {CustomerId}", reclassificationId, customerId);
            return null;
        }
    }

    public async Task<IEnumerable<ReclassificationCustomerImpact>> GetCustomersRequiringReQualificationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(ImpactEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_requiresrequalification", ConditionOperator.Equal, true),
                        new ConditionExpression("phr_requalificationdate", ConditionOperator.Null)
                    }
                }
            };

            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(e => MapImpactToDto(e).ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers requiring re-qualification");
            return Enumerable.Empty<ReclassificationCustomerImpact>();
        }
    }

    public async Task<Guid> CreateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default)
    {
        impact.ImpactId = Guid.NewGuid();
        impact.CreatedDate = DateTime.UtcNow;

        var dto = ReclassificationCustomerImpactDto.FromDomainModel(impact);
        var entity = MapImpactToEntity(dto);

        var id = await _client.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Created customer impact {Id} for reclassification {ReclassificationId}", id, impact.ReclassificationId);
        return id;
    }

    public async Task UpdateCustomerImpactAsync(ReclassificationCustomerImpact impact, CancellationToken cancellationToken = default)
    {
        var dto = ReclassificationCustomerImpactDto.FromDomainModel(impact);
        var entity = MapImpactToEntity(dto);

        await _client.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated customer impact {Id}", impact.ImpactId);
    }

    public async Task CreateCustomerImpactsBatchAsync(IEnumerable<ReclassificationCustomerImpact> impacts, CancellationToken cancellationToken = default)
    {
        var impactList = impacts.ToList();
        _logger.LogInformation("Creating {Count} customer impacts in batch", impactList.Count);

        foreach (var impact in impactList)
        {
            await CreateCustomerImpactAsync(impact, cancellationToken);
        }
    }

    #region Entity Mapping

    private SubstanceReclassificationDto MapReclassificationToDto(Entity entity)
    {
        return new SubstanceReclassificationDto
        {
            phr_substancereclassificationid = entity.Id,
            phr_substancecode = entity.GetAttributeValue<string>("phr_substancecode") ?? string.Empty,
            phr_previousopiumactlist = entity.GetAttributeValue<int?>("phr_previousopiumactlist"),
            phr_newopiumactlist = entity.GetAttributeValue<int?>("phr_newopiumactlist"),
            phr_previousprecursorcategory = entity.GetAttributeValue<int?>("phr_previousprecursorcategory"),
            phr_newprecursorcategory = entity.GetAttributeValue<int?>("phr_newprecursorcategory"),
            phr_effectivedate = entity.GetAttributeValue<DateTime?>("phr_effectivedate"),
            phr_regulatoryreference = entity.GetAttributeValue<string>("phr_regulatoryreference"),
            phr_regulatoryauthority = entity.GetAttributeValue<string>("phr_regulatoryauthority"),
            phr_reason = entity.GetAttributeValue<string>("phr_reason"),
            phr_status = entity.GetAttributeValue<int>("phr_status"),
            phr_affectedcustomercount = entity.GetAttributeValue<int>("phr_affectedcustomercount"),
            phr_flaggedcustomercount = entity.GetAttributeValue<int>("phr_flaggedcustomercount"),
            phr_initiatedbyuserid = entity.GetAttributeValue<Guid?>("phr_initiatedbyuserid"),
            phr_createdon = entity.GetAttributeValue<DateTime>("phr_createdon"),
            phr_processeddate = entity.GetAttributeValue<DateTime?>("phr_processeddate"),
            phr_modifiedon = entity.GetAttributeValue<DateTime>("phr_modifiedon")
        };
    }

    private Entity MapReclassificationToEntity(SubstanceReclassificationDto dto)
    {
        var entity = new Entity(ReclassificationEntityName) { Id = dto.phr_substancereclassificationid };
        entity["phr_substancecode"] = dto.phr_substancecode;
        entity["phr_previousopiumactlist"] = dto.phr_previousopiumactlist;
        entity["phr_newopiumactlist"] = dto.phr_newopiumactlist;
        entity["phr_previousprecursorcategory"] = dto.phr_previousprecursorcategory;
        entity["phr_newprecursorcategory"] = dto.phr_newprecursorcategory;
        entity["phr_effectivedate"] = dto.phr_effectivedate;
        entity["phr_regulatoryreference"] = dto.phr_regulatoryreference;
        entity["phr_regulatoryauthority"] = dto.phr_regulatoryauthority;
        entity["phr_reason"] = dto.phr_reason;
        entity["phr_status"] = dto.phr_status;
        entity["phr_affectedcustomercount"] = dto.phr_affectedcustomercount;
        entity["phr_flaggedcustomercount"] = dto.phr_flaggedcustomercount;
        entity["phr_initiatedbyuserid"] = dto.phr_initiatedbyuserid;
        entity["phr_createdon"] = dto.phr_createdon;
        entity["phr_processeddate"] = dto.phr_processeddate;
        entity["phr_modifiedon"] = dto.phr_modifiedon;
        return entity;
    }

    private ReclassificationCustomerImpactDto MapImpactToDto(Entity entity)
    {
        return new ReclassificationCustomerImpactDto
        {
            phr_impactid = entity.Id,
            phr_reclassificationid = entity.GetAttributeValue<Guid>("phr_reclassificationid"),
            phr_customerid = entity.GetAttributeValue<Guid>("phr_customerid"),
            phr_customername = entity.GetAttributeValue<string>("phr_customername"),
            phr_hassufficientlicence = entity.GetAttributeValue<bool>("phr_hassufficientlicence"),
            phr_requiresrequalification = entity.GetAttributeValue<bool>("phr_requiresrequalification"),
            phr_relevantlicenceids = entity.GetAttributeValue<string>("phr_relevantlicenceids"),
            phr_licencegapsummary = entity.GetAttributeValue<string>("phr_licencegapsummary"),
            phr_notificationsent = entity.GetAttributeValue<bool>("phr_notificationsent"),
            phr_notificationdate = entity.GetAttributeValue<DateTime?>("phr_notificationdate"),
            phr_requalificationdate = entity.GetAttributeValue<DateTime?>("phr_requalificationdate"),
            phr_createdon = entity.GetAttributeValue<DateTime>("phr_createdon")
        };
    }

    private Entity MapImpactToEntity(ReclassificationCustomerImpactDto dto)
    {
        var entity = new Entity(ImpactEntityName) { Id = dto.phr_impactid };
        entity["phr_reclassificationid"] = dto.phr_reclassificationid;
        entity["phr_customerid"] = dto.phr_customerid;
        entity["phr_customername"] = dto.phr_customername;
        entity["phr_hassufficientlicence"] = dto.phr_hassufficientlicence;
        entity["phr_requiresrequalification"] = dto.phr_requiresrequalification;
        entity["phr_relevantlicenceids"] = dto.phr_relevantlicenceids;
        entity["phr_licencegapsummary"] = dto.phr_licencegapsummary;
        entity["phr_notificationsent"] = dto.phr_notificationsent;
        entity["phr_notificationdate"] = dto.phr_notificationdate;
        entity["phr_requalificationdate"] = dto.phr_requalificationdate;
        entity["phr_createdon"] = dto.phr_createdon;
        return entity;
    }

    #endregion
}
