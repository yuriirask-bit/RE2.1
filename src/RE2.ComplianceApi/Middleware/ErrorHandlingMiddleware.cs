using System.Net;
using System.Text.Json;
using RE2.Shared.Constants;

namespace RE2.ComplianceApi.Middleware;

/// <summary>
/// Global error handling middleware that captures exceptions and returns standardized error responses.
/// Per FR-064: Returns standardized error codes and messages in API responses.
/// T044: Error handling middleware implementation.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = MapExceptionToErrorResponse(exception);

        var response = new ErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Include stack trace in development environment only
        if (_environment.IsDevelopment() && exception.StackTrace != null)
        {
            response.Details = exception.StackTrace;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private (HttpStatusCode statusCode, string errorCode, string message) MapExceptionToErrorResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException or ArgumentException =>
                (HttpStatusCode.BadRequest, ErrorCodes.VALIDATION_ERROR, "Invalid request: " + exception.Message),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, ErrorCodes.UNAUTHORIZED, "Authentication required"),

            InvalidOperationException when exception.Message.Contains("not found") =>
                (HttpStatusCode.NotFound, ErrorCodes.NOT_FOUND, exception.Message),

            InvalidOperationException =>
                (HttpStatusCode.BadRequest, ErrorCodes.VALIDATION_ERROR, exception.Message),

            TimeoutException =>
                (HttpStatusCode.GatewayTimeout, ErrorCodes.EXTERNAL_SYSTEM_UNAVAILABLE, "Operation timed out"),

            HttpRequestException =>
                (HttpStatusCode.ServiceUnavailable, ErrorCodes.EXTERNAL_SYSTEM_UNAVAILABLE, "External service unavailable"),

            _ =>
                (HttpStatusCode.InternalServerError, ErrorCodes.INTERNAL_ERROR, "An internal error occurred")
        };
    }
}

/// <summary>
/// Standardized error response model per FR-064.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Standardized error code (e.g., "VALIDATION_ERROR", "LICENCE_EXPIRED").
    /// </summary>
    public required string ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Request trace identifier for debugging.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Additional error details (only included in development environment).
    /// </summary>
    public string? Details { get; set; }
}
