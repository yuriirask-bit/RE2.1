using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;

namespace RE2.DataAccess.D365FinanceOperations.Repositories;

/// <summary>
/// D365 Finance & Operations implementation of IAuditRepository.
/// T155: Repository for managing audit events in D365 F&O virtual data entity per FR-027.
/// Entity set: PharmaComplianceAuditEventEntity (per data-model.md entity 15).
/// </summary>
public class D365FoAuditRepository : IAuditRepository
{
    private readonly ID365FoClient _client;
    private readonly ILogger<D365FoAuditRepository> _logger;
    private const string EntitySetName = "PharmaComplianceAuditEventEntity";

    public D365FoAuditRepository(ID365FoClient client, ILogger<D365FoAuditRepository> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Core Operations

    public async Task<Guid> CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (auditEvent.EventId == Guid.Empty)
            {
                auditEvent.EventId = Guid.NewGuid();
            }

            var dto = AuditEventDto.FromDomainModel(auditEvent);
            await _client.CreateAsync(EntitySetName, dto, cancellationToken);

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
                // Continue with other events
            }
        }

        _logger.LogInformation("Created {Count} audit events in batch", createdIds.Count);
        return createdIds;
    }

    public async Task<AuditEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _client.GetByKeyAsync<AuditEventDto>(
                EntitySetName,
                $"'{eventId}'",
                cancellationToken);

            return dto?.ToDomainModel();
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
            var query = $"$filter=EntityType eq {(int)entityType} and EntityId eq {entityId}&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var query = $"$filter=EntityType eq {(int)entityType} and EntityId eq {entityId} and EventDate ge {fromStr} and EventDate le {toStr}&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var query = $"$filter=EntityType eq {(int)entityType} and EventDate ge {fromStr} and EventDate le {toStr}&$orderby=EventDate desc&$skip={skip}&$top={take}&$count=true";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            var items = response?.value.Select(d => d.ToDomainModel()).ToList() ?? new List<AuditEvent>();
            var totalCount = response?.odataCount ?? items.Count;

            return PagedResult<AuditEvent>.Create(items, totalCount, skip, take);
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var query = $"$filter=EventType eq {(int)eventType} and EventDate ge {fromStr} and EventDate le {toStr}&$orderby=EventDate desc&$skip={skip}&$top={take}&$count=true";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            var items = response?.value.Select(d => d.ToDomainModel()).ToList() ?? new List<AuditEvent>();
            var totalCount = response?.odataCount ?? items.Count;

            return PagedResult<AuditEvent>.Create(items, totalCount, skip, take);
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var query = $"$filter=PerformedBy eq {userId} and EventDate ge {fromStr} and EventDate le {toStr}&$orderby=EventDate desc&$skip={skip}&$top={take}&$count=true";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            var items = response?.value.Select(d => d.ToDomainModel()).ToList() ?? new List<AuditEvent>();
            var totalCount = response?.odataCount ?? items.Count;

            return PagedResult<AuditEvent>.Create(items, totalCount, skip, take);
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Transaction events, filtering by substance requires parsing Details JSON
            // Using contains for simple substring match - production may need advanced querying
            var query = $"$filter=EntityType eq {(int)AuditEntityType.Transaction} and EventDate ge {fromStr} and EventDate le {toStr} and contains(Details,'{substanceId}')&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Filter by customer ID in Details JSON
            var query = $"$filter=EntityType eq {(int)AuditEntityType.Transaction} and EventDate ge {fromStr} and EventDate le {toStr} and contains(Details,'{customerId}')&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var fromStr = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = toDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Filter by country code in Details JSON
            var query = $"$filter=EntityType eq {(int)AuditEntityType.Transaction} and EventDate ge {fromStr} and EventDate le {toStr} and contains(Details,'\"{countryCode}\"')&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var filters = new List<string>
            {
                $"EntityType eq {(int)AuditEntityType.Customer}",
                $"EntityId eq {customerId}"
            };

            if (fromDate.HasValue)
            {
                filters.Add($"EventDate ge {fromDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (toDate.HasValue)
            {
                filters.Add($"EventDate le {toDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            var query = $"$filter={string.Join(" and ", filters)}&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var query = $"$filter=EventType eq {(int)AuditEventType.ConflictResolved} and EntityType eq {(int)entityType} and EntityId eq {entityId}&$orderby=EventDate desc";
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<AuditEvent>();
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
            var filters = new List<string>();

            if (criteria.EntityType.HasValue)
            {
                filters.Add($"EntityType eq {(int)criteria.EntityType.Value}");
            }

            if (criteria.EntityId.HasValue)
            {
                filters.Add($"EntityId eq {criteria.EntityId.Value}");
            }

            if (criteria.EventType.HasValue)
            {
                filters.Add($"EventType eq {(int)criteria.EventType.Value}");
            }

            if (criteria.PerformedBy.HasValue)
            {
                filters.Add($"PerformedBy eq {criteria.PerformedBy.Value}");
            }

            if (criteria.FromDate.HasValue)
            {
                filters.Add($"EventDate ge {criteria.FromDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (criteria.ToDate.HasValue)
            {
                filters.Add($"EventDate le {criteria.ToDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (!string.IsNullOrEmpty(criteria.DetailsContains))
            {
                filters.Add($"contains(Details,'{criteria.DetailsContains}')");
            }

            if (!string.IsNullOrEmpty(criteria.CorrelationId))
            {
                filters.Add($"CorrelationId eq '{criteria.CorrelationId}'");
            }

            var queryParts = new List<string>();

            if (filters.Any())
            {
                queryParts.Add($"$filter={string.Join(" and ", filters)}");
            }

            queryParts.Add("$orderby=EventDate desc");
            queryParts.Add($"$skip={criteria.Skip}");
            queryParts.Add($"$top={criteria.Take}");
            queryParts.Add("$count=true");

            var query = string.Join("&", queryParts);
            var response = await _client.GetAsync<AuditEventODataResponse>(EntitySetName, query, cancellationToken);

            var items = response?.value.Select(d => d.ToDomainModel()).ToList() ?? new List<AuditEvent>();
            var totalCount = response?.odataCount ?? items.Count;

            return PagedResult<AuditEvent>.Create(items, totalCount, criteria.Skip, criteria.Take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit events with criteria");
            return PagedResult<AuditEvent>.Empty();
        }
    }

    #endregion
}
