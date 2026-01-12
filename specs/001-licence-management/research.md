# .NET 8 Azure Best Practices Research Document

**Project**: RE2 Licence Management System
**Target Platform**: Azure (App Service + Functions)
**Framework**: .NET 8
**Date**: 2026-01-09
**Purpose**: Technical decision documentation for architecture and integration patterns

---

## Table of Contents

1. [Dataverse Integration](#1-dataverse-integration)
2. [D365 F&O OData Integration](#2-d365-fo-odata-integration)
3. [Azure App Service + Functions Architecture](#3-azure-app-service--functions-architecture)
4. [Resilience Patterns](#4-resilience-patterns)
5. [API Versioning in ASP.NET Core](#5-api-versioning-in-aspnet-core)
6. [Stateless Authentication](#6-stateless-authentication)
7. [References](#7-references)

---

## 1. Dataverse Integration

### Decision

**Recommended Approach**: Use `Microsoft.PowerPlatform.Dataverse.Client` (v1.2.10+) with Managed Identity authentication via `Azure.Identity.DefaultAzureCredential`.

### Rationale

- **99.5% Uptime Requirement**: Built-in retry logic with exponential backoff handles Dataverse service protection API limits automatically
- **Security**: Managed Identity eliminates credential management overhead and secret rotation risks
- **Native Support**: ServiceClient includes integrated throttling handling and respects Retry-After headers
- **Modern Authentication**: Uses Azure Identity SDK which provides seamless local development (Visual Studio credentials) and production (Managed Identity) experience

### Authentication Options Comparison

#### Option A: Managed Identity (RECOMMENDED)

**Pros:**
- Zero credential management - no secrets, no rotation
- Automatic credential discovery in Azure environments
- Seamless local development with Visual Studio/VS Code credentials
- Tied to resource lifecycle - deleted with resource

**Cons:**
- Only works for Azure-hosted resources
- Requires Application User setup in Dataverse

**Implementation:**
```csharp
using Azure.Identity;
using Microsoft.PowerPlatform.Dataverse.Client;

var credential = new DefaultAzureCredential();
var dataverseUrl = "https://yourorg.crm.dynamics.com";

var serviceClient = new ServiceClient(
    instanceUrl: new Uri(dataverseUrl),
    tokenProviderFunction: async (uri) =>
    {
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(new[] { $"{uri}/.default" }));
        return token.Token;
    },
    useUniqueInstance: true,
    logger: logger
);
```

#### Option B: Service Principal with Client Secret

**Pros:**
- Works outside Azure environments
- Can be shared across multiple applications
- Easier to test in CI/CD pipelines

**Cons:**
- Requires credential storage (Azure Key Vault recommended)
- Manual secret rotation required (typically 1-2 year expiry)
- Higher security risk if secrets are compromised

**When to Use:**
- On-premises applications
- CI/CD automation scenarios
- Multi-tenant scenarios across subscriptions

### Connection Management

#### Best Practices

1. **ServiceClient Lifecycle**: Use singleton pattern - ServiceClient is thread-safe and expensive to create
2. **Connection Pooling**: ServiceClient handles this internally
3. **Dispose Pattern**: Always dispose ServiceClient when application shuts down
4. **MaxConnectionTimeout**: Configure via ServiceClient properties

**Dependency Injection Setup:**
```csharp
// Program.cs
builder.Services.AddSingleton<IOrganizationServiceAsync>(sp =>
{
    var credential = new DefaultAzureCredential();
    var dataverseUrl = builder.Configuration["Dataverse:Url"];
    var logger = sp.GetRequiredService<ILogger<ServiceClient>>();

    return new ServiceClient(
        instanceUrl: new Uri(dataverseUrl),
        tokenProviderFunction: async (uri) =>
        {
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(new[] { $"{uri}/.default" }));
            return token.Token;
        },
        useUniqueInstance: true,
        logger: logger
    );
});
```

### Retry Policies

#### Built-in Retry Handling

ServiceClient includes sophisticated retry logic:
- **Throttling Handling**: Automatically respects Retry-After headers for burst, time, and concurrency throttling
- **Exponential Backoff**: Formula: `RetryPauseTime + TimeSpan.FromSeconds(Math.Pow(2, retryCount))`
- **Configurable**: `RetryPauseTime` property (default: 1 second)

**Configuration:**
```csharp
serviceClient.RetryPauseTime = TimeSpan.FromSeconds(2);
serviceClient.UseExponentialRetryDelayForConcurrencyThrottle = true; // v1.2.9+
```

#### Service Protection Limits

Dataverse enforces API limits to ensure service quality:
- **Per user entitlement limits**: 6,000 API requests per 5 minutes per user
- **Per organization limits**: 60,000 requests per 5 minutes per organization
- **Concurrent requests**: Maximum concurrent API requests per organization

**Error Handling Pattern:**
```csharp
try
{
    var response = await serviceClient.ExecuteAsync(request);
    return response;
}
catch (DataverseOperationException ex) when (ex.ErrorCode == -2147204344) // Throttling
{
    // ServiceClient retry logic should handle this automatically
    // Log and monitor throttling occurrences
    logger.LogWarning("Dataverse throttling occurred: {Message}", ex.Message);
    throw;
}
catch (DataverseOperationException ex)
{
    logger.LogError(ex, "Dataverse operation failed: {ErrorCode}", ex.ErrorCode);
    throw;
}
```

### Error Handling Patterns

#### Common Error Codes

- **-2147204344**: Throttling (429 Too Many Requests)
- **-2147220969**: Record not found
- **-2147220937**: Duplicate record
- **-2147220891**: Missing required field
- **-2147220970**: Permission denied

#### Recommended Pattern

```csharp
public class DataverseService
{
    private readonly IOrganizationServiceAsync _service;
    private readonly ILogger<DataverseService> _logger;

    public async Task<Entity> RetrieveWithRetryAsync(
        string entityName,
        Guid id,
        ColumnSet columnSet,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _service.RetrieveAsync(entityName, id, columnSet, cancellationToken);
        }
        catch (DataverseOperationException ex) when (IsTransientError(ex))
        {
            _logger.LogWarning(ex, "Transient error in Dataverse operation");
            throw; // Let ServiceClient retry logic handle
        }
        catch (DataverseOperationException ex)
        {
            _logger.LogError(ex, "Non-transient Dataverse error: {ErrorCode}", ex.ErrorCode);
            throw new DataAccessException("Failed to retrieve entity", ex);
        }
    }

    private static bool IsTransientError(DataverseOperationException ex)
    {
        // Throttling and network errors are transient
        return ex.ErrorCode == -2147204344 ||
               ex.InnerException is HttpRequestException;
    }
}
```

### Implementation Notes

#### Key NuGet Packages
- `Microsoft.PowerPlatform.Dataverse.Client` (1.2.10+) - requires .NET 8.0
- `Azure.Identity` (1.12.0+) - for Managed Identity authentication

#### Gotchas

1. **Scope Format**: Token scope must end with `/.default` (e.g., `https://yourorg.crm.dynamics.com/.default`)
2. **Application User Setup**: Managed Identity requires corresponding Application User in Dataverse with appropriate security roles
3. **Local Development**: DefaultAzureCredential falls back to Visual Studio credentials - ensure signed in
4. **Cancellation Token**: As of v1.2.7, CancellationToken properly cancels retry delays
5. **Unique Instance**: Set `useUniqueInstance: true` to avoid connection pooling issues with long-running services

#### Monitoring Recommendations

- Track throttling occurrences via Application Insights
- Monitor ServiceClient performance metrics
- Set up alerts for error rate thresholds (>0.5% for 99.5% uptime)
- Log Retry-After header values to understand throttling patterns

---

## 2. D365 F&O OData Integration

### Decision

**Recommended Approach**: Use `Simple.OData.Client` (v6.x) or direct `HttpClient` with custom OData v4 query building for D365 Finance & Operations virtual data entities. Authenticate via OAuth2 Client Credentials flow with Azure AD service principal or managed identity.

### Rationale

- **Standard Protocol**: OData v4 is the native integration method for D365 F&O
- **Batch Support**: Enables atomic multi-operation transactions via $batch endpoint
- **Query Flexibility**: Full OData query capabilities ($select, $filter, $expand, $top, $skip)
- **Real-time Integration**: Suitable for CRUD operations and reading status information
- **Limitation Awareness**: Not suitable for large data migrations - use Data Management Framework (DMF) instead

### Authentication

#### OAuth2 Client Credentials Flow

D365 F&O uses Azure AD OAuth2 authentication:

**Configuration:**
```json
{
  "D365FO": {
    "Authority": "https://login.microsoftonline.com/{tenantId}",
    "ClientId": "{app-registration-client-id}",
    "ClientSecret": "{stored-in-keyvault}",
    "Resource": "https://{environment}.operations.dynamics.com",
    "ODataEndpoint": "https://{environment}.operations.dynamics.com/data"
  }
}
```

**Implementation with Managed Identity (RECOMMENDED):**
```csharp
using Azure.Identity;
using Azure.Core;

public class D365FOAuthenticationService
{
    private readonly TokenCredential _credential;
    private readonly string _resource;

    public D365FOAuthenticationService(IConfiguration configuration)
    {
        _credential = new DefaultAzureCredential();
        _resource = configuration["D365FO:Resource"];
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tokenResult = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { $"{_resource}/.default" }),
            cancellationToken);

        return tokenResult.Token;
    }
}
```

**HttpClient Configuration:**
```csharp
builder.Services.AddHttpClient("D365FO", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["D365FO:ODataEndpoint"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<D365FOAuthenticationHandler>() // Add auth token to each request
.AddStandardResilienceHandler(); // Add retry/circuit breaker
```

### Batch Operations

#### $batch Endpoint

OData batch operations allow multiple operations in a single HTTP request:

**Benefits:**
- **Atomicity**: All operations in a changeset succeed or fail together
- **Performance**: Reduces network round trips
- **Throttling**: Counts as single API call for rate limiting

**Implementation:**
```csharp
public async Task<BatchResponse> ExecuteBatchAsync(
    List<ODataOperation> operations,
    CancellationToken cancellationToken)
{
    var batchId = $"batch_{Guid.NewGuid()}";
    var changesetId = $"changeset_{Guid.NewGuid()}";

    var batchContent = new MultipartContent("mixed", batchId);
    var changesetContent = new MultipartContent("mixed", changesetId);

    foreach (var operation in operations)
    {
        var httpContent = new HttpMessageContent(operation.ToHttpRequestMessage());
        changesetContent.Add(httpContent);
    }

    batchContent.Add(changesetContent);

    var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
    {
        Content = batchContent
    };

    var response = await _httpClient.SendAsync(request, cancellationToken);
    return await ParseBatchResponseAsync(response);
}
```

**Batch Best Practices:**
- Limit batch size to 100 operations per request
- Use changesets for related operations requiring atomicity
- Monitor batch execution time (typically slower than individual requests)
- Implement proper error parsing for batch responses

### Query Optimization

#### OData Query Options

**$select**: Retrieve only required fields
```http
GET /data/Products?$select=ProductNumber,Name,Price
```

**$filter**: Server-side filtering
```http
GET /data/Products?$filter=Price gt 100 and Status eq 'Active'
```

**$expand**: Include related entities
```http
GET /data/SalesOrders?$expand=OrderLines($select=ProductId,Quantity)
```

**$top and $skip**: Pagination
```http
GET /data/Products?$top=100&$skip=200
```

**$count**: Get total record count
```http
GET /data/Products/$count?$filter=Status eq 'Active'
```

#### Performance Optimization Strategies

1. **Use $select**: Always specify required fields to minimize payload
2. **Pagination**: Use $top with $skiptoken (preferred) or $skip for large datasets
3. **Avoid Deep Expands**: Limit $expand depth to 2 levels maximum
4. **Filter Server-Side**: Use $filter instead of client-side filtering
5. **Batch Small Operations**: Group creates/updates into batches

**Example Optimized Query:**
```csharp
public async Task<List<Product>> GetActiveProductsAsync(
    int pageSize = 100,
    string? skipToken = null)
{
    var queryBuilder = new StringBuilder($"Products");
    queryBuilder.Append("?$select=ProductNumber,Name,Price,Status");
    queryBuilder.Append("&$filter=Status eq 'Active'");
    queryBuilder.Append($"&$top={pageSize}");

    if (!string.IsNullOrEmpty(skipToken))
    {
        queryBuilder.Append($"&$skiptoken={Uri.EscapeDataString(skipToken)}");
    }

    var response = await _httpClient.GetAsync(queryBuilder.ToString());
    var odata = await response.Content.ReadFromJsonAsync<ODataResponse<Product>>();

    return odata.Value;
}
```

### Handling Long-Running Operations

#### Pattern: Polling for Completion

Some D365 F&O operations are asynchronous (e.g., DMF imports):

```csharp
public async Task<OperationResult> ExecuteLongRunningOperationAsync(
    string operationId,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();
    var pollingInterval = TimeSpan.FromSeconds(5);

    while (stopwatch.Elapsed < timeout)
    {
        var status = await CheckOperationStatusAsync(operationId, cancellationToken);

        if (status.IsCompleted)
        {
            return status;
        }

        if (status.IsFailed)
        {
            throw new OperationFailedException($"Operation {operationId} failed: {status.Error}");
        }

        await Task.Delay(pollingInterval, cancellationToken);

        // Exponential backoff for polling
        pollingInterval = TimeSpan.FromSeconds(Math.Min(pollingInterval.TotalSeconds * 1.5, 60));
    }

    throw new TimeoutException($"Operation {operationId} did not complete within {timeout}");
}
```

### Throttling Management

#### D365 F&O Throttling Limits

- **Priority-based throttling**: Different endpoints have different priorities
- **HTTP 429**: Too Many Requests response with Retry-After header
- **Service Protection**: Prevents system lockdowns from excessive API calls

**Handling Throttling:**
```csharp
public class D365FOThrottlingHandler : DelegatingHandler
{
    private readonly ILogger<D365FOThrottlingHandler> _logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);

            _logger.LogWarning(
                "D365 F&O throttling encountered. Retry after {RetryAfter} seconds",
                retryAfter.TotalSeconds);

            // Let Polly retry handler manage the retry
            return response;
        }

        return response;
    }
}
```

### Alternatives Considered

#### Option A: Custom .NET SDK

**Pros**: Type-safe entity access, IntelliSense support
**Cons**: Maintenance overhead, version coupling, limited flexibility

#### Option B: Data Management Framework (DMF)

**Pros**: Optimized for bulk data transfers, supports complex transformations
**Cons**: Not suitable for real-time operations, requires separate integration pattern

**Decision**: Use OData for real-time CRUD and status queries; use DMF for bulk imports/exports

### Implementation Notes

#### Key NuGet Packages
- `Simple.OData.Client` (6.x) - optional helper library
- `Azure.Identity` (1.12.0+) - for authentication
- `System.Net.Http.Json` (8.x) - for JSON serialization

#### Gotchas

1. **Date Handling**: OData dates use ISO 8601 format - ensure proper timezone handling
2. **Entity Keys**: Some entities use composite keys - requires special syntax
3. **Null Values**: OData returns null fields as absent properties, not explicit nulls
4. **Case Sensitivity**: Entity and field names are case-sensitive
5. **Slow Queries**: Complex queries with joins can be very slow - monitor performance
6. **Not for Bulk**: OData is NOT suitable for large data migrations (use DMF instead)

#### Performance Limits

- **Query Timeout**: Default 3 minutes server-side timeout
- **Result Set Size**: Recommended maximum 10,000 records per query
- **Batch Size**: Maximum 100 operations per batch request

---

## 3. Azure App Service + Functions Architecture

### Decision

**Recommended Approach**: Hybrid architecture with ASP.NET Core Web API (App Service) for synchronous operations and Azure Functions (Isolated Worker model) for event-driven and scheduled tasks. Share common code via class library projects with unified dependency injection configuration.

### Rationale

- **Separation of Concerns**: App Service handles HTTP APIs; Functions handle events, scheduled jobs, and async processing
- **Cost Optimization**: Functions scale to zero when idle; App Service provides always-on API availability
- **Development Experience**: Unified .NET 8 programming model across both services
- **Isolated Worker Model**: Better process isolation, support for middleware, and unified DI container
- **99.5% Uptime**: App Service with multiple instances + Functions with retry policies = high availability

### Project Structure

#### Recommended Solution Layout

```
src/
├── RE2.Web.Api/                    # ASP.NET Core Web API (App Service)
│   ├── Controllers/
│   ├── Program.cs
│   └── RE2.Web.Api.csproj
│
├── RE2.Functions/                  # Azure Functions (Isolated Worker)
│   ├── Functions/
│   │   ├── LicenceExpiryChecker.cs
│   │   ├── LicenceStatusProcessor.cs
│   │   └── WebhookHandler.cs
│   ├── Program.cs
│   └── RE2.Functions.csproj
│
├── RE2.Core/                       # Shared domain logic
│   ├── Domain/
│   │   ├── Entities/
│   │   └── ValueObjects/
│   ├── Interfaces/
│   └── RE2.Core.csproj
│
├── RE2.Infrastructure/             # Shared infrastructure
│   ├── Dataverse/
│   │   ├── DataverseService.cs
│   │   └── Repositories/
│   ├── D365FO/
│   │   └── ODataService.cs
│   ├── DependencyInjection/
│   │   └── InfrastructureExtensions.cs
│   └── RE2.Infrastructure.csproj
│
└── RE2.Shared/                     # Shared utilities
    ├── Configuration/
    ├── Models/
    └── RE2.Shared.csproj
```

### Shared Code Strategies

#### Option A: Extension Method Pattern (RECOMMENDED)

Create reusable DI configuration in shared library:

**RE2.Infrastructure/DependencyInjection/InfrastructureExtensions.cs:**
```csharp
public static class InfrastructureExtensions
{
    public static IServiceCollection AddDataverseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IOrganizationServiceAsync>(sp =>
        {
            var credential = new DefaultAzureCredential();
            var url = configuration["Dataverse:Url"];
            var logger = sp.GetRequiredService<ILogger<ServiceClient>>();

            return new ServiceClient(
                instanceUrl: new Uri(url),
                tokenProviderFunction: async (uri) =>
                {
                    var token = await credential.GetTokenAsync(
                        new TokenRequestContext(new[] { $"{uri}/.default" }));
                    return token.Token;
                },
                useUniqueInstance: true,
                logger: logger
            );
        });

        services.AddScoped<ILicenceRepository, DataverseLicenceRepository>();

        return services;
    }

    public static IServiceCollection AddD365FOServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient("D365FO", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(config["D365FO:ODataEndpoint"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<D365FOAuthenticationHandler>()
        .AddStandardResilienceHandler();

        services.AddSingleton<ID365FOService, D365FOService>();

        return services;
    }
}
```

**Usage in App Service (Program.cs):**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add shared services
builder.Services.AddDataverseServices(builder.Configuration);
builder.Services.AddD365FOServices(builder.Configuration);

// Add API-specific services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Usage in Functions (Program.cs):**
```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add shared services
        services.AddDataverseServices(context.Configuration);
        services.AddD365FOServices(context.Configuration);

        // Add Function-specific services
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();
```

### Dependency Injection Across Projects

#### Constructor Injection in Functions

**Azure Functions (Isolated Worker):**
```csharp
public class LicenceExpiryChecker
{
    private readonly ILicenceRepository _licenceRepo;
    private readonly ILogger<LicenceExpiryChecker> _logger;

    public LicenceExpiryChecker(
        ILicenceRepository licenceRepo,
        ILogger<LicenceExpiryChecker> logger)
    {
        _licenceRepo = licenceRepo;
        _logger = logger;
    }

    [Function("CheckExpiredLicences")]
    public async Task RunAsync(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
        FunctionContext context)
    {
        _logger.LogInformation("Checking expired licences at {Time}", DateTime.UtcNow);

        var expiredLicences = await _licenceRepo.GetExpiredLicencesAsync();

        // Process expired licences
    }
}
```

#### Constructor Injection in Controllers

**ASP.NET Core API:**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class LicencesController : ControllerBase
{
    private readonly ILicenceRepository _licenceRepo;
    private readonly ILogger<LicencesController> _logger;

    public LicencesController(
        ILicenceRepository licenceRepo,
        ILogger<LicencesController> logger)
    {
        _licenceRepo = licenceRepo;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LicenceDto>> GetLicenceAsync(Guid id)
    {
        var licence = await _licenceRepo.GetByIdAsync(id);
        return Ok(licence);
    }
}
```

### Configuration Management

#### Unified Configuration Strategy

**Azure Configuration Hierarchy:**
1. **Local Development**: `appsettings.json`, `appsettings.Development.json`, User Secrets
2. **Azure Environment**: App Service Configuration / Function App Settings
3. **Secrets**: Azure Key Vault (injected via configuration provider)

**Configuration Setup with Key Vault:**
```csharp
// App Service Program.cs
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Functions Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        if (context.HostingEnvironment.IsProduction())
        {
            var builtConfig = config.Build();
            var keyVaultUrl = builtConfig["KeyVault:Url"];

            config.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());
        }
    })
    .Build();
```

**Configuration Structure (appsettings.json):**
```json
{
  "Dataverse": {
    "Url": "https://yourorg.crm.dynamics.com"
  },
  "D365FO": {
    "ODataEndpoint": "https://yourenv.operations.dynamics.com/data"
  },
  "KeyVault": {
    "Url": "https://your-keyvault.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

**Key Vault Naming Convention:**
```
Dataverse--ClientSecret
D365FO--ClientSecret
```
(Note: Key Vault uses `--` instead of `:` for hierarchical configuration)

### Communication Between App Service and Functions

#### Option A: Azure Storage Queue (RECOMMENDED for async operations)

**Producer (App Service):**
```csharp
public class LicenceController : ControllerBase
{
    private readonly QueueClient _queueClient;

    [HttpPost]
    public async Task<IActionResult> CreateLicenceAsync([FromBody] CreateLicenceRequest request)
    {
        // Create licence in Dataverse
        var licenceId = await _licenceRepo.CreateAsync(request);

        // Queue message for async processing
        var message = new LicenceCreatedMessage { LicenceId = licenceId };
        await _queueClient.SendMessageAsync(
            BinaryData.FromObjectAsJson(message));

        return CreatedAtAction(nameof(GetLicence), new { id = licenceId }, licenceId);
    }
}
```

**Consumer (Function):**
```csharp
public class LicenceProcessor
{
    [Function("ProcessNewLicence")]
    public async Task RunAsync(
        [QueueTrigger("licence-created")] LicenceCreatedMessage message,
        FunctionContext context)
    {
        // Process licence
        await _licenceService.ProcessNewLicenceAsync(message.LicenceId);
    }
}
```

#### Option B: Service Bus (for complex routing and pub/sub)

Use when multiple consumers need same message or advanced routing required.

#### Option C: Direct HTTP (for synchronous operations)

Use for immediate operations where Functions expose HTTP endpoints.

### Alternatives Considered

#### Monolithic App Service Only

**Pros**: Simpler deployment, single codebase
**Cons**: Cannot leverage serverless scaling, higher baseline costs, less separation

#### Functions Only

**Pros**: Maximum cost optimization, serverless benefits
**Cons**: Cold starts for API calls, limited HTTP features, complexity for simple APIs

#### Microservices with Container Apps

**Pros**: Maximum flexibility, independent scaling
**Cons**: Higher complexity, more infrastructure management, overkill for initial implementation

### Implementation Notes

#### Key NuGet Packages

**Shared:**
- `Microsoft.Extensions.DependencyInjection` (8.x)
- `Microsoft.Extensions.Configuration` (8.x)
- `Azure.Identity` (1.12.0+)
- `Azure.Extensions.AspNetCore.Configuration.Secrets` (1.3.0+)

**App Service Specific:**
- `Microsoft.AspNetCore.App` (8.x framework reference)

**Functions Specific:**
- `Microsoft.Azure.Functions.Worker` (1.21.0+)
- `Microsoft.Azure.Functions.Worker.Sdk` (1.17.0+)
- `Microsoft.Azure.Functions.Worker.Extensions.Http` (3.1.0+)
- `Microsoft.Azure.Functions.Worker.Extensions.Timer` (4.3.0+)
- `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` (6.0.0+)

#### Gotchas

1. **Isolated Worker Model Required**: In-process model for Functions ends support November 10, 2026
2. **Configuration Naming**: Azure Key Vault uses `--` instead of `:` for hierarchical keys
3. **Shared Project References**: Use `ProjectReference` not `PackageReference` for shared libraries
4. **Function Context**: Use `FunctionContext` not `ExecutionContext` in isolated worker
5. **Startup Registration**: No `Startup.cs` in .NET 8 - all configuration in `Program.cs`
6. **Cold Starts**: Use App Service Always On and Functions Premium plan to minimize for critical paths

#### Deployment Considerations

- **App Service**: Deploy as single package, use deployment slots for zero-downtime
- **Functions**: Can deploy individual function apps, supports slot swapping
- **Shared Libraries**: Automatically included in deployment packages
- **Configuration**: Use Azure DevOps or GitHub Actions with variable substitution

---

## 4. Resilience Patterns

### Decision

**Recommended Approach**: Use `Microsoft.Extensions.Http.Resilience` with `AddStandardResilienceHandler()` for HTTP-based integrations. This provides Microsoft's official implementation built on Polly v8 with recommended defaults for retry, circuit breaker, timeout, and rate limiting.

### Rationale

- **Official Microsoft Support**: Part of .NET 8 ecosystem with long-term support
- **Production-Ready Defaults**: Based on industry best practices and Azure service patterns
- **Integrated Monitoring**: Works seamlessly with Application Insights
- **99.5% Uptime Target**: Multiple resilience layers ensure high availability
- **Built on Polly v8**: Leverages mature, battle-tested resilience library

### Standard Resilience Handler

#### Overview

The standard resilience pipeline chains **five** resilience strategies:

1. **Rate Limiter** (Outermost): Limits concurrent requests
2. **Total Request Timeout**: Maximum time for entire operation including retries
3. **Retry**: Exponential backoff with jitter for transient failures
4. **Circuit Breaker**: Prevents cascading failures by breaking circuit when error threshold exceeded
5. **Attempt Timeout** (Innermost): Timeout for individual attempt

#### Configuration

**Basic Setup:**
```csharp
builder.Services.AddHttpClient("Dataverse")
    .AddStandardResilienceHandler()
    .ConfigureHttpClient((sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        client.BaseAddress = new Uri(config["Dataverse:Url"]);
    });
```

**Custom Configuration:**
```csharp
builder.Services.AddHttpClient("D365FO")
    .AddStandardResilienceHandler(options =>
    {
        // Retry Configuration
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true; // Recommended to prevent thundering herd
        options.Retry.Delay = TimeSpan.FromSeconds(1);

        // Circuit Breaker Configuration
        options.CircuitBreaker.FailureRatio = 0.3; // Break at 30% failure rate
        options.CircuitBreaker.MinimumThroughput = 10; // Minimum requests before breaking
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);

        // Timeout Configuration
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30); // Total including retries
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10); // Per attempt
    });
```

### Retry Patterns

#### Exponential Backoff with Jitter

**Why Jitter?** Prevents synchronized retry storms when multiple clients fail simultaneously.

**Formula:**
```
delay = baseDelay * (2 ^ attemptNumber) + random(0, jitterRange)
```

**Implementation:**
```csharp
services.AddHttpClient("ExternalApi")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromSeconds(2);

        // Retry on specific status codes
        options.Retry.ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout);

        // Honor Retry-After header (for 429 responses)
        options.Retry.DelayGenerator = args =>
        {
            if (args.Outcome.Result?.Headers?.RetryAfter?.Delta is TimeSpan retryAfter)
            {
                return new ValueTask<TimeSpan?>(retryAfter);
            }

            return new ValueTask<TimeSpan?>((TimeSpan?)null); // Use default exponential backoff
        };
    });
```

### Circuit Breaker Pattern

#### Purpose

Prevents cascading failures by "breaking the circuit" when failure rate exceeds threshold. Allows system to recover instead of overwhelming failing service.

**States:**
- **Closed**: Normal operation, requests pass through
- **Open**: Circuit broken, requests fail immediately without calling service
- **Half-Open**: Testing if service recovered, limited requests allowed

**Configuration:**
```csharp
services.AddHttpClient("UnreliableService")
    .AddStandardResilienceHandler(options =>
    {
        options.CircuitBreaker.FailureRatio = 0.5; // Break at 50% failure
        options.CircuitBreaker.MinimumThroughput = 20; // At least 20 requests before evaluation
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60); // Stay open for 60s
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2); // Evaluate over 2min window

        // Custom event handlers
        options.CircuitBreaker.OnOpened = args =>
        {
            // Log circuit breaker opened
            var logger = args.Context.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(
                "Circuit breaker opened for {Duration}s due to {FailureRate}% failure rate",
                args.BreakDuration.TotalSeconds,
                args.Context.Properties["FailureRate"]);

            return default;
        };

        options.CircuitBreaker.OnClosed = args =>
        {
            // Log circuit breaker closed (recovered)
            var logger = args.Context.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Circuit breaker closed - service recovered");

            return default;
        };
    });
```

### Timeout Patterns

#### Two-Level Timeout Strategy

**Attempt Timeout**: Timeout for single attempt
**Total Request Timeout**: Timeout for entire operation including retries

**Configuration:**
```csharp
options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10); // Single attempt
options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60); // All attempts + delays
```

**Calculation:**
```
MaxPossibleDuration = AttemptTimeout * (MaxRetries + 1) + SumOfDelays

Example:
- AttemptTimeout: 10s
- MaxRetries: 3
- Delays: 2s, 4s, 8s (exponential backoff)
- Total: 10*4 + 14 = 54s
- TotalRequestTimeout should be >= 60s
```

### Rate Limiting

#### Purpose

Prevents overwhelming external services and controls concurrent requests.

**Configuration:**
```csharp
services.AddHttpClient("RateLimitedApi")
    .AddStandardResilienceHandler(options =>
    {
        options.RateLimiter.RateLimitingMode = RateLimitingMode.Sliding;
        options.RateLimiter.PermitLimit = 100; // Max 100 requests
        options.RateLimiter.Window = TimeSpan.FromSeconds(10); // Per 10 seconds
        options.RateLimiter.QueueLimit = 50; // Queue up to 50 requests when limit reached
    });
```

### Custom Resilience Pipelines

#### For Non-HTTP Operations

Use `ResiliencePipelineBuilder` for database operations, message queue operations, etc.:

```csharp
public class DataverseService
{
    private readonly ResiliencePipeline _pipeline;

    public DataverseService()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.3,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public async Task<Entity> RetrieveEntityAsync(string entityName, Guid id)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            return await _service.RetrieveAsync(entityName, id, new ColumnSet(true), ct);
        });
    }
}
```

### Monitoring and Telemetry

#### Integration with Application Insights

**Automatic Metrics:**
- Retry attempts per endpoint
- Circuit breaker state changes
- Timeout occurrences
- Request duration percentiles

**Custom Telemetry:**
```csharp
services.AddHttpClient("MonitoredApi")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.OnRetry = args =>
        {
            var telemetry = args.Context.ServiceProvider.GetRequiredService<TelemetryClient>();

            telemetry.TrackEvent("ResilienceRetry", new Dictionary<string, string>
            {
                ["Endpoint"] = args.Context.Properties["Endpoint"]?.ToString(),
                ["AttemptNumber"] = args.AttemptNumber.ToString(),
                ["Exception"] = args.Outcome.Exception?.GetType().Name
            });

            return default;
        };
    });
```

### Alternatives Considered

#### Option A: Manual Polly Configuration

**Pros**: Maximum control, can use advanced Polly features
**Cons**: More boilerplate, must maintain configuration, easy to misconfigure

#### Option B: Azure API Management Policies

**Pros**: Centralized policy management, works for all client types
**Cons**: Additional Azure resource cost, external dependency, limited to HTTP

#### Option C: Application-Level Retry Logic

**Pros**: Simple to understand
**Cons**: Inconsistent implementation, no circuit breaker, error-prone

**Decision**: Standard Resilience Handler provides best balance of simplicity, features, and maintainability.

### Implementation Notes

#### Key NuGet Packages

- `Microsoft.Extensions.Http.Resilience` (10.1.0+) - Standard resilience handler
- `Microsoft.Extensions.Resilience` (10.1.0+) - Core resilience primitives
- `Polly.Core` (8.x) - Underlying resilience library
- `Polly.Extensions` (8.x) - Polly DI integration

#### Gotchas

1. **Order Matters**: Standard handler applies strategies in specific order - don't fight it
2. **Retry-After Header**: Handler automatically honors it for 429 responses
3. **Cancellation Token**: Always pass `CancellationToken` for proper timeout handling
4. **Circuit Breaker Isolation**: Each HttpClient gets its own circuit breaker instance
5. **Dynamic Configuration**: Configuration changes reload automatically via IOptionsMonitor
6. **Telemetry Overhead**: Extensive logging can impact performance - adjust levels appropriately

#### Best Practices for 99.5% Uptime

1. **Layer Defenses**: Use retry + circuit breaker + timeout together
2. **Monitor Circuit State**: Alert when circuit breakers open frequently
3. **Set Realistic Timeouts**: Based on P95/P99 latency measurements
4. **Use Jitter**: Always enable jitter to prevent thundering herd
5. **Test Resilience**: Use chaos engineering tools (Azure Chaos Studio) to validate
6. **Fallback Strategies**: Implement graceful degradation where possible

**Example Fallback Pattern:**
```csharp
public async Task<Product> GetProductAsync(string productId)
{
    try
    {
        return await _primaryHttpClient.GetFromJsonAsync<Product>($"products/{productId}");
    }
    catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
    {
        _logger.LogWarning(ex, "Primary service failed, trying cache");

        // Fallback to cache
        return await _cache.GetAsync<Product>($"product:{productId}");
    }
}
```

---

## 5. API Versioning in ASP.NET Core

### Decision

**Recommended Approach**: Use `Asp.Versioning.Mvc` package with **URL path versioning** as primary strategy, supplemented by header versioning for backward compatibility during transitions. Maintain active versions for minimum 6 months per FR-062 requirement.

### Rationale

- **FR-062 Compliance**: Support 6-month backward compatibility requirement
- **Clear Contract**: URL path versioning makes version explicit and easy to discover
- **Client Simplicity**: Easier for clients to understand and implement than header-based
- **Documentation**: Swagger/OpenAPI tools natively support path-based versioning
- **Gradual Deprecation**: Allows controlled migration from v1 to v2 over time
- **99.5% Uptime**: Prevents breaking changes from impacting availability

### Versioning Strategy

#### Semantic Versioning Approach

Use semantic versioning to indicate breaking vs non-breaking changes:

- **v1.0, v1.1, v1.2**: Minor versions (backward compatible)
- **v2.0**: Major version (breaking changes)

**Breaking Changes:**
- Removing endpoints or operations
- Changing required fields
- Modifying response structure
- Changing authentication requirements

**Non-Breaking Changes:**
- Adding new endpoints
- Adding optional fields to requests
- Adding new fields to responses
- Deprecating (but not removing) operations

### URL Path Versioning (Primary)

#### Configuration

**Program.cs:**
```csharp
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Adds api-supported-versions header

    // Support multiple versioning strategies
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(), // Primary: /api/v1/licences
        new HeaderApiVersionReader("api-version"), // Fallback: api-version: 1.0
        new QueryStringApiVersionReader("api-version") // Legacy: ?api-version=1.0
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Format: v1, v2
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();

// Swagger configuration for multiple versions
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RE2 Licence Management API",
        Version = "v1",
        Description = "Version 1 - Initial release"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "RE2 Licence Management API",
        Version = "v2",
        Description = "Version 2 - Enhanced features"
    });
});

var app = builder.Build();

// Configure Swagger UI for multiple versions
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
});

app.MapControllers();
app.Run();
```

#### Controller Implementation

**Version 1 Controller:**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class LicencesController : ControllerBase
{
    private readonly ILicenceService _licenceService;

    [HttpGet("{id}")]
    public async Task<ActionResult<LicenceV1Dto>> GetLicenceAsync(Guid id)
    {
        var licence = await _licenceService.GetByIdAsync(id);
        return Ok(MapToV1Dto(licence));
    }

    [HttpPost]
    public async Task<ActionResult<LicenceV1Dto>> CreateLicenceAsync(
        [FromBody] CreateLicenceV1Request request)
    {
        var licence = await _licenceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetLicence), new { id = licence.Id }, licence);
    }
}
```

**Version 2 Controller (with breaking changes):**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class LicencesController : ControllerBase
{
    private readonly ILicenceService _licenceService;

    [HttpGet("{id}")]
    public async Task<ActionResult<LicenceV2Dto>> GetLicenceAsync(Guid id)
    {
        var licence = await _licenceService.GetByIdAsync(id);
        return Ok(MapToV2Dto(licence)); // V2 has additional fields
    }

    [HttpPost]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<LicenceV2Dto>> CreateLicenceAsync(
        [FromBody] CreateLicenceV2Request request) // V2 has different request model
    {
        var licence = await _licenceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetLicence), new { id = licence.Id }, licence);
    }
}
```

### Maintaining Multiple Versions

#### Strategy 1: Separate Controllers (Recommended)

**Structure:**
```
Controllers/
├── V1/
│   ├── LicencesController.cs
│   └── CompaniesController.cs
└── V2/
    ├── LicencesController.cs
    └── CompaniesController.cs
```

**Benefits:**
- Clear separation of versions
- Easy to maintain and deprecate entire version
- No confusion about which code serves which version

#### Strategy 2: Shared Controller with Branching

**Use for minor changes only:**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class LicencesController : ControllerBase
{
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<LicenceV1Dto>> GetLicenceV1Async(Guid id)
    {
        var licence = await _licenceService.GetByIdAsync(id);
        return Ok(MapToV1Dto(licence));
    }

    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<LicenceV2Dto>> GetLicenceV2Async(Guid id)
    {
        var licence = await _licenceService.GetByIdAsync(id);
        return Ok(MapToV2Dto(licence));
    }
}
```

### Backward Compatibility Techniques

#### 1. Adding New Optional Fields

**Safe - Non-Breaking:**
```csharp
// V1 Response
public class LicenceV1Dto
{
    public Guid Id { get; set; }
    public string LicenceNumber { get; set; }
}

// V1.1 Response - Add optional field
public class LicenceV1Dto
{
    public Guid Id { get; set; }
    public string LicenceNumber { get; set; }
    public DateTime? ExpiryDate { get; set; } // New optional field
}
```

#### 2. Supporting Old Request Formats

**Use default values:**
```csharp
public class CreateLicenceRequest
{
    [Required]
    public string LicenceNumber { get; set; }

    // New field with default - maintains backward compatibility
    public string? Category { get; set; } = "Standard";
}
```

#### 3. Adapter Pattern for Model Evolution

```csharp
public class LicenceModelAdapter
{
    // Convert internal model to version-specific DTOs
    public static LicenceV1Dto ToV1Dto(Licence licence)
    {
        return new LicenceV1Dto
        {
            Id = licence.Id,
            LicenceNumber = licence.Number
        };
    }

    public static LicenceV2Dto ToV2Dto(Licence licence)
    {
        return new LicenceV2Dto
        {
            Id = licence.Id,
            Number = licence.Number, // Renamed field
            Status = licence.Status,
            ExpiryDate = licence.ExpiryDate,
            Categories = licence.Categories // New field
        };
    }
}
```

### Deprecation Process

#### 6-Month Timeline (FR-062)

**Month 0 (Launch v2.0):**
- Release v2.0 with new features
- v1.0 fully supported
- Documentation indicates v2.0 available

**Month 1:**
- Add deprecation warnings to v1.0 responses
- Update documentation with migration guide
- Send notifications to API consumers

**Month 3:**
- Begin tracking v1.0 usage metrics
- Reach out to high-volume v1.0 consumers
- Provide migration support

**Month 5:**
- Final warning: v1.0 sunset in 30 days
- Block new consumers from v1.0 (existing continue)

**Month 6:**
- Remove v1.0 endpoints
- Return HTTP 410 Gone for v1.0 requests

#### Implementation

**Add Deprecation Header:**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0", Deprecated = true)] // Mark as deprecated
[ApiVersion("2.0")]
public class LicencesController : ControllerBase
{
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<LicenceV1Dto>> GetLicenceV1Async(Guid id)
    {
        // Add deprecation warning header
        Response.Headers.Add("X-API-Deprecated", "true");
        Response.Headers.Add("X-API-Sunset-Date", "2026-07-01");
        Response.Headers.Add("Link", "</api/v2/licences>; rel=\"successor-version\"");

        var licence = await _licenceService.GetByIdAsync(id);
        return Ok(MapToV1Dto(licence));
    }
}
```

**Monitor Deprecated Version Usage:**
```csharp
public class ApiVersioningMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TelemetryClient _telemetry;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var apiVersion = context.GetRequestedApiVersion();
        if (apiVersion != null)
        {
            _telemetry.TrackEvent("ApiVersionUsage", new Dictionary<string, string>
            {
                ["Version"] = apiVersion.ToString(),
                ["Endpoint"] = context.Request.Path,
                ["IsDeprecated"] = apiVersion.MajorVersion < 2 ? "true" : "false"
            });
        }
    }
}
```

### Alternatives Considered

#### Option A: Header-Only Versioning

**Pros**: Clean URLs, flexible versioning
**Cons**: Less discoverable, harder to test, clients must remember to send header

**Example:**
```http
GET /api/licences/123
api-version: 1.0
```

#### Option B: Query String Versioning

**Pros**: Easy to test in browser
**Cons**: Ugly URLs, caching issues, not RESTful

**Example:**
```http
GET /api/licences/123?api-version=1.0
```

#### Option C: Media Type Versioning

**Pros**: Most RESTful, version tied to content negotiation
**Cons**: Complex for clients, requires custom media types

**Example:**
```http
GET /api/licences/123
Accept: application/vnd.re2.licence.v1+json
```

**Decision**: URL path versioning provides best developer experience and clearest documentation.

### Implementation Notes

#### Key NuGet Packages

- `Asp.Versioning.Mvc` (8.x) - Core versioning support for ASP.NET Core
- `Asp.Versioning.Mvc.ApiExplorer` (8.x) - Swagger/OpenAPI integration

**Note**: `Microsoft.AspNetCore.Mvc.Versioning` is deprecated - use `Asp.Versioning.Mvc` instead.

#### Gotchas

1. **Route Template**: Must include `{version:apiVersion}` in route template
2. **Multiple Versions**: Use separate controllers for major versions
3. **Default Version**: Set `AssumeDefaultVersionWhenUnspecified = true` for unversioned requests
4. **Swagger**: Requires `AddApiExplorer` for proper OpenAPI documentation
5. **MapToApiVersion**: Use when same controller serves multiple versions
6. **Deprecated Attribute**: Set `Deprecated = true` in `[ApiVersion]` attribute

#### Testing Versioning

```csharp
[Fact]
public async Task GetLicence_V1_ReturnsV1Model()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/licences/123");
    var licence = await response.Content.ReadFromJsonAsync<LicenceV1Dto>();

    // Assert
    Assert.NotNull(licence);
    Assert.IsType<LicenceV1Dto>(licence);
}

[Fact]
public async Task GetLicence_V2_ReturnsV2Model()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v2/licences/123");
    var licence = await response.Content.ReadFromJsonAsync<LicenceV2Dto>();

    // Assert
    Assert.NotNull(licence);
    Assert.IsType<LicenceV2Dto>(licence);
    Assert.NotNull(licence.Categories); // V2-specific field
}
```

---

## 6. Stateless Authentication

### Decision

**Recommended Approach**: Use **Microsoft Entra ID (Azure AD)** with JWT bearer tokens for stateless authentication. Implement dual authentication schemes:
- **Scheme 1**: Azure AD SSO for internal users (employees)
- **Scheme 2**: Azure AD B2C for external users (contractors, customers) with local account support

### Rationale

- **Stateless**: JWT tokens enable horizontal scaling without session state
- **99.5% Uptime**: No dependency on session storage or sticky sessions
- **Security**: Industry-standard OAuth2/OpenID Connect protocols
- **SSO**: Seamless integration with Microsoft 365 for internal users
- **Flexibility**: B2C supports local accounts, social logins, and custom policies
- **Microsoft Identity Platform**: Unified authentication across Azure services

### Architecture Overview

```
                    ┌─────────────────────┐
                    │  Azure AD (Entra)   │
                    │  Internal Users SSO │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Azure AD B2C      │
                    │  External Users     │
                    │  Local Credentials  │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │   ASP.NET Core API  │
                    │  JWT Validation     │
                    │  Multiple Schemes   │
                    └─────────────────────┘
```

### Internal Users: Azure AD SSO

#### Configuration

**appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourcompany.com",
    "TenantId": "{your-tenant-id}",
    "ClientId": "{api-app-registration-client-id}",
    "Audience": "api://{api-app-registration-client-id}",
    "CallbackPath": "/signin-oidc"
  }
}
```

**Program.cs:**
```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Azure AD JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        options =>
        {
            builder.Configuration.Bind("AzureAd", options);
            options.TokenValidationParameters.ValidAudiences = new[]
            {
                builder.Configuration["AzureAd:ClientId"],
                $"api://{builder.Configuration["AzureAd:ClientId"]}"
            };
        },
        options =>
        {
            builder.Configuration.Bind("AzureAd", options);
        },
        jwtBearerScheme: "AzureAd"); // Named scheme

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

#### Controller Usage

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "AzureAd")] // Require Azure AD auth
public class InternalLicencesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetInternalLicences()
    {
        var userId = User.FindFirst("oid")?.Value; // Azure AD Object ID
        var userName = User.Identity?.Name;
        var roles = User.FindAll("roles").Select(c => c.Value);

        return Ok(new { userId, userName, roles });
    }
}
```

### External Users: Azure AD B2C

#### Configuration

**appsettings.json:**
```json
{
  "AzureAdB2C": {
    "Instance": "https://{your-tenant}.b2clogin.com/",
    "Domain": "{your-tenant}.onmicrosoft.com",
    "TenantId": "{b2c-tenant-id}",
    "ClientId": "{b2c-app-registration-client-id}",
    "SignUpSignInPolicyId": "B2C_1_signupsignin1",
    "ResetPasswordPolicyId": "B2C_1_reset",
    "EditProfilePolicyId": "B2C_1_edit_profile"
  }
}
```

**Program.cs:**
```csharp
// Add multiple authentication schemes
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        options =>
        {
            builder.Configuration.Bind("AzureAd", options);
            // Configure token validation
        },
        options => { builder.Configuration.Bind("AzureAd", options); },
        jwtBearerScheme: "AzureAd")
    .AddMicrosoftIdentityWebApi(
        options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options);
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.ValidIssuers = new[]
            {
                $"https://{builder.Configuration["AzureAdB2C:Domain"]}/{builder.Configuration["AzureAdB2C:TenantId"]}/v2.0/"
            };
        },
        options => { builder.Configuration.Bind("AzureAdB2C", options); },
        jwtBearerScheme: "AzureAdB2C"); // Named scheme

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Policy for internal users only
    options.AddPolicy("InternalUsers", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("AzureAd"));

    // Policy for external users only
    options.AddPolicy("ExternalUsers", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("AzureAdB2C"));

    // Policy for any authenticated user
    options.AddPolicy("AnyUser", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("AzureAd", "AzureAdB2C"));
});
```

#### Controller Usage for Mixed Auth

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "AnyUser")] // Accept both internal and external users
public class LicencesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetLicences()
    {
        var authScheme = User.Identity?.AuthenticationType;
        var isInternal = authScheme == "AzureAd";
        var isExternal = authScheme == "AzureAdB2C";

        // Different logic based on user type
        if (isInternal)
        {
            return Ok(GetAllLicences()); // Internal users see all
        }
        else
        {
            var userId = User.FindFirst("sub")?.Value;
            return Ok(GetUserLicences(userId)); // External users see only their licences
        }
    }

    [HttpPost]
    [Authorize(Policy = "InternalUsers")] // Only internal users can create
    public async Task<IActionResult> CreateLicence([FromBody] CreateLicenceRequest request)
    {
        var licence = await _licenceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetLicence), new { id = licence.Id }, licence);
    }
}
```

