namespace RE2.ComplianceApi.Middleware;

/// <summary>
/// Global error handling middleware for consistent API error responses
/// T044: Error handling middleware
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // TODO: Implement structured error response per transaction-validation-api.yaml ErrorResponse schema
        // - Map exception types to appropriate HTTP status codes
        // - Return standardized error format with error code, message, timestamp
        // - Include correlation ID for tracking

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            error = new
            {
                code = "INTERNAL_ERROR",
                message = "An unexpected error occurred",
                timestamp = DateTime.UtcNow,
                // Don't expose exception details in production
            }
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
