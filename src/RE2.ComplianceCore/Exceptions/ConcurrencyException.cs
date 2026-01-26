namespace RE2.ComplianceCore.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected.
/// T157a: Custom exception for concurrency handling per FR-027a and research.md section 7.
/// Thrown when attempting to update an entity that has been modified by another user/process.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// The type of entity that had the conflict.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// The ID of the entity that had the conflict.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// The version that the client was trying to update from.
    /// </summary>
    public string? LocalVersion { get; }

    /// <summary>
    /// The current version in the database (that differs from local).
    /// </summary>
    public string? RemoteVersion { get; }

    /// <summary>
    /// Fields that have conflicting values, if known.
    /// </summary>
    public IReadOnlyList<string> ConflictingFields { get; }

    /// <summary>
    /// The current values in the database, if available.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? CurrentValues { get; }

    /// <summary>
    /// Creates a new ConcurrencyException.
    /// </summary>
    /// <param name="entityType">Type of entity with the conflict.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="message">Error message.</param>
    public ConcurrencyException(string entityType, Guid entityId, string? message = null)
        : base(message ?? $"Concurrency conflict detected for {entityType} with ID {entityId}. The entity was modified by another user.")
    {
        EntityType = entityType;
        EntityId = entityId;
        LocalVersion = null;
        RemoteVersion = null;
        ConflictingFields = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a new ConcurrencyException with version information.
    /// </summary>
    /// <param name="entityType">Type of entity with the conflict.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="localVersion">The version the client had.</param>
    /// <param name="remoteVersion">The current version in the database.</param>
    /// <param name="message">Error message.</param>
    public ConcurrencyException(string entityType, Guid entityId, string? localVersion, string? remoteVersion, string? message = null)
        : base(message ?? $"Concurrency conflict detected for {entityType} with ID {entityId}. Local version: {localVersion}, Remote version: {remoteVersion}")
    {
        EntityType = entityType;
        EntityId = entityId;
        LocalVersion = localVersion;
        RemoteVersion = remoteVersion;
        ConflictingFields = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a new ConcurrencyException with detailed conflict information.
    /// </summary>
    /// <param name="entityType">Type of entity with the conflict.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="localVersion">The version the client had.</param>
    /// <param name="remoteVersion">The current version in the database.</param>
    /// <param name="conflictingFields">Fields that have different values.</param>
    /// <param name="currentValues">Current values in the database.</param>
    /// <param name="message">Error message.</param>
    public ConcurrencyException(
        string entityType,
        Guid entityId,
        string? localVersion,
        string? remoteVersion,
        IEnumerable<string>? conflictingFields,
        IReadOnlyDictionary<string, object?>? currentValues = null,
        string? message = null)
        : base(message ?? BuildDetailedMessage(entityType, entityId, localVersion, remoteVersion, conflictingFields))
    {
        EntityType = entityType;
        EntityId = entityId;
        LocalVersion = localVersion;
        RemoteVersion = remoteVersion;
        ConflictingFields = conflictingFields?.ToList() ?? new List<string>();
        CurrentValues = currentValues;
    }

    /// <summary>
    /// Creates a ConcurrencyException from an inner exception (e.g., from Dataverse or D365).
    /// </summary>
    /// <param name="entityType">Type of entity with the conflict.</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="innerException">The original exception.</param>
    public ConcurrencyException(string entityType, Guid entityId, Exception innerException)
        : base($"Concurrency conflict detected for {entityType} with ID {entityId}. The entity was modified by another user.", innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
        LocalVersion = null;
        RemoteVersion = null;
        ConflictingFields = Array.Empty<string>();
    }

    private static string BuildDetailedMessage(
        string entityType,
        Guid entityId,
        string? localVersion,
        string? remoteVersion,
        IEnumerable<string>? conflictingFields)
    {
        var fields = conflictingFields?.ToList() ?? new List<string>();
        var fieldInfo = fields.Any()
            ? $" Conflicting fields: {string.Join(", ", fields)}."
            : string.Empty;

        return $"Concurrency conflict detected for {entityType} with ID {entityId}. " +
               $"Local version: {localVersion ?? "unknown"}, Remote version: {remoteVersion ?? "unknown"}.{fieldInfo}";
    }
}
