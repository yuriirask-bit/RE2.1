namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Abstraction for Dynamics 365 Finance & Operations OData API client.
/// Per research.md section 2: Uses OData v4 with HttpClient and OAuth2 authentication.
/// D365 F&O stores transactional data: validation requests/results, audit events, alerts/notifications.
/// </summary>
public interface ID365FoClient
{
    /// <summary>
    /// Executes an OData GET request to retrieve entities.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="entitySetName">The entity set name (e.g., "Products", "SalesOrders").</param>
    /// <param name="query">Optional OData query string (e.g., "$filter=Status eq 'Active'").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    Task<T?> GetAsync<T>(
        string entitySetName,
        string? query = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an OData GET request to retrieve a single entity by key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="entitySetName">The entity set name.</param>
    /// <param name="key">The entity key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized entity.</returns>
    Task<T?> GetByKeyAsync<T>(
        string entitySetName,
        string key,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an OData POST request to create a new entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity to create.</typeparam>
    /// <param name="entitySetName">The entity set name.</param>
    /// <param name="entity">The entity data to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity with server-generated values.</returns>
    Task<T?> CreateAsync<T>(
        string entitySetName,
        T entity,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an OData PATCH request to update an existing entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity to update.</typeparam>
    /// <param name="entitySetName">The entity set name.</param>
    /// <param name="key">The entity key value.</param>
    /// <param name="entity">The entity data to update (partial updates supported).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync<T>(
        string entitySetName,
        string key,
        T entity,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an OData PATCH request with optimistic concurrency control via ETag.
    /// T157: Uses If-Match header with ETag for concurrency per research.md section 7.
    /// </summary>
    /// <typeparam name="T">The type of the entity to update.</typeparam>
    /// <param name="entitySetName">The entity set name.</param>
    /// <param name="key">The entity key value.</param>
    /// <param name="entity">The entity data to update.</param>
    /// <param name="etag">The ETag from the original read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="RE2.ComplianceCore.Exceptions.ConcurrencyException">
    /// Thrown when the entity has been modified by another user since it was read.
    /// </exception>
    Task UpdateWithConcurrencyAsync<T>(
        string entitySetName,
        string key,
        T entity,
        string etag,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an OData DELETE request to remove an entity.
    /// </summary>
    /// <param name="entitySetName">The entity set name.</param>
    /// <param name="key">The entity key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string entitySetName,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom OData action or function.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="actionOrFunctionName">The action or function name.</param>
    /// <param name="request">The request payload (null for parameterless actions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the action/function.</returns>
    Task<TResponse?> ExecuteActionAsync<TRequest, TResponse>(
        string actionOrFunctionName,
        TRequest? request = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}
