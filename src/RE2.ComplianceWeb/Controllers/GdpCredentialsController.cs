using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for GDP Credential management, document attachments, and verification logging.
/// T244: Web UI for credential validity management per US10 (FR-043, FR-044, FR-045).
/// </summary>
[Authorize]
public class GdpCredentialsController : Controller
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpCredentialsController> _logger;

    public GdpCredentialsController(
        IGdpComplianceService gdpService,
        ILogger<GdpCredentialsController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    #region Credential Listing

    /// <summary>
    /// Lists all GDP credentials with validity status.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        // Get all credentials across both entity types
        var providers = await _gdpService.GetAllProvidersAsync(cancellationToken);
        var allCredentials = new List<CredentialIndexItem>();

        foreach (var provider in providers)
        {
            var creds = await _gdpService.GetCredentialsByEntityAsync(
                GdpCredentialEntityType.ServiceProvider, provider.ProviderId, cancellationToken);
            foreach (var cred in creds)
            {
                allCredentials.Add(new CredentialIndexItem
                {
                    Credential = cred,
                    EntityName = provider.ProviderName,
                    EntityTypeName = "Service Provider"
                });
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiryWindow = today.AddDays(90);

        var viewModel = new CredentialIndexViewModel
        {
            Credentials = allCredentials,
            TotalCount = allCredentials.Count,
            ValidCount = allCredentials.Count(c => c.Credential.IsValid() && !(c.Credential.ValidityEndDate.HasValue && c.Credential.ValidityEndDate.Value <= expiryWindow)),
            ExpiringCount = allCredentials.Count(c => c.Credential.IsValid() && c.Credential.ValidityEndDate.HasValue && c.Credential.ValidityEndDate.Value <= expiryWindow && c.Credential.ValidityEndDate.Value >= today),
            ExpiredCount = allCredentials.Count(c => c.Credential.ValidityEndDate.HasValue && c.Credential.ValidityEndDate.Value < today)
        };

        return View(viewModel);
    }

    /// <summary>
    /// Shows credential details with documents and verifications.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _gdpService.GetCredentialAsync(id, cancellationToken);
        if (credential == null)
            return NotFound();

        var documents = await _gdpService.GetDocumentsByEntityAsync(
            GdpDocumentEntityType.Credential, id, cancellationToken);
        var verifications = await _gdpService.GetVerificationsByCredentialAsync(id, cancellationToken);

        // Resolve entity name
        string entityName = "Unknown";
        if (credential.EntityType == GdpCredentialEntityType.ServiceProvider)
        {
            var provider = await _gdpService.GetProviderAsync(credential.EntityId, cancellationToken);
            entityName = provider?.ProviderName ?? "Unknown Provider";
        }

        var viewModel = new CredentialDetailsViewModel
        {
            Credential = credential,
            EntityName = entityName,
            Documents = documents.ToList(),
            Verifications = verifications.OrderByDescending(v => v.VerificationDate).ToList()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Shows credentials expiring within configurable window.
    /// </summary>
    public async Task<IActionResult> Expiring(int days = 90, CancellationToken cancellationToken = default)
    {
        var expiring = await _gdpService.GetCredentialsExpiringAsync(days, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = new List<CredentialIndexItem>();
        foreach (var cred in expiring.OrderBy(c => c.ValidityEndDate))
        {
            string entityName = "Unknown";
            if (cred.EntityType == GdpCredentialEntityType.ServiceProvider)
            {
                var provider = await _gdpService.GetProviderAsync(cred.EntityId, cancellationToken);
                entityName = provider?.ProviderName ?? "Unknown Provider";
            }

            items.Add(new CredentialIndexItem
            {
                Credential = cred,
                EntityName = entityName,
                EntityTypeName = cred.EntityType == GdpCredentialEntityType.ServiceProvider ? "Service Provider" : "Supplier"
            });
        }

        ViewBag.DaysAhead = days;
        ViewBag.ExpiredCount = items.Count(i => i.Credential.ValidityEndDate.HasValue && i.Credential.ValidityEndDate.Value < today);

        return View(items);
    }

    #endregion

    #region Verification Recording

    /// <summary>
    /// Shows the record verification form.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> RecordVerification(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _gdpService.GetCredentialAsync(credentialId, cancellationToken);
        if (credential == null)
            return NotFound();

        string entityName = "Unknown";
        if (credential.EntityType == GdpCredentialEntityType.ServiceProvider)
        {
            var provider = await _gdpService.GetProviderAsync(credential.EntityId, cancellationToken);
            entityName = provider?.ProviderName ?? "Unknown Provider";
        }

        var model = new GdpRecordVerificationViewModel
        {
            CredentialId = credentialId,
            CredentialNumber = credential.GdpCertificateNumber ?? credential.WdaNumber ?? "-",
            EntityName = entityName,
            LastVerificationDate = credential.LastVerificationDate,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        ViewBag.VerificationMethods = GetVerificationMethodSelectList();
        ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList();
        return View(model);
    }

    /// <summary>
    /// Handles record verification form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> RecordVerification(GdpRecordVerificationViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VerificationMethods = GetVerificationMethodSelectList(model.VerificationMethod.ToString());
            ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList(model.Outcome.ToString());
            return View(model);
        }

        var verification = new GdpCredentialVerification
        {
            CredentialId = model.CredentialId,
            VerificationDate = model.VerificationDate,
            VerificationMethod = model.VerificationMethod,
            VerifiedBy = model.VerifiedBy,
            Outcome = model.Outcome,
            Notes = model.Notes
        };

        var (id, result) = await _gdpService.RecordVerificationAsync(verification, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.VerificationMethods = GetVerificationMethodSelectList(model.VerificationMethod.ToString());
            ViewBag.VerificationOutcomes = GetVerificationOutcomeSelectList(model.Outcome.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = "Credential verification recorded successfully.";
        return RedirectToAction(nameof(Details), new { id = model.CredentialId });
    }

    #endregion

    #region Document Upload & Delete

    /// <summary>
    /// Shows the upload document form.
    /// </summary>
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> UploadDocument(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _gdpService.GetCredentialAsync(credentialId, cancellationToken);
        if (credential == null)
            return NotFound();

        string entityName = "Unknown";
        if (credential.EntityType == GdpCredentialEntityType.ServiceProvider)
        {
            var provider = await _gdpService.GetProviderAsync(credential.EntityId, cancellationToken);
            entityName = provider?.ProviderName ?? "Unknown Provider";
        }

        var model = new GdpUploadDocumentViewModel
        {
            OwnerEntityType = GdpDocumentEntityType.Credential,
            OwnerEntityId = credentialId,
            CredentialNumber = credential.GdpCertificateNumber ?? credential.WdaNumber ?? "-",
            EntityName = entityName
        };

        ViewBag.DocumentTypes = GetDocumentTypeSelectList();
        return View(model);
    }

    /// <summary>
    /// Handles document upload form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<IActionResult> UploadDocument(GdpUploadDocumentViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Please select a file to upload.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.DocumentTypes = GetDocumentTypeSelectList(model.DocumentType.ToString());
            return View(model);
        }

        var document = new GdpDocument
        {
            OwnerEntityType = model.OwnerEntityType,
            OwnerEntityId = model.OwnerEntityId,
            DocumentType = model.DocumentType,
            FileName = model.File!.FileName,
            ContentType = model.File.ContentType,
            FileSizeBytes = model.File.Length,
            Description = model.Description,
            UploadedBy = User.Identity?.Name ?? "Unknown",
            UploadedDate = DateTime.UtcNow
        };

        using var stream = model.File.OpenReadStream();
        var (id, result) = await _gdpService.UploadDocumentAsync(document, stream, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(string.Empty, violation.Message);

            ViewBag.DocumentTypes = GetDocumentTypeSelectList(model.DocumentType.ToString());
            return View(model);
        }

        TempData["SuccessMessage"] = $"Document '{model.File.FileName}' uploaded successfully.";
        return RedirectToAction(nameof(Details), new { id = model.OwnerEntityId });
    }

    /// <summary>
    /// Deletes a document and redirects back to credential details.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    public async Task<IActionResult> DeleteDocument(Guid documentId, Guid credentialId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.DeleteDocumentAsync(documentId, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to delete document.";
        }
        else
        {
            TempData["SuccessMessage"] = "Document deleted successfully.";
        }

        return RedirectToAction(nameof(Details), new { id = credentialId });
    }

    #endregion

    #region Helpers

    private static List<SelectListItem> GetVerificationMethodSelectList(string? selected = null) =>
        Enum.GetValues<GdpVerificationMethod>().Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = m switch
            {
                GdpVerificationMethod.EudraGMDP => "EudraGMDP Database",
                GdpVerificationMethod.NationalDatabase => "National Database",
                GdpVerificationMethod.Other => "Other",
                _ => m.ToString()
            },
            Selected = m.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetVerificationOutcomeSelectList(string? selected = null) =>
        Enum.GetValues<GdpVerificationOutcome>().Select(o => new SelectListItem
        {
            Value = o.ToString(),
            Text = o.ToString(),
            Selected = o.ToString() == selected
        }).ToList();

    private static List<SelectListItem> GetDocumentTypeSelectList(string? selected = null) =>
        Enum.GetValues<DocumentType>().Select(t => new SelectListItem
        {
            Value = t.ToString(),
            Text = t switch
            {
                DocumentType.Certificate => "Certificate",
                DocumentType.Letter => "Letter",
                DocumentType.InspectionReport => "Inspection Report",
                DocumentType.Other => "Other",
                _ => t.ToString()
            },
            Selected = t.ToString() == selected
        }).ToList();

    private static string FormatEnumName(string enumName)
    {
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    #endregion
}

