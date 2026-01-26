using Microsoft.AspNetCore.Authentication;
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
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "roles";
        });

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

// T039: Register data services (uses in-memory for local dev, Dataverse for production)
// Set "UseInMemoryRepositories": true in appsettings.Development.json to test locally
builder.Services.AddComplianceDataServices(builder.Configuration);

// Only add D365 F&O and Blob Storage when not using in-memory mode
if (!builder.Configuration.GetValue<bool>("UseInMemoryRepositories"))
{
    builder.Services.AddD365FOServices(builder.Configuration);
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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
