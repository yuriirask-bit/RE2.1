using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Customer entity operations.
/// Supports composite D365FO + Dataverse pattern with CustomerAccount/DataAreaId composite key.
/// </summary>
public interface ICustomerRepository
{
    // D365FO customer queries (read-only master data)

    /// <summary>
    /// Gets all customers from D365FO (master data).
    /// </summary>
    Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single D365FO customer by composite key.
    /// </summary>
    Task<Customer?> GetD365CustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    // Composite queries (D365FO + compliance extension merged)

    /// <summary>
    /// Gets a customer by composite key with compliance extension merged.
    /// </summary>
    Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all compliance-configured customers (with extensions).
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

    // Compliance extension CRUD

    /// <summary>
    /// Saves a new compliance extension for a customer.
    /// </summary>
    Task<Guid> SaveComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing compliance extension.
    /// </summary>
    Task UpdateComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a compliance extension by composite key.
    /// </summary>
    Task DeleteComplianceExtensionAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists by composite key (in D365FO or compliance extensions).
    /// </summary>
    Task<bool> ExistsAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by organization name (partial match).
    /// </summary>
    Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}
