using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for AuditEvent entity operations.
/// T154: Repository interface for audit trail storage and retrieval per FR-027.
/// Supports query operations for reporting (FR-026) and compliance history (FR-029).
/// </summary>
public interface IAuditRepository
{
    #region Core Operations

    /// <summary>
    /// Creates a new audit event.
    /// T154: Primary method for recording all data changes per FR-027.
    /// </summary>
    /// <param name="auditEvent">The audit event to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event's ID.</returns>
    Task<Guid> CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple audit events in a batch.
    /// T154: Bulk operation for efficient logging of related events.
    /// </summary>
    /// <param name="auditEvents">The audit events to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event IDs.</returns>
    Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an audit event by ID.
    /// T154: Retrieve specific event for detail view.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit event, or null if not found.</returns>
    Task<AuditEvent?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    #endregion

    #region Entity-Based Queries

    /// <summary>
    /// Gets all audit events for a specific entity.
    /// T154: View complete history of changes to an entity per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit events for the entity, ordered by date descending.</returns>
    Task<IEnumerable<AuditEvent>> GetByEntityAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit events for a specific entity within a date range.
    /// T154: View history for a specific time period per FR-027.
    /// </summary>
    /// <param name="entityType">Type of entity.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="fromDate">Start of date range (inclusive).</param>
    /// <param name="toDate">End of date range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit events for the entity in the date range.</returns>
    Task<IEnumerable<AuditEvent>> GetByEntityAndDateRangeAsync(
        AuditEntityType entityType,
        Guid entityId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit events for entities of a specific type.
    /// T154: Query all changes to a type of entity for reporting.
    /// </summary>
    /// <param name="entityType">Type of entity.</param>
    /// <param name="fromDate">Start of date range (inclusive).</param>
    /// <param name="toDate">End of date range (inclusive).</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Number of records to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged audit events for the entity type.</returns>
    Task<PagedResult<AuditEvent>> GetByEntityTypeAsync(
        AuditEntityType entityType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    #endregion

    #region Event Type Queries

    /// <summary>
    /// Gets audit events by event type within a date range.
    /// T154: Query specific types of events for analysis.
    /// </summary>
    /// <param name="eventType">Type of audit event.</param>
    /// <param name="fromDate">Start of date range (inclusive).</param>
    /// <param name="toDate">End of date range (inclusive).</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Number of records to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged audit events of the specified type.</returns>
    Task<PagedResult<AuditEvent>> GetByEventTypeAsync(
        AuditEventType eventType,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    #endregion

    #region User-Based Queries

    /// <summary>
    /// Gets audit events performed by a specific user.
    /// T154: View all actions by a user for user activity reporting.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="fromDate">Start of date range (inclusive).</param>
    /// <param name="toDate">End of date range (inclusive).</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Number of records to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged audit events performed by the user.</returns>
    Task<PagedResult<AuditEvent>> GetByPerformedByAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Audit Report Queries (FR-026)

    /// <summary>
    /// Gets transaction audit events for reporting by substance.
    /// T161: Transaction audit report generation by substance per FR-026.
    /// </summary>
    /// <param name="substanceId">The substance ID to filter by.</param>
    /// <param name="fromDate">Start of date range.</param>
    /// <param name="toDate">End of date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction events involving the substance.</returns>
    Task<IEnumerable<AuditEvent>> GetTransactionEventsBySubstanceAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction audit events for reporting by customer.
    /// T161: Transaction audit report generation by customer per FR-026.
    /// </summary>
    /// <param name="customerId">The customer ID to filter by.</param>
    /// <param name="fromDate">Start of date range.</param>
    /// <param name="toDate">End of date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction events for the customer.</returns>
    Task<IEnumerable<AuditEvent>> GetTransactionEventsByCustomerAsync(
        Guid customerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction audit events for reporting by country.
    /// T161: Transaction audit report generation by country per FR-026.
    /// </summary>
    /// <param name="countryCode">The ISO country code to filter by.</param>
    /// <param name="fromDate">Start of date range.</param>
    /// <param name="toDate">End of date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction events for the country.</returns>
    Task<IEnumerable<AuditEvent>> GetTransactionEventsByCountryAsync(
        string countryCode,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Customer Compliance History (FR-029)

    /// <summary>
    /// Gets complete compliance history for a customer.
    /// T163: Customer compliance history report per FR-029.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All compliance-related events for the customer.</returns>
    Task<IEnumerable<AuditEvent>> GetCustomerComplianceHistoryAsync(
        Guid customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Conflict Resolution Auditing (FR-027c)

    /// <summary>
    /// Gets conflict resolution events for an entity.
    /// T159: View conflict resolution history per FR-027c.
    /// </summary>
    /// <param name="entityType">Type of entity.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conflict resolution events for the entity.</returns>
    Task<IEnumerable<AuditEvent>> GetConflictResolutionEventsAsync(
        AuditEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Search and Filter

    /// <summary>
    /// Searches audit events with flexible criteria.
    /// T154: Advanced search for audit trail analysis.
    /// </summary>
    /// <param name="criteria">Search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged audit events matching the criteria.</returns>
    Task<PagedResult<AuditEvent>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Search criteria for audit event queries.
/// </summary>
public class AuditSearchCriteria
{
    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public AuditEntityType? EntityType { get; set; }

    /// <summary>
    /// Filter by specific entity ID.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Filter by event type.
    /// </summary>
    public AuditEventType? EventType { get; set; }

    /// <summary>
    /// Filter by user who performed the action.
    /// </summary>
    public Guid? PerformedBy { get; set; }

    /// <summary>
    /// Start of date range (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End of date range (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Free text search in details JSON.
    /// </summary>
    public string? DetailsContains { get; set; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Number of records to skip (for pagination).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of records to return (for pagination).
    /// </summary>
    public int Take { get; set; } = 100;
}

/// <summary>
/// Paged result for queries returning large datasets.
/// </summary>
/// <typeparam name="T">Type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Total count of items matching the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of items skipped.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Number of items requested.
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Whether there are more items after this page.
    /// </summary>
    public bool HasMore => Skip + Items.Count < TotalCount;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty() => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        Skip = 0,
        Take = 0
    };

    /// <summary>
    /// Creates a paged result from items and total count.
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int skip, int take) => new()
    {
        Items = items.ToList(),
        TotalCount = totalCount,
        Skip = skip,
        Take = take
    };
}