### JWT Token Validation

#### Token Validation Parameters

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("CustomScheme", options =>
    {
        options.Authority = "https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = "api://{clientId}";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5min clock skew

            NameClaimType = "name", // Map "name" claim to User.Identity.Name
            RoleClaimType = "roles" // Map "roles" claim to User.IsInRole()
        };

        // Event handlers
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });
```

### Role-Based Authorization

#### Azure AD App Roles

**App Registration Manifest (Azure Portal):**
```json
{
  "appRoles": [
    {
      "allowedMemberTypes": ["User"],
      "description": "Licence Administrators can manage all licences",
      "displayName": "Licence Administrator",
      "id": "a816142a-2e8e-46c4-9997-f984faccb625",
      "isEnabled": true,
      "lang": null,
      "origin": "Application",
      "value": "Licence.Admin"
    },
    {
      "allowedMemberTypes": ["User"],
      "description": "Licence Viewers can only read licences",
      "displayName": "Licence Viewer",
      "id": "b816142a-2e8e-46c4-9997-f984faccb626",
      "isEnabled": true,
      "lang": null,
      "origin": "Application",
      "value": "Licence.Viewer"
    }
  ]
}
```

**Controller Usage:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class LicencesController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Licence.Admin,Licence.Viewer")]
    public IActionResult GetLicences()
    {
        return Ok(GetAllLicences());
    }

    [HttpPost]
    [Authorize(Roles = "Licence.Admin")] // Only admins can create
    public async Task<IActionResult> CreateLicence([FromBody] CreateLicenceRequest request)
    {
        var licence = await _licenceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetLicence), new { id = licence.Id }, licence);
    }
}
```

