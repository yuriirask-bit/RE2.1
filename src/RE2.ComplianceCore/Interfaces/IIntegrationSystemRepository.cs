using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for IntegrationSystem entity operations.
/// T047c: Repository interface for managing API client registrations per FR-061.
/// Used to track which external systems can call compliance validation APIs.
/// </summary>
public interface IIntegrationSystemRepository
{
    /// <summary>
    /// Gets an integration system by ID.
    /// </summary>
    /// <param name="integrationSystemId">The unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration system, or null if not found.</returns>
    Task<IntegrationSystem?> GetByIdAsync(Guid integrationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration system by system name.
    /// System names are unique.
    /// </summary>
    /// <param name="systemName">The system name to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration system, or null if not found.</returns>
    Task<IntegrationSystem?> GetBySystemNameAsync(string systemName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration system by OAuth client ID.
    /// Used for authentication validation.
    /// </summary>
    /// <param name="oauthClientId">The OAuth client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration system, or null if not found.</returns>
    Task<IntegrationSystem?> GetByOAuthClientIdAsync(string oauthClientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all integration systems.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all integration systems.</returns>
    Task<IEnumerable<IntegrationSystem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active integration systems.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active integration systems.</returns>
    Task<IEnumerable<IntegrationSystem>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integration systems by type.
    /// </summary>
    /// <param name="systemType">The system type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of integration systems of the specified type.</returns>
    Task<IEnumerable<IntegrationSystem>> GetBySystemTypeAsync(IntegrationSystemType systemType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new integration system.
    /// </summary>
    /// <param name="integrationSystem">The integration system to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created integration system.</returns>
    Task<Guid> CreateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing integration system.
    /// </summary>
    /// <param name="integrationSystem">The integration system to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(IntegrationSystem integrationSystem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an integration system.
    /// </summary>
    /// <param name="integrationSystemId">The ID of the integration system to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid integrationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an integration system exists by ID.
    /// </summary>
    /// <param name="integrationSystemId">The integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the integration system exists.</returns>
    Task<bool> ExistsAsync(Guid integrationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a system name is already in use.
    /// </summary>
    /// <param name="systemName">The system name to check.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the system name is already in use.</returns>
    Task<bool> SystemNameExistsAsync(string systemName, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an integration system is authorized to call a specific endpoint.
    /// </summary>
    /// <param name="integrationSystemId">The integration system ID.</param>
    /// <param name="endpoint">The API endpoint being accessed.</param>
    /// <param name="ipAddress">The IP address of the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the system is authorized.</returns>
    Task<bool> IsAuthorizedAsync(Guid integrationSystemId, string endpoint, string? ipAddress = null, CancellationToken cancellationToken = default);
}