#region View Models

public class CredentialIndexViewModel
{
    public List<CredentialIndexItem> Credentials { get; set; } = new();
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
}

public class CredentialIndexItem
{
    public GdpCredential Credential { get; set; } = null!;
    public string EntityName { get; set; } = string.Empty;
    public string EntityTypeName { get; set; } = string.Empty;
}

public class CredentialDetailsViewModel
{
    public GdpCredential Credential { get; set; } = null!;
    public string EntityName { get; set; } = string.Empty;
    public List<GdpDocument> Documents { get; set; } = new();
    public List<GdpCredentialVerification> Verifications { get; set; } = new();
}

public class GdpRecordVerificationViewModel
{
    public Guid CredentialId { get; set; }
    public string CredentialNumber { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public DateOnly? LastVerificationDate { get; set; }
    public DateOnly VerificationDate { get; set; }
    public GdpVerificationMethod VerificationMethod { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public GdpVerificationOutcome Outcome { get; set; }
    public string? Notes { get; set; }
}

public class GdpUploadDocumentViewModel
{
    public GdpDocumentEntityType OwnerEntityType { get; set; }
    public Guid OwnerEntityId { get; set; }
    public string CredentialNumber { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string? Description { get; set; }
    public IFormFile? File { get; set; }
}

#endregion