### Claims-Based Authorization

#### Custom Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    // Policy: User must have specific claim
    options.AddPolicy("CanManageLicences", policy =>
        policy.RequireClaim("permissions", "Licences.Manage"));

    // Policy: User must be from specific tenant
    options.AddPolicy("InternalTenantOnly", policy =>
        policy.RequireClaim("tid", "{your-tenant-id}"));

    // Policy: Custom requirement
    options.AddPolicy("ActiveEmployeeOnly", policy =>
        policy.Requirements.Add(new ActiveEmployeeRequirement()));
});

// Custom authorization handler
public class ActiveEmployeeRequirement : IAuthorizationRequirement { }

public class ActiveEmployeeHandler : AuthorizationHandler<ActiveEmployeeRequirement>
{
    private readonly IEmployeeService _employeeService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveEmployeeRequirement requirement)
    {
        var userId = context.User.FindFirst("oid")?.Value;

        if (userId != null)
        {
            var isActive = await _employeeService.IsActiveEmployeeAsync(userId);

            if (isActive)
            {
                context.Succeed(requirement);
            }
        }
    }
}

// Register handler
builder.Services.AddSingleton<IAuthorizationHandler, ActiveEmployeeHandler>();
```

### Token Refresh Strategy

#### Client-Side Token Refresh

JWT tokens are short-lived (typically 1 hour). Clients should:

1. **Detect 401 Unauthorized**: Token expired
2. **Request New Token**: Use refresh token with identity provider
3. **Retry Request**: With new access token

**Example Client Implementation (JavaScript):**
```javascript
async function callApi(endpoint, accessToken) {
    const response = await fetch(endpoint, {
        headers: {
            'Authorization': `Bearer ${accessToken}`
        }
    });

    if (response.status === 401) {
        // Token expired, refresh it
        const newToken = await refreshAccessToken();

        // Retry with new token
        return await fetch(endpoint, {
            headers: {
                'Authorization': `Bearer ${newToken}`
            }
        });
    }

    return response;
}
```

#### Server-Side Considerations

- **No Server-Side Refresh**: API doesn't store or refresh tokens - stateless
- **Validate Every Request**: Token validation happens on each request
- **Short Token Lifetime**: Reduces risk of token compromise
- **Revocation**: Use token validation cache with short TTL for revocation checks

### Alternatives Considered

#### Option A: Cookie-Based Sessions

**Pros**: Simple, built-in ASP.NET Core support
**Cons**: Not stateless, requires sticky sessions, doesn't scale horizontally, CSRF concerns

#### Option B: API Keys

**Pros**: Simple for programmatic access
**Cons**: No user identity, hard to rotate, no expiration, not suitable for user authentication

#### Option C: Custom JWT Implementation

**Pros**: Full control over token format and claims
**Cons**: Security risks, maintenance burden, reinventing the wheel, no SSO

**Decision**: Azure AD with JWT provides industry-standard security and seamless integration.

### Implementation Notes

#### Key NuGet Packages

- `Microsoft.Identity.Web` (3.x) - Azure AD integration for ASP.NET Core
- `Microsoft.Identity.Web.MicrosoftGraph` (3.x) - Optional: Call Microsoft Graph
- `System.IdentityModel.Tokens.Jwt` (8.x) - JWT token handling

#### Gotchas

1. **Audience Validation**: Must configure both `ClientId` and `api://{ClientId}` as valid audiences
2. **Issuer Validation**: Azure AD issues tokens from multiple issuers (v1, v2 endpoints)
3. **Clock Skew**: Default 5 minutes - adjust if needed for tight expiration
4. **Claims Mapping**: Different claims between Azure AD and B2C (e.g., "oid" vs "sub")
5. **B2C Policy**: Must validate token was issued by correct user flow policy
6. **CORS**: Configure CORS properly for browser-based clients
7. **Swagger Auth**: Configure Swagger to send Authorization header for testing

