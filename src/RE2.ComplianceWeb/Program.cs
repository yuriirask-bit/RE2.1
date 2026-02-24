using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using RE2.ComplianceWeb.Authentication;
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
        options.AddPolicy("ComplianceManager", policy =>
            policy.RequireRole("ComplianceManager"));
        options.AddPolicy("QAUser", policy =>
            policy.RequireRole("QAUser", "ComplianceManager"));
        options.AddPolicy("SalesAdmin", policy =>
            policy.RequireRole("SalesAdmin", "ComplianceManager"));
        options.AddPolicy("TrainingCoordinator", policy =>
            policy.RequireRole("TrainingCoordinator", "QAUser", "ComplianceManager"));
        // T169: Reports accessible to ComplianceManager and QAUser per FR-026, FR-029
        options.AddPolicy("ComplianceManagerOrQAUser", policy =>
            policy.RequireRole("ComplianceManager", "QAUser"));
    });
}
else
{
    // T025: Configure Azure AD authentication for ComplianceWeb (internal users only)
    // Per research.md section 6: Web UI uses OpenID Connect for browser-based authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            builder.Configuration.Bind("AzureAd", options);
        });

    // Set RoleClaimType on the actual OpenIdConnectOptions (not MicrosoftIdentityOptions)
    // so the OIDC handler maps the JWT "roles" claim correctly for RequireRole policies.
    builder.Services.PostConfigure<OpenIdConnectOptions>(
        OpenIdConnectDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "roles";

            // Diagnostic: log claims when OIDC token is validated
            var existingOnTokenValidated = options.Events?.OnTokenValidated;
            options.Events ??= new OpenIdConnectEvents();
            var previousHandler = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                if (previousHandler != null)
                    await previousHandler(context);

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDiagnostic");

                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity != null)
                {
                    logger.LogWarning(
                        "DIAG OnTokenValidated — RoleClaimType: {RoleClaimType}, NameClaimType: {NameClaimType}, AuthType: {AuthType}",
                        identity.RoleClaimType, identity.NameClaimType, identity.AuthenticationType);

                    foreach (var claim in identity.Claims)
                    {
                        logger.LogWarning("DIAG Claim — Type: {Type}, Value: {Value}", claim.Type, claim.Value);
                    }

                    logger.LogWarning(
                        "DIAG IsInRole checks — ComplianceManager: {CM}, SalesAdmin: {SA}",
                        context.Principal!.IsInRole("ComplianceManager"),
                        context.Principal!.IsInRole("SalesAdmin"));
                }
            };
        });

    // Point AccessDenied to our own view instead of the default MicrosoftIdentity path
    builder.Services.Configure<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.AccessDeniedPath = "/Home/AccessDenied");

    // Configure authorization policies (same as API for consistency)
    builder.Services.AddAuthorization(options =>
    {
        // Role-based policies (per data-model.md entity 28 User roles)
        options.AddPolicy("ComplianceManager", policy =>
            policy.RequireRole("ComplianceManager"));

        options.AddPolicy("QAUser", policy =>
            policy.RequireRole("QAUser", "ComplianceManager"));

        options.AddPolicy("SalesAdmin", policy =>
            policy.RequireRole("SalesAdmin", "ComplianceManager"));

        options.AddPolicy("TrainingCoordinator", policy =>
            policy.RequireRole("TrainingCoordinator", "QAUser", "ComplianceManager"));

        // T169: Reports accessible to ComplianceManager and QAUser per FR-026, FR-029
        options.AddPolicy("ComplianceManagerOrQAUser", policy =>
            policy.RequireRole("ComplianceManager", "QAUser"));
    });
}

// Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// T039: Register data services (uses in-memory for local dev, Dataverse for production)
// Set "UseInMemoryRepositories": true in appsettings.Development.json to test locally
builder.Services.AddComplianceDataServices(builder.Configuration);

// Only add Blob Storage when not using in-memory mode
// (D365 F&O is already registered by AddComplianceDataServices above)
if (!builder.Configuration.GetValue<bool>("UseInMemoryRepositories"))
{
    builder.Services.AddBlobStorageServices(builder.Configuration);
}

// Add services to the container.
if (useInMemory && builder.Environment.IsDevelopment())
{
    builder.Services.AddControllersWithViews();
}
else
{
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI(); // Adds AccountController for sign-in/sign-out
}

// Add session support for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Health checks — liveness (/health) and readiness (/ready)
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (!builder.Configuration.GetValue<bool>("UseInMemoryRepositories"))
{
    healthChecksBuilder
        .AddCheck<RE2.ComplianceWeb.HealthChecks.DataverseHealthCheck>(
            "dataverse", tags: new[] { "ready" })
        .AddCheck<RE2.ComplianceWeb.HealthChecks.D365FoHealthCheck>(
            "d365fo", tags: new[] { "ready" })
        .AddCheck<RE2.ComplianceWeb.HealthChecks.BlobStorageHealthCheck>(
            "blobstorage", tags: new[] { "ready" });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Authentication and Authorization middleware (order matters!)
app.UseAuthentication();

// Diagnostic middleware: log claims between authentication and authorization
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Customers/Configure") && context.User.Identity?.IsAuthenticated == true)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthDiagnostic");

        var identity = context.User.Identity as System.Security.Claims.ClaimsIdentity;
        if (identity != null)
        {
            logger.LogWarning(
                "DIAG PreAuth — Path: {Path}, RoleClaimType: {RoleClaimType}, NameClaimType: {NameClaimType}, AuthType: {AuthType}",
                context.Request.Path, identity.RoleClaimType, identity.NameClaimType, identity.AuthenticationType);

            foreach (var claim in identity.Claims)
            {
                logger.LogWarning("DIAG PreAuth Claim — Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }

            logger.LogWarning(
                "DIAG PreAuth IsInRole — ComplianceManager: {CM}, SalesAdmin: {SA}",
                context.User.IsInRole("ComplianceManager"),
                context.User.IsInRole("SalesAdmin"));
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness: always 200, no dependency checks
});
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
