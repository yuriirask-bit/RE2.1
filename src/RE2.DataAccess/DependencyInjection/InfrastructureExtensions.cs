using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using RE2.ComplianceCore.Interfaces;
using RE2.DataAccess.BlobStorage;
using RE2.DataAccess.D365FinanceOperations;
using RE2.DataAccess.Dataverse;

namespace RE2.DataAccess.DependencyInjection;

/// <summary>
/// Dependency injection extension methods for external system integration.
/// Per research.md section 3: Shared DI configuration across App Service and Functions.
/// Includes resilience configuration per research.md section 4.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Registers Dataverse services with Managed Identity authentication.
    /// Per research.md section 1: Uses ServiceClient with DefaultAzureCredential.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddDataverseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DataverseClient as singleton (ServiceClient is thread-safe and expensive to create)
        services.AddSingleton<IDataverseClient>(sp =>
        {
            var dataverseUrl = configuration["Dataverse:Url"]
                ?? throw new InvalidOperationException("Dataverse:Url configuration is missing");

            var logger = sp.GetRequiredService<ILogger<DataverseClient>>();

            return new DataverseClient(dataverseUrl, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers D365 Finance & Operations OData services with resilience handling.
    /// Per research.md section 2: Uses HttpClient with OAuth2 and OData v4.
    /// Per research.md section 4: Includes standard resilience handler.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddD365FOServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var odataEndpoint = configuration["D365FO:ODataEndpoint"]
            ?? throw new InvalidOperationException("D365FO:ODataEndpoint configuration is missing");

        var resource = configuration["D365FO:Resource"]
            ?? throw new InvalidOperationException("D365FO:Resource configuration is missing");

        // Register HttpClient with standard resilience handler (T033-T034)
        services.AddHttpClient<ID365FoClient, D365FoClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(odataEndpoint);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
        })
        .AddStandardResilienceHandler(options =>
        {
            // Retry configuration per research.md section 4
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true; // Prevent thundering herd
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            // Circuit breaker configuration
            options.CircuitBreaker.FailureRatio = 0.3; // Break at 30% failure rate
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);

            // Timeout configuration
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10); // Per attempt
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30); // Total including retries
        });

        // Register factory for D365FoClient with resource parameter
        services.AddSingleton<Func<ID365FoClient>>(sp =>
        {
            return () =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(D365FoClient));
                var logger = sp.GetRequiredService<ILogger<D365FoClient>>();
                return new D365FoClient(httpClient, resource, logger);
            };
        });

        return services;
    }

    /// <summary>
    /// Registers Azure Blob Storage services for document management.
    /// Uses Managed Identity authentication via DefaultAzureCredential.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddBlobStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var storageAccountUrl = configuration["BlobStorage:AccountUrl"]
            ?? throw new InvalidOperationException("BlobStorage:AccountUrl configuration is missing");

        // Register DocumentStorageClient as singleton
        services.AddSingleton<IDocumentStorage>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DocumentStorageClient>>();
            return new DocumentStorageClient(storageAccountUrl, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers all external system integration services.
    /// Convenience method to register Dataverse, D365 F&O, and Blob Storage in one call.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddExternalSystemIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDataverseServices(configuration);
        services.AddD365FOServices(configuration);
        services.AddBlobStorageServices(configuration);

        return services;
    }
}
