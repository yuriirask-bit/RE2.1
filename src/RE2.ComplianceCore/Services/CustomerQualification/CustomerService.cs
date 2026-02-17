using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.CustomerQualification;

/// <summary>
/// Service for customer qualification management business logic.
/// Uses composite key (CustomerAccount + DataAreaId) per D365FO + Dataverse pattern.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        ILicenceRepository licenceRepository,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _licenceRepository = licenceRepository;
        _logger = logger;
    }

    public async Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetAllD365CustomersAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByApprovalStatusAsync(status, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByBusinessCategoryAsync(category, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetReVerificationDueAsync(daysAhead, cancellationToken);
    }

    public async Task<(Guid? Id, ValidationResult Result)> ConfigureComplianceAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        // Validate the customer
        var validationResult = await ValidateCustomerAsync(customer, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify customer exists in D365FO
        var d365Customer = await _customerRepository.GetD365CustomerAsync(
            customer.CustomerAccount, customer.DataAreaId, cancellationToken);

        if (d365Customer == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer '{customer.CustomerAccount}' not found in D365FO for data area '{customer.DataAreaId}'"
                }
            }));
        }

        // Check if compliance extension already exists
        var existing = await _customerRepository.GetByAccountAsync(
            customer.CustomerAccount, customer.DataAreaId, cancellationToken);
        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Compliance extension already configured for customer '{customer.CustomerAccount}'"
                }
            }));
        }

        // Set timestamps
        customer.CreatedDate = DateTime.UtcNow;
        customer.ModifiedDate = DateTime.UtcNow;

        var id = await _customerRepository.SaveComplianceExtensionAsync(customer, cancellationToken);
        _logger.LogInformation("Configured compliance for customer {Account}/{DataArea} with extension ID {Id}",
            customer.CustomerAccount, customer.DataAreaId, id);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateComplianceAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        // Check compliance extension exists
        var existing = await _customerRepository.GetByAccountAsync(
            customer.CustomerAccount, customer.DataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Compliance extension not found for customer '{customer.CustomerAccount}' in data area '{customer.DataAreaId}'"
                }
            });
        }

        // Validate the customer
        var validationResult = await ValidateCustomerAsync(customer, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Preserve the extension ID
        customer.ComplianceExtensionId = existing.ComplianceExtensionId;
        customer.ModifiedDate = DateTime.UtcNow;

        await _customerRepository.UpdateComplianceExtensionAsync(customer, cancellationToken);
        _logger.LogInformation("Updated compliance for customer {Account}/{DataArea}",
            customer.CustomerAccount, customer.DataAreaId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> RemoveComplianceAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var existing = await _customerRepository.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Compliance extension not found for customer '{customerAccount}' in data area '{dataAreaId}'"
                }
            });
        }

        await _customerRepository.DeleteComplianceExtensionAsync(customerAccount, dataAreaId, cancellationToken);
        _logger.LogInformation("Removed compliance extension for customer {Account}/{DataArea}",
            customerAccount, dataAreaId);

        return ValidationResult.Success();
    }

    public Task<ValidationResult> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var result = customer.Validate();
        return Task.FromResult(result);
    }

    public async Task<ValidationResult> SuspendCustomerAsync(string customerAccount, string dataAreaId, string reason, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (customer == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer '{customerAccount}' not found in data area '{dataAreaId}'"
                }
            });
        }

        customer.Suspend(reason);
        await _customerRepository.UpdateComplianceExtensionAsync(customer, cancellationToken);

        _logger.LogWarning("Suspended customer {Account}/{DataArea} ({BusinessName}): {Reason}",
            customerAccount, dataAreaId, customer.BusinessName, reason);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ReinstateCustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (customer == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer '{customerAccount}' not found in data area '{dataAreaId}'"
                }
            });
        }

        customer.Reinstate();
        await _customerRepository.UpdateComplianceExtensionAsync(customer, cancellationToken);

        _logger.LogInformation("Reinstated customer {Account}/{DataArea} ({BusinessName})",
            customerAccount, dataAreaId, customer.BusinessName);

        return ValidationResult.Success();
    }

    public async Task<CustomerComplianceStatus> GetComplianceStatusAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (customer == null)
        {
            return new CustomerComplianceStatus
            {
                CustomerAccount = customerAccount,
                DataAreaId = dataAreaId,
                BusinessName = "Unknown",
                ApprovalStatus = ApprovalStatus.Rejected,
                CanTransact = false,
                Warnings = new List<ComplianceWarning>
                {
                    new ComplianceWarning
                    {
                        WarningCode = ErrorCodes.NOT_FOUND,
                        Message = "Customer not found",
                        Severity = "Error"
                    }
                }
            };
        }

        var warnings = new List<ComplianceWarning>();

        // Check suspension
        if (customer.IsSuspended)
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = ErrorCodes.CUSTOMER_SUSPENDED,
                Message = $"Customer is suspended: {customer.SuspensionReason ?? "No reason provided"}",
                Severity = "Error"
            });
        }

        // Check approval status
        if (customer.ApprovalStatus == ApprovalStatus.Pending)
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = ErrorCodes.CUSTOMER_NOT_APPROVED,
                Message = "Customer qualification is pending",
                Severity = "Warning"
            });
        }
        else if (customer.ApprovalStatus == ApprovalStatus.Rejected)
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = ErrorCodes.CUSTOMER_NOT_APPROVED,
                Message = "Customer qualification was rejected",
                Severity = "Error"
            });
        }
        else if (customer.ApprovalStatus == ApprovalStatus.ConditionallyApproved)
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = "CONDITIONALLY_APPROVED",
                Message = "Customer is conditionally approved - review restrictions may apply",
                Severity = "Info"
            });
        }

        // Check re-verification due
        if (customer.IsReVerificationDue())
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = "REVERIFICATION_DUE",
                Message = $"Customer re-verification was due on {customer.NextReVerificationDate}",
                Severity = "Warning"
            });
        }
        else if (customer.NextReVerificationDate.HasValue &&
                 customer.NextReVerificationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)))
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = "REVERIFICATION_APPROACHING",
                Message = $"Customer re-verification due on {customer.NextReVerificationDate}",
                Severity = "Info"
            });
        }

        // Check GDP qualification for certain business categories
        if ((customer.BusinessCategory == BusinessCategory.WholesalerEU ||
             customer.BusinessCategory == BusinessCategory.WholesalerNonEU ||
             customer.BusinessCategory == BusinessCategory.Manufacturer) &&
            customer.GdpQualificationStatus != GdpQualificationStatus.Approved &&
            customer.GdpQualificationStatus != GdpQualificationStatus.NotRequired)
        {
            warnings.Add(new ComplianceWarning
            {
                WarningCode = "GDP_NOT_QUALIFIED",
                Message = $"Customer requires GDP qualification (current status: {customer.GdpQualificationStatus})",
                Severity = "Warning"
            });
        }

        return new CustomerComplianceStatus
        {
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            BusinessName = customer.BusinessName,
            ApprovalStatus = customer.ApprovalStatus,
            GdpQualificationStatus = customer.GdpQualificationStatus,
            IsSuspended = customer.IsSuspended,
            SuspensionReason = customer.SuspensionReason,
            CanTransact = customer.CanTransact(),
            NextReVerificationDate = customer.NextReVerificationDate,
            IsReVerificationDue = customer.IsReVerificationDue(),
            Warnings = warnings
        };
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.SearchByNameAsync(searchTerm, cancellationToken);
    }
}
