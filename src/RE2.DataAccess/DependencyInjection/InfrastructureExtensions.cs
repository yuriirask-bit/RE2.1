using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using RE2.ComplianceCore.Configuration;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.AlertGeneration;
using RE2.ComplianceCore.Services.Auditing;
using RE2.ComplianceCore.Services.CustomerQualification;
using RE2.ComplianceCore.Services.LicenceValidation;
using RE2.ComplianceCore.Services.SubstanceManagement;
using RE2.ComplianceCore.Services.RiskMonitoring;
using RE2.ComplianceCore.Services.TransactionValidation;
using RE2.ComplianceCore.Services.Notifications;
using RE2.ComplianceCore.Services.Reporting;
using RE2.ComplianceCore.Services.GdpCompliance;
using RE2.DataAccess.BlobStorage;
using RE2.DataAccess.D365FinanceOperations;
using RE2.DataAccess.D365FinanceOperations.Repositories;
using RE2.DataAccess.Dataverse;
using RE2.DataAccess.Dataverse.Repositories;
using RE2.DataAccess.InMemory;

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

        // Register repositories
        services.AddScoped<ILicenceRepository, DataverseLicenceRepository>();
        services.AddScoped<ILicenceTypeRepository, DataverseLicenceTypeRepository>();
        services.AddScoped<IControlledSubstanceRepository, DataverseControlledSubstanceRepository>();
        services.AddScoped<ISubstanceReclassificationRepository, DataverseSubstanceReclassificationRepository>();
        services.AddScoped<ILicenceSubstanceMappingRepository, DataverseLicenceSubstanceMappingRepository>();

        // Register customer repository (composite: Dataverse + D365FO via ID365FoClient)
        services.AddScoped<ICustomerRepository, DataverseCustomerRepository>();

        // Register GDP site repository (T189)
        services.AddScoped<IGdpSiteRepository, DataverseGdpSiteRepository>();

        // Register GDP credential repository (T204)
        services.AddScoped<IGdpCredentialRepository, DataverseGdpCredentialRepository>();

        // Register GDP inspection repository (T221)
        services.AddScoped<IGdpInspectionRepository, DataverseGdpInspectionRepository>();

        // Register CAPA repository (T223)
        services.AddScoped<ICapaRepository, DataverseCapaRepository>();

        // Register GDP document repository (T235)
        services.AddScoped<IGdpDocumentRepository, DataverseGdpDocumentRepository>();

        // Register GDP equipment repository (T258)
        services.AddScoped<IGdpEquipmentRepository, DataverseGdpEquipmentRepository>();

        // Register GDP SOP repository (T278)
        services.AddScoped<IGdpSopRepository, DataverseGdpSopRepository>();

        // Register training repository (T281)
        services.AddScoped<ITrainingRepository, DataverseTrainingRepository>();

        // Register GDP change repository (T284)
        services.AddScoped<IGdpChangeRepository, DataverseGdpChangeRepository>();

        // Register business services
        services.AddScoped<ILicenceService, LicenceService>();
        services.AddScoped<ISubstanceReclassificationService, SubstanceReclassificationService>();
        services.AddScoped<IControlledSubstanceService, ControlledSubstanceService>();
        services.AddScoped<ILicenceSubstanceMappingService, LicenceSubstanceMappingService>();

        // Register GDP compliance service (T190)
        services.AddScoped<IGdpComplianceService, GdpComplianceService>();

        // Register GDP operational service (T262)
        services.AddScoped<IGdpOperationalService, GdpOperationalService>();

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

        // Register D365 F&O audit repository (T155)
        services.AddScoped<IAuditRepository, D365FoAuditRepository>();

        // Register audit logging service (T156)
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();

        // Register reporting service (T164)
        services.AddScoped<IReportingService, ReportingService>();

        // Register licence correction impact service (T163a-T163c)
        services.AddScoped<ILicenceCorrectionImpactService, LicenceCorrectionImpactService>();

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

    /// <summary>
    /// Registers in-memory repositories for local development and testing.
    /// Use this instead of AddDataverseServices when running locally without Dataverse access.
    /// Enables testing of User Story 1 (Licence Management) without external dependencies.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="seedData">Whether to seed initial test data (default: true).</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryRepositories(
        this IServiceCollection services,
        bool seedData = true)
    {
        // Create singleton instances so data persists across requests
        var licenceTypeRepo = new InMemoryLicenceTypeRepository();
        var substanceRepo = new InMemoryControlledSubstanceRepository();
        var licenceRepo = new InMemoryLicenceRepository();
        var reclassificationRepo = new InMemorySubstanceReclassificationRepository();
        var mappingRepo = new InMemoryLicenceSubstanceMappingRepository();
        var alertRepo = new InMemoryAlertRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var transactionRepo = new InMemoryTransactionRepository();
        var thresholdRepo = new InMemoryThresholdRepository();

        var gdpSiteRepo = new InMemoryGdpSiteRepository();
        var productRepo = new InMemoryProductRepository();
        var gdpCredentialRepo = new InMemoryGdpCredentialRepository();
        var gdpInspectionRepo = new InMemoryGdpInspectionRepository();
        var capaRepo = new InMemoryCapaRepository();
        var gdpDocumentRepo = new InMemoryGdpDocumentRepository();
        var gdpEquipmentRepo = new InMemoryGdpEquipmentRepository();
        var gdpSopRepo = new InMemoryGdpSopRepository();
        var trainingRepo = new InMemoryTrainingRepository();
        var gdpChangeRepo = new InMemoryGdpChangeRepository();

        // Seed test data if requested
        if (seedData)
        {
            InMemorySeedData.SeedAll(licenceTypeRepo, substanceRepo, licenceRepo, customerRepo, thresholdRepo, gdpSiteRepo, productRepo, gdpCredentialRepo, gdpInspectionRepo, capaRepo, gdpDocumentRepo, gdpEquipmentRepo, gdpSopRepo, trainingRepo, gdpChangeRepo);
        }

        // Register as singletons
        services.AddSingleton<ILicenceTypeRepository>(licenceTypeRepo);
        services.AddSingleton<IControlledSubstanceRepository>(substanceRepo);
        services.AddSingleton<ILicenceRepository>(licenceRepo);
        services.AddSingleton<ISubstanceReclassificationRepository>(reclassificationRepo);
        services.AddSingleton<ILicenceSubstanceMappingRepository>(mappingRepo);
        services.AddSingleton<IAlertRepository>(alertRepo);
        services.AddSingleton<ICustomerRepository>(customerRepo);
        services.AddSingleton<ITransactionRepository>(transactionRepo);
        services.AddSingleton<IThresholdRepository>(thresholdRepo);

        // Register in-memory GDP site repository (T189)
        services.AddSingleton<IGdpSiteRepository>(gdpSiteRepo);

        // Register in-memory GDP credential repository (T204)
        services.AddSingleton<IGdpCredentialRepository>(gdpCredentialRepo);

        // Register in-memory GDP inspection repository (T221)
        services.AddSingleton<IGdpInspectionRepository>(gdpInspectionRepo);

        // Register in-memory CAPA repository (T223)
        services.AddSingleton<ICapaRepository>(capaRepo);

        // Register in-memory GDP document repository (T234)
        services.AddSingleton<IGdpDocumentRepository>(gdpDocumentRepo);

        // Register in-memory GDP equipment repository (T257)
        services.AddSingleton<IGdpEquipmentRepository>(gdpEquipmentRepo);

        // Register in-memory GDP SOP repository (T277)
        services.AddSingleton<IGdpSopRepository>(gdpSopRepo);

        // Register in-memory training repository (T280)
        services.AddSingleton<ITrainingRepository>(trainingRepo);

        // Register in-memory GDP change repository (T283)
        services.AddSingleton<IGdpChangeRepository>(gdpChangeRepo);

        // Register in-memory product repository for D365 product browsing
        services.AddSingleton<IProductRepository>(productRepo);

        // Register in-memory document storage for local development
        services.AddSingleton<IDocumentStorage, InMemoryDocumentStorage>();

        // Register in-memory webhook repository (T149c-T149f)
        var webhookRepo = new InMemoryWebhookSubscriptionRepository();
        services.AddSingleton<IWebhookSubscriptionRepository>(webhookRepo);

        // Register in-memory integration system repository
        var integrationSystemRepo = new InMemoryIntegrationSystemRepository();
        services.AddSingleton<IIntegrationSystemRepository>(integrationSystemRepo);

        // Register in-memory audit repository (T155-T156)
        var auditRepo = new InMemoryAuditRepository();
        services.AddSingleton<IAuditRepository>(auditRepo);

        // Register in-memory regulatory inspection repository (T167)
        var inspectionRepo = new InMemoryRegulatoryInspectionRepository();
        services.AddSingleton<IRegulatoryInspectionRepository>(inspectionRepo);

        // Register audit logging service (T156)
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();

        // Register business services (same as Dataverse setup)
        services.AddScoped<ILicenceService, LicenceService>();
        services.AddScoped<ISubstanceReclassificationService, SubstanceReclassificationService>();
        services.AddScoped<IControlledSubstanceService, ControlledSubstanceService>();
        services.AddScoped<ILicenceSubstanceMappingService, LicenceSubstanceMappingService>();

        // Register alert generation service for compliance monitoring
        services.AddScoped<AlertGenerationService>();

        // Register customer service for customer qualification management
        services.AddScoped<ICustomerService, CustomerService>();

        // Register webhook dispatch service (T149g-T149i)
        services.AddHttpClient("WebhookClient");
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();

        // Register transaction compliance service for order/shipment validation
        services.AddScoped<ITransactionComplianceService, TransactionComplianceService>();

        // Register threshold service for threshold configuration management
        services.AddScoped<IThresholdService, ThresholdService>();

        // Register reporting service (T164)
        services.AddScoped<IReportingService, ReportingService>();

        // Register licence correction impact service (T163a-T163c)
        services.AddScoped<ILicenceCorrectionImpactService, LicenceCorrectionImpactService>();

        // Register GDP compliance service (T190)
        services.AddScoped<IGdpComplianceService, GdpComplianceService>();

        // Register GDP operational service (T262)
        services.AddScoped<IGdpOperationalService, GdpOperationalService>();

        return services;
    }

    /// <summary>
    /// Registers services based on configuration.
    /// If "UseInMemoryRepositories" is true, uses in-memory repositories.
    /// Otherwise, uses Dataverse repositories.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddComplianceDataServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind product attribute configuration
        services.Configure<ProductAttributeConfiguration>(
            configuration.GetSection(ProductAttributeConfiguration.SectionName));

        var useInMemory = configuration.GetValue<bool>("UseInMemoryRepositories");

        if (useInMemory)
        {
            services.AddInMemoryRepositories(seedData: true);
        }
        else
        {
            services.AddDataverseServices(configuration);
        }

        return services;
    }
}
