using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Exceptions;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.Auditing;
using RE2.ComplianceWeb.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// Controller for handling conflict resolution workflow.
/// T159: Implements conflict detection and resolution per FR-027b.
/// </summary>
[Authorize]
public class ConflictsController : Controller
{
    private readonly ILicenceService _licenceService;
    private readonly ICustomerService _customerService;
    private readonly IAuditLoggingService _auditService;
    private readonly ILogger<ConflictsController> _logger;

    public ConflictsController(
        ILicenceService licenceService,
        ICustomerService customerService,
        IAuditLoggingService auditService,
        ILogger<ConflictsController> logger)
    {
        _licenceService = licenceService;
        _customerService = customerService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the conflict resolution page for a specific entity.
    /// </summary>
    [HttpGet]
    public IActionResult Resolve(string entityType, Guid entityId, string? returnUrl = null)
    {
        // This would typically be called from TempData set by another controller
        // when a ConcurrencyException is caught

        var model = TempData.Get<ConflictResolutionViewModel>("ConflictModel");

        if (model == null)
        {
            // No conflict data available, redirect back
            return Redirect(returnUrl ?? Url.Action("Index", "Dashboard") ?? "/");
        }

        model.ReturnUrl = returnUrl;
        return View(model);
    }

    /// <summary>
    /// Handles the conflict resolution submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveConflict([FromForm] ConflictResolutionRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Resolving conflict for {EntityType} {EntityId} with resolution {Resolution}",
                request.EntityType, request.EntityId, request.Resolution);

            Dictionary<string, string>? fieldChoices = null;

            if (request.Resolution == "merge" && !string.IsNullOrEmpty(request.FieldChoicesJson))
            {
                fieldChoices = JsonSerializer.Deserialize<Dictionary<string, string>>(request.FieldChoicesJson);
            }

            var result = await ProcessResolutionAsync(request.EntityType, request.EntityId, request.Resolution, fieldChoices);

            if (result.Success)
            {
                // Log the resolution
                var auditEvent = new AuditEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = AuditEventType.OverrideApproved, // Using existing type for conflict resolution
                    EventDate = DateTime.UtcNow,
                    PerformedBy = GetCurrentUserId(),
                    EntityType = request.EntityType switch
                    {
                        "licence" => AuditEntityType.Licence,
                        "customer" => AuditEntityType.Customer,
                        _ => AuditEntityType.Transaction // Default fallback
                    },
                    EntityId = request.EntityId,
                    Details = JsonSerializer.Serialize(new
                    {
                        Action = "ConflictResolved",
                        Resolution = request.Resolution,
                        FieldChoices = fieldChoices,
                        ResolvedBy = User.Identity?.Name
                    })
                };
                await _auditService.LogEventAsync(auditEvent, HttpContext.RequestAborted);

