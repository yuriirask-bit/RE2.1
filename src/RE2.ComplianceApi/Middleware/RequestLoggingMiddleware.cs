using System.Diagnostics;

namespace RE2.ComplianceApi.Middleware;

/// <summary>
/// Request logging middleware that logs HTTP requests and responses with timing information.
/// T045: Request logging middleware implementation.
/// Integrates with Application Insights for structured logging.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        // Log incoming request
        LogRequest(context, requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);
        }
    }

    private void LogRequest(HttpContext context, string requestId)
    {
        var request = context.Request;

        _logger.LogInformation(
            "HTTP {Method} {Path} started. RequestId: {RequestId}, User: {User}",
            request.Method,
            request.Path,
            requestId,
            context.User.Identity?.Name ?? "Anonymous");
    }

    private void LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        var request = context.Request;
        var response = context.Response;

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. RequestId: {RequestId}",
            request.Method,
            request.Path,
            response.StatusCode,
            elapsedMs,
            requestId);
    }
}
