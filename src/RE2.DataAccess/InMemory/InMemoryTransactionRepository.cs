using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ITransactionRepository for local development and testing.
/// T131: In-memory transaction repository implementation.
/// </summary>
public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<Guid, Transaction> _transactions = new();
    private readonly ConcurrentDictionary<Guid, TransactionLine> _transactionLines = new();
    private readonly ConcurrentDictionary<Guid, TransactionViolation> _violations = new();
    private readonly ConcurrentDictionary<Guid, TransactionLicenceUsage> _licenceUsages = new();

    #region Core Transaction Operations

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _transactions.TryGetValue(id, out var transaction);
        if (transaction != null)
        {
            // Load related lines
            transaction.Lines = _transactionLines.Values
                .Where(l => l.TransactionId == id)
                .OrderBy(l => l.LineNumber)
                .ToList();

            // Load violations
            transaction.Violations = _violations.Values
                .Where(v => v.TransactionId == id)
                .ToList();

            // Load licence usages
            transaction.LicenceUsages = _licenceUsages.Values
                .Where(u => u.TransactionId == id)
                .ToList();
        }
        return Task.FromResult(transaction);
    }

    public Task<Transaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var transaction = _transactions.Values.FirstOrDefault(t =>
            t.ExternalId.Equals(externalId, StringComparison.OrdinalIgnoreCase));

        if (transaction != null)
        {
            return GetByIdAsync(transaction.Id, cancellationToken);
        }

        return Task.FromResult<Transaction?>(null);
    }

    public Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        foreach (var t in transactions)
        {
            t.Lines = _transactionLines.Values
                .Where(l => l.TransactionId == t.Id)
                .OrderBy(l => l.LineNumber)
                .ToList();
        }

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<Guid> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.Id == Guid.Empty)
        {
            transaction.Id = Guid.NewGuid();
        }
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.ModifiedAt = DateTime.UtcNow;

        // Save transaction
        _transactions.TryAdd(transaction.Id, transaction);

        // Save lines
        foreach (var line in transaction.Lines)
        {
            if (line.Id == Guid.Empty)
            {
                line.Id = Guid.NewGuid();
            }
            line.TransactionId = transaction.Id;
            _transactionLines.TryAdd(line.Id, line);
        }

        return Task.FromResult(transaction.Id);
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        transaction.ModifiedAt = DateTime.UtcNow;
        _transactions[transaction.Id] = transaction;

        // Update lines
        foreach (var line in transaction.Lines)
        {
            if (_transactionLines.ContainsKey(line.Id))
            {
                _transactionLines[line.Id] = line;
            }
            else
            {
                if (line.Id == Guid.Empty)
                {
                    line.Id = Guid.NewGuid();
                }
                line.TransactionId = transaction.Id;
                _transactionLines.TryAdd(line.Id, line);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _transactions.TryRemove(id, out _);

        // Remove related lines
        var linesToRemove = _transactionLines.Values
            .Where(l => l.TransactionId == id)
            .Select(l => l.Id)
            .ToList();
        foreach (var lineId in linesToRemove)
        {
            _transactionLines.TryRemove(lineId, out _);
        }

        // Remove related violations
        var violationsToRemove = _violations.Values
            .Where(v => v.TransactionId == id)
            .Select(v => v.Id)
            .ToList();
        foreach (var violationId in violationsToRemove)
        {
            _violations.TryRemove(violationId, out _);
        }

        // Remove related licence usages
        var usagesToRemove = _licenceUsages.Values
            .Where(u => u.TransactionId == id)
            .Select(u => u.Id)
            .ToList();
        foreach (var usageId in usagesToRemove)
        {
            _licenceUsages.TryRemove(usageId, out _);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Filtered Queries

    public Task<IEnumerable<Transaction>> GetByStatusAsync(ValidationStatus status, CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .Where(t => t.ValidationStatus == status)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<IEnumerable<Transaction>> GetPendingOverrideAsync(CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .Where(t => t.RequiresOverride && t.OverrideStatus == OverrideStatus.Pending)
            .OrderByDescending(t => t.ValidationDate)
            .ToList();

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<IEnumerable<Transaction>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<IEnumerable<Transaction>> GetCustomerTransactionsInPeriodAsync(
        Guid customerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var transactions = _transactions.Values
            .Where(t => t.CustomerId == customerId &&
                        t.TransactionDate >= fromDate &&
                        t.TransactionDate <= toDate &&
                        t.CanProceed()) // Only include approved transactions
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        foreach (var t in transactions)
        {
            t.Lines = _transactionLines.Values
                .Where(l => l.TransactionId == t.Id)
                .OrderBy(l => l.LineNumber)
                .ToList();
        }

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    public Task<IEnumerable<Transaction>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return GetByCustomerIdAsync(customerId, cancellationToken);
    }

    public Task<IEnumerable<Transaction>> GetBySubstanceAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // Get all transactions in date range that have lines with the given substance
        var transactionIds = _transactionLines.Values
            .Where(l => l.SubstanceId == substanceId)
            .Select(l => l.TransactionId)
            .Distinct()
            .ToHashSet();

        var transactions = _transactions.Values
            .Where(t => transactionIds.Contains(t.Id) &&
                        t.TransactionDate >= fromDate &&
                        t.TransactionDate <= toDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        foreach (var t in transactions)
        {
            t.Lines = _transactionLines.Values
                .Where(l => l.TransactionId == t.Id)
                .OrderBy(l => l.LineNumber)
                .ToList();

            t.LicenceUsages = _licenceUsages.Values
                .Where(u => u.TransactionId == t.Id)
                .ToList();
        }

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }

    #endregion

    #region Transaction Lines

    public Task<IEnumerable<TransactionLine>> GetLinesAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var lines = _transactionLines.Values
            .Where(l => l.TransactionId == transactionId)
            .OrderBy(l => l.LineNumber)
            .ToList();

        return Task.FromResult<IEnumerable<TransactionLine>>(lines);
    }

    public Task<IEnumerable<TransactionLine>> GetLinesBySubstanceInPeriodAsync(
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var transactionIdsInPeriod = _transactions.Values
            .Where(t => t.TransactionDate >= fromDate &&
                        t.TransactionDate <= toDate &&
                        t.CanProceed())
            .Select(t => t.Id)
            .ToHashSet();

        var lines = _transactionLines.Values
            .Where(l => l.SubstanceId == substanceId &&
                        transactionIdsInPeriod.Contains(l.TransactionId))
            .ToList();

        return Task.FromResult<IEnumerable<TransactionLine>>(lines);
    }

    public Task<IEnumerable<TransactionLine>> GetCustomerSubstanceLinesInPeriodAsync(
        Guid customerId,
        Guid substanceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var transactionIdsInPeriod = _transactions.Values
            .Where(t => t.CustomerId == customerId &&
                        t.TransactionDate >= fromDate &&
                        t.TransactionDate <= toDate &&
                        t.CanProceed())
            .Select(t => t.Id)
            .ToHashSet();

        var lines = _transactionLines.Values
            .Where(l => l.SubstanceId == substanceId &&
                        transactionIdsInPeriod.Contains(l.TransactionId))
            .ToList();

        return Task.FromResult<IEnumerable<TransactionLine>>(lines);
    }

    #endregion

    #region Violations

    public Task<IEnumerable<TransactionViolation>> GetViolationsAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var violations = _violations.Values
            .Where(v => v.TransactionId == transactionId)
            .ToList();

        return Task.FromResult<IEnumerable<TransactionViolation>>(violations);
    }

    public Task AddViolationsAsync(Guid transactionId, IEnumerable<TransactionViolation> violations, CancellationToken cancellationToken = default)
    {
        foreach (var violation in violations)
        {
            if (violation.Id == Guid.Empty)
            {
                violation.Id = Guid.NewGuid();
            }
            violation.TransactionId = transactionId;
            _violations.TryAdd(violation.Id, violation);
        }

        return Task.CompletedTask;
    }

    public Task ClearViolationsAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var violationsToRemove = _violations.Values
            .Where(v => v.TransactionId == transactionId)
            .Select(v => v.Id)
            .ToList();

        foreach (var id in violationsToRemove)
        {
            _violations.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Licence Usage

    public Task<IEnumerable<TransactionLicenceUsage>> GetLicenceUsagesAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var usages = _licenceUsages.Values
            .Where(u => u.TransactionId == transactionId)
            .ToList();

        return Task.FromResult<IEnumerable<TransactionLicenceUsage>>(usages);
    }

    public Task AddLicenceUsageAsync(TransactionLicenceUsage usage, CancellationToken cancellationToken = default)
    {
        if (usage.Id == Guid.Empty)
        {
            usage.Id = Guid.NewGuid();
        }
        _licenceUsages.TryAdd(usage.Id, usage);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<TransactionLicenceUsage>> GetLicenceUsageInPeriodAsync(
        Guid licenceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var transactionIdsInPeriod = _transactions.Values
            .Where(t => t.TransactionDate >= fromDate &&
                        t.TransactionDate <= toDate)
            .Select(t => t.Id)
            .ToHashSet();

        var usages = _licenceUsages.Values
            .Where(u => u.LicenceId == licenceId &&
                        transactionIdsInPeriod.Contains(u.TransactionId))
            .ToList();

        return Task.FromResult<IEnumerable<TransactionLicenceUsage>>(usages);
    }

    #endregion

    #region Statistics

    public Task<int> GetPendingOverrideCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _transactions.Values
            .Count(t => t.RequiresOverride && t.OverrideStatus == OverrideStatus.Pending);

        return Task.FromResult(count);
    }

    public Task<int> GetBlockedTransactionCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _transactions.Values
            .Count(t => t.IsBlocked());

        return Task.FromResult(count);
    }

    #endregion

    #region Seeding

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            _transactions.TryAdd(transaction.Id, transaction);

            foreach (var line in transaction.Lines)
            {
                _transactionLines.TryAdd(line.Id, line);
            }

            foreach (var violation in transaction.Violations)
            {
                _violations.TryAdd(violation.Id, violation);
            }

            foreach (var usage in transaction.LicenceUsages)
            {
                _licenceUsages.TryAdd(usage.Id, usage);
            }
        }
    }

    #endregion
}