                return Json(new
                {
                    success = true,
                    redirectUrl = result.RedirectUrl ?? request.ReturnUrl
                });
            }

            return Json(new
            {
                success = false,
                error = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conflict for {EntityType} {EntityId}",
                request.EntityType, request.EntityId);

            return Json(new
            {
                success = false,
                error = "An unexpected error occurred while resolving the conflict."
            });
        }
    }

    private async Task<ConflictResolutionResult> ProcessResolutionAsync(
        string entityType,
        Guid entityId,
        string resolution,
        Dictionary<string, string>? fieldChoices)
    {
        // The actual resolution logic depends on the entity type
        // For each entity type, we need to:
        // 1. Fetch the current database values
        // 2. Get the user's values from session/cache
        // 3. Apply the resolution strategy
        // 4. Save with the new RowVersion

        return entityType.ToLowerInvariant() switch
        {
            "licence" => await ResolveLicenceConflictAsync(entityId, resolution, fieldChoices),
            "customer" => await ResolveCustomerConflictAsync(entityId, resolution, fieldChoices),
            _ => new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = $"Unknown entity type: {entityType}"
            }
        };
    }

    private async Task<ConflictResolutionResult> ResolveLicenceConflictAsync(
        Guid licenceId,
        string resolution,
        Dictionary<string, string>? fieldChoices)
    {
        // In a full implementation, this would:
        // 1. Retrieve the stored user changes from session/TempData
        // 2. Fetch fresh data from the database
        // 3. Apply the merge strategy based on fieldChoices
        // 4. Save with optimistic locking

        if (resolution == "database")
        {
            // User chose to discard their changes
            return new ConflictResolutionResult
            {
                Success = true,
                RedirectUrl = Url.Action("Details", "Licences", new { id = licenceId })
            };
        }

        // For "user" or "merge" resolutions, we'd need the user's original values
        // which should be stored in TempData or session when the conflict was detected

        var userValues = TempData.Get<Dictionary<string, object?>>("UserValues_" + licenceId);

        if (userValues == null)
        {
            return new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = "User values not found. Please make your changes again."
            };
        }

        try
        {
            // Fetch current licence
            var licence = await _licenceService.GetByIdAsync(licenceId);

            if (licence == null)
            {
                return new ConflictResolutionResult
                {
                    Success = false,
                    ErrorMessage = "Licence not found."
                };
            }

            // Apply merge strategy
            if (fieldChoices != null)
            {
                foreach (var choice in fieldChoices)
                {
                    if (choice.Value == "user" && userValues.TryGetValue(choice.Key, out var userValue))
                    {
                        // Apply user's value to the licence property
                        ApplyPropertyValue(licence, choice.Key, userValue);
                    }
                    // If choice.Value == "database", we keep the current value
                }
            }
            else if (resolution == "user")
            {
                // Apply all user values
                foreach (var kvp in userValues)
                {
                    ApplyPropertyValue(licence, kvp.Key, kvp.Value);
                }
            }

            // Save the updated licence
            await _licenceService.UpdateAsync(licence);

            return new ConflictResolutionResult
            {
                Success = true,
                RedirectUrl = Url.Action("Details", "Licences", new { id = licenceId })
            };
        }
        catch (ConcurrencyException)
        {
            return new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = "Another conflict occurred. Please try again."
            };
        }
    }

    private async Task<ConflictResolutionResult> ResolveCustomerConflictAsync(
        Guid complianceExtensionId,
        string resolution,
        Dictionary<string, string>? fieldChoices)
    {
        // Look up customer by ComplianceExtensionId (the Guid entity ID used in conflict resolution)
        var allCustomers = await _customerService.GetAllAsync();
        var customer = allCustomers.FirstOrDefault(c => c.ComplianceExtensionId == complianceExtensionId);

        if (resolution == "database")
        {
            return new ConflictResolutionResult
            {
                Success = true,
                RedirectUrl = customer != null
                    ? Url.Action("Details", "Customers", new { customerAccount = customer.CustomerAccount, dataAreaId = customer.DataAreaId })
                    : Url.Action("Index", "Customers")
            };
        }

        var userValues = TempData.Get<Dictionary<string, object?>>("UserValues_" + complianceExtensionId);

        if (userValues == null)
        {
            return new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = "User values not found. Please make your changes again."
            };
        }

        try
        {
            if (customer == null)
            {
                return new ConflictResolutionResult
                {
                    Success = false,
                    ErrorMessage = "Customer not found."
                };
            }

            if (fieldChoices != null)
            {
                foreach (var choice in fieldChoices)
                {
                    if (choice.Value == "user" && userValues.TryGetValue(choice.Key, out var userValue))
                    {
                        ApplyPropertyValue(customer, choice.Key, userValue);
                    }
                }
            }
            else if (resolution == "user")
            {
                foreach (var kvp in userValues)
                {
                    ApplyPropertyValue(customer, kvp.Key, kvp.Value);
                }
            }

            await _customerService.UpdateComplianceAsync(customer);

            return new ConflictResolutionResult
            {
                Success = true,
                RedirectUrl = Url.Action("Details", "Customers", new { customerAccount = customer.CustomerAccount, dataAreaId = customer.DataAreaId })
            };
        }
        catch (ConcurrencyException)
        {
            return new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = "Another conflict occurred. Please try again."
            };
        }
    }

    private static void ApplyPropertyValue(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(target, convertedValue);
            }
            catch
            {
                // Silently skip if conversion fails
            }
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value.GetType() == underlyingType)
            return value;

        return Convert.ChangeType(value, underlyingType);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("oid")?.Value ??
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// Result of a conflict resolution attempt.
/// </summary>
public class ConflictResolutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// TempData extension methods for storing complex objects.
/// </summary>
public static class TempDataExtensions
{
    public static void Set<T>(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, string key, T value)
    {
        tempData[key] = JsonSerializer.Serialize(value);
    }

    public static T? Get<T>(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, string key)
    {
        if (tempData.TryGetValue(key, out var value) && value is string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        return default;
    }
}
