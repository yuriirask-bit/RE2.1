using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.AlertGeneration;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCli.Commands;

/// <summary>
/// T052e: Generate report command implementation.
/// Accepts report type and filters via args, returns report data to stdout.
/// </summary>
public class GenerateReportCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    private static readonly string[] ValidReportTypes =
    {
        "expiring-licences",
        "customer-compliance",
        "alerts-summary",
        "transaction-history"
    };

    public GenerateReportCommand(IServiceProvider serviceProvider, JsonSerializerOptions jsonOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = jsonOptions;
    }

    public async Task<int> ExecuteAsync(GenerateReportOptions options)
    {
        if (!ValidReportTypes.Contains(options.ReportType.ToLowerInvariant()))
        {
            OutputError($"Invalid report type: {options.ReportType}. Valid types: {string.Join(", ", ValidReportTypes)}");
            return 1;
        }

        object? report = options.ReportType.ToLowerInvariant() switch
        {
            "expiring-licences" => await GenerateExpiringLicencesReport(options),
            "customer-compliance" => await GenerateCustomerComplianceReport(options),
            "alerts-summary" => await GenerateAlertsSummaryReport(options),
            "transaction-history" => await GenerateTransactionHistoryReport(options),
            _ => null
        };

        if (report == null)
        {
            OutputError("Failed to generate report.");
            return 1;
        }

        var json = JsonSerializer.Serialize(report, _jsonOptions);

        // Output to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            await File.WriteAllTextAsync(options.OutputFile, json);
            Console.Error.WriteLine($"Report written to: {options.OutputFile}");
        }
        else
        {
            Console.WriteLine(json);
        }

        return 0;
    }

    private async Task<ExpiringLicencesReport> GenerateExpiringLicencesReport(GenerateReportOptions options)
    {
        var licenceService = _serviceProvider.GetRequiredService<ILicenceService>();
        var licences = await licenceService.GetExpiringLicencesAsync(options.DaysAhead);

        var licenceList = licences.ToList();

        return new ExpiringLicencesReport
        {
            ReportType = "expiring-licences",
            GeneratedAt = DateTime.UtcNow,
            Parameters = new ReportParameters
            {
                DaysAhead = options.DaysAhead
            },
            Summary = new ExpiringLicencesSummary
            {
                TotalExpiring = licenceList.Count,
                ExpiredCount = licenceList.Count(l => l.IsExpired()),
                ExpiringIn30Days = licenceList.Count(l => !l.IsExpired() && l.ExpiryDate.HasValue &&
                    (l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays <= 30),
                ExpiringIn60Days = licenceList.Count(l => !l.IsExpired() && l.ExpiryDate.HasValue &&
                    (l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays <= 60),
                ExpiringIn90Days = licenceList.Count(l => !l.IsExpired() && l.ExpiryDate.HasValue &&
                    (l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays <= 90)
            },
            Licences = licenceList.OrderBy(l => l.ExpiryDate).Select(l => new ExpiringLicenceItem
            {
                LicenceId = l.LicenceId,
                LicenceNumber = l.LicenceNumber,
                LicenceTypeName = l.LicenceType?.Name,
                HolderType = l.HolderType,
                HolderId = l.HolderId,
                ExpiryDate = l.ExpiryDate?.ToString("yyyy-MM-dd"),
                DaysUntilExpiry = l.ExpiryDate.HasValue
                    ? (int)(l.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays
                    : 0,
                IsExpired = l.IsExpired(),
                Status = l.Status
            }).ToList()
        };
    }

    private async Task<CustomerComplianceReport> GenerateCustomerComplianceReport(GenerateReportOptions options)
    {
        var customerService = _serviceProvider.GetRequiredService<ICustomerService>();

        IEnumerable<RE2.ComplianceCore.Models.Customer> customers;

        if (!string.IsNullOrEmpty(options.CustomerAccount) && !string.IsNullOrEmpty(options.DataAreaId))
        {
            var customer = await customerService.GetByAccountAsync(options.CustomerAccount, options.DataAreaId);
            customers = customer != null ? new[] { customer } : Array.Empty<RE2.ComplianceCore.Models.Customer>();
        }
        else
        {
            customers = await customerService.GetAllAsync();
        }

        var customerList = customers.ToList();
        var complianceItems = new List<CustomerComplianceItem>();

        foreach (var customer in customerList)
        {
            var status = await customerService.GetComplianceStatusAsync(customer.CustomerAccount, customer.DataAreaId);
            complianceItems.Add(new CustomerComplianceItem
            {
                CustomerAccount = customer.CustomerAccount,
                DataAreaId = customer.DataAreaId,
                BusinessName = customer.BusinessName,
                Country = customer.Country,
                ApprovalStatus = customer.ApprovalStatus.ToString(),
                IsSuspended = customer.IsSuspended,
                IsCompliant = status.CanTransact && !status.IsSuspended,
                CanTransact = status.CanTransact,
                Issues = status.Warnings.Select(w => w.Message).ToList(),
                NextReVerificationDate = customer.NextReVerificationDate?.ToString("yyyy-MM-dd"),
                IsReVerificationDue = customer.IsReVerificationDue()
            });
        }

        return new CustomerComplianceReport
        {
            ReportType = "customer-compliance",
            GeneratedAt = DateTime.UtcNow,
            Parameters = new ReportParameters
            {
                CustomerAccount = options.CustomerAccount,
                DataAreaId = options.DataAreaId
            },
            Summary = new CustomerComplianceSummary
            {
                TotalCustomers = customerList.Count,
                CompliantCount = complianceItems.Count(c => c.IsCompliant),
                NonCompliantCount = complianceItems.Count(c => !c.IsCompliant),
                SuspendedCount = complianceItems.Count(c => c.IsSuspended),
                ReVerificationDueCount = complianceItems.Count(c => c.IsReVerificationDue)
            },
            Customers = complianceItems
        };
    }

    private async Task<AlertsSummaryReport> GenerateAlertsSummaryReport(GenerateReportOptions options)
    {
        var alertService = _serviceProvider.GetRequiredService<AlertGenerationService>();
        var summary = await alertService.GetDashboardSummaryAsync();

        return new AlertsSummaryReport
        {
            ReportType = "alerts-summary",
            GeneratedAt = DateTime.UtcNow,
            Parameters = new ReportParameters(),
            Summary = new AlertsSummary
            {
                TotalUnacknowledged = summary.TotalUnacknowledged,
                CriticalCount = summary.CriticalCount,
                WarningCount = summary.WarningCount,
                InfoCount = summary.InfoCount,
                OverdueCount = summary.OverdueCount,
                LicenceExpiringCount = summary.LicenceExpiringCount,
                LicenceExpiredCount = summary.LicenceExpiredCount,
                ReVerificationCount = summary.ReVerificationCount,
                MissingDocumentationCount = summary.MissingDocumentationCount
            },
            RecentAlerts = summary.RecentAlerts.Select(a => new AlertItem
            {
                AlertId = a.AlertId,
                AlertType = a.AlertType.ToString(),
                Severity = a.Severity.ToString(),
                Message = a.Message,
                GeneratedDate = a.GeneratedDate,
                TargetEntityType = a.TargetEntityType.ToString(),
                TargetEntityId = a.TargetEntityId
            }).ToList()
        };
    }

    private async Task<TransactionHistoryReport> GenerateTransactionHistoryReport(GenerateReportOptions options)
    {
        var transactionRepository = _serviceProvider.GetService<ITransactionRepository>();

        if (transactionRepository == null)
        {
            return new TransactionHistoryReport
            {
                ReportType = "transaction-history",
                GeneratedAt = DateTime.UtcNow,
                Parameters = new ReportParameters
                {
                    CustomerAccount = options.CustomerAccount,
                    DataAreaId = options.DataAreaId,
                    FromDate = options.FromDate,
                    ToDate = options.ToDate
                },
                Summary = new TransactionSummary(),
                Transactions = new List<TransactionItem>(),
                Message = "Transaction repository not available."
            };
        }

        // Get transactions (would need filtering implementation)
        var transactions = await transactionRepository.GetAllAsync();
        var transactionList = transactions.ToList();

        // Apply filters
        if (!string.IsNullOrEmpty(options.CustomerAccount))
        {
            transactionList = transactionList.Where(t =>
                t.CustomerAccount.Equals(options.CustomerAccount, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(options.DataAreaId) ||
                 t.CustomerDataAreaId.Equals(options.DataAreaId, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        if (!string.IsNullOrEmpty(options.FromDate) && DateTime.TryParse(options.FromDate, out var fromDate))
        {
            transactionList = transactionList.Where(t => t.TransactionDate >= fromDate).ToList();
        }

        if (!string.IsNullOrEmpty(options.ToDate) && DateTime.TryParse(options.ToDate, out var toDate))
        {
            transactionList = transactionList.Where(t => t.TransactionDate <= toDate).ToList();
        }

        return new TransactionHistoryReport
        {
            ReportType = "transaction-history",
            GeneratedAt = DateTime.UtcNow,
            Parameters = new ReportParameters
            {
                CustomerAccount = options.CustomerAccount,
                DataAreaId = options.DataAreaId,
                FromDate = options.FromDate,
                ToDate = options.ToDate
            },
            Summary = new TransactionSummary
            {
                TotalTransactions = transactionList.Count,
                PassedCount = transactionList.Count(t => t.ValidationStatus == ValidationStatus.Passed),
                FailedCount = transactionList.Count(t => t.ValidationStatus == ValidationStatus.Failed),
                PendingCount = transactionList.Count(t => t.ValidationStatus == ValidationStatus.Pending),
                OverrideApprovedCount = transactionList.Count(t => t.ValidationStatus == ValidationStatus.ApprovedWithOverride)
            },
            Transactions = transactionList.OrderByDescending(t => t.TransactionDate).Take(100).Select(t => new TransactionItem
            {
                TransactionId = t.Id,
                ExternalTransactionId = t.ExternalId,
                CustomerAccount = t.CustomerAccount,
                CustomerDataAreaId = t.CustomerDataAreaId,
                TransactionType = t.TransactionType.ToString(),
                TransactionDirection = t.Direction.ToString(),
                TransactionDate = t.TransactionDate,
                ValidationStatus = t.ValidationStatus.ToString(),
                LineCount = t.Lines.Count
            }).ToList()
        };
    }

    private void OutputError(string message)
    {
        var error = new { error = message, errorType = "ReportError" };
        Console.WriteLine(JsonSerializer.Serialize(error, _jsonOptions));
    }
}

#region Report DTOs

public class ReportParameters
{
    public int? DaysAhead { get; set; }
    public string? CustomerAccount { get; set; }
    public string? DataAreaId { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
}

// Expiring Licences Report
public class ExpiringLicencesReport
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportParameters Parameters { get; set; } = new();
    public ExpiringLicencesSummary Summary { get; set; } = new();
    public List<ExpiringLicenceItem> Licences { get; set; } = new();
}

public class ExpiringLicencesSummary
{
    public int TotalExpiring { get; set; }
    public int ExpiredCount { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ExpiringIn60Days { get; set; }
    public int ExpiringIn90Days { get; set; }
}

public class ExpiringLicenceItem
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public string? LicenceTypeName { get; set; }
    public string HolderType { get; set; } = string.Empty;
    public Guid HolderId { get; set; }
    public string? ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Customer Compliance Report
public class CustomerComplianceReport
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportParameters Parameters { get; set; } = new();
    public CustomerComplianceSummary Summary { get; set; } = new();
    public List<CustomerComplianceItem> Customers { get; set; } = new();
}

public class CustomerComplianceSummary
{
    public int TotalCustomers { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int SuspendedCount { get; set; }
    public int ReVerificationDueCount { get; set; }
}

public class CustomerComplianceItem
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public bool IsCompliant { get; set; }
    public bool CanTransact { get; set; }
    public List<string> Issues { get; set; } = new();
    public string? NextReVerificationDate { get; set; }
    public bool IsReVerificationDue { get; set; }
}

// Alerts Summary Report
public class AlertsSummaryReport
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportParameters Parameters { get; set; } = new();
    public AlertsSummary Summary { get; set; } = new();
    public List<AlertItem> RecentAlerts { get; set; } = new();
}

public class AlertsSummary
{
    public int TotalUnacknowledged { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int OverdueCount { get; set; }
    public int LicenceExpiringCount { get; set; }
    public int LicenceExpiredCount { get; set; }
    public int ReVerificationCount { get; set; }
    public int MissingDocumentationCount { get; set; }
}

public class AlertItem
{
    public Guid AlertId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
}

// Transaction History Report
public class TransactionHistoryReport
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ReportParameters Parameters { get; set; } = new();
    public TransactionSummary Summary { get; set; } = new();
    public List<TransactionItem> Transactions { get; set; } = new();
    public string? Message { get; set; }
}

public class TransactionSummary
{
    public int TotalTransactions { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public int OverrideApprovedCount { get; set; }
}

public class TransactionItem
{
    public Guid TransactionId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string TransactionDirection { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public int LineCount { get; set; }
}

#endregion
