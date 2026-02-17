using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Integration system management API endpoints.
/// T047e: IntegrationSystemsController v1 for managing API client registrations per data-model.md entity 27.
/// Used to register and manage external systems that can access compliance validation APIs (FR-061).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class IntegrationSystemsController : ControllerBase
{
    private readonly IIntegrationSystemRepository _repository;
    private readonly ILogger<IntegrationSystemsController> _logger;

    public IntegrationSystemsController(
        IIntegrationSystemRepository repository,
        ILogger<IntegrationSystemsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all integration systems.
    /// </summary>
    /// <param name="activeOnly">If true, only returns active systems. Default: false.</param>
    /// <param name="systemType">Filter by system type (ERP, OrderManagement, WMS, CustomSystem).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integration systems.</returns>
    [HttpGet]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<IntegrationSystemResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntegrationSystems(
        [FromQuery] bool activeOnly = false,
        [FromQuery] string? systemType = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<IntegrationSystem> systems;

        if (!string.IsNullOrEmpty(systemType) && Enum.TryParse<IntegrationSystemType>(systemType, true, out var type))
        {
            systems = await _repository.GetBySystemTypeAsync(type, cancellationToken);
        }
        else if (activeOnly)
        {
            systems = await _repository.GetActiveAsync(cancellationToken);
        }
        else
        {
            systems = await _repository.GetAllAsync(cancellationToken);
        }

        var response = systems.Select(IntegrationSystemResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific integration system by ID.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration system details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntegrationSystem(Guid id, CancellationToken cancellationToken = default)
    {
        var system = await _repository.GetByIdAsync(id, cancellationToken);

        if (system == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(IntegrationSystemResponseDto.FromDomainModel(system));
    }

    /// <summary>
    /// Gets an integration system by name.
    /// </summary>
    /// <param name="name">The system name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration system details.</returns>
    [HttpGet("by-name/{name}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntegrationSystemByName(string name, CancellationToken cancellationToken = default)
    {
        var system = await _repository.GetBySystemNameAsync(name, cancellationToken);

        if (system == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with name '{name}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(IntegrationSystemResponseDto.FromDomainModel(system));
    }

    /// <summary>
    /// Creates a new integration system registration.
    /// Only SystemAdmin role can create integration systems.
    /// </summary>
    /// <param name="request">Integration system creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created integration system details.</returns>
    [HttpPost]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntegrationSystem(
        [FromBody] CreateIntegrationSystemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var system = request.ToDomainModel();

        // Validate the integration system
        var validationResult = system.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Integration system validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate name
        if (await _repository.SystemNameExistsAsync(system.SystemName, null, cancellationToken))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Integration system with name '{system.SystemName}' already exists",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var id = await _repository.CreateAsync(system, cancellationToken);
        system.IntegrationSystemId = id;

        _logger.LogInformation("Created integration system {Name} with ID {Id}", system.SystemName, id);

        return CreatedAtAction(
            nameof(GetIntegrationSystem),
            new { id = id },
            IntegrationSystemResponseDto.FromDomainModel(system));
    }

    /// <summary>
    /// Updates an existing integration system.
    /// Only SystemAdmin role can modify integration systems.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="request">Updated integration system data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated integration system details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIntegrationSystem(
        Guid id,
        [FromBody] UpdateIntegrationSystemRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Check system exists
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var system = request.ToDomainModel(id);
        system.CreatedDate = existing.CreatedDate; // Preserve original creation date

        // Validate the integration system
        var validationResult = system.Validate();
        if (!validationResult.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Integration system validation failed",
                Details = string.Join("; ", validationResult.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Check for duplicate name (if changed)
        if (existing.SystemName != system.SystemName &&
            await _repository.SystemNameExistsAsync(system.SystemName, id, cancellationToken))
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Integration system with name '{system.SystemName}' already exists",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        await _repository.UpdateAsync(system, cancellationToken);
        _logger.LogInformation("Updated integration system {Id}", id);

        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return Ok(IntegrationSystemResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes an integration system.
    /// Only SystemAdmin role can delete integration systems.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIntegrationSystem(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted integration system {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Activates an integration system.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated integration system details.</returns>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateIntegrationSystem(Guid id, CancellationToken cancellationToken = default)
    {
        var system = await _repository.GetByIdAsync(id, cancellationToken);
        if (system == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        system.Activate();
        await _repository.UpdateAsync(system, cancellationToken);
        _logger.LogInformation("Activated integration system {Id}", id);

        return Ok(IntegrationSystemResponseDto.FromDomainModel(system));
    }

    /// <summary>
    /// Deactivates an integration system.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated integration system details.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(IntegrationSystemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateIntegrationSystem(Guid id, CancellationToken cancellationToken = default)
    {
        var system = await _repository.GetByIdAsync(id, cancellationToken);
        if (system == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        system.Deactivate();
        await _repository.UpdateAsync(system, cancellationToken);
        _logger.LogInformation("Deactivated integration system {Id}", id);

        return Ok(IntegrationSystemResponseDto.FromDomainModel(system));
    }

    /// <summary>
    /// Validates if an integration system is authorized to call a specific endpoint.
    /// For testing authorization configuration.
    /// </summary>
    /// <param name="id">Integration system ID.</param>
    /// <param name="endpoint">The endpoint to check.</param>
    /// <param name="ipAddress">Optional IP address to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization check result.</returns>
    [HttpGet("{id:guid}/check-authorization")]
    [Authorize(Roles = UserRoles.SYSTEM_ADMIN)]
    [ProducesResponseType(typeof(AuthorizationCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAuthorization(
        Guid id,
        [FromQuery] string endpoint,
        [FromQuery] string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var system = await _repository.GetByIdAsync(id, cancellationToken);
        if (system == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Integration system with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var isAuthorized = await _repository.IsAuthorizedAsync(id, endpoint, ipAddress, cancellationToken);

        return Ok(new AuthorizationCheckResultDto
        {
            IntegrationSystemId = id,
            SystemName = system.SystemName,
            Endpoint = endpoint,
            IpAddress = ipAddress,
            IsAuthorized = isAuthorized,
            Reason = isAuthorized
                ? "System is authorized to call this endpoint"
                : GetAuthorizationDenialReason(system, endpoint, ipAddress)
        });
    }

    private static string GetAuthorizationDenialReason(IntegrationSystem system, string endpoint, string? ipAddress)
    {
        if (!system.IsActive)
        {
            return "System is inactive";
        }

        if (!system.IsEndpointAuthorized(endpoint))
        {
            return $"Endpoint '{endpoint}' is not in the authorized endpoints list";
        }

        if (!string.IsNullOrEmpty(ipAddress) && !system.IsIpAllowed(ipAddress))
        {
            return $"IP address '{ipAddress}' is not in the whitelist";
        }

        return "Unknown reason";
    }
}

#region DTOs

/// <summary>
/// Integration system response DTO for API responses.
/// </summary>
public class IntegrationSystemResponseDto
{
    public Guid IntegrationSystemId { get; set; }
    public required string SystemName { get; set; }
    public required string SystemType { get; set; }
    public bool HasApiKey { get; set; }
    public string? OAuthClientId { get; set; }
    public string[]? AuthorizedEndpoints { get; set; }
    public string[]? IpWhitelist { get; set; }
    public bool IsActive { get; set; }
    public string? ContactPerson { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public static IntegrationSystemResponseDto FromDomainModel(IntegrationSystem system)
    {
        return new IntegrationSystemResponseDto
        {
            IntegrationSystemId = system.IntegrationSystemId,
            SystemName = system.SystemName,
            SystemType = system.SystemType.ToString(),
            HasApiKey = !string.IsNullOrEmpty(system.ApiKeyHash),
            OAuthClientId = system.OAuthClientId,
            AuthorizedEndpoints = system.GetAuthorizedEndpointsArray(),
            IpWhitelist = system.GetIpWhitelistArray(),
            IsActive = system.IsActive,
            ContactPerson = system.ContactPerson,
            CreatedDate = system.CreatedDate,
            ModifiedDate = system.ModifiedDate
        };
    }
}

/// <summary>
/// Request DTO for creating a new integration system.
/// </summary>
public class CreateIntegrationSystemRequestDto
{
    public required string SystemName { get; set; }
    public required string SystemType { get; set; }
    public string? ApiKeyHash { get; set; }
    public string? OAuthClientId { get; set; }
    public string[]? AuthorizedEndpoints { get; set; }
    public string[]? IpWhitelist { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ContactPerson { get; set; }

    public IntegrationSystem ToDomainModel()
    {
        return new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = SystemName,
            SystemType = Enum.Parse<IntegrationSystemType>(SystemType, true),
            ApiKeyHash = ApiKeyHash,
            OAuthClientId = OAuthClientId,
            AuthorizedEndpoints = AuthorizedEndpoints != null ? string.Join(",", AuthorizedEndpoints) : null,
            IpWhitelist = IpWhitelist != null ? string.Join(",", IpWhitelist) : null,
            IsActive = IsActive,
            ContactPerson = ContactPerson
        };
    }
}

/// <summary>
/// Request DTO for updating an integration system.
/// </summary>
public class UpdateIntegrationSystemRequestDto
{
    public required string SystemName { get; set; }
    public required string SystemType { get; set; }
    public string? ApiKeyHash { get; set; }
    public string? OAuthClientId { get; set; }
    public string[]? AuthorizedEndpoints { get; set; }
    public string[]? IpWhitelist { get; set; }
    public bool IsActive { get; set; }
    public string? ContactPerson { get; set; }

    public IntegrationSystem ToDomainModel(Guid integrationSystemId)
    {
        return new IntegrationSystem
        {
            IntegrationSystemId = integrationSystemId,
            SystemName = SystemName,
            SystemType = Enum.Parse<IntegrationSystemType>(SystemType, true),
            ApiKeyHash = ApiKeyHash,
            OAuthClientId = OAuthClientId,
            AuthorizedEndpoints = AuthorizedEndpoints != null ? string.Join(",", AuthorizedEndpoints) : null,
            IpWhitelist = IpWhitelist != null ? string.Join(",", IpWhitelist) : null,
            IsActive = IsActive,
            ContactPerson = ContactPerson
        };
    }
}

/// <summary>
/// Authorization check result DTO.
/// </summary>
public class AuthorizationCheckResultDto
{
    public Guid IntegrationSystemId { get; set; }
    public required string SystemName { get; set; }
    public required string Endpoint { get; set; }
    public string? IpAddress { get; set; }
    public bool IsAuthorized { get; set; }
    public required string Reason { get; set; }
}

#endregion
