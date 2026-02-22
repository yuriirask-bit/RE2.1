using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using RE2.ComplianceApi.Authentication;
using RE2.ComplianceApi.Authorization;
using RE2.ComplianceApi.HealthChecks;
using RE2.ComplianceApi.Middleware;
using RE2.DataAccess.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Check if we're using in-memory mode (local development without Azure AD)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryRepositories");

if (useInMemory && builder.Environment.IsDevelopment())
{
    // Development mode: Use auto-authenticating handler (no Azure AD required)
    builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(
            DevelopmentAuthHandler.SchemeName, _ => { });

    // T174: Register ActiveEmployeeHandler for authorization
    builder.Services.AddSingleton<IAuthorizationHandler, ActiveEmployeeHandler>();

    // Simple authorization that accepts the development scheme
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("InternalUsers", policy =>
            policy.RequireAuthenticatedUser());
        options.AddPolicy("ExternalUsers", policy =>
            policy.RequireAuthenticatedUser());
        options.AddPolicy("AnyUser", policy =>
            policy.RequireAuthenticatedUser());
        options.AddPolicy("ComplianceManager", policy =>
            policy.RequireRole("ComplianceManager"));
        options.AddPolicy("QAUser", policy =>
            policy.RequireRole("QAUser", "ComplianceManager"));
        options.AddPolicy("SalesAdmin", policy =>
            policy.RequireRole("SalesAdmin", "ComplianceManager"));

        // T174: Custom authorization policies for User Story 6
        options.AddPolicy("CanManageLicences", policy =>
            policy.RequireRole("ComplianceManager"));
        options.AddPolicy("InternalTenantOnly", policy =>
            policy.RequireAuthenticatedUser());
        options.AddPolicy("ActiveEmployeeOnly", policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ActiveEmployeeRequirement()));
    });
}
else
{
    // T021-T023: Configure Azure AD authentication with multiple schemes (internal and external users)
    // Per research.md section 6: Stateless JWT authentication with Azure AD + Azure AD B2C

    // Scheme 1: Azure AD for internal users (employees)
    // Default scheme must match the registered handler name ("AzureAd"), not the generic "Bearer"
    builder.Services.AddAuthentication("AzureAd")
        .AddMicrosoftIdentityWebApi(
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);
                options.TokenValidationParameters.ValidAudiences = new[]
                {
                    builder.Configuration["AzureAd:ClientId"]!,
                    $"api://{builder.Configuration["AzureAd:ClientId"]}"
                };
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
            },
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);
            },
            jwtBearerScheme: "AzureAd");

    // Scheme 2: Azure AD B2C for external users (customers, contractors)
    builder.Services.AddAuthentication()
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
            options =>
            {
                builder.Configuration.Bind("AzureAdB2C", options);
            },
            jwtBearerScheme: "AzureAdB2C");

    // T174: Register ActiveEmployeeHandler for authorization
    builder.Services.AddSingleton<IAuthorizationHandler, ActiveEmployeeHandler>();

    // T024: Configure authorization policies
    builder.Services.AddAuthorization(options =>
    {
        // Policy for internal users only (employees via Azure AD)
        options.AddPolicy("InternalUsers", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes("AzureAd"));

        // Policy for external users only (customers via Azure AD B2C)
        options.AddPolicy("ExternalUsers", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes("AzureAdB2C"));

        // Policy for any authenticated user (internal or external)
        options.AddPolicy("AnyUser", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes("AzureAd", "AzureAdB2C"));

        // Role-based policies (per data-model.md entity 28 User roles)
        options.AddPolicy("ComplianceManager", policy =>
            policy.RequireRole("ComplianceManager")
                  .AddAuthenticationSchemes("AzureAd")); // Only internal users

        options.AddPolicy("QAUser", policy =>
            policy.RequireRole("QAUser", "ComplianceManager")
                  .AddAuthenticationSchemes("AzureAd"));

        options.AddPolicy("SalesAdmin", policy =>
            policy.RequireRole("SalesAdmin", "ComplianceManager")
                  .AddAuthenticationSchemes("AzureAd"));

        // T174: Custom authorization policies for User Story 6
        options.AddPolicy("CanManageLicences", policy =>
            policy.RequireRole("ComplianceManager")
                  .AddAuthenticationSchemes("AzureAd")
                  .AddRequirements(new ActiveEmployeeRequirement()));

        options.AddPolicy("InternalTenantOnly", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes("AzureAd"));

        options.AddPolicy("ActiveEmployeeOnly", policy =>
            policy.RequireAuthenticatedUser()
                  .AddAuthenticationSchemes("AzureAd")
                  .AddRequirements(new ActiveEmployeeRequirement()));
    });
}

