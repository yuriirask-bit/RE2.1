using System.Text.Json;
using RE2.ComplianceApi.HealthChecks;
using RE2.Shared.Constants;

namespace RE2.ComplianceApi.Middleware;

/// <summary>
/// Middleware that returns HTTP 503 with Retry-After header for non-critical endpoints
/// when dependent external services (Dataverse, D365 F&O) are unavailable.
/// T047h: Graceful degradation per FR-054/FR-055.
///
/// Critical endpoints (always attempted, even during degradation):
///   - POST /api/v{n}/transactions/validate (FR-018)
///   - GET  /api/v{n}/customers/{id}/compliance-status (FR-060)
///   - POST /api/v{n}/warehouse/operations/validate (FR-023)
///   - /health, /ready (health check endpoints)
///
/// Non-critical endpoints (return 503 during degradation):
///   - /api/v{n}/reports/* (FR-026)
///   - /api/v{n}/dashboard/*
///   - /api/v{n}/inspections/*
///   - /api/v{n}/gdp-sops/*
///   - /api/v{n}/training/*
///   - /api/v{n}/change-control/*
///   - All other non-critical API paths
/// </summary>
public class GracefulDegradationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GracefulDegradationMiddleware> _logger;

    /// <summary>
    /// Retry-After header value in seconds (5 minutes per plan.md degradation strategy).
    /// </summary>
    private const int RetryAfterSeconds = 300;

    /// <summary>
    /// Path prefixes for critical endpoints that must always be attempted.
    /// Uses ordinal case-insensitive comparison.
    /// </summary>
    private static readonly string[] CriticalPathPrefixes =
    {
        "/api/v",  // Will be further matched below
        "/health",
        "/ready"
    };

    /// <summary>
    /// API path segments that identify critical endpoints within /api/v{n}/.
    /// These are attempted even when services are degraded.
    /// </summary>
    private static readonly string[] CriticalApiSegments =
    {
        "/transactions/validate",
        "/compliance-status",
        "/warehouse/operations/validate"
    };

    /// <summary>
    /// API path segments that identify non-critical endpoints.
    /// These return 503 when core services are unavailable.
    /// </summary>
    private static readonly string[] NonCriticalApiSegments =
    {
        "/reports",
        "/dashboard",
        "/inspections",
        "/gdp-sops",
        "/training",
        "/change-control",
        "/gdp-equipment",
        "/gdp-operations",
        "/workflows"
    };

    public GracefulDegradationMiddleware(RequestDelegate next, ILogger<GracefulDegradationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ServiceHealthStatus serviceHealthStatus)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Health/ready endpoints always pass through
        if (IsHealthEndpoint(path))
        {
            await _next(context);
            return;
        }

        // If all core services are healthy, pass through
        if (serviceHealthStatus.CoreServicesHealthy)
        {
            await _next(context);
            return;
        }

        // Core services are degraded - determine if this is a critical or non-critical endpoint
        if (IsCriticalEndpoint(path))
        {
            // Critical endpoints are always attempted; errors handled by ErrorHandlingMiddleware
            _logger.LogWarning(
                "Serving critical endpoint {Path} during degraded state. Dataverse: {Dataverse}, D365: {D365}",
                path, serviceHealthStatus.IsDataverseHealthy, serviceHealthStatus.IsD365FoHealthy);
            await _next(context);
            return;
        }

        // Non-critical endpoint during degraded state → return 503
        _logger.LogWarning(
            "Returning 503 for non-critical endpoint {Path} during degraded state. Dataverse: {Dataverse}, D365: {D365}",
            path, serviceHealthStatus.IsDataverseHealthy, serviceHealthStatus.IsD365FoHealthy);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
        context.Response.ContentType = "application/json";

        var response = new
        {
            errorCode = ErrorCodes.EXTERNAL_SYSTEM_UNAVAILABLE,
            message = "Service temporarily unavailable. Non-critical features are degraded while external systems recover.",
            retryAfterSeconds = RetryAfterSeconds,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private static bool IsHealthEndpoint(string path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/ready", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCriticalEndpoint(string path)
    {
        // Check if the path contains any critical API segments
        foreach (var segment in CriticalApiSegments)
        {
            if (path.Contains(segment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // If it's an API path but doesn't match non-critical segments,
        // treat it as critical by default (conservative approach)
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var segment in NonCriticalApiSegments)
            {
                if (path.Contains(segment, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Unknown API path → treat as critical (safe default)
            return true;
        }

        // Non-API paths (e.g., web UI) → treat as non-critical
        return false;
    }
}
