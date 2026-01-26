namespace RE2.ComplianceWeb.Models;

/// <summary>
/// View model for conflict resolution UI.
/// T158: Per FR-027b, displays when optimistic concurrency conflict is detected.
/// </summary>
public class ConflictResolutionViewModel
{
    /// <summary>
    /// Type of entity with the conflict (e.g., "Licence", "Customer").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity with the conflict.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Name of the user who modified the record (if known).
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// When the record was modified by the other user.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// List of fields with conflicting values.
    /// </summary>
    public List<ConflictingFieldViewModel> ConflictingFields { get; set; } = new();

    /// <summary>
    /// URL to return to after resolution.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Whether to show the conflict resolution modal immediately.
    /// </summary>
    public bool ShowModal { get; set; } = true;

    /// <summary>
    /// Creates a ConflictResolutionViewModel from a ConcurrencyException.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <param name="userValues">The user's submitted values.</param>
    /// <param name="fieldLabels">Optional friendly labels for field names.</param>
    /// <param name="returnUrl">URL to return to after resolution.</param>
    public static ConflictResolutionViewModel FromException(
        RE2.ComplianceCore.Exceptions.ConcurrencyException exception,
        IReadOnlyDictionary<string, object?>? userValues = null,
        IReadOnlyDictionary<string, string>? fieldLabels = null,
        string? returnUrl = null)
    {
        var model = new ConflictResolutionViewModel
        {
            EntityType = exception.EntityType,
            EntityId = exception.EntityId,
            ReturnUrl = returnUrl
        };

        foreach (var fieldName in exception.ConflictingFields)
        {
            var field = new ConflictingFieldViewModel
            {
                FieldName = fieldName,
                FieldLabel = fieldLabels?.GetValueOrDefault(fieldName),
                UserValue = userValues?.GetValueOrDefault(fieldName)?.ToString(),
                DatabaseValue = exception.CurrentValues?.GetValueOrDefault(fieldName)?.ToString(),
                UserValueChanged = userValues?.ContainsKey(fieldName) == true
            };

            model.ConflictingFields.Add(field);
        }

        // If we have current values but no specific conflicting fields, show all changed fields
        if (!exception.ConflictingFields.Any() && exception.CurrentValues != null && userValues != null)
        {
            foreach (var kvp in exception.CurrentValues)
            {
                if (userValues.TryGetValue(kvp.Key, out var userValue))
                {
                    var dbValueStr = kvp.Value?.ToString();
                    var userValueStr = userValue?.ToString();

                    if (dbValueStr != userValueStr)
                    {
                        model.ConflictingFields.Add(new ConflictingFieldViewModel
                        {
                            FieldName = kvp.Key,
                            FieldLabel = fieldLabels?.GetValueOrDefault(kvp.Key),
                            UserValue = userValueStr,
                            DatabaseValue = dbValueStr,
                            UserValueChanged = true
                        });
                    }
                }
            }
        }

        return model;
    }
}

/// <summary>
/// Represents a single field with conflicting values.
/// </summary>
public class ConflictingFieldViewModel
{
    /// <summary>
    /// Name of the field (property name).
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Friendly display label for the field.
    /// </summary>
    public string? FieldLabel { get; set; }

    /// <summary>
    /// The value the user submitted.
    /// </summary>
    public string? UserValue { get; set; }

    /// <summary>
    /// The current value in the database.
    /// </summary>
    public string? DatabaseValue { get; set; }

    /// <summary>
    /// Whether the user changed this field from its original value.
    /// </summary>
    public bool UserValueChanged { get; set; }
}

/// <summary>
/// Request model for conflict resolution submission.
/// </summary>
public class ConflictResolutionRequest
{
    /// <summary>
    /// Type of entity being resolved.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Resolution strategy: "user", "database", or "merge".
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// JSON string of field choices when resolution is "merge".
    /// Keys are field names, values are "user" or "database".
    /// </summary>
    public string? FieldChoicesJson { get; set; }

    /// <summary>
    /// URL to return to after resolution.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
