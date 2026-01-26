using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Abstraction for Dataverse API client operations.
/// Per research.md section 1: Uses Microsoft.PowerPlatform.Dataverse.Client with Managed Identity.
/// Dataverse stores master data: licences, customers, GDP sites, partners, qualifications, inspections, CAPAs.
/// </summary>
public interface IDataverseClient
{
    /// <summary>
    /// Gets whether the client is connected to Dataverse.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Retrieves a single entity by ID.
    /// </summary>
    /// <param name="entityName">Logical name of the entity (e.g., "phr_licence").</param>
    /// <param name="id">Unique identifier of the record.</param>
    /// <param name="columnSet">Columns to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retrieved entity.</returns>
    Task<Entity> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columnSet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves multiple entities using a query.
    /// </summary>
    /// <param name="query">Query expression defining the retrieval criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of entities matching the query.</returns>
    Task<EntityCollection> RetrieveMultipleAsync(
        QueryExpression query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity record.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created record.</returns>
    Task<Guid> CreateAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity record.
    /// </summary>
    /// <param name="entity">The entity to update (must include Id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity record with optimistic concurrency control.
    /// T157: Uses RowVersion for concurrency per research.md section 7.
    /// </summary>
    /// <param name="entity">The entity to update (must include Id and RowVersion).</param>
    /// <param name="rowVersion">The row version from the original read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="RE2.ComplianceCore.Exceptions.ConcurrencyException">
    /// Thrown when the entity has been modified by another user since it was read.
    /// </exception>
    Task UpdateWithConcurrencyAsync(
        Entity entity,
        string rowVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity record.
    /// </summary>
    /// <param name="entityName">Logical name of the entity.</param>
    /// <param name="id">Unique identifier of the record to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string entityName,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an organization request (for advanced operations).
    /// </summary>
    /// <param name="request">The organization request to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The organization response.</returns>
    Task<OrganizationResponse> ExecuteAsync(
        OrganizationRequest request,
        CancellationToken cancellationToken = default);
}
