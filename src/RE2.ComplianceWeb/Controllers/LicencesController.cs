using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for licence management web UI.
/// T077: Web UI controller for licence CRUD operations.
/// T094: Extended to support associating licences with customers.
/// </summary>
[Authorize]
public class LicencesController : Controller
{
    private readonly ILicenceService _licenceService;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILicenceSubstanceMappingService _mappingService;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ICustomerService _customerService;
    private readonly ILogger<LicencesController> _logger;

    public LicencesController(
        ILicenceService licenceService,
        ILicenceTypeRepository licenceTypeRepository,
        ILicenceSubstanceMappingService mappingService,
        IControlledSubstanceRepository substanceRepository,
        ICustomerService customerService,
        ILogger<LicencesController> logger)
    {
        _licenceService = licenceService;
        _licenceTypeRepository = licenceTypeRepository;
        _mappingService = mappingService;
        _substanceRepository = substanceRepository;
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Loads licence types for dropdown lists.
    /// </summary>
    private async Task<SelectList> GetLicenceTypeSelectListAsync(Guid? selectedId = null, CancellationToken cancellationToken = default)
    {
        var licenceTypes = await _licenceTypeRepository.GetAllActiveAsync(cancellationToken);
        return new SelectList(licenceTypes, "LicenceTypeId", "Name", selectedId);
    }

    /// <summary>
    /// Loads customers for dropdown lists.
    /// T094: Customer selection for licence association.
    /// </summary>
    private async Task<SelectList> GetCustomerSelectListAsync(Guid? selectedId = null, CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        var customerList = customers
            .OrderBy(c => c.BusinessName)
            .Select(c => new { c.CustomerId, DisplayName = $"{c.BusinessName} ({c.Country})" });
        return new SelectList(customerList, "CustomerId", "DisplayName", selectedId);
    }

    /// <summary>
    /// Gets customer name by ID for display purposes.
    /// T094: Display customer name in licence details.
    /// </summary>
    private async Task<string?> GetCustomerNameAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        return customer?.BusinessName;
    }

    /// <summary>
    /// Displays the licence list page.
    /// </summary>
    public async Task<IActionResult> Index(string? status = null, CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(status))
        {
            licences = licences.Where(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.StatusFilter = status;
        return View(licences);
    }

    /// <summary>
    /// Displays the create licence form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(cancellationToken: cancellationToken);
        ViewBag.Customers = await GetCustomerSelectListAsync(cancellationToken: cancellationToken);
        return View(new LicenceCreateViewModel());
    }