// T280: Configure distributed caching (Redis for production, in-memory for development)
var cachingConfig = builder.Configuration.GetSection("Caching");
if (cachingConfig.GetValue<bool>("UseInMemory"))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    var redisConnection = cachingConfig["RedisConnectionString"];
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = cachingConfig["KeyPrefix"] ?? "re2:";
        });
    }
    else
    {
        // Fallback to in-memory if no Redis connection configured
        builder.Services.AddDistributedMemoryCache();
    }
}

// T038: Register data services (uses in-memory for local dev, Dataverse for production)
// Set "UseInMemoryRepositories": true in appsettings.Development.json to test locally
builder.Services.AddComplianceDataServices(builder.Configuration);

// Only add D365 F&O and Blob Storage when not using in-memory mode
if (!builder.Configuration.GetValue<bool>("UseInMemoryRepositories"))
{
    builder.Services.AddD365FOServices(builder.Configuration);
    builder.Services.AddBlobStorageServices(builder.Configuration);
}

// T041: Configure API versioning per research.md section 5 (URL path versioning)
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

// T046: Configure Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// T047g: Register health checks for external service connectivity per FR-056
builder.Services.AddSingleton<ServiceHealthStatus>();

var healthChecksBuilder = builder.Services.AddHealthChecks();

if (!builder.Configuration.GetValue<bool>("UseInMemoryRepositories"))
{
    healthChecksBuilder
        .AddCheck<DataverseHealthCheck>("dataverse", tags: new[] { "ready", "external" })
        .AddCheck<D365FoHealthCheck>("d365fo", tags: new[] { "ready", "external" })
        .AddCheck<BlobStorageHealthCheck>("blobstorage", tags: new[] { "ready", "external" });
}

// T047g: Publish health check results to ServiceHealthStatus for use by degradation middleware
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Period = TimeSpan.FromSeconds(30);
    options.Delay = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();

// T180: Configure API rate limiting per FR-063
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per client IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Named policy for transaction validation endpoints (more restrictive)
    options.AddFixedWindowLimiter("TransactionValidation", limiterOptions =>
    {
        limiterOptions.PermitLimit = 50;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Named policy for workflow trigger endpoints (most restrictive)
    options.AddFixedWindowLimiter("WorkflowTrigger", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Rate limit exceeded for {IP}", context.HttpContext.Connection.RemoteIpAddress);
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            errorCode = "RATE_LIMIT_EXCEEDED",
            message = "Too many requests. Please try again later."
        }, cancellationToken);
    };
});

// T289: Register FluentValidation validators for all request DTOs
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// Add controllers
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();

// T042-T043: Configure Swagger with API versioning and OAuth2 security
builder.Services.AddSwaggerGen(options =>
{
    // API v1 documentation
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RE2 Compliance Management API",
        Version = "v1",
        Description = "API for controlled drug licence and GDP compliance management - Version 1",
        Contact = new OpenApiContact
        {
            Name = "RE2 Development Team",
            Email = "dev@re2.com"
        }
    });

    // Add JWT bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Also include ComplianceCore XML for model descriptions
    var coreXmlFile = "RE2.ComplianceCore.xml";
    var coreXmlPath = Path.Combine(AppContext.BaseDirectory, coreXmlFile);
    if (File.Exists(coreXmlPath))
    {
        options.IncludeXmlComments(coreXmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline

// T044: Add error handling middleware (must be first to catch all exceptions)
app.UseMiddleware<ErrorHandlingMiddleware>();

// T045: Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// T047h: Graceful degradation middleware per FR-054/FR-055
// Returns 503 for non-critical endpoints when external services are unavailable
app.UseMiddleware<GracefulDegradationMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RE2 Compliance API v1");
        options.DocumentTitle = "RE2 Compliance API Documentation";
    });
}

app.UseHttpsRedirection();

// Authentication and Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// T180: Rate limiting middleware per FR-063
app.UseRateLimiter();

app.MapControllers();

// T047g: Map health check endpoints per FR-056
// /health - liveness probe (always returns 200 if app is running)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false, // No checks - just confirms app is running
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

// /ready - readiness probe (checks external service connectivity)
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.Run();
