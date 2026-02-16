using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for customer management web UI.
/// T093: Web UI controller for customer compliance operations.
/// Composite key: CustomerAccount (string) + DataAreaId (string) per D365FO + Dataverse pattern.
/// </summary>
[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Displays compliance-configured customers.
    /// T092: Customer listing with filtering.
    /// </summary>
    public async Task<IActionResult> Index(
        string? status = null,
        string? category = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Customer> customers;

        if (!string.IsNullOrEmpty(search))
        {
            customers = await _customerService.SearchByNameAsync(search, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, out var approvalStatus))
        {
            customers = await _customerService.GetByApprovalStatusAsync(approvalStatus, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(category) && Enum.TryParse<BusinessCategory>(category, out var businessCategory))
        {
            customers = await _customerService.GetByBusinessCategoryAsync(businessCategory, cancellationToken);
        }
        else
        {
            customers = await _customerService.GetAllAsync(cancellationToken);
        }

        ViewBag.StatusFilter = status;
        ViewBag.CategoryFilter = category;
        ViewBag.SearchTerm = search;
        ViewBag.ApprovalStatuses = GetApprovalStatusSelectList(status);
        ViewBag.BusinessCategories = GetBusinessCategorySelectList(category);

        return View(customers);
    }

    /// <summary>
    /// Browses all D365FO customers (master data).
    /// Similar to GdpSites Browse - shows all customers from D365FO regardless of compliance configuration.
    /// </summary>
    public async Task<IActionResult> Browse(CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetAllD365CustomersAsync(cancellationToken);
        return View(customers);
    }

    /// <summary>
    /// Displays customer details by composite key.
    /// </summary>
    public async Task<IActionResult> Details(
        string customerAccount,
        string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);

        if (customer == null)
        {
            return NotFound();
        }

        // Get compliance status for additional details
        var complianceStatus = await _customerService.GetComplianceStatusAsync(customerAccount, dataAreaId, cancellationToken);
        ViewBag.ComplianceStatus = complianceStatus;

        return View(customer);
    }

    /// <summary>
    /// Displays the configure compliance form for a D365FO customer.
    /// </summary>
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Configure(
        string customerAccount,
        string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerConfigureViewModel
        {
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            OrganizationName = customer.OrganizationName,
            AddressCountryRegionId = customer.AddressCountryRegionId
        };

        ViewBag.BusinessCategories = GetBusinessCategorySelectList();
        ViewBag.GdpStatuses = GetGdpStatusSelectList();

        return View(model);
    }

    /// <summary>
    /// Handles configure compliance form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Configure(
        CustomerConfigureViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.GdpStatuses = GetGdpStatusSelectList();
            return View(model);
        }

        var customer = new Customer
        {
            CustomerAccount = model.CustomerAccount,
            DataAreaId = model.DataAreaId,
            BusinessCategory = model.BusinessCategory,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = model.GdpQualificationStatus,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var (id, result) = await _customerService.ConfigureComplianceAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.GdpStatuses = GetGdpStatusSelectList();
            return View(model);
        }

        _logger.LogInformation(
            "Configured compliance for customer {CustomerAccount}/{DataAreaId} via web UI",
            customer.CustomerAccount, customer.DataAreaId);
        TempData["SuccessMessage"] = $"Compliance configured for customer '{model.CustomerAccount}'.";

        return RedirectToAction(nameof(Details), new { customerAccount = model.CustomerAccount, dataAreaId = model.DataAreaId });
    }

    /// <summary>
    /// Displays the edit compliance form.
    /// </summary>
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Edit(
        string customerAccount,
        string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByAccountAsync(customerAccount, dataAreaId, cancellationToken);

        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerEditViewModel
        {
            CustomerAccount = customer.CustomerAccount,
            DataAreaId = customer.DataAreaId,
            BusinessCategory = customer.BusinessCategory,
            ApprovalStatus = customer.ApprovalStatus,
            GdpQualificationStatus = customer.GdpQualificationStatus,
            OnboardingDate = customer.OnboardingDate?.ToDateTime(TimeOnly.MinValue),
            NextReVerificationDate = customer.NextReVerificationDate?.ToDateTime(TimeOnly.MinValue),
            IsSuspended = customer.IsSuspended,
            SuspensionReason = customer.SuspensionReason
        };

        ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
        ViewBag.ApprovalStatuses = GetApprovalStatusSelectList(model.ApprovalStatus.ToString());
        ViewBag.GdpStatuses = GetGdpStatusSelectList(model.GdpQualificationStatus.ToString());

        return View(model);
    }

    /// <summary>
    /// Handles edit compliance form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Edit(
        string customerAccount,
        string dataAreaId,
        CustomerEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (customerAccount != model.CustomerAccount || dataAreaId != model.DataAreaId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.ApprovalStatuses = GetApprovalStatusSelectList(model.ApprovalStatus.ToString());
            ViewBag.GdpStatuses = GetGdpStatusSelectList(model.GdpQualificationStatus.ToString());
            return View(model);
        }

        var customer = new Customer
        {
            CustomerAccount = model.CustomerAccount,
            DataAreaId = model.DataAreaId,
            BusinessCategory = model.BusinessCategory,
            ApprovalStatus = model.ApprovalStatus,
            GdpQualificationStatus = model.GdpQualificationStatus,
            OnboardingDate = model.OnboardingDate.HasValue ? DateOnly.FromDateTime(model.OnboardingDate.Value) : null,
            NextReVerificationDate = model.NextReVerificationDate.HasValue ? DateOnly.FromDateTime(model.NextReVerificationDate.Value) : null,
            IsSuspended = model.IsSuspended,
            SuspensionReason = model.SuspensionReason,
            ModifiedDate = DateTime.UtcNow
        };

        var result = await _customerService.UpdateComplianceAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.ApprovalStatuses = GetApprovalStatusSelectList(model.ApprovalStatus.ToString());
            ViewBag.GdpStatuses = GetGdpStatusSelectList(model.GdpQualificationStatus.ToString());
            return View(model);
        }

        _logger.LogInformation(
            "Updated compliance for customer {CustomerAccount}/{DataAreaId} via web UI",
            customer.CustomerAccount, customer.DataAreaId);
        TempData["SuccessMessage"] = $"Compliance updated for customer '{model.CustomerAccount}'.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Suspends a customer.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Suspend(
        string customerAccount,
        string dataAreaId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Suspension reason is required.";
            return RedirectToAction(nameof(Details), new { customerAccount, dataAreaId });
        }

        var result = await _customerService.SuspendCustomerAsync(customerAccount, dataAreaId, reason, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to suspend customer.";
            return RedirectToAction(nameof(Details), new { customerAccount, dataAreaId });
        }

        _logger.LogInformation(
            "Suspended customer {CustomerAccount}/{DataAreaId} with reason: {Reason}",
            customerAccount, dataAreaId, reason);
        TempData["SuccessMessage"] = "Customer suspended successfully.";

        return RedirectToAction(nameof(Details), new { customerAccount, dataAreaId });
    }

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Reinstate(
        string customerAccount,
        string dataAreaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.ReinstateCustomerAsync(customerAccount, dataAreaId, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to reinstate customer.";
            return RedirectToAction(nameof(Details), new { customerAccount, dataAreaId });
        }

        _logger.LogInformation(
            "Reinstated customer {CustomerAccount}/{DataAreaId}",
            customerAccount, dataAreaId);
        TempData["SuccessMessage"] = "Customer reinstated successfully.";

        return RedirectToAction(nameof(Details), new { customerAccount, dataAreaId });
    }

    /// <summary>
    /// Displays customers with re-verification due.
    /// Per FR-017: Re-verification tracking.
    /// </summary>
    public async Task<IActionResult> ReVerificationDue(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetReVerificationDueAsync(daysAhead, cancellationToken);

        ViewBag.DaysAhead = daysAhead;
        return View(customers);
    }

    /// <summary>
    /// Alias for ReVerificationDue - linked from dashboard.
    /// T097a: Pending re-verification page accessible from dashboard.
    /// </summary>
    public async Task<IActionResult> PendingReVerification(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetReVerificationDueAsync(daysAhead, cancellationToken);
        ViewBag.DaysAhead = daysAhead;
        return View("ReVerificationDue", customers);
    }

    #region Helper Methods

    private static SelectList GetApprovalStatusSelectList(string? selectedValue = null)
    {
        var statuses = Enum.GetValues<ApprovalStatus>()
            .Select(s => new { Value = s.ToString(), Text = FormatEnumName(s.ToString()) });
        return new SelectList(statuses, "Value", "Text", selectedValue);
    }

    private static SelectList GetBusinessCategorySelectList(string? selectedValue = null)
    {
        var categories = Enum.GetValues<BusinessCategory>()
            .Select(c => new { Value = c.ToString(), Text = FormatEnumName(c.ToString()) });
        return new SelectList(categories, "Value", "Text", selectedValue);
    }

    private static SelectList GetGdpStatusSelectList(string? selectedValue = null)
    {
        var statuses = Enum.GetValues<GdpQualificationStatus>()
            .Select(s => new { Value = s.ToString(), Text = FormatEnumName(s.ToString()) });
        return new SelectList(statuses, "Value", "Text", selectedValue);
    }

    private static string FormatEnumName(string enumName)
    {
        // Convert PascalCase to words with spaces
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for configuring compliance for a D365FO customer.
/// D365FO fields (OrganizationName, AddressCountryRegionId) are display-only.
/// </summary>
public class CustomerConfigureViewModel
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string AddressCountryRegionId { get; set; } = string.Empty;
    public BusinessCategory BusinessCategory { get; set; }
    public GdpQualificationStatus GdpQualificationStatus { get; set; } = GdpQualificationStatus.NotRequired;
}

/// <summary>
/// View model for editing compliance extensions.
/// Only compliance-specific fields are editable. D365FO master data is read-only.
/// </summary>
public class CustomerEditViewModel
{
    public string CustomerAccount { get; set; } = string.Empty;
    public string DataAreaId { get; set; } = string.Empty;
    public BusinessCategory BusinessCategory { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public GdpQualificationStatus GdpQualificationStatus { get; set; }
    public DateTime? OnboardingDate { get; set; }
    public DateTime? NextReVerificationDate { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
}

#endregion
