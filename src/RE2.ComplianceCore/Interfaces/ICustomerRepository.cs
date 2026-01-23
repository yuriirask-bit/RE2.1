using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Customer entity operations.
/// T088: Repository interface for customer CRUD operations.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by business name.
    /// </summary>
    Task<Customer?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by registration number.
    /// </summary>
    Task<Customer?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers.
    /// </summary>
    Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by approval status.
    /// </summary>
    Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by business category.
    /// </summary>
    Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by country.
    /// </summary>
    Task<IEnumerable<Customer>> GetByCountryAsync(string country, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suspended customers.
    /// </summary>
    Task<IEnumerable<Customer>> GetSuspendedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers with re-verification due within specified days.
    /// Per FR-017: periodic re-verification tracking.
    /// </summary>
    Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers that can transact (approved or conditionally approved, not suspended).
    /// </summary>
    Task<IEnumerable<Customer>> GetCanTransactAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    Task<Guid> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    Task DeleteAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists by ID.
    /// </summary>
    Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by business name (partial match).
    /// </summary>
    Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}
