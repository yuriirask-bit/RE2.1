using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCli.Commands;

/// <summary>
/// T052c: Lookup customer command implementation.
/// Accepts customer account + data area via args, returns compliance status JSON to stdout.
/// Uses composite key (CustomerAccount + DataAreaId) per D365FO pattern.
/// </summary>
public class LookupCustomerCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public LookupCustomerCommand(IServiceProvider serviceProvider, JsonSerializerOptions jsonOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = jsonOptions;
    }

    public async Task<int> ExecuteAsync(LookupCustomerOptions options)
    {
        if (string.IsNullOrEmpty(options.CustomerAccount) && string.IsNullOrEmpty(options.Name))
        {
            OutputError("Either --account or --name must be provided.");
            return 1;
        }

        var customerService = _serviceProvider.GetRequiredService<ICustomerService>();
        var licenceService = _serviceProvider.GetService<ILicenceService>();

        Customer? customer = null;

        // Lookup by composite key (CustomerAccount + DataAreaId)
        if (!string.IsNullOrEmpty(options.CustomerAccount))
        {
            customer = await customerService.GetByAccountAsync(options.CustomerAccount, options.DataAreaId);
        }
        // Lookup by name
        else if (!string.IsNullOrEmpty(options.Name))
        {
            var matches = await customerService.SearchByNameAsync(options.Name);
            var matchList = matches.ToList();

            if (matchList.Count == 0)
            {
                OutputError($"No customers found matching: {options.Name}");
                return 1;
            }

            if (matchList.Count > 1)
            {
                // Return list of matches for user to choose
                var matchOutput = new CustomerSearchOutput
                {
                    Message = $"Multiple customers found ({matchList.Count}). Please use --account with specific customer account.",
                    Matches = matchList.Select(c => new CustomerMatch
                    {
                        CustomerAccount = c.CustomerAccount,
                        DataAreaId = c.DataAreaId,
                        BusinessName = c.BusinessName,
                        Country = c.Country,
                        ApprovalStatus = c.ApprovalStatus.ToString()
                    }).ToList()
                };
                Console.WriteLine(JsonSerializer.Serialize(matchOutput, _jsonOptions));
                return 0;
            }

            customer = matchList.First();
        }

        if (customer == null)
        {
            OutputError("Customer not found.");
            return 1;
        }

        // Get compliance status
        var complianceStatus = await customerService.GetComplianceStatusAsync(customer.CustomerAccount, customer.DataAreaId);

        // Build output
        var output = new CustomerOutput
        {
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            BusinessName = customer.BusinessName,
            Country = customer.Country,
            BusinessCategory = customer.BusinessCategory.ToString(),
            ApprovalStatus = customer.ApprovalStatus.ToString(),
            GdpQualificationStatus = customer.GdpQualificationStatus.ToString(),
            IsSuspended = customer.IsSuspended,
            SuspensionReason = customer.SuspensionReason,
            OnboardingDate = customer.OnboardingDate?.ToString("yyyy-MM-dd"),
            NextReVerificationDate = customer.NextReVerificationDate?.ToString("yyyy-MM-dd"),
            IsReVerificationDue = customer.IsReVerificationDue(),
            ComplianceStatus = new ComplianceStatusOutput
            {
                CanTransact = complianceStatus.CanTransact,
                IsSuspended = complianceStatus.IsSuspended,
                SuspensionReason = complianceStatus.SuspensionReason,
                IsReVerificationDue = complianceStatus.IsReVerificationDue,
                Warnings = complianceStatus.Warnings.Select(w => new WarningOutput
                {
                    WarningCode = w.WarningCode,
                    Message = w.Message,
                    Severity = w.Severity
                }).ToList()
            }
        };

        // Include licences if requested
        if (options.IncludeLicences && licenceService != null)
        {
            var licences = await licenceService.GetByHolderAsync(customer.ComplianceExtensionId, "Customer");
            output.Licences = licences.Select(l => new LicenceOutput
            {
                LicenceId = l.LicenceId,
                LicenceNumber = l.LicenceNumber,
                LicenceTypeName = l.LicenceType?.Name,
                Status = l.Status,
                IssueDate = l.IssueDate.ToString("yyyy-MM-dd"),
                ExpiryDate = l.ExpiryDate?.ToString("yyyy-MM-dd"),
                IsExpired = l.IsExpired(),
                IssuingAuthority = l.IssuingAuthority
            }).ToList();
        }

        Console.WriteLine(JsonSerializer.Serialize(output, _jsonOptions));
        return 0;
    }

    private void OutputError(string message)
    {
        var error = new { error = message, errorType = "LookupError" };
        Console.WriteLine(JsonSerializer.Serialize(error, _jsonOptions));
    }
}

#region Output DTOs

public class CustomerSearchOutput
{
    public string Message { get; set; } = string.Empty;
    public List<CustomerMatch> Matches { get; set; } = new();
}

public class CustomerMatch
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
}

public class CustomerOutput
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string GdpQualificationStatus { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public bool IsReVerificationDue { get; set; }
    public ComplianceStatusOutput ComplianceStatus { get; set; } = new();
    public List<LicenceOutput>? Licences { get; set; }
}

public class ComplianceStatusOutput
{
    public bool CanTransact { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool IsReVerificationDue { get; set; }
    public List<WarningOutput> Warnings { get; set; } = new();
}

public class WarningOutput
{
    public string WarningCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public class LicenceOutput
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string? LicenceTypeName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IssueDate { get; set; }
    public string? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public string? IssuingAuthority { get; set; }
}

#endregion
