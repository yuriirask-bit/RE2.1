using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IAuditRepository.
/// Entity: phr_auditevent — migrated from D365 F&amp;O (PharmaComplianceAuditEventEntity).
/// Append-only: all writes use CreateAsync, no update/delete.
/// </summary>
public class DataverseAuditRepository : IAuditRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseAuditRepository> _logger;

    private const string AuditEntityName = "phr_auditevent";

    public DataverseAuditRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseAuditRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    #region Core Operations

    public async Task<Guid> CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (auditEvent.EventId == Guid.Empty)
                auditEvent.EventId = Guid.NewGuid();

            var entity = MapToEntity(auditEvent);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogDebug("Created audit event {EventId} type {EventType} for entity {EntityType} {EntityId}",
                auditEvent.EventId, auditEvent.EventType, auditEvent.EntityType, auditEvent.EntityId);

            return auditEvent.EventId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit event type {EventType} for entity {EntityType} {EntityId}",
                auditEvent.EventType, auditEvent.EntityType, auditEvent.EntityId);
            throw;
        }
    }

    public async Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        var createdIds = new List<Guid>();

        foreach (var auditEvent in auditEvents)
        {
            try
            {
                var id = await CreateAsync(auditEvent, cancellationToken);
                createdIds.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create audit event for {EntityType} {EntityId}, continuing batch",
                    auditEvent.EntityType, auditEvent.EntityId);
            }
        }

        _logger.LogInformation("Created {Count} audit events in batch", createdIds.Count);
        return createdIds;
    }

    public async Task<AuditEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(AuditEntityName, eventId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToAuditEvent(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit event {EventId}", eventId);
            return null;
        }
    }

    #endregion

    #region Entity-Based Queries

    public async Task<IEnumerable<AuditEvent>> GetByEntityAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_entityid", ConditionOperator.Equal, entityId)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for entity type {EntityType} ID {EntityId}",
                entityType, entityId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetByEntityAndDateRangeAsync(
        AuditEntityType entityType,
        Guid entityId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_entityid", ConditionOperator.Equal, entityId),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for entity {EntityType} {EntityId} in date range",
                entityType, entityId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    public async Task<PagedResult<AuditEvent>> GetByEntityTypeAsync(
        AuditEntityType entityType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var allItems = result.Entities.Select(MapToAuditEvent).ToList();
            var paged = allItems.Skip(skip).Take(take).ToList();

            return PagedResult<AuditEvent>.Create(paged, allItems.Count, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for entity type {EntityType}", entityType);
            return PagedResult<AuditEvent>.Empty();
        }
    }

    #endregion

    #region Event Type Queries

    public async Task<PagedResult<AuditEvent>> GetByEventTypeAsync(
        AuditEventType eventType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_eventtype", ConditionOperator.Equal, (int)eventType),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var allItems = result.Entities.Select(MapToAuditEvent).ToList();
            var paged = allItems.Skip(skip).Take(take).ToList();

            return PagedResult<AuditEvent>.Create(paged, allItems.Count, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events of type {EventType}", eventType);
            return PagedResult<AuditEvent>.Empty();
        }
    }

    #endregion

    #region User-Based Queries

    public async Task<PagedResult<AuditEvent>> GetByPerformedByAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_performedby", ConditionOperator.Equal, userId),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var allItems = result.Entities.Select(MapToAuditEvent).ToList();
            var paged = allItems.Skip(skip).Take(take).ToList();

            return PagedResult<AuditEvent>.Create(paged, allItems.Count, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events performed by {UserId}", userId);
            return PagedResult<AuditEvent>.Empty();
        }
    }

    #endregion

    #region Transaction Audit Report Queries (FR-026)

    public async Task<IEnumerable<AuditEvent>> GetTransactionEventsBySubstanceAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_eventtype", ConditionOperator.Equal, (int)AuditEventType.TransactionValidated),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate),
                        new ConditionExpression("phr_details", ConditionOperator.Contains, substanceId.ToString())
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction events for substance {SubstanceId}", substanceId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetTransactionEventsByCustomerAsync(
        Guid customerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_eventtype", ConditionOperator.Equal, (int)AuditEventType.TransactionValidated),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate),
                        new ConditionExpression("phr_details", ConditionOperator.Contains, customerId.ToString())
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction events for customer {CustomerId}", customerId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetTransactionEventsByCountryAsync(
        string countryCode,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_eventtype", ConditionOperator.Equal, (int)AuditEventType.TransactionValidated),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrAfter, fromDate),
                        new ConditionExpression("phr_eventdate", ConditionOperator.OnOrBefore, toDate),
                        new ConditionExpression("phr_details", ConditionOperator.Contains, countryCode)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction events for country {CountryCode}", countryCode);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    #endregion

    #region Customer Compliance History (FR-029)

    public async Task<IEnumerable<AuditEvent>> GetCustomerComplianceHistoryAsync(
        Guid customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition("phr_entitytype", ConditionOperator.Equal, (int)AuditEntityType.Customer);
            filter.AddCondition("phr_entityid", ConditionOperator.Equal, customerId);

            if (fromDate.HasValue)
                filter.AddCondition("phr_eventdate", ConditionOperator.OnOrAfter, fromDate.Value);

            if (toDate.HasValue)
                filter.AddCondition("phr_eventdate", ConditionOperator.OnOrBefore, toDate.Value);

            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = filter,
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compliance history for customer {CustomerId}", customerId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    #endregion

    #region Conflict Resolution Auditing (FR-027c)

    public async Task<IEnumerable<AuditEvent>> GetConflictResolutionEventsAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_eventtype", ConditionOperator.Equal, (int)AuditEventType.ConflictResolved),
                        new ConditionExpression("phr_entitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_entityid", ConditionOperator.Equal, entityId)
                    }
                },
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAuditEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conflict resolution events for {EntityType} {EntityId}",
                entityType, entityId);
            return Enumerable.Empty<AuditEvent>();
        }
    }

    #endregion

    #region Search and Filter

    public async Task<PagedResult<AuditEvent>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new FilterExpression(LogicalOperator.And);

            if (criteria.EntityType.HasValue)
                filter.AddCondition("phr_entitytype", ConditionOperator.Equal, (int)criteria.EntityType.Value);

            if (criteria.EntityId.HasValue)
                filter.AddCondition("phr_entityid", ConditionOperator.Equal, criteria.EntityId.Value);

            if (criteria.EventType.HasValue)
                filter.AddCondition("phr_eventtype", ConditionOperator.Equal, (int)criteria.EventType.Value);

            if (criteria.PerformedBy.HasValue)
                filter.AddCondition("phr_performedby", ConditionOperator.Equal, criteria.PerformedBy.Value);

            if (criteria.FromDate.HasValue)
                filter.AddCondition("phr_eventdate", ConditionOperator.OnOrAfter, criteria.FromDate.Value);

            if (criteria.ToDate.HasValue)
                filter.AddCondition("phr_eventdate", ConditionOperator.OnOrBefore, criteria.ToDate.Value);

            if (!string.IsNullOrEmpty(criteria.DetailsContains))
                filter.AddCondition("phr_details", ConditionOperator.Contains, criteria.DetailsContains);

            if (!string.IsNullOrEmpty(criteria.CorrelationId))
                filter.AddCondition("phr_correlationid", ConditionOperator.Equal, criteria.CorrelationId);

            var query = new QueryExpression(AuditEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = filter,
                Orders = { new OrderExpression("phr_eventdate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            var allItems = result.Entities.Select(MapToAuditEvent).ToList();
            var paged = allItems.Skip(criteria.Skip).Take(criteria.Take).ToList();

            return PagedResult<AuditEvent>.Create(paged, allItems.Count, criteria.Skip, criteria.Take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit events with criteria");
            return PagedResult<AuditEvent>.Empty();
        }
    }

    #endregion

    #region Mapping Helpers

    private static AuditEvent MapToAuditEvent(Entity entity)
    {
        return new AuditEvent
        {
            EventId = entity.Id,
            EventType = (AuditEventType)entity.GetAttributeValue<int>("phr_eventtype"),
            EventDate = entity.GetAttributeValue<DateTime>("phr_eventdate"),
            PerformedBy = entity.GetAttributeValue<Guid>("phr_performedby"),
            PerformedByName = entity.GetAttributeValue<string>("phr_performedbyname"),
            EntityType = (AuditEntityType)entity.GetAttributeValue<int>("phr_entitytype"),
            EntityId = entity.GetAttributeValue<Guid>("phr_entityid"),
            Details = entity.GetAttributeValue<string>("phr_details"),
            SupportingEvidenceUrl = entity.GetAttributeValue<string>("phr_supportingevidenceurl"),
            ClientIpAddress = entity.GetAttributeValue<string>("phr_clientipaddress"),
            UserAgent = entity.GetAttributeValue<string>("phr_useragent"),
            CorrelationId = entity.GetAttributeValue<string>("phr_correlationid")
        };
    }

    private static Entity MapToEntity(AuditEvent auditEvent)
    {
        var entity = new Entity(AuditEntityName, auditEvent.EventId);
        entity["phr_eventtype"] = (int)auditEvent.EventType;
        entity["phr_eventdate"] = auditEvent.EventDate;
        entity["phr_performedby"] = auditEvent.PerformedBy;
        entity["phr_entitytype"] = (int)auditEvent.EntityType;
        entity["phr_entityid"] = auditEvent.EntityId;

        if (auditEvent.PerformedByName != null)
            entity["phr_performedbyname"] = auditEvent.PerformedByName;

        if (auditEvent.Details != null)
            entity["phr_details"] = auditEvent.Details;

        if (auditEvent.SupportingEvidenceUrl != null)
            entity["phr_supportingevidenceurl"] = auditEvent.SupportingEvidenceUrl;

        if (auditEvent.ClientIpAddress != null)
            entity["phr_clientipaddress"] = auditEvent.ClientIpAddress;

        if (auditEvent.UserAgent != null)
            entity["phr_useragent"] = auditEvent.UserAgent;

        if (auditEvent.CorrelationId != null)
            entity["phr_correlationid"] = auditEvent.CorrelationId;

        return entity;
    }

    #endregion
}
