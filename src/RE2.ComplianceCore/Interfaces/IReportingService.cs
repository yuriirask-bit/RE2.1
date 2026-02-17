using RE2.ComplianceCore.Services.Reporting;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for generating compliance reports.
/// T160-T163: Report generation per FR-026 (transaction audit) and FR-029 (customer compliance history).
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generates a transaction audit report.
    /// T161: Per FR-026 - report by substance, customer, country.
    /// </summary>
    /// <param name="criteria">Report criteria including filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction audit report.</returns>
    Task<TransactionAuditReport> GenerateTransactionAuditReportAsync(
        TransactionAuditReportCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a licence usage report.
    /// T162: Shows which licences were used in transactions.
    /// </summary>
    /// <param name="criteria">Report criteria including filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Licence usage report.</returns>
    Task<LicenceUsageReport> GenerateLicenceUsageReportAsync(
        LicenceUsageReportCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a customer compliance history report.
    /// T163: Per FR-029 - complete compliance history for a customer.
    /// </summary>
    /// <param name="criteria">Report criteria including customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer compliance history, or null if customer not found.</returns>
    Task<CustomerComplianceHistoryReport?> GenerateCustomerComplianceHistoryAsync(
        CustomerComplianceHistoryCriteria criteria,
        CancellationToken cancellationToken = default);
}
