using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.CustomerQualification;

/// <summary>
/// Service for customer qualification management business logic.
/// T090: Business logic for customer qualification including validation and compliance status.
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

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByIdAsync(customerId, cancellationToken);
    }

    public async Task<Customer?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByRegistrationNumberAsync(registrationNumber, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetAllAsync(cancellationToken);
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

    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        // Validate the customer
        var validationResult = await ValidateCustomerAsync(customer, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Check for duplicate registration number
        if (!string.IsNullOrEmpty(customer.RegistrationNumber))
        {
            var existing = await _customerRepository.GetByRegistrationNumberAsync(customer.RegistrationNumber, cancellationToken);
            if (existing != null)
            {
                return (null, ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"Customer with registration number '{customer.RegistrationNumber}' already exists"
                    }
                }));
            }
        }

        // Set timestamps
        customer.CreatedDate = DateTime.UtcNow;
        customer.ModifiedDate = DateTime.UtcNow;

        var id = await _customerRepository.CreateAsync(customer, cancellationToken);
        _logger.LogInformation("Created customer {BusinessName} with ID {Id}", customer.BusinessName, id);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        // Check customer exists
        var existing = await _customerRepository.GetByIdAsync(customer.CustomerId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer with ID '{customer.CustomerId}' not found"
                }
            });
        }

        // Validate the customer
        var validationResult = await ValidateCustomerAsync(customer, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Check for duplicate registration number (if changed)
        if (!string.IsNullOrEmpty(customer.RegistrationNumber) &&
            existing.RegistrationNumber != customer.RegistrationNumber)
        {
            var duplicate = await _customerRepository.GetByRegistrationNumberAsync(customer.RegistrationNumber, cancellationToken);
            if (duplicate != null)
            {
                return ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"Customer with registration number '{customer.RegistrationNumber}' already exists"
                    }
                });
            }
        }

        // Update timestamp
        customer.ModifiedDate = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        _logger.LogInformation("Updated customer {BusinessName} with ID {Id}", customer.BusinessName, customer.CustomerId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var existing = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer with ID '{customerId}' not found"
                }
            });
        }

        await _customerRepository.DeleteAsync(customerId, cancellationToken);
        _logger.LogInformation("Deleted customer {Id}", customerId);

        return ValidationResult.Success();
    }

    public Task<ValidationResult> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        // Use model validation
        var result = customer.Validate();
        return Task.FromResult(result);
    }

    public async Task<ValidationResult> SuspendCustomerAsync(Guid customerId, string reason, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer with ID '{customerId}' not found"
                }
            });
        }

        customer.Suspend(reason);
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        _logger.LogWarning("Suspended customer {CustomerId} ({BusinessName}): {Reason}",
            customerId, customer.BusinessName, reason);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ReinstateCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer with ID '{customerId}' not found"
                }
            });
        }

        customer.Reinstate();
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        _logger.LogInformation("Reinstated customer {CustomerId} ({BusinessName})",
            customerId, customer.BusinessName);

        return ValidationResult.Success();
    }

    public async Task<CustomerComplianceStatus> GetComplianceStatusAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return new CustomerComplianceStatus
            {
                CustomerId = customerId,
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
            CustomerId = customer.CustomerId,
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
