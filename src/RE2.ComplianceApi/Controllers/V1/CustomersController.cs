using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Customer compliance API endpoints
/// T091: CustomersController v1 with compliance status endpoint (FR-060)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets customer compliance status.
    /// Per FR-060: Customer compliance status lookup.
    /// </summary>
    [HttpGet("{id}/compliance-status")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComplianceStatus(Guid id, CancellationToken cancellationToken = default)
    {
        var status = await _customerService.GetComplianceStatusAsync(id, cancellationToken);

        // Check if customer was not found
        if (status.Warnings.Any(w => w.WarningCode == ErrorCodes.NOT_FOUND))
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Customer with ID '{id}' not found"
            });
        }

        return Ok(CustomerComplianceStatusResponse.FromDomain(status));
    }

    /// <summary>
    /// Gets all customers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? country = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Customer> customers;

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var approvalStatus))
        {
            customers = await _customerService.GetByApprovalStatusAsync(approvalStatus, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(category) && Enum.TryParse<BusinessCategory>(category, true, out var businessCategory))
        {
            customers = await _customerService.GetByBusinessCategoryAsync(businessCategory, cancellationToken);
        }
        else
        {
            customers = await _customerService.GetAllAsync(cancellationToken);
        }

        // Filter by country if provided
        if (!string.IsNullOrEmpty(country))
        {
            customers = customers.Where(c => c.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(customers.Select(CustomerResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Customer with ID '{id}' not found"
            });
        }

        return Ok(CustomerResponseDto.FromDomain(customer));
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var customer = request.ToDomain();
        var (id, result) = await _customerService.CreateAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        customer.CustomerId = id!.Value;
        var createdCustomer = await _customerService.GetByIdAsync(id.Value, cancellationToken);

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = id.Value },
            CustomerResponseDto.FromDomain(createdCustomer!));
    }

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var customer = request.ToDomain(id);
        var result = await _customerService.UpdateAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = result.Violations.First().Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = errorCode,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var updatedCustomer = await _customerService.GetByIdAsync(id, cancellationToken);
        return Ok(CustomerResponseDto.FromDomain(updatedCustomer!));
    }

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Suspends a customer.
    /// Per FR-015: Compliance managers can mark a customer as suspended.
    /// </summary>
    [HttpPost("{id}/suspend")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendCustomer(Guid id, [FromBody] SuspendCustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.SuspendCustomerAsync(id, request.Reason, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        var status = await _customerService.GetComplianceStatusAsync(id, cancellationToken);
        return Ok(CustomerComplianceStatusResponse.FromDomain(status));
    }

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    [HttpPost("{id}/reinstate")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReinstateCustomer(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.ReinstateCustomerAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        var status = await _customerService.GetComplianceStatusAsync(id, cancellationToken);
        return Ok(CustomerComplianceStatusResponse.FromDomain(status));
    }

    /// <summary>
    /// Searches customers by name.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCustomers([FromQuery] string q, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Enumerable.Empty<CustomerResponseDto>());
        }

        var customers = await _customerService.SearchByNameAsync(q, cancellationToken);
        return Ok(customers.Select(CustomerResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets customers due for re-verification.
    /// Per FR-017: Periodic re-verification tracking.
    /// </summary>
    [HttpGet("reverification-due")]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReVerificationDue([FromQuery] int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetReVerificationDueAsync(daysAhead, cancellationToken);
        return Ok(customers.Select(CustomerResponseDto.FromDomain));
    }
}

#region DTOs

/// <summary>
/// Customer response DTO.
/// </summary>
public class CustomerResponseDto
{
    public Guid CustomerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string BusinessCategory { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public string GdpQualificationStatus { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool CanTransact { get; set; }

    public static CustomerResponseDto FromDomain(Customer customer)
    {
        return new CustomerResponseDto
        {
            CustomerId = customer.CustomerId,
            BusinessName = customer.BusinessName,
            RegistrationNumber = customer.RegistrationNumber,
            BusinessCategory = customer.BusinessCategory.ToString(),
            Country = customer.Country,
            ApprovalStatus = customer.ApprovalStatus.ToString(),
            OnboardingDate = customer.OnboardingDate?.ToString("yyyy-MM-dd"),
            NextReVerificationDate = customer.NextReVerificationDate?.ToString("yyyy-MM-dd"),
            GdpQualificationStatus = customer.GdpQualificationStatus.ToString(),
            IsSuspended = customer.IsSuspended,
            SuspensionReason = customer.SuspensionReason,
            CanTransact = customer.CanTransact()
        };
    }
}

/// <summary>
/// Customer compliance status response DTO.
/// </summary>
public class CustomerComplianceStatusResponse
{
    public Guid CustomerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string GdpQualificationStatus { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool CanTransact { get; set; }
    public string? NextReVerificationDate { get; set; }
    public bool IsReVerificationDue { get; set; }
    public List<ComplianceWarningDto> Warnings { get; set; } = new();

    public static CustomerComplianceStatusResponse FromDomain(CustomerComplianceStatus status)
    {
        return new CustomerComplianceStatusResponse
        {
            CustomerId = status.CustomerId,
            BusinessName = status.BusinessName,
            ApprovalStatus = status.ApprovalStatus.ToString(),
            GdpQualificationStatus = status.GdpQualificationStatus.ToString(),
            IsSuspended = status.IsSuspended,
            SuspensionReason = status.SuspensionReason,
            CanTransact = status.CanTransact,
            NextReVerificationDate = status.NextReVerificationDate?.ToString("yyyy-MM-dd"),
            IsReVerificationDue = status.IsReVerificationDue,
            Warnings = status.Warnings.Select(w => new ComplianceWarningDto
            {
                WarningCode = w.WarningCode,
                Message = w.Message,
                Severity = w.Severity
            }).ToList()
        };
    }
}

/// <summary>
/// Compliance warning DTO.
/// </summary>
public class ComplianceWarningDto
{
    public string WarningCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// Create customer request DTO.
/// </summary>
public class CreateCustomerRequestDto
{
    public required string BusinessName { get; set; }
    public string? RegistrationNumber { get; set; }
    public required string BusinessCategory { get; set; }
    public required string Country { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public string GdpQualificationStatus { get; set; } = "NotRequired";

    public Customer ToDomain()
    {
        return new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = BusinessName,
            RegistrationNumber = RegistrationNumber,
            BusinessCategory = Enum.Parse<BusinessCategory>(BusinessCategory, true),
            Country = Country,
            ApprovalStatus = Enum.Parse<ApprovalStatus>(ApprovalStatus, true),
            OnboardingDate = string.IsNullOrEmpty(OnboardingDate) ? null : DateOnly.Parse(OnboardingDate),
            NextReVerificationDate = string.IsNullOrEmpty(NextReVerificationDate) ? null : DateOnly.Parse(NextReVerificationDate),
            GdpQualificationStatus = Enum.Parse<GdpQualificationStatus>(GdpQualificationStatus, true),
            IsSuspended = false
        };
    }
}

/// <summary>
/// Update customer request DTO.
/// </summary>
public class UpdateCustomerRequestDto
{
    public required string BusinessName { get; set; }
    public string? RegistrationNumber { get; set; }
    public required string BusinessCategory { get; set; }
    public required string Country { get; set; }
    public required string ApprovalStatus { get; set; }
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public required string GdpQualificationStatus { get; set; }

    public Customer ToDomain(Guid customerId)
    {
        return new Customer
        {
            CustomerId = customerId,
            BusinessName = BusinessName,
            RegistrationNumber = RegistrationNumber,
            BusinessCategory = Enum.Parse<BusinessCategory>(BusinessCategory, true),
            Country = Country,
            ApprovalStatus = Enum.Parse<ApprovalStatus>(ApprovalStatus, true),
            OnboardingDate = string.IsNullOrEmpty(OnboardingDate) ? null : DateOnly.Parse(OnboardingDate),
            NextReVerificationDate = string.IsNullOrEmpty(NextReVerificationDate) ? null : DateOnly.Parse(NextReVerificationDate),
            GdpQualificationStatus = Enum.Parse<GdpQualificationStatus>(GdpQualificationStatus, true),
            IsSuspended = false
        };
    }
}

/// <summary>
/// Suspend customer request DTO.
/// </summary>
public class SuspendCustomerRequestDto
{
    public required string Reason { get; set; }
}

#endregion
