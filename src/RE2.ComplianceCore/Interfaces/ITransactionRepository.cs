using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Transaction entity operations.
/// T128-T130: Repository interface for transaction compliance validation.
/// </summary>
public interface ITransactionRepository
{
    #region Core Transaction Operations

    /// <summary>
    /// Gets a transaction by its internal ID.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by its external ID (ERP order number).
    /// </summary>
    Task<Transaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions.
    /// </summary>
    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new transaction record.
    /// </summary>
    Task<Guid> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction record.
    /// </summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transaction record.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    /// <summary>
    /// Gets transactions by validation status.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByStatusAsync(ValidationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions pending override approval.
    /// Per FR-019a: Override approval queue.
    /// </summary>
    Task<IEnumerable<Transaction>> GetPendingOverrideAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions for a specific customer.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions within a date range.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions for a customer within a date range (for threshold calculation).
    /// </summary>
    Task<IEnumerable<Transaction>> GetCustomerTransactionsInPeriodAsync(
        Guid customerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Lines

    /// <summary>
    /// Gets transaction lines for a transaction.
    /// </summary>
    Task<IEnumerable<TransactionLine>> GetLinesAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction lines for a substance (across all transactions in a period).
    /// Used for threshold calculation.
    /// </summary>
    Task<IEnumerable<TransactionLine>> GetLinesBySubstanceInPeriodAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction lines for a customer and substance in a period.
    /// Used for customer-substance threshold calculation.
    /// </summary>
    Task<IEnumerable<TransactionLine>> GetCustomerSubstanceLinesInPeriodAsync(
        Guid customerId,
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Violations

    /// <summary>
    /// Gets all violations for a transaction.
    /// </summary>
    Task<IEnumerable<TransactionViolation>> GetViolationsAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds violations to a transaction.
    /// </summary>
    Task AddViolationsAsync(Guid transactionId, IEnumerable<TransactionViolation> violations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all violations for a transaction (for re-validation).
    /// </summary>
    Task ClearViolationsAsync(Guid transactionId, CancellationToken cancellationToken = default);

    #endregion

    #region Licence Usage

    /// <summary>
    /// Gets licence usage records for a transaction.
    /// </summary>
    Task<IEnumerable<TransactionLicenceUsage>> GetLicenceUsagesAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds licence usage record.
    /// </summary>
    Task AddLicenceUsageAsync(TransactionLicenceUsage usage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets licence usage for a specific licence in a period (for utilization tracking).
    /// </summary>
    Task<IEnumerable<TransactionLicenceUsage>> GetLicenceUsageInPeriodAsync(
        Guid licenceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets count of pending override transactions.
    /// Used for dashboard widget.
    /// </summary>
    Task<int> GetPendingOverrideCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of blocked transactions (failed validation).
    /// </summary>
    Task<int> GetBlockedTransactionCountAsync(CancellationToken cancellationToken = default);

    #endregion
}
