using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IAuditRepository for local development and testing.
/// T156: Provides audit event storage without external dependencies.
/// </summary>
public class InMemoryAuditRepository : IAuditRepository
{
    private readonly List<AuditEvent> _events = new();
    private readonly object _lock = new();

    #region Core Operations

    public Task<Guid> CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent.EventId == Guid.Empty)
            auditEvent.EventId = Guid.NewGuid();

        lock (_lock)
        {
            _events.Add(auditEvent);
        }

        return Task.FromResult(auditEvent.EventId);
    }

    public Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();

        lock (_lock)
        {
            foreach (var auditEvent in auditEvents)
            {
                if (auditEvent.EventId == Guid.Empty)
                    auditEvent.EventId = Guid.NewGuid();

                _events.Add(auditEvent);
                ids.Add(auditEvent.EventId);
            }
        }

        return Task.FromResult<IEnumerable<Guid>>(ids);
    }

    public Task<AuditEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_events.FirstOrDefault(e => e.EventId == eventId));
        }
    }

    #endregion

    #region Entity-Based Queries

    public Task<IEnumerable<AuditEvent>> GetByEntityAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _events
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    public Task<IEnumerable<AuditEvent>> GetByEntityAndDateRangeAsync(
        AuditEntityType entityType,
        Guid entityId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _events
                .Where(e => e.EntityType == entityType &&
                           e.EntityId == entityId &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate)
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    public Task<PagedResult<AuditEvent>> GetByEntityTypeAsync(
        AuditEntityType entityType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events
                .Where(e => e.EntityType == entityType &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate)
                .OrderByDescending(e => e.EventDate);

            var totalCount = query.Count();
            var items = query.Skip(skip).Take(take).ToList();

            return Task.FromResult(PagedResult<AuditEvent>.Create(items, totalCount, skip, take));
        }
    }

    #endregion

    #region Event Type Queries

    public Task<PagedResult<AuditEvent>> GetByEventTypeAsync(
        AuditEventType eventType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events
                .Where(e => e.EventType == eventType &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate)
                .OrderByDescending(e => e.EventDate);

            var totalCount = query.Count();
            var items = query.Skip(skip).Take(take).ToList();

            return Task.FromResult(PagedResult<AuditEvent>.Create(items, totalCount, skip, take));
        }
    }

    #endregion

    #region User-Based Queries

    public Task<PagedResult<AuditEvent>> GetByPerformedByAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events
                .Where(e => e.PerformedBy == userId &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate)
                .OrderByDescending(e => e.EventDate);

            var totalCount = query.Count();
            var items = query.Skip(skip).Take(take).ToList();

            return Task.FromResult(PagedResult<AuditEvent>.Create(items, totalCount, skip, take));
        }
    }

    #endregion

    #region Transaction Audit Report Queries (FR-026)

    public Task<IEnumerable<AuditEvent>> GetTransactionEventsBySubstanceAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var substanceIdStr = substanceId.ToString();
            var result = _events
                .Where(e => e.EntityType == AuditEntityType.Transaction &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate &&
                           (e.Details?.Contains(substanceIdStr) ?? false))
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    public Task<IEnumerable<AuditEvent>> GetTransactionEventsByCustomerAsync(
        Guid customerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var customerIdStr = customerId.ToString();
            var result = _events
                .Where(e => e.EntityType == AuditEntityType.Transaction &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate &&
                           (e.Details?.Contains(customerIdStr) ?? false))
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    public Task<IEnumerable<AuditEvent>> GetTransactionEventsByCountryAsync(
        string countryCode,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _events
                .Where(e => e.EntityType == AuditEntityType.Transaction &&
                           e.EventDate >= fromDate &&
                           e.EventDate <= toDate &&
                           (e.Details?.Contains($"\"{countryCode}\"") ?? false))
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    #endregion

    #region Customer Compliance History (FR-029)

    public Task<IEnumerable<AuditEvent>> GetCustomerComplianceHistoryAsync(
        Guid customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events
                .Where(e => e.EntityType == AuditEntityType.Customer && e.EntityId == customerId);

            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.EventDate <= toDate.Value);

            var result = query.OrderByDescending(e => e.EventDate).ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    #endregion

    #region Conflict Resolution Auditing (FR-027c)

    public Task<IEnumerable<AuditEvent>> GetConflictResolutionEventsAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _events
                .Where(e => e.EventType == AuditEventType.ConflictResolved &&
                           e.EntityType == entityType &&
                           e.EntityId == entityId)
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Task.FromResult<IEnumerable<AuditEvent>>(result);
        }
    }

    #endregion

    #region Search and Filter

    public Task<PagedResult<AuditEvent>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events.AsEnumerable();

            if (criteria.EntityType.HasValue)
                query = query.Where(e => e.EntityType == criteria.EntityType.Value);

            if (criteria.EntityId.HasValue)
                query = query.Where(e => e.EntityId == criteria.EntityId.Value);

            if (criteria.EventType.HasValue)
                query = query.Where(e => e.EventType == criteria.EventType.Value);

            if (criteria.PerformedBy.HasValue)
                query = query.Where(e => e.PerformedBy == criteria.PerformedBy.Value);

            if (criteria.FromDate.HasValue)
                query = query.Where(e => e.EventDate >= criteria.FromDate.Value);

            if (criteria.ToDate.HasValue)
                query = query.Where(e => e.EventDate <= criteria.ToDate.Value);

            if (!string.IsNullOrEmpty(criteria.DetailsContains))
                query = query.Where(e => e.Details?.Contains(criteria.DetailsContains) ?? false);

            if (!string.IsNullOrEmpty(criteria.CorrelationId))
                query = query.Where(e => e.CorrelationId == criteria.CorrelationId);

            var orderedQuery = query.OrderByDescending(e => e.EventDate);
            var totalCount = orderedQuery.Count();
            var items = orderedQuery.Skip(criteria.Skip).Take(criteria.Take).ToList();

            return Task.FromResult(PagedResult<AuditEvent>.Create(items, totalCount, criteria.Skip, criteria.Take));
        }
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Clears all events (for testing).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }

    /// <summary>
    /// Gets all events (for testing).
    /// </summary>
    public IReadOnlyList<AuditEvent> GetAll()
    {
        lock (_lock)
        {
            return _events.ToList();
        }
    }

    #endregion
}
