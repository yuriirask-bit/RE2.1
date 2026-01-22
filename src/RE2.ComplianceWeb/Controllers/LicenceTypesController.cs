using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for licence type management web UI.
/// T078: Web UI controller for licence type CRUD operations.
/// </summary>
[Authorize]
public class LicenceTypesController : Controller
{
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILogger<LicenceTypesController> _logger;

    public LicenceTypesController(
        ILicenceTypeRepository licenceTypeRepository,
        ILogger<LicenceTypesController> logger)
    {
        _licenceTypeRepository = licenceTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Displays the licence type list page.
    /// </summary>
    public async Task<IActionResult> Index(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var licenceTypes = activeOnly == true
            ? await _licenceTypeRepository.GetAllActiveAsync(cancellationToken)
            : await _licenceTypeRepository.GetAllAsync(cancellationToken);

        ViewBag.ActiveOnlyFilter = activeOnly;
        return View(licenceTypes);
    }

    /// <summary>
    /// Displays licence type details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var licenceType = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);

        if (licenceType == null)
        {
            return NotFound();
        }

        return View(licenceType);
    }

    /// <summary>
    /// Displays the create licence type form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public IActionResult Create()
    {
        return View(new LicenceTypeCreateViewModel());
    }

    /// <summary>
    /// Handles licence type creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(LicenceTypeCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = model.Name,
            IssuingAuthority = model.IssuingAuthority,
            TypicalValidityMonths = model.TypicalValidityMonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities,
            IsActive = model.IsActive
        };

        // Validate the licence type
        var validationResult = licenceType.Validate();
        if (!validationResult.IsValid)
        {
            foreach (var violation in validationResult.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        // Check for duplicate name
        var existing = await _licenceTypeRepository.GetByNameAsync(licenceType.Name, cancellationToken);
        if (existing != null)
        {
            ModelState.AddModelError("Name", $"A licence type with name '{licenceType.Name}' already exists.");
            return View(model);
        }

        var id = await _licenceTypeRepository.CreateAsync(licenceType, cancellationToken);

        _logger.LogInformation("Created licence type {Name} with ID {Id} via web UI", licenceType.Name, id);
        TempData["SuccessMessage"] = $"Licence type '{licenceType.Name}' created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit licence type form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var licenceType = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);

        if (licenceType == null)
        {
            return NotFound();
        }

        var model = new LicenceTypeEditViewModel
        {
            LicenceTypeId = licenceType.LicenceTypeId,
            Name = licenceType.Name,
            IssuingAuthority = licenceType.IssuingAuthority,
            TypicalValidityMonths = licenceType.TypicalValidityMonths,
            PermittedActivities = (int)licenceType.PermittedActivities,
            IsActive = licenceType.IsActive
        };

        return View(model);
    }

    /// <summary>
    /// Handles licence type edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, LicenceTypeEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.LicenceTypeId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check licence type exists
        var existing = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var licenceType = new LicenceType
        {
            LicenceTypeId = model.LicenceTypeId,
            Name = model.Name,
            IssuingAuthority = model.IssuingAuthority,
            TypicalValidityMonths = model.TypicalValidityMonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities,
            IsActive = model.IsActive
        };

        // Validate the licence type
        var validationResult = licenceType.Validate();
        if (!validationResult.IsValid)
        {
            foreach (var violation in validationResult.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        // Check for duplicate name (if changed)
        if (existing.Name != licenceType.Name)
        {
            var duplicate = await _licenceTypeRepository.GetByNameAsync(licenceType.Name, cancellationToken);
            if (duplicate != null)
            {
                ModelState.AddModelError("Name", $"A licence type with name '{licenceType.Name}' already exists.");
                return View(model);
            }
        }

        await _licenceTypeRepository.UpdateAsync(licenceType, cancellationToken);

        _logger.LogInformation("Updated licence type {Name} with ID {Id} via web UI", licenceType.Name, id);
        TempData["SuccessMessage"] = $"Licence type '{licenceType.Name}' updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles licence type deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            TempData["ErrorMessage"] = "Licence type not found.";
            return RedirectToAction(nameof(Index));
        }

        await _licenceTypeRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Deleted licence type {Id} via web UI", id);
        TempData["SuccessMessage"] = $"Licence type '{existing.Name}' deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Toggles the active status of a licence type.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken cancellationToken = default)
    {
        var licenceType = await _licenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (licenceType == null)
        {
            TempData["ErrorMessage"] = "Licence type not found.";
            return RedirectToAction(nameof(Index));
        }

        licenceType.IsActive = !licenceType.IsActive;
        await _licenceTypeRepository.UpdateAsync(licenceType, cancellationToken);

        var status = licenceType.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Toggled licence type {Id} to {Status} via web UI", id, status);
        TempData["SuccessMessage"] = $"Licence type '{licenceType.Name}' {status} successfully.";

        return RedirectToAction(nameof(Index));
    }
}

#region View Models

/// <summary>
/// View model for creating a new licence type.
/// </summary>
public class LicenceTypeCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public int? TypicalValidityMonths { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// View model for editing a licence type.
/// </summary>
public class LicenceTypeEditViewModel
{
    public Guid LicenceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public int? TypicalValidityMonths { get; set; }
    public int PermittedActivities { get; set; }
    public bool IsActive { get; set; }
}

#endregion
