using Microsoft.AspNetCore.Mvc;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Licence management API endpoints
/// T075: LicencesController v1 with GET, POST, PUT, DELETE
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class LicencesController : ControllerBase
{
    private readonly ILogger<LicencesController> _logger;

    public LicencesController(ILogger<LicencesController> logger)
    {
        _logger = logger;
    }

    // GET: api/v1/licences
    [HttpGet]
    public IActionResult GetLicences()
    {
        // TODO: Implement licence listing with filtering
        return Ok(new { message = "Licence listing - to be implemented" });
    }

    // GET: api/v1/licences/{id}
    [HttpGet("{id}")]
    public IActionResult GetLicence(Guid id)
    {
        // TODO: Implement licence retrieval by ID
        return Ok(new { message = $"Get licence {id} - to be implemented" });
    }

    // POST: api/v1/licences
    [HttpPost]
    public IActionResult CreateLicence()
    {
        // TODO: Implement licence creation
        return Ok(new { message = "Create licence - to be implemented" });
    }

    // PUT: api/v1/licences/{id}
    [HttpPut("{id}")]
    public IActionResult UpdateLicence(Guid id)
    {
        // TODO: Implement licence update
        return Ok(new { message = $"Update licence {id} - to be implemented" });
    }

    // DELETE: api/v1/licences/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteLicence(Guid id)
    {
        // TODO: Implement licence soft delete
        return Ok(new { message = $"Delete licence {id} - to be implemented" });
    }
}
