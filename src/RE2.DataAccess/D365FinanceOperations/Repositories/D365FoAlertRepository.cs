using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;

namespace RE2.DataAccess.D365FinanceOperations.Repositories;

/// <summary>
/// D365 Finance & Operations implementation of IAlertRepository.
/// T120: Repository for managing compliance alerts in D365 F&O virtual data entity.
/// Entity set: PharmaComplianceAlertEntity (per data-model.md entity 11).
/// </summary>
public class D365FoAlertRepository : IAlertRepository
{
    private readonly ID365FoClient _client;
    private readonly ILogger<D365FoAlertRepository> _logger;
    private const string EntitySetName = "PharmaComplianceAlertEntity";

    public D365FoAlertRepository(ID365FoClient client, ILogger<D365FoAlertRepository> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _client.GetByKeyAsync<AlertDto>(
                EntitySetName,
                $"'{alertId}'",
                cancellationToken);

            return dto?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", alertId);
            return null;
        }
    }

    public async Task<IEnumerable<Alert>> GetByTargetEntityAsync(
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"$filter=TargetEntityType eq {(int)entityType} and TargetEntityId eq {entityId}&$orderby=GeneratedDate desc";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for entity type {EntityType} ID {EntityId}", entityType, entityId);
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<IEnumerable<Alert>> GetUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = "$filter=AcknowledgedDate eq null&$orderby=Severity desc,GeneratedDate desc";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unacknowledged alerts");
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<IEnumerable<Alert>> GetByTypeAsync(AlertType alertType, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"$filter=AlertType eq {(int)alertType}&$orderby=GeneratedDate desc";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts of type {AlertType}", alertType);
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<IEnumerable<Alert>> GetBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"$filter=Severity eq {(int)severity}&$orderby=GeneratedDate desc";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts with severity {Severity}", severity);
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<IEnumerable<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertSeverity? severity = null,
        TargetEntityType? entityType = null,
        bool? isAcknowledged = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new List<string>();

            if (type.HasValue)
            {
                filters.Add($"AlertType eq {(int)type.Value}");
            }

            if (severity.HasValue)
            {
                filters.Add($"Severity eq {(int)severity.Value}");
            }

            if (entityType.HasValue)
            {
                filters.Add($"TargetEntityType eq {(int)entityType.Value}");
            }

            if (isAcknowledged.HasValue)
            {
                filters.Add(isAcknowledged.Value ? "AcknowledgedDate ne null" : "AcknowledgedDate eq null");
            }

            var queryParts = new List<string>();
            if (filters.Any())
            {
                queryParts.Add($"$filter={string.Join(" and ", filters)}");
            }

            queryParts.Add("$orderby=Severity desc,GeneratedDate desc");

            if (maxResults.HasValue)
            {
                queryParts.Add($"$top={maxResults.Value}");
            }

            var query = string.Join("&", queryParts);
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts with filters");
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<IEnumerable<Alert>> GetOverdueAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var query = $"$filter=AcknowledgedDate eq null and DueDate lt {today}&$orderby=DueDate asc";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Select(d => d.ToDomainModel()) ?? Enumerable.Empty<Alert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue alerts");
            return Enumerable.Empty<Alert>();
        }
    }

    public async Task<Guid> CreateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            if (alert.AlertId == Guid.Empty)
            {
                alert.AlertId = Guid.NewGuid();
            }

            var dto = AlertDto.FromDomainModel(alert);
            await _client.CreateAsync(EntitySetName, dto, cancellationToken);

            _logger.LogInformation("Created alert {AlertId} of type {AlertType} for entity {TargetEntityType} {TargetEntityId}",
                alert.AlertId, alert.AlertType, alert.TargetEntityType, alert.TargetEntityId);

            return alert.AlertId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert of type {AlertType}", alert.AlertType);
            throw;
        }
    }

    public async Task<IEnumerable<Guid>> CreateBatchAsync(IEnumerable<Alert> alerts, CancellationToken cancellationToken = default)
    {
        var createdIds = new List<Guid>();

        foreach (var alert in alerts)
        {
            try
            {
                var id = await CreateAsync(alert, cancellationToken);
                createdIds.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create alert for entity {TargetEntityType} {TargetEntityId}, continuing batch",
                    alert.TargetEntityType, alert.TargetEntityId);
                // Continue with other alerts
            }
        }

        _logger.LogInformation("Created {Count} alerts in batch", createdIds.Count);
        return createdIds;
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = AlertDto.FromDomainModel(alert);
            await _client.UpdateAsync(EntitySetName, $"'{alert.AlertId}'", dto, cancellationToken);

            _logger.LogInformation("Updated alert {AlertId}", alert.AlertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert {AlertId}", alert.AlertId);
            throw;
        }
    }

    public async Task AcknowledgeAsync(Guid alertId, Guid acknowledgedBy, string? acknowledgerName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await GetByIdAsync(alertId, cancellationToken);
            if (alert == null)
            {
                _logger.LogWarning("Cannot acknowledge alert {AlertId} - not found", alertId);
                return;
            }

            alert.Acknowledge(acknowledgedBy, acknowledgerName);
            await UpdateAsync(alert, cancellationToken);

            _logger.LogInformation("Acknowledged alert {AlertId} by {AcknowledgerName}", alertId, acknowledgerName ?? acknowledgedBy.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteAsync(EntitySetName, $"'{alertId}'", cancellationToken);
            _logger.LogInformation("Deleted alert {AlertId}", alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        AlertType alertType,
        TargetEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for existing unacknowledged alert of same type for same entity
            var query = $"$filter=AlertType eq {(int)alertType} and TargetEntityType eq {(int)entityType} and TargetEntityId eq {entityId} and AcknowledgedDate eq null&$top=1&$select=AlertId";
            var response = await _client.GetAsync<AlertODataResponse>(EntitySetName, query, cancellationToken);

            return response?.value.Any() ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if alert exists for {AlertType} {EntityType} {EntityId}",
                alertType, entityType, entityId);
            return false;
        }
    }
}
