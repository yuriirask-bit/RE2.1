using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of IAlertRepository.
/// Entity: phr_compliancealert — migrated from D365 F&amp;O (PharmaComplianceAlertEntity).
/// </summary>
public class DataverseAlertRepository : IAlertRepository
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ILogger<DataverseAlertRepository> _logger;

    private const string AlertEntityName = "phr_compliancealert";

    public DataverseAlertRepository(
        IDataverseClient dataverseClient,
        ILogger<DataverseAlertRepository> logger)
    {
        _dataverseClient = dataverseClient;
        _logger = logger;
    }

    public async Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dataverseClient.RetrieveAsync(AlertEntityName, alertId, new ColumnSet(true), cancellationToken);
            return entity != null ? MapToAlert(entity) : null;
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_targetentitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_targetentityid", ConditionOperator.Equal, entityId)
                    }
                },
                Orders = { new OrderExpression("phr_generateddate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_acknowledgeddate", ConditionOperator.Null)
                    }
                },
                Orders =
                {
                    new OrderExpression("phr_severity", OrderType.Descending),
                    new OrderExpression("phr_generateddate", OrderType.Descending)
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_alerttype", ConditionOperator.Equal, (int)alertType)
                    }
                },
                Orders = { new OrderExpression("phr_generateddate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_severity", ConditionOperator.Equal, (int)severity)
                    }
                },
                Orders = { new OrderExpression("phr_generateddate", OrderType.Descending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
            var filter = new FilterExpression(LogicalOperator.And);

            if (type.HasValue)
                filter.AddCondition("phr_alerttype", ConditionOperator.Equal, (int)type.Value);

            if (severity.HasValue)
                filter.AddCondition("phr_severity", ConditionOperator.Equal, (int)severity.Value);

            if (entityType.HasValue)
                filter.AddCondition("phr_targetentitytype", ConditionOperator.Equal, (int)entityType.Value);

            if (isAcknowledged.HasValue)
            {
                filter.AddCondition("phr_acknowledgeddate",
                    isAcknowledged.Value ? ConditionOperator.NotNull : ConditionOperator.Null);
            }

            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = filter,
                Orders =
                {
                    new OrderExpression("phr_severity", OrderType.Descending),
                    new OrderExpression("phr_generateddate", OrderType.Descending)
                }
            };

            if (maxResults.HasValue)
                query.TopCount = maxResults.Value;

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_duedate", ConditionOperator.LessThan, DateTime.UtcNow),
                        new ConditionExpression("phr_acknowledgeddate", ConditionOperator.Null)
                    }
                },
                Orders = { new OrderExpression("phr_duedate", OrderType.Ascending) }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Select(MapToAlert).ToList();
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
                alert.AlertId = Guid.NewGuid();

            if (alert.GeneratedDate == default)
                alert.GeneratedDate = DateTime.UtcNow;

            var entity = MapToEntity(alert);
            await _dataverseClient.CreateAsync(entity, cancellationToken);

            _logger.LogInformation("Created alert {AlertId} type {AlertType} for {TargetEntityType} {TargetEntityId}",
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
                _logger.LogWarning(ex, "Failed to create alert for {TargetEntityType} {TargetEntityId}, continuing batch",
                    alert.TargetEntityType, alert.TargetEntityId);
            }
        }

        _logger.LogInformation("Created {Count} alerts in batch", createdIds.Count);
        return createdIds;
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = MapToEntity(alert);
            await _dataverseClient.UpdateAsync(entity, cancellationToken);

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
            var existing = await _dataverseClient.RetrieveAsync(AlertEntityName, alertId, new ColumnSet(true), cancellationToken);
            if (existing == null)
            {
                _logger.LogWarning("Cannot acknowledge alert {AlertId} - not found", alertId);
                return;
            }

            existing["phr_acknowledgeddate"] = DateTime.UtcNow;
            existing["phr_acknowledgedby"] = acknowledgedBy;
            existing["phr_acknowledgername"] = acknowledgerName;

            await _dataverseClient.UpdateAsync(existing, cancellationToken);

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
            await _dataverseClient.DeleteAsync(AlertEntityName, alertId, cancellationToken);
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
            var query = new QueryExpression(AlertEntityName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("phr_alerttype", ConditionOperator.Equal, (int)alertType),
                        new ConditionExpression("phr_targetentitytype", ConditionOperator.Equal, (int)entityType),
                        new ConditionExpression("phr_targetentityid", ConditionOperator.Equal, entityId),
                        new ConditionExpression("phr_acknowledgeddate", ConditionOperator.Null)
                    }
                }
            };

            var result = await _dataverseClient.RetrieveMultipleAsync(query, cancellationToken);
            return result.Entities.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if alert exists for {AlertType} {EntityType} {EntityId}",
                alertType, entityType, entityId);
            return false;
        }
    }

    #region Mapping Helpers

    private static Alert MapToAlert(Entity entity)
    {
        return new Alert
        {
            AlertId = entity.Id,
            AlertType = (AlertType)entity.GetAttributeValue<int>("phr_alerttype"),
            Severity = (AlertSeverity)entity.GetAttributeValue<int>("phr_severity"),
            TargetEntityType = (TargetEntityType)entity.GetAttributeValue<int>("phr_targetentitytype"),
            TargetEntityId = entity.GetAttributeValue<Guid>("phr_targetentityid"),
            GeneratedDate = entity.GetAttributeValue<DateTime>("phr_generateddate"),
            AcknowledgedDate = entity.Contains("phr_acknowledgeddate")
                ? entity.GetAttributeValue<DateTime?>("phr_acknowledgeddate")
                : null,
            AcknowledgedBy = entity.Contains("phr_acknowledgedby")
                ? entity.GetAttributeValue<Guid?>("phr_acknowledgedby")
                : null,
            AcknowledgerName = entity.GetAttributeValue<string>("phr_acknowledgername"),
            Message = entity.GetAttributeValue<string>("phr_message") ?? string.Empty,
            Details = entity.GetAttributeValue<string>("phr_details"),
            RelatedEntityId = entity.Contains("phr_relatedentityid")
                ? entity.GetAttributeValue<Guid?>("phr_relatedentityid")
                : null,
            DueDate = entity.Contains("phr_duedate")
                ? DateOnly.FromDateTime(entity.GetAttributeValue<DateTime>("phr_duedate"))
                : null
        };
    }

    private static Entity MapToEntity(Alert alert)
    {
        var entity = new Entity(AlertEntityName, alert.AlertId);
        entity["phr_alerttype"] = (int)alert.AlertType;
        entity["phr_severity"] = (int)alert.Severity;
        entity["phr_targetentitytype"] = (int)alert.TargetEntityType;
        entity["phr_targetentityid"] = alert.TargetEntityId;
        entity["phr_generateddate"] = alert.GeneratedDate;
        entity["phr_message"] = alert.Message;
        entity["phr_details"] = alert.Details;

        if (alert.AcknowledgedDate.HasValue)
            entity["phr_acknowledgeddate"] = alert.AcknowledgedDate.Value;

        if (alert.AcknowledgedBy.HasValue)
            entity["phr_acknowledgedby"] = alert.AcknowledgedBy.Value;

        if (alert.AcknowledgerName != null)
            entity["phr_acknowledgername"] = alert.AcknowledgerName;

        if (alert.RelatedEntityId.HasValue)
            entity["phr_relatedentityid"] = alert.RelatedEntityId.Value;

        if (alert.DueDate.HasValue)
            entity["phr_duedate"] = alert.DueDate.Value.ToDateTime(TimeOnly.MinValue);

        return entity;
    }

    #endregion
}
