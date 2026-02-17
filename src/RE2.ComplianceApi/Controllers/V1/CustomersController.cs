using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Customer compliance API endpoints.
/// T091: CustomersController v1 with compliance status endpoint (FR-060).
/// T098: Route authorization - SalesAdmin and ComplianceManager can create/modify customers.
/// Composite key: CustomerAccount (string) + DataAreaId (string) per D365FO + Dataverse pattern.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
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
    [HttpGet("{customerAccount}/compliance-status")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComplianceStatus(
        string customerAccount,
        [FromQuery] string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var status = await _customerService.GetComplianceStatusAsync(customerAccount, dataAreaId, cancellationToken);

        // Check if customer was not found
        if (status.Warnings.Any(w => w.WarningCode == ErrorCodes.NOT_FOUND))
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Customer '{customerAccount}' in data area '{dataAreaId}' not found"
            });
        }

        return Ok(CustomerComplianceStatusResponse.FromDomain(status));
    }

    /// <summary>
    /// Gets all compliance-configured customers.
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
    /// Gets all D365FO customers (master data browse).
    /// </summary>
    [HttpGet("d365")]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetD365Customers(CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetAllD365CustomersAsync(cancellationToken);
        return Ok(customers.Select(CustomerResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a customer by composite key.
    /// </summary>
    [HttpGet("{customerAccount}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(
        string customerAccount,
        [FromQuery] string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (customer == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Customer '{customerAccount}' in data area '{dataAreaId}' not found"
            });
        }

        return Ok(CustomerResponseDto.FromDomain(customer));
    }

    /// <summary>
    /// Configures compliance extension for a D365FO customer.
    /// T098: SalesAdmin or ComplianceManager can configure compliance.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SalesAdmin,ComplianceManager")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfigureCompliance(
        [FromBody] ConfigureCustomerComplianceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var customer = request.ToDomain();
        var (id, result) = await _customerService.ConfigureComplianceAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        customer.ComplianceExtensionId = id!.Value;
        var configuredCustomer = await _customerService.GetByAccountAsync(
            customer.CustomerAccount, customer.DataAreaId, cancellationToken);

        return CreatedAtAction(
            nameof(GetCustomer),
            new { customerAccount = customer.CustomerAccount, dataAreaId = customer.DataAreaId },
            CustomerResponseDto.FromDomain(configuredCustomer!));
    }

    /// <summary>
    /// Updates an existing compliance extension.
    /// T098: SalesAdmin or ComplianceManager can modify compliance.
    /// </summary>
    [HttpPut("{customerAccount}")]
    [Authorize(Roles = "SalesAdmin,ComplianceManager")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompliance(
        string customerAccount,
        [FromQuery] string dataAreaId,
        [FromBody] UpdateCustomerComplianceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var customer = request.ToDomain(customerAccount, dataAreaId);
        var result = await _customerService.UpdateComplianceAsync(customer, cancellationToken);

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

        var updatedCustomer = await _customerService.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        return Ok(CustomerResponseDto.FromDomain(updatedCustomer!));
    }

    /// <summary>
    /// Removes compliance extension for a customer.
    /// T098: SalesAdmin or ComplianceManager can remove compliance configuration.
    /// </summary>
    [HttpDelete("{customerAccount}")]
    [Authorize(Roles = "SalesAdmin,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCompliance(
        string customerAccount,
        [FromQuery] string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.RemoveComplianceAsync(customerAccount, dataAreaId, cancellationToken);

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
    /// T098: Only ComplianceManager role can suspend customers.
    /// </summary>
    [HttpPost("{customerAccount}/suspend")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendCustomer(
        string customerAccount,
        [FromQuery] string dataAreaId,
        [FromBody] SuspendCustomerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.SuspendCustomerAsync(customerAccount, dataAreaId, request.Reason, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        var status = await _customerService.GetComplianceStatusAsync(customerAccount, dataAreaId, cancellationToken);
        return Ok(CustomerComplianceStatusResponse.FromDomain(status));
    }

    /// <summary>
    /// Reinstates a suspended customer.
    /// T098: Only ComplianceManager role can reinstate customers.
    /// </summary>
    [HttpPost("{customerAccount}/reinstate")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(CustomerComplianceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReinstateCustomer(
        string customerAccount,
        [FromQuery] string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.ReinstateCustomerAsync(customerAccount, dataAreaId, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = result.Violations.First().Message
            });
        }

        var status = await _customerService.GetComplianceStatusAsync(customerAccount, dataAreaId, cancellationToken);
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
/// Composite key: CustomerAccount + DataAreaId.
/// </summary>
public class CustomerResponseDto
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public string GdpQualificationStatus { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool CanTransact { get; set; }
    public bool IsComplianceConfigured { get; set; }

    public static CustomerResponseDto FromDomain(Customer customer)
    {
        return new CustomerResponseDto
        {
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            BusinessName = customer.BusinessName,
            BusinessCategory = customer.BusinessCategory.ToString(),
            Country = customer.Country,
            ApprovalStatus = customer.ApprovalStatus.ToString(),
            OnboardingDate = customer.OnboardingDate?.ToString("yyyy-MM-dd"),
            NextReVerificationDate = customer.NextReVerificationDate?.ToString("yyyy-MM-dd"),
            GdpQualificationStatus = customer.GdpQualificationStatus.ToString(),
            IsSuspended = customer.IsSuspended,
            SuspensionReason = customer.SuspensionReason,
            CanTransact = customer.CanTransact(),
            IsComplianceConfigured = customer.IsComplianceConfigured
        };
    }
}

/// <summary>
/// Customer compliance status response DTO.
/// </summary>
public class CustomerComplianceStatusResponse
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
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
            CustomerAccount = status.CustomerAccount,
            DataAreaId = status.DataAreaId,
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
/// Configure compliance request DTO.
/// Used to set up compliance extension for a D365FO customer.
/// BusinessName and Country come from D365FO master data, not from this request.
/// </summary>
public class ConfigureCustomerComplianceRequestDto
{
    public required string CustomerAccount { get; set; }
    public required string DataAreaId { get; set; }
    public required string BusinessCategory { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public string GdpQualificationStatus { get; set; } = "NotRequired";

    public Customer ToDomain()
    {
        return new Customer
        {
            CustomerAccount = CustomerAccount,
            DataAreaId = DataAreaId,
            BusinessCategory = Enum.Parse<BusinessCategory>(BusinessCategory, true),
            ApprovalStatus = Enum.Parse<ApprovalStatus>(ApprovalStatus, true),
            OnboardingDate = string.IsNullOrEmpty(OnboardingDate) ? null : DateOnly.Parse(OnboardingDate),
            NextReVerificationDate = string.IsNullOrEmpty(NextReVerificationDate) ? null : DateOnly.Parse(NextReVerificationDate),
            GdpQualificationStatus = Enum.Parse<GdpQualificationStatus>(GdpQualificationStatus, true),
            IsSuspended = false
        };
    }
}

/// <summary>
/// Update compliance request DTO.
/// Updates compliance extension fields only. Master data fields are read-only from D365FO.
/// </summary>
public class UpdateCustomerComplianceRequestDto
{
    public required string BusinessCategory { get; set; }
    public required string ApprovalStatus { get; set; }
    public string? OnboardingDate { get; set; }
    public string? NextReVerificationDate { get; set; }
    public required string GdpQualificationStatus { get; set; }

    public Customer ToDomain(string customerAccount, string dataAreaId)
    {
        return new Customer
        {
            CustomerAccount = customerAccount,
            DataAreaId = dataAreaId,
            BusinessCategory = Enum.Parse<BusinessCategory>(BusinessCategory, true),
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
