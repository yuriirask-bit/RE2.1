using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RE2.ComplianceCore.Services.AlertGeneration;
using RE2.DataAccess.DependencyInjection;

// T040: Configure Azure Functions with Isolated Worker model per research.md section 3
// T121: Added AlertGenerationService for LicenceExpiryMonitor function
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Register external system integration services (Dataverse, D365 F&O, Blob Storage)
        services.AddExternalSystemIntegration(context.Configuration);

        // Register alert generation service (T120)
        services.AddScoped<AlertGenerationService>();

        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();