    /// <summary>
    /// Handles licence creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Create(LicenceCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            ViewBag.Customers = await GetCustomerSelectListAsync(model.HolderType == "Customer" ? model.HolderId : null, cancellationToken);
            return View(model);
        }

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = model.LicenceNumber,
            LicenceTypeId = model.LicenceTypeId,
            HolderType = model.HolderType,
            HolderId = model.HolderId,
            IssuingAuthority = model.IssuingAuthority,
            IssueDate = DateOnly.FromDateTime(model.IssueDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            Status = "Valid",
            Scope = model.Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities
        };

        var (id, result) = await _licenceService.CreateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            ViewBag.Customers = await GetCustomerSelectListAsync(model.HolderType == "Customer" ? model.HolderId : null, cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Created licence {LicenceNumber} via web UI", licence.LicenceNumber);
        TempData["SuccessMessage"] = $"Licence {licence.LicenceNumber} created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays licence details with substance mappings (FR-004).
    /// T079f: Loads substance mappings for the licence details view.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound();
        }

        // Load substance mappings for this licence (FR-004)
        var mappings = await _mappingService.GetByLicenceIdAsync(id, cancellationToken);
        ViewBag.SubstanceMappings = mappings;
        ViewBag.LicenceId = id;
        ViewBag.LicenceNumber = licence.LicenceNumber;

        // Load available substances for add mapping modal
        var substances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        ViewBag.Substances = substances;

        // T094: Load customer name if HolderType is "Customer"
        if (licence.HolderType == "Customer")
        {
            ViewBag.HolderName = await GetCustomerNameAsync(licence.HolderId, cancellationToken);
        }

        return View(licence);
    }

    /// <summary>
    /// Displays the edit licence form.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound();
        }

        var model = new LicenceEditViewModel
        {
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeId = licence.LicenceTypeId,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            IssuingAuthority = licence.IssuingAuthority,
            IssueDate = licence.IssueDate.ToDateTime(TimeOnly.MinValue),
            ExpiryDate = licence.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
            Status = licence.Status,
            Scope = licence.Scope,
            PermittedActivities = (int)licence.PermittedActivities
        };

        ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
        ViewBag.Customers = await GetCustomerSelectListAsync(model.HolderType == "Customer" ? model.HolderId : null, cancellationToken);
        return View(model);
    }

    /// <summary>
    /// Handles licence edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Edit(Guid id, LicenceEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.LicenceId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            ViewBag.Customers = await GetCustomerSelectListAsync(model.HolderType == "Customer" ? model.HolderId : null, cancellationToken);
            return View(model);
        }

        var licence = new Licence
        {
            LicenceId = model.LicenceId,
            LicenceNumber = model.LicenceNumber,
            LicenceTypeId = model.LicenceTypeId,
            HolderType = model.HolderType,
            HolderId = model.HolderId,
            IssuingAuthority = model.IssuingAuthority,
            IssueDate = DateOnly.FromDateTime(model.IssueDate),
            ExpiryDate = model.ExpiryDate.HasValue ? DateOnly.FromDateTime(model.ExpiryDate.Value) : null,
            Status = model.Status,
            Scope = model.Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)model.PermittedActivities
        };

        var result = await _licenceService.UpdateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.LicenceTypes = await GetLicenceTypeSelectListAsync(model.LicenceTypeId, cancellationToken);
            ViewBag.Customers = await GetCustomerSelectListAsync(model.HolderType == "Customer" ? model.HolderId : null, cancellationToken);
            return View(model);
        }

        _logger.LogInformation("Updated licence {LicenceNumber} via web UI", licence.LicenceNumber);
        TempData["SuccessMessage"] = $"Licence {licence.LicenceNumber} updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles licence deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _licenceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to delete licence.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted licence {Id} via web UI", id);
        TempData["SuccessMessage"] = "Licence deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays expiring licences.
    /// Per FR-007: Generate alerts for licences expiring within configurable period.
    /// </summary>
    public async Task<IActionResult> Expiring(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetExpiringLicencesAsync(daysAhead, cancellationToken);

        ViewBag.DaysAhead = daysAhead;
        return View(licences);
    }

    #region Document Management (T117)

    /// <summary>
    /// Displays the document upload form.
    /// T117: File upload UI per FR-008.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> UploadDocument(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound();
        }

        var documents = await _licenceService.GetDocumentsAsync(id, cancellationToken);
        ViewBag.ExistingDocuments = documents;
        ViewBag.DocumentTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            Enum.GetValues<RE2.ComplianceCore.Models.DocumentType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() }),
            "Value", "Text");

        return View(new UploadDocumentViewModel
        {
            LicenceId = id,
            LicenceNumber = licence.LicenceNumber
        });
    }

    /// <summary>
    /// Handles document upload form submission.
    /// T117: File upload processing per FR-008.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> UploadDocument(Guid id, UploadDocumentViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || model.File == null)
        {
            var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
            model.LicenceNumber = licence?.LicenceNumber ?? "Unknown";
            ViewBag.DocumentTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.DocumentType>()
                    .Select(t => new { Value = (int)t, Text = t.ToString() }),
                "Value", "Text");
            return View(model);
        }

        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        var document = new RE2.ComplianceCore.Models.LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = id,
            DocumentType = (RE2.ComplianceCore.Models.DocumentType)model.DocumentType,
            FileName = model.File.FileName,
            ContentType = model.File.ContentType,
            FileSizeBytes = model.File.Length,
            UploadedBy = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty
        };

        using var stream = model.File.OpenReadStream();
        var (docId, result) = await _licenceService.UploadDocumentAsync(id, document, stream, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.DocumentTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.DocumentType>()
                    .Select(t => new { Value = (int)t, Text = t.ToString() }),
                "Value", "Text");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Document '{document.FileName}' uploaded successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    #endregion

    #region Verification Recording (T118)

    /// <summary>
    /// Displays the verification recording form.
    /// T118: Verification recording UI per FR-009.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RecordVerification(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound();
        }

        var verifications = await _licenceService.GetVerificationHistoryAsync(id, cancellationToken);
        ViewBag.RecentVerifications = verifications.Take(5);
        ViewBag.VerificationMethods = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            Enum.GetValues<RE2.ComplianceCore.Models.VerificationMethod>()
                .Select(m => new { Value = (int)m, Text = m.ToString() }),
            "Value", "Text");
        ViewBag.Outcomes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            Enum.GetValues<RE2.ComplianceCore.Models.VerificationOutcome>()
                .Select(o => new { Value = (int)o, Text = o.ToString() }),
            "Value", "Text");

        return View(new RecordVerificationViewModel
        {
            LicenceId = id,
            LicenceNumber = licence.LicenceNumber,
            VerificationDate = DateTime.Today
        });
    }

    /// <summary>
    /// Handles verification recording form submission.
    /// T118: Verification recording per FR-009.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RecordVerification(Guid id, RecordVerificationViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
            model.LicenceNumber = licence?.LicenceNumber ?? "Unknown";
            ViewBag.VerificationMethods = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.VerificationMethod>()
                    .Select(m => new { Value = (int)m, Text = m.ToString() }),
                "Value", "Text");
            ViewBag.Outcomes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.VerificationOutcome>()
                    .Select(o => new { Value = (int)o, Text = o.ToString() }),
                "Value", "Text");
            return View(model);
        }

        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        var verification = new RE2.ComplianceCore.Models.LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = id,
            VerificationMethod = (RE2.ComplianceCore.Models.VerificationMethod)model.VerificationMethod,
            VerificationDate = DateOnly.FromDateTime(model.VerificationDate),
            VerifiedBy = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty,
            VerifierName = model.VerifierName,
            Outcome = (RE2.ComplianceCore.Models.VerificationOutcome)model.Outcome,
            Notes = model.Notes,
            AuthorityReferenceNumber = model.AuthorityReferenceNumber
        };

        var (verificationId, result) = await _licenceService.RecordVerificationAsync(verification, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Verification recorded successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    #endregion

    #region Scope Change History (T119)

    /// <summary>
    /// Displays scope change history for a licence.
    /// T119: Scope change history UI per FR-010.
    /// </summary>
    public async Task<IActionResult> ScopeHistory(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound();
        }

        var scopeChanges = await _licenceService.GetScopeChangesAsync(id, cancellationToken);

        ViewBag.LicenceId = id;
        ViewBag.LicenceNumber = licence.LicenceNumber;

        return View(scopeChanges);
    }

    /// <summary>
    /// Displays the record scope change form.
    /// T119: Scope change recording UI per FR-010.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RecordScopeChange(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound();
        }

        ViewBag.ChangeTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            Enum.GetValues<RE2.ComplianceCore.Models.ScopeChangeType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() }),
            "Value", "Text");

        return View(new RecordScopeChangeViewModel
        {
            LicenceId = id,
            LicenceNumber = licence.LicenceNumber,
            EffectiveDate = DateTime.Today
        });
    }

    /// <summary>
    /// Handles scope change recording form submission.
    /// T119: Scope change recording per FR-010.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RecordScopeChange(Guid id, RecordScopeChangeViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
            model.LicenceNumber = licence?.LicenceNumber ?? "Unknown";
            ViewBag.ChangeTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.ScopeChangeType>()
                    .Select(t => new { Value = (int)t, Text = t.ToString() }),
                "Value", "Text");
            return View(model);
        }

        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        var scopeChange = new RE2.ComplianceCore.Models.LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = id,
            EffectiveDate = DateOnly.FromDateTime(model.EffectiveDate),
            ChangeDescription = model.ChangeDescription,
            ChangeType = (RE2.ComplianceCore.Models.ScopeChangeType)model.ChangeType,
            RecordedBy = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty,
            RecorderName = model.RecorderName ?? User.Identity?.Name,
            SubstancesAdded = model.SubstancesAdded,
            SubstancesRemoved = model.SubstancesRemoved,
            ActivitiesAdded = model.ActivitiesAdded,
            ActivitiesRemoved = model.ActivitiesRemoved
        };

        var (changeId, result) = await _licenceService.RecordScopeChangeAsync(scopeChange, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.ChangeTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                Enum.GetValues<RE2.ComplianceCore.Models.ScopeChangeType>()
                    .Select(t => new { Value = (int)t, Text = t.ToString() }),
                "Value", "Text");
            return View(model);
        }

        TempData["SuccessMessage"] = "Scope change recorded successfully.";
        return RedirectToAction(nameof(ScopeHistory), new { id });
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for creating a new licence.
/// </summary>
public class LicenceCreateViewModel
{
    public string LicenceNumber { get; set; } = string.Empty;
    public Guid LicenceTypeId { get; set; }
    public string HolderType { get; set; } = "Company";
    public Guid HolderId { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime? ExpiryDate { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
}

/// <summary>
/// View model for editing a licence.
/// </summary>
public class LicenceEditViewModel
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public Guid LicenceTypeId { get; set; }
    public string HolderType { get; set; } = "Company";
    public Guid HolderId { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "Valid";
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
}

/// <summary>
/// View model for uploading a document to a licence.
/// T117: Document upload per FR-008.
/// </summary>
public class UploadDocumentViewModel
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public int DocumentType { get; set; }
    public IFormFile? File { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// View model for recording a licence verification.
/// T118: Verification recording per FR-009.
/// </summary>
public class RecordVerificationViewModel
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public int VerificationMethod { get; set; }
    public DateTime VerificationDate { get; set; } = DateTime.Today;
    public int Outcome { get; set; }
    public string VerifierName { get; set; } = string.Empty;
    public string? AuthorityReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// View model for recording a scope change.
/// T119: Scope change recording per FR-010.
/// </summary>
public class RecordScopeChangeViewModel
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public int ChangeType { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
    public string ChangeDescription { get; set; } = string.Empty;
    public string? RecorderName { get; set; }
    public string? SubstancesAdded { get; set; }
    public string? SubstancesRemoved { get; set; }
    public string? ActivitiesAdded { get; set; }
    public string? ActivitiesRemoved { get; set; }
}

#endregion
