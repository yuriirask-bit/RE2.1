using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using RE2.ComplianceApi.Authentication;
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
    });
}
else
{
    // T021-T023: Configure Azure AD authentication with multiple schemes (internal and external users)
    // Per research.md section 6: Stateless JWT authentication with Azure AD + Azure AD B2C

    // Scheme 1: Azure AD for internal users (employees)
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });
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
});

var app = builder.Build();

// Configure the HTTP request pipeline

// T044: Add error handling middleware (must be first to catch all exceptions)
app.UseMiddleware<ErrorHandlingMiddleware>();

// T045: Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
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

app.MapControllers();

app.Run();
