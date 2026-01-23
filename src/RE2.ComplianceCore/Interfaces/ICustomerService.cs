using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Service interface for customer business logic.
/// T090: Service interface for customer qualification management.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

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
    /// Gets customers with re-verification due within specified days.
    /// </summary>
    Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer after validation.
    /// </summary>
    Task<(Guid? Id, ValidationResult Result)> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer after validation.
    /// </summary>
    Task<ValidationResult> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    Task<ValidationResult> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a customer meets business rules.
    /// </summary>
    Task<ValidationResult> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a customer with a reason.
    /// </summary>
    Task<ValidationResult> SuspendCustomerAsync(Guid customerId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    Task<ValidationResult> ReinstateCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the compliance status for a customer.
    /// Per FR-060: Customer compliance status lookup.
    /// </summary>
    Task<CustomerComplianceStatus> GetComplianceStatusAsync(Guid customerId, CancellationToken cancellationToken = default);

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
    public Guid CustomerId { get; set; }
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
