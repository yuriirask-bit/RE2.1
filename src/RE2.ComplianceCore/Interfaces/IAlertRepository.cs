using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Alert entity operations.
/// T120: Repository for managing compliance alerts stored in D365 F&O.
/// </summary>
public interface IAlertRepository
{
    /// <summary>
    /// Gets an alert by ID.
    /// </summary>
    Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all alerts for a specific target entity.
    /// </summary>
    Task<IEnumerable<Alert>> GetByTargetEntityAsync(
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unacknowledged alerts.
    /// </summary>
    Task<IEnumerable<Alert>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all alerts by type.
    /// </summary>
    Task<IEnumerable<Alert>> GetByTypeAsync(AlertType alertType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all alerts by severity.
    /// </summary>
    Task<IEnumerable<Alert>> GetBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts with optional filtering.
    /// </summary>
    Task<IEnumerable<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertSeverity? severity = null,
        TargetEntityType? entityType = null,
        bool? isAcknowledged = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue alerts (past due date and unacknowledged).
    /// </summary>
    Task<IEnumerable<Alert>> GetOverdueAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new alert.
    /// </summary>
    Task<Guid> CreateAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple alerts in batch.
    /// T121: Batch creation for expiry monitor processing.
    /// </summary>
    Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<Alert> alerts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an alert.
    /// </summary>
    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task AcknowledgeAsync(Guid alertId, Guid acknowledgedBy, string? acknowledgerName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an alert.
    /// </summary>
    Task DeleteAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an alert already exists for the given criteria.
    /// Used to prevent duplicate alerts.
    /// </summary>
    Task<bool> ExistsAsync(
        AlertType alertType,
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
