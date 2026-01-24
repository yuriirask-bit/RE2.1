using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ICustomerRepository for local development and testing.
/// </summary>
public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default)
    {
        var customer = _customers.Values
            .FirstOrDefault(c => c.BusinessName.Equals(businessName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        var customer = _customers.Values
            .FirstOrDefault(c => c.RegistrationNumber != null &&
                c.RegistrationNumber.Equals(registrationNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(customer);
    }

    public Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Customer>>(_customers.Values.ToList());
    }

    public Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values.Where(c => c.ApprovalStatus == status).ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values.Where(c => c.BusinessCategory == category).ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values
            .Where(c => c.Country.Equals(country, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetSuspendedAsync(CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values.Where(c => c.IsSuspended).ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(daysAhead);

        var customers = _customers.Values
            .Where(c => c.NextReVerificationDate.HasValue && c.NextReVerificationDate.Value <= threshold)
            .ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetCanTransactAsync(CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values.Where(c => c.CanTransact()).ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<Guid> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (customer.CustomerId == Guid.Empty)
            customer.CustomerId = Guid.NewGuid();

        customer.CreatedDate = DateTime.UtcNow;
        customer.ModifiedDate = DateTime.UtcNow;

        _customers[customer.CustomerId] = customer;
        return Task.FromResult(customer.CustomerId);
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        customer.ModifiedDate = DateTime.UtcNow;
        _customers[customer.CustomerId] = customer;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _customers.TryRemove(customerId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_customers.ContainsKey(customerId));
    }

    public Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var customers = _customers.Values
            .Where(c => c.BusinessName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    /// <summary>
    /// Seeds initial data for testing. Called by InMemorySeedData.
    /// </summary>
    internal void Seed(IEnumerable<Customer> customers)
    {
        foreach (var customer in customers)
        {
            _customers.TryAdd(customer.CustomerId, customer);
        }
    }
}
