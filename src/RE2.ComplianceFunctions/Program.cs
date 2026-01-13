using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RE2.DataAccess.DependencyInjection;

// T040: Configure Azure Functions with Isolated Worker model per research.md section 3
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Register external system integration services (Dataverse, D365 F&O, Blob Storage)
        services.AddExternalSystemIntegration(context.Configuration);

        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();
