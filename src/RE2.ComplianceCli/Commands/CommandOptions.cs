using CommandLine;

namespace RE2.ComplianceCli.Commands;

/// <summary>
/// Base options shared across all commands.
/// </summary>
public abstract class BaseOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Enable verbose output to stderr.")]
    public bool Verbose { get; set; }
}

/// <summary>
/// T052b: Options for validate-transaction command.
/// Accepts transaction JSON via stdin and returns ValidationResult to stdout.
/// </summary>
[Verb("validate-transaction", HelpText = "Validate a transaction against compliance rules. Reads JSON from stdin.")]
public class ValidateTransactionOptions : BaseOptions
{
    [Option('f', "file", Required = false, HelpText = "Read transaction JSON from file instead of stdin.")]
    public string? InputFile { get; set; }
}

/// <summary>
/// T052c: Options for lookup-customer command.
/// Returns customer compliance status as JSON to stdout.
/// Uses composite key (CustomerAccount + DataAreaId) per D365FO pattern.
/// </summary>
[Verb("lookup-customer", HelpText = "Lookup customer compliance status by account or name.")]
public class LookupCustomerOptions : BaseOptions
{
    [Option('a', "account", Required = false, HelpText = "Customer account number (D365FO).")]
    public string? CustomerAccount { get; set; }

    [Option('d', "data-area", Required = false, Default = "nlpd", HelpText = "Data area ID (default: nlpd).")]
    public string DataAreaId { get; set; } = "nlpd";

    [Option('n', "name", Required = false, HelpText = "Customer organization name (partial match).")]
    public string? Name { get; set; }

    [Option("include-licences", Required = false, HelpText = "Include associated licences in output.")]
    public bool IncludeLicences { get; set; }
}

/// <summary>
/// T052d: Options for lookup-licence command.
/// Returns licence details as JSON to stdout.
/// </summary>
[Verb("lookup-licence", HelpText = "Lookup licence details by number or ID.")]
public class LookupLicenceOptions : BaseOptions
{
    [Option('i', "id", Required = false, HelpText = "Licence ID (GUID).")]
    public string? LicenceId { get; set; }

    [Option('n', "number", Required = false, HelpText = "Licence number.")]
    public string? LicenceNumber { get; set; }

    [Option("include-substances", Required = false, HelpText = "Include substance mappings in output.")]
    public bool IncludeSubstances { get; set; }

    [Option("include-documents", Required = false, HelpText = "Include document metadata in output.")]
    public bool IncludeDocuments { get; set; }
}

/// <summary>
/// T052e: Options for generate-report command.
/// Returns report data as JSON to stdout.
/// </summary>
[Verb("generate-report", HelpText = "Generate a compliance report.")]
public class GenerateReportOptions : BaseOptions
{
    [Option('t', "type", Required = true, HelpText = "Report type: expiring-licences, customer-compliance, alerts-summary, transaction-history.")]
    public string ReportType { get; set; } = string.Empty;

    [Option("days-ahead", Required = false, Default = 90, HelpText = "Days ahead for expiring items (default: 90).")]
    public int DaysAhead { get; set; } = 90;

    [Option("customer-account", Required = false, HelpText = "Filter by customer account number.")]
    public string? CustomerAccount { get; set; }

    [Option("data-area-id", Required = false, HelpText = "Data area ID (legal entity) for customer filter.")]
    public string? DataAreaId { get; set; }

    [Option("from-date", Required = false, HelpText = "Start date filter (yyyy-MM-dd).")]
    public string? FromDate { get; set; }

    [Option("to-date", Required = false, HelpText = "End date filter (yyyy-MM-dd).")]
    public string? ToDate { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output file path (default: stdout).")]
    public string? OutputFile { get; set; }
}