#### Swagger Configuration for Auth

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"api://{clientId}/access_as_user", "Access API as user" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { $"api://{clientId}/access_as_user" }
        }
    });
});
```

#### Testing Authentication

```csharp
[Fact]
public async Task GetLicences_WithValidToken_ReturnsOk()
{
    // Arrange
    var token = await GetTestTokenAsync(); // Helper to get real token
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/v1/licences");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task GetLicences_WithoutToken_ReturnsUnauthorized()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/licences");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

#### Security Best Practices

1. **HTTPS Only**: Always enforce HTTPS in production
2. **Short Token Lifetime**: 1 hour or less for access tokens
3. **Validate Audience**: Prevent token reuse across applications
4. **Validate Issuer**: Ensure token issued by trusted authority
5. **Log Auth Failures**: Monitor for brute force attacks
6. **Rate Limiting**: Implement rate limiting on authentication endpoints
7. **Secure Key Storage**: Use Azure Key Vault for secrets
8. **Monitor Token Usage**: Track unusual patterns with Application Insights

---

## 7. References

### Official Microsoft Documentation

1. [ServiceClient Class - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.powerplatform.dataverse.client.serviceclient?view=dataverse-sdk-latest)
2. [Service Protection API Limits - Microsoft Learn](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/api-limits)
3. [D365 F&O Open Data Protocol (OData) - Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/data-entities/odata)
4. [D365 F&O Batch OData API - Microsoft Learn](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/sysadmin/batch-odata-api)
5. [Build Resilient HTTP Apps - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
6. [Building Resilient Cloud Services with .NET 8 - .NET Blog](https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/)
7. [Implement HTTP Call Retries with Polly - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)
8. [Azure Key Vault Configuration Provider - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0)
9. [Azure Functions .NET Class Library - Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library)

### NuGet Packages

10. [Microsoft.PowerPlatform.Dataverse.Client - NuGet](https://www.nuget.org/packages/Microsoft.PowerPlatform.Dataverse.Client/)
11. [Microsoft.Extensions.Http.Resilience - NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/)
12. [Microsoft.Identity.Web - NuGet](https://www.nuget.org/packages/Microsoft.Identity.Web/)
13. [Asp.Versioning.Mvc - NuGet](https://www.nuget.org/packages/Asp.Versioning.Mvc/)

### GitHub Repositories

14. [PowerPlatform-DataverseServiceClient - GitHub](https://github.com/microsoft/PowerPlatform-DataverseServiceClient)
15. [Polly - GitHub](https://github.com/App-vNext/Polly)

### Community Articles & Guides

16. [Implementing Versioning in ASP.NET Core Web API with .NET 8 - Medium](https://medium.com/@solomongetachew112/implementing-versioning-in-asp-net-core-web-api-with-net-8-a-comprehensive-guide-68c6d1981b88)
17. [API Versioning in ASP.NET Core with .NET 8 - Medium](https://medium.com/@omsingh1149/api-versioning-in-asp-net-core-with-net-8-a-practical-guide-07a2704b445e)
18. [API Versioning Best Practices in .NET 8 - C# Corner](https://www.c-sharpcorner.com/article/api-versioning-best-practices-in-net-8/)
19. [Designing Future-Proof APIs - C# Corner](https://www.c-sharpcorner.com/article/designing-future-proof-apis-versioning-and-backward-compatibility-strategies-in/)
20. [Securing ASP.NET Core with Azure B2C - Damien Bod](https://damienbod.com/2021/07/26/securing-asp-net-core-razor-pages-web-apis-with-azure-b2c-external-and-azure-ad-internal-identities/)
21. [Using ASP.NET Core with Azure Key Vault - Damien Bod](https://damienbod.com/2024/12/02/using-asp-net-core-with-azure-key-vault/)
22. [How to Integrate with D365 F&O - Sertan's Blog](https://devblog.sertanyaman.com/2020/08/21/how-to-integrate-with-d365-for-finance-and-operations/)
23. [Integration with OData D365 F&O and .NET - Medium](https://medium.com/@mateen462/integration-with-odata-d365f-o-and-net-12699f6fb7d1)
24. [D365 F&O OData for Integrating Applications - Sikich](https://www.sikich.com/insight/dynamics-365-finance-and-supply-chain-management-odata-for-integrating-applications/)
25. [Connecting to Dataverse from Function App using Managed Identity - Dreaming in CRM](https://dreamingincrm.com/2021/11/16/connecting-to-dataverse-from-function-app-using-managed-identity/)
26. [Use Azure Managed Identity with Dataverse - KingswaySoft](https://www.kingswaysoft.com/blog/2025/04/07/Use-Azure-Managed-Identity-Authentication-to-Secure-your-DataverseDynamics-CRM-Connections)

### Polly & Resilience

27. [Circuit Breaker Resilience Strategy - Polly Docs](https://www.pollydocs.org/strategies/circuit-breaker.html)
28. [Retry Resilience Strategy - Polly Docs](https://www.pollydocs.org/strategies/retry.html)
29. [How To Implement Retries with Polly - Anton Dev Tips](https://antondevtips.com/blog/how-to-implement-retries-and-resilience-patterns-with-polly-and-microsoft-resilience)
30. [Resilient .NET 8 Microservices with Polly - Medium](https://medium.com/simform-engineering/resilient-net-8-micro-services-with-polly-retry-circuit-breaker-timeout-fallback-and-more-4bb220464be3)

### Azure Functions & App Service

31. [Clean Architecture in Serverless Azure Function - Medium](https://medium.com/@yusufsarikaya023/clean-architecture-in-serverless-azure-function-713582c7dc9b)
32. [Mastering Function Chaining in .NET 8 Durable Functions - Medium](https://medium.com/@robertdennyson/mastering-the-function-chaining-pattern-in-net-8-durable-functions-a-comprehensive-guide-4cf4daeda821)
33. [Azure App Service Multi-tenant Considerations - Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/app-service)
34. [Exploring .NET 8 Functions in Isolated Mode - Medium](https://staslebedenko.medium.com/net-8-functions-in-isolated-mode-68e13a054358)

---

## Document Metadata

**Author**: Claude Sonnet 4.5 (AI Research Assistant)
**Review Status**: Draft - Requires Technical Review
**Last Updated**: 2026-01-09
**Target Audience**: Solution Architects, Lead Developers
**Related Documents**:
- `spec.md` - Feature specification
- `plan.md` - Implementation plan
- `constitution.md` - Project principles

**Recommended Next Steps:**
1. Review decisions with solution architect
2. Validate against Azure Well-Architected Framework
3. Create proof-of-concept for critical integrations (Dataverse, D365 F&O)
4. Document any deviations from these recommendations with rationale
5. Schedule architecture review session with development team
