using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for customer management web UI.
/// T093: Web UI controller for customer CRUD operations.
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
    /// Displays the customer list page.
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
    /// Displays customer details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);

        if (customer == null)
        {
            return NotFound();
        }

        // Get compliance status for additional details
        var complianceStatus = await _customerService.GetComplianceStatusAsync(id, cancellationToken);
        ViewBag.ComplianceStatus = complianceStatus;

        return View(customer);
    }

    /// <summary>
    /// Displays the create customer form.
    /// </summary>
    [Authorize(Policy = "SalesAdmin")]
    public IActionResult Create()
    {
        ViewBag.BusinessCategories = GetBusinessCategorySelectList();
        ViewBag.ApprovalStatuses = GetApprovalStatusSelectList();
        ViewBag.GdpStatuses = GetGdpStatusSelectList();

        return View(new CustomerCreateViewModel());
    }

    /// <summary>
    /// Handles customer creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Create(CustomerCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.ApprovalStatuses = GetApprovalStatusSelectList();
            ViewBag.GdpStatuses = GetGdpStatusSelectList();
            return View(model);
        }

        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            BusinessName = model.BusinessName,
            RegistrationNumber = model.RegistrationNumber,
            BusinessCategory = model.BusinessCategory,
            Country = model.Country,
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = model.GdpQualificationStatus,
            OnboardingDate = model.OnboardingDate.HasValue ? DateOnly.FromDateTime(model.OnboardingDate.Value) : null,
            NextReVerificationDate = model.NextReVerificationDate.HasValue ? DateOnly.FromDateTime(model.NextReVerificationDate.Value) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var (id, result) = await _customerService.CreateAsync(customer, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var violation in result.Violations)
            {
                ModelState.AddModelError(string.Empty, violation.Message);
            }
            ViewBag.BusinessCategories = GetBusinessCategorySelectList(model.BusinessCategory.ToString());
            ViewBag.ApprovalStatuses = GetApprovalStatusSelectList();
            ViewBag.GdpStatuses = GetGdpStatusSelectList();
            return View(model);
        }

        _logger.LogInformation("Created customer {BusinessName} via web UI", customer.BusinessName);
        TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' created successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit customer form.
    /// </summary>
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);

        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerEditViewModel
        {
            CustomerId = customer.CustomerId,
            BusinessName = customer.BusinessName,
            RegistrationNumber = customer.RegistrationNumber,
            BusinessCategory = customer.BusinessCategory,
            Country = customer.Country,
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
    /// Handles customer edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Edit(Guid id, CustomerEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.CustomerId)
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
            CustomerId = model.CustomerId,
            BusinessName = model.BusinessName,
            RegistrationNumber = model.RegistrationNumber,
            BusinessCategory = model.BusinessCategory,
            Country = model.Country,
            ApprovalStatus = model.ApprovalStatus,
            GdpQualificationStatus = model.GdpQualificationStatus,
            OnboardingDate = model.OnboardingDate.HasValue ? DateOnly.FromDateTime(model.OnboardingDate.Value) : null,
            NextReVerificationDate = model.NextReVerificationDate.HasValue ? DateOnly.FromDateTime(model.NextReVerificationDate.Value) : null,
            IsSuspended = model.IsSuspended,
            SuspensionReason = model.SuspensionReason,
            ModifiedDate = DateTime.UtcNow
        };

        var result = await _customerService.UpdateAsync(customer, cancellationToken);

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

        _logger.LogInformation("Updated customer {BusinessName} via web UI", customer.BusinessName);
        TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles customer deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SalesAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to delete customer.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted customer {Id} via web UI", id);
        TempData["SuccessMessage"] = "Customer deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Suspends a customer.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Suspend(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Suspension reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _customerService.SuspendCustomerAsync(id, reason, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to suspend customer.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Suspended customer {Id} with reason: {Reason}", id, reason);
        TempData["SuccessMessage"] = "Customer suspended successfully.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Reinstates a suspended customer.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.ReinstateCustomerAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = "Failed to reinstate customer.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Reinstated customer {Id}", id);
        TempData["SuccessMessage"] = "Customer reinstated successfully.";

        return RedirectToAction(nameof(Details), new { id });
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
/// View model for creating a new customer.
/// </summary>
public class CustomerCreateViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public BusinessCategory BusinessCategory { get; set; }
    public string Country { get; set; } = string.Empty;
    public GdpQualificationStatus GdpQualificationStatus { get; set; } = GdpQualificationStatus.NotRequired;
    public DateTime? OnboardingDate { get; set; }
    public DateTime? NextReVerificationDate { get; set; }
}

/// <summary>
/// View model for editing a customer.
/// </summary>
public class CustomerEditViewModel
{
    public Guid CustomerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public BusinessCategory BusinessCategory { get; set; }
    public string Country { get; set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; set; }
    public GdpQualificationStatus GdpQualificationStatus { get; set; }
    public DateTime? OnboardingDate { get; set; }
    public DateTime? NextReVerificationDate { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
}

#endregion
