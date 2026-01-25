using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCli.Commands;
using RE2.DataAccess.DependencyInjection;

namespace RE2.ComplianceCli;

/// <summary>
/// RE2 Compliance CLI - Command-line interface for compliance operations.
/// T052a: Console application implementing Constitution Principle IV (CLI Interface Requirement).
/// Provides text I/O protocol for debugging, scripting, and automation.
/// </summary>
public class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await Parser.Default.ParseArguments<
                ValidateTransactionOptions,
                LookupCustomerOptions,
                LookupLicenceOptions,
                GenerateReportOptions>(args)
                .MapResult(
                    async (ValidateTransactionOptions opts) => await RunValidateTransaction(opts),
                    async (LookupCustomerOptions opts) => await RunLookupCustomer(opts),
                    async (LookupLicenceOptions opts) => await RunLookupLicence(opts),
                    async (GenerateReportOptions opts) => await RunGenerateReport(opts),
                    errs => Task.FromResult(1));
        }
        catch (Exception ex)
        {
            var error = new CliError
            {
                Error = ex.Message,
                ErrorType = ex.GetType().Name
            };
            Console.WriteLine(JsonSerializer.Serialize(error, JsonOptions));
            return 1;
        }
    }

    private static ServiceProvider BuildServiceProvider(bool verbose)
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning);
            if (verbose)
            {
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
            }
        });

        // Register in-memory repositories and services for CLI (same as local development)
        // This includes all repositories and business services (LicenceService, CustomerService, etc.)
        services.AddInMemoryRepositories();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// T052b: Validate transaction command - accepts JSON via stdin, returns ValidationResult to stdout.
    /// </summary>
    private static async Task<int> RunValidateTransaction(ValidateTransactionOptions opts)
    {
        using var serviceProvider = BuildServiceProvider(opts.Verbose);
        var command = new ValidateTransactionCommand(serviceProvider, JsonOptions);
        return await command.ExecuteAsync(opts);
    }

    /// <summary>
    /// T052c: Lookup customer command - accepts customer ID via args, returns compliance status JSON to stdout.
    /// </summary>
    private static async Task<int> RunLookupCustomer(LookupCustomerOptions opts)
    {
        using var serviceProvider = BuildServiceProvider(opts.Verbose);
        var command = new LookupCustomerCommand(serviceProvider, JsonOptions);
        return await command.ExecuteAsync(opts);
    }

    /// <summary>
    /// T052d: Lookup licence command - accepts licence number via args, returns licence details JSON to stdout.
    /// </summary>
    private static async Task<int> RunLookupLicence(LookupLicenceOptions opts)
    {
        using var serviceProvider = BuildServiceProvider(opts.Verbose);
        var command = new LookupLicenceCommand(serviceProvider, JsonOptions);
        return await command.ExecuteAsync(opts);
    }

    /// <summary>
    /// T052e: Generate report command - accepts report type and filters via args, returns report data to stdout.
    /// </summary>
    private static async Task<int> RunGenerateReport(GenerateReportOptions opts)
    {
        using var serviceProvider = BuildServiceProvider(opts.Verbose);
        var command = new GenerateReportCommand(serviceProvider, JsonOptions);
        return await command.ExecuteAsync(opts);
    }
}

/// <summary>
/// Standard CLI error response format.
/// </summary>
public class CliError
{
    public string Error { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
}
