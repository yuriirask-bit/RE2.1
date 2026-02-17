using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for customer business logic.
/// Uses composite key (CustomerAccount + DataAreaId) per D365FO + Dataverse pattern.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets a customer by composite key.
    /// </summary>
    Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all compliance-configured customers.
    /// </summary>
    Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all D365FO customers (master data).
    /// </summary>
    Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by approval status.
    /// </summary>
    Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by business category.
    /// </summary>
    Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers with re-verification due within specified days.
    /// </summary>
    Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures compliance extension for a D365FO customer.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> ConfigureComplianceAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing compliance extension.
    /// </summary>
    Task<ValidationResult> UpdateComplianceAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes compliance extension for a customer.
    /// </summary>
    Task<ValidationResult> RemoveComplianceAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a customer meets business rules.
    /// </summary>
    Task<ValidationResult> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a customer with a reason.
    /// </summary>
    Task<ValidationResult> SuspendCustomerAsync(string customerAccount, string dataAreaId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    Task<ValidationResult> ReinstateCustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the compliance status for a customer.
    /// Per FR-060: Customer compliance status lookup.
    /// </summary>
    Task<CustomerComplianceStatus> GetComplianceStatusAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by business name (partial match).
    /// </summary>
    Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer compliance status including approval status, licences, and warnings.
/// Per FR-060: Customer compliance status for transaction validation.
/// </summary>
public class CustomerComplianceStatus
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; set; }
    public GdpQualificationStatus GdpQualificationStatus { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool CanTransact { get; set; }
    public DateOnly? NextReVerificationDate { get; set; }
    public bool IsReVerificationDue { get; set; }
    public List<ComplianceWarning> Warnings { get; set; } = new();
}

/// <summary>
/// A compliance warning for a customer.
/// </summary>
public class ComplianceWarning
{
    public string WarningCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning"; // Warning, Error, Info
}
