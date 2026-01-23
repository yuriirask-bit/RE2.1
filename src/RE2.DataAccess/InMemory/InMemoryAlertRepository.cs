using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IAlertRepository for local development and testing.
/// </summary>
public class InMemoryAlertRepository : IAlertRepository
{
    private readonly ConcurrentDictionary<Guid, Alert> _alerts = new();

    public Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        _alerts.TryGetValue(alertId, out var alert);
        return Task.FromResult(alert);
    }

    public Task<IEnumerable<Alert>> GetByTargetEntityAsync(
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var alerts = _alerts.Values
            .Where(a => a.TargetEntityType == entityType && a.TargetEntityId == entityId)
            .ToList();
        return Task.FromResult<IEnumerable<Alert>>(alerts);
    }

    public Task<IEnumerable<Alert>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        var alerts = _alerts.Values
            .Where(a => !a.IsAcknowledged())
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.GeneratedDate)
            .ToList();
        return Task.FromResult<IEnumerable<Alert>>(alerts);
    }

    public Task<IEnumerable<Alert>> GetByTypeAsync(AlertType alertType, CancellationToken cancellationToken = default)
    {
        var alerts = _alerts.Values.Where(a => a.AlertType == alertType).ToList();
        return Task.FromResult<IEnumerable<Alert>>(alerts);
    }

    public Task<IEnumerable<Alert>> GetBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        var alerts = _alerts.Values.Where(a => a.Severity == severity).ToList();
        return Task.FromResult<IEnumerable<Alert>>(alerts);
    }

    public Task<IEnumerable<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertSeverity? severity = null,
        TargetEntityType? entityType = null,
        bool? isAcknowledged = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        var query = _alerts.Values.AsEnumerable();

        if (type.HasValue)
            query = query.Where(a => a.AlertType == type.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (entityType.HasValue)
            query = query.Where(a => a.TargetEntityType == entityType.Value);

        if (isAcknowledged.HasValue)
            query = query.Where(a => a.IsAcknowledged() == isAcknowledged.Value);

        query = query.OrderByDescending(a => a.Severity).ThenByDescending(a => a.GeneratedDate);

        if (maxResults.HasValue)
            query = query.Take(maxResults.Value);

        return Task.FromResult<IEnumerable<Alert>>(query.ToList());
    }

    public Task<IEnumerable<Alert>> GetOverdueAlertsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var alerts = _alerts.Values
            .Where(a => !a.IsAcknowledged() && a.DueDate.HasValue && a.DueDate.Value < today)
            .ToList();
        return Task.FromResult<IEnumerable<Alert>>(alerts);
    }

    public Task<Guid> CreateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        if (alert.AlertId == Guid.Empty)
            alert.AlertId = Guid.NewGuid();

        _alerts[alert.AlertId] = alert;
        return Task.FromResult(alert.AlertId);
    }

    public Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<Alert> alerts, CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();
        foreach (var alert in alerts)
        {
            if (alert.AlertId == Guid.Empty)
                alert.AlertId = Guid.NewGuid();

            _alerts[alert.AlertId] = alert;
            ids.Add(alert.AlertId);
        }
        return Task.FromResult<IEnumerable<Guid>>(ids);
    }

    public Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _alerts[alert.AlertId] = alert;
        return Task.CompletedTask;
    }

    public Task AcknowledgeAsync(Guid alertId, Guid acknowledgedBy, string? acknowledgerName = null, CancellationToken cancellationToken = default)
    {
        if (_alerts.TryGetValue(alertId, out var alert))
        {
            alert.Acknowledge(acknowledgedBy, acknowledgerName);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        _alerts.TryRemove(alertId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        AlertType alertType,
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var exists = _alerts.Values.Any(a =>
            a.AlertType == alertType &&
            a.TargetEntityType == entityType &&
            a.TargetEntityId == entityId &&
            !a.IsAcknowledged());
        return Task.FromResult(exists);
    }
}
