using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCli.Commands;

/// <summary>
/// T052b: Validate transaction command implementation.
/// Accepts transaction JSON via stdin, returns ValidationResult to stdout.
/// Lines use ItemNumber + DataAreaId; substance resolution happens server-side.
/// </summary>
public class ValidateTransactionCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public ValidateTransactionCommand(IServiceProvider serviceProvider, JsonSerializerOptions jsonOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = jsonOptions;
    }

    public async Task<int> ExecuteAsync(ValidateTransactionOptions options)
    {
        // Read transaction JSON from stdin or file
        string jsonInput;
        if (!string.IsNullOrEmpty(options.InputFile))
        {
            if (!File.Exists(options.InputFile))
            {
                OutputError($"Input file not found: {options.InputFile}");
                return 1;
            }
            jsonInput = await File.ReadAllTextAsync(options.InputFile);
        }
        else
        {
            // Read from stdin
            using var reader = new StreamReader(Console.OpenStandardInput());
            jsonInput = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            OutputError("No input provided. Provide transaction JSON via stdin or --file option.");
            return 1;
        }

        // Parse transaction input
        TransactionInput? input;
        try
        {
            input = JsonSerializer.Deserialize<TransactionInput>(jsonInput, _jsonOptions);
            if (input == null)
            {
                OutputError("Failed to parse transaction JSON.");
                return 1;
            }
        }
        catch (JsonException ex)
        {
            OutputError($"Invalid JSON: {ex.Message}");
            return 1;
        }

        // Build transaction from input
        var transaction = BuildTransaction(input);

        // Get validation service and validate
        var transactionService = _serviceProvider.GetService<ITransactionComplianceService>();
        if (transactionService == null)
        {
            OutputError("Transaction compliance service not available.");
            return 1;
        }

        var result = await transactionService.ValidateTransactionAsync(transaction);

        // Output result as JSON
        var output = new ValidationOutput
        {
            IsValid = result.ValidationResult.IsValid,
            CanProceed = result.CanProceed,
            Status = result.Transaction.ValidationStatus.ToString(),
            Violations = result.ValidationResult.Violations.Select(v => new ViolationOutput
            {
                Code = v.ErrorCode,
                Message = v.Message,
                Severity = v.Severity.ToString(),
                Field = v.SubstanceCode ?? v.LicenceNumber
            }).ToList(),
            LicenceUsage = result.LicenceUsages.Select(u => new LicenceUsageOutput
            {
                LicenceId = u.LicenceId,
                LicenceNumber = u.LicenceNumber,
                SubstanceCodes = u.CoveredSubstanceCodes,
                QuantityUsed = u.CoveredQuantity
            }).ToList(),
            ValidatedAt = DateTime.UtcNow
        };

        Console.WriteLine(JsonSerializer.Serialize(output, _jsonOptions));
        return result.ValidationResult.IsValid ? 0 : 2; // Return 2 for validation failures (distinct from errors)
    }

    private Transaction BuildTransaction(TransactionInput input)
    {
        var transaction = new Transaction
        {
            Id = input.TransactionId ?? Guid.NewGuid(),
            ExternalId = input.ExternalTransactionId ?? string.Empty,
            CustomerAccount = input.CustomerAccount,
            CustomerDataAreaId = input.CustomerDataAreaId,
            TransactionType = Enum.Parse<TransactionType>(input.TransactionType, ignoreCase: true),
            Direction = Enum.Parse<TransactionDirection>(input.TransactionDirection, ignoreCase: true),
            TransactionDate = input.TransactionDate ?? DateTime.UtcNow,
            OriginCountry = input.SourceCountry ?? "NL",
            DestinationCountry = input.DestinationCountry,
            WarehouseSiteId = input.WarehouseSiteId,
            Lines = input.Lines.Select(l => new TransactionLine
            {
                Id = l.LineId ?? Guid.NewGuid(),
                ItemNumber = l.ItemNumber,
                DataAreaId = l.DataAreaId,
                // SubstanceCode is resolved by the system during validation
                Quantity = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure ?? "EA"
            }).ToList()
        };

        return transaction;
    }

    private void OutputError(string message)
    {
        var error = new { error = message, errorType = "ValidationError" };
        Console.WriteLine(JsonSerializer.Serialize(error, _jsonOptions));
    }
}

#region Input/Output DTOs

public class TransactionInput
{
    public Guid? TransactionId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = "Order";
    public string TransactionDirection { get; set; } = "Outbound";
    public DateTime? TransactionDate { get; set; }
    public string? SourceCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? WarehouseSiteId { get; set; }
    public List<TransactionLineInput> Lines { get; set; } = new();
}

public class TransactionLineInput
{
    public Guid? LineId { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
}

public class ValidationOutput
{
    public bool IsValid { get; set; }
    public bool CanProceed { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ViolationOutput> Violations { get; set; } = new();
    public List<LicenceUsageOutput> LicenceUsage { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}

public class ViolationOutput
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Field { get; set; }
}

public class LicenceUsageOutput
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public List<string> SubstanceCodes { get; set; } = new();
    public decimal QuantityUsed { get; set; }
}

#endregion
