using Microsoft.AspNetCore.Mvc;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Customer compliance API endpoints
/// T091: CustomersController v1 with compliance status endpoint
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ILogger<CustomersController> logger)
    {
        _logger = logger;
    }

    // GET: api/v1/customers/{id}/compliance-status
    [HttpGet("{id}/compliance-status")]
    public IActionResult GetComplianceStatus(Guid id)
    {
        // TODO: Implement customer compliance status lookup (FR-060)
        // Returns: approval status, held licences, missing licences, warnings
        return Ok(new
        {
            customerId = id,
            approvalStatus = "Approved",
            complianceWarnings = new List<string>(),
            message = "Customer compliance status - to be implemented"
        });
    }

    // GET: api/v1/customers
    [HttpGet]
    public IActionResult GetCustomers()
    {
        // TODO: Implement customer listing
        return Ok(new { message = "Customer listing - to be implemented" });
    }

    // GET: api/v1/customers/{id}
    [HttpGet("{id}")]
    public IActionResult GetCustomer(Guid id)
    {
        // TODO: Implement customer retrieval
        return Ok(new { message = $"Get customer {id} - to be implemented" });
    }
}
