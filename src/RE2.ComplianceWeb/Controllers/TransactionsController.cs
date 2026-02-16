using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceWeb.Controllers;

/// <summary>
/// MVC controller for transaction compliance management web UI.
/// T147-T149: Web UI controller for transaction validation and override management.
/// </summary>
[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionComplianceService _complianceService;
    private readonly ICustomerService _customerService;
    private readonly IControlledSubstanceService _substanceService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionComplianceService complianceService,
        ICustomerService customerService,
        IControlledSubstanceService substanceService,
        ILogger<TransactionsController> logger)
    {
        _complianceService = complianceService;
        _customerService = customerService;
        _substanceService = substanceService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the transaction list page with filtering.
    /// </summary>
    public async Task<IActionResult> Index(
        string? status = null,
        string? customerAccount = null,
        string? customerDataAreaId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidationStatus? validationStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ValidationStatus>(status, out var parsedStatus))
        {
            validationStatus = parsedStatus;
        }

        var transactions = await _complianceService.GetTransactionsAsync(
            validationStatus,
            customerAccount,
            customerDataAreaId,
            fromDate,
            toDate,
            cancellationToken);

        ViewBag.StatusFilter = status;
        ViewBag.CustomerAccountFilter = customerAccount;
        ViewBag.CustomerDataAreaIdFilter = customerDataAreaId;
        ViewBag.FromDateFilter = fromDate;
        ViewBag.ToDateFilter = toDate;
        ViewBag.ValidationStatuses = GetValidationStatusSelectList(status);

        // Get pending override count for dashboard
        ViewBag.PendingOverrideCount = await _complianceService.GetPendingOverrideCountAsync(cancellationToken);

        return View(transactions);
    }

    /// <summary>
    /// Displays transaction details.
    /// </summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _complianceService.GetTransactionByIdAsync(id, cancellationToken);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    /// <summary>
    /// Displays pending override approvals queue.
    /// Per FR-019a: Override approval queue for ComplianceManager.
    /// </summary>
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> PendingOverrides(CancellationToken cancellationToken = default)
    {
        var transactions = await _complianceService.GetPendingOverridesAsync(cancellationToken);
        return View(transactions);
    }

    /// <summary>
    /// Displays the validate transaction form.
    /// </summary>
    public async Task<IActionResult> Validate(CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        var substances = await _substanceService.GetAllAsync(cancellationToken);

        ViewBag.Customers = new SelectList(
            customers.Where(c => c.CanTransact()),
            "CustomerAccount",
            "BusinessName");
        ViewBag.Substances = substances.ToList();
        ViewBag.TransactionTypes = GetTransactionTypeSelectList();
        ViewBag.Directions = GetDirectionSelectList();

        return View(new TransactionValidateViewModel());
    }

    /// <summary>
    /// Handles transaction validation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(TransactionValidateViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllAsync(cancellationToken);
            var substances = await _substanceService.GetAllAsync(cancellationToken);

            ViewBag.Customers = new SelectList(
                customers.Where(c => c.CanTransact()),
                "CustomerId",
                "BusinessName");
            ViewBag.Substances = substances.ToList();
            ViewBag.TransactionTypes = GetTransactionTypeSelectList();
            ViewBag.Directions = GetDirectionSelectList();

            return View(model);
        }

        try
        {
            var transaction = model.ToDomainModel();
            var result = await _complianceService.ValidateTransactionAsync(transaction, cancellationToken);

            if (result.ValidationResult.IsValid)
            {
                TempData["SuccessMessage"] = $"Transaction '{transaction.ExternalId}' passed compliance validation.";
            }
            else
            {
                TempData["WarningMessage"] = $"Transaction '{transaction.ExternalId}' failed validation with {result.ValidationResult.Violations.Count} violation(s).";
            }

            return RedirectToAction(nameof(Details), new { id = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction");
            ModelState.AddModelError(string.Empty, "An error occurred during validation.");

            var customers = await _customerService.GetAllAsync(cancellationToken);
            var substances = await _substanceService.GetAllAsync(cancellationToken);

            ViewBag.Customers = new SelectList(
                customers.Where(c => c.CanTransact()),
                "CustomerId",
                "BusinessName");
            ViewBag.Substances = substances.ToList();
            ViewBag.TransactionTypes = GetTransactionTypeSelectList();
            ViewBag.Directions = GetDirectionSelectList();

            return View(model);
        }
    }

    /// <summary>
    /// Approves an override for a failed transaction.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> ApproveOverride(Guid id, string justification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            TempData["ErrorMessage"] = "Justification is required for override approval.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _complianceService.ApproveOverrideAsync(id, userId, justification, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = result.Violations.First().Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Override approved for transaction {Id} by {UserId}", id, userId);
        TempData["SuccessMessage"] = "Override approved successfully.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Rejects an override for a failed transaction.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ComplianceManager")]
    public async Task<IActionResult> RejectOverride(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Reason is required for override rejection.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _complianceService.RejectOverrideAsync(id, userId, reason, cancellationToken);

        if (!result.IsValid)
        {
            TempData["ErrorMessage"] = result.Violations.First().Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Override rejected for transaction {Id} by {UserId}", id, userId);
        TempData["SuccessMessage"] = "Override rejected.";

        return RedirectToAction(nameof(Details), new { id });
    }

    #region Helper Methods

    private static SelectList GetValidationStatusSelectList(string? selectedValue = null)
    {
        var statuses = Enum.GetValues<ValidationStatus>()
            .Select(s => new { Value = s.ToString(), Text = FormatEnumName(s.ToString()) });
        return new SelectList(statuses, "Value", "Text", selectedValue);
    }

    private static SelectList GetTransactionTypeSelectList(string? selectedValue = null)
    {
        var types = Enum.GetValues<TransactionTypes.TransactionType>()
            .Select(t => new { Value = t.ToString(), Text = FormatEnumName(t.ToString()) });
        return new SelectList(types, "Value", "Text", selectedValue ?? "Order");
    }

    private static SelectList GetDirectionSelectList(string? selectedValue = null)
    {
        var directions = Enum.GetValues<TransactionDirection>()
            .Select(d => new { Value = d.ToString(), Text = FormatEnumName(d.ToString()) });
        return new SelectList(directions, "Value", "Text", selectedValue ?? "Internal");
    }

    private static string FormatEnumName(string enumName)
    {
        return string.Concat(enumName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for transaction validation.
/// </summary>
public class TransactionValidateViewModel
{
    public string ExternalId { get; set; } = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
    public TransactionTypes.TransactionType TransactionType { get; set; } = TransactionTypes.TransactionType.Order;
    public TransactionDirection Direction { get; set; } = TransactionDirection.Internal;
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerDataAreaId { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = "NL";
    public string? DestinationCountry { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public List<TransactionLineViewModel> Lines { get; set; } = new()
    {
        new TransactionLineViewModel()
    };

    public Transaction ToDomainModel()
    {
        var transaction = new Transaction
        {
            Id = Guid.Empty,
            ExternalId = ExternalId,
            TransactionType = TransactionType,
            Direction = Direction,
            CustomerAccount = CustomerAccount,
            CustomerDataAreaId = CustomerDataAreaId,
            OriginCountry = OriginCountry,
            DestinationCountry = DestinationCountry,
            TransactionDate = TransactionDate,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "web-user"
        };

        var lineNumber = 1;
        foreach (var lineVm in Lines.Where(l => l.SubstanceId != Guid.Empty))
        {
            transaction.Lines.Add(new TransactionLine
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                LineNumber = lineNumber++,
                SubstanceId = lineVm.SubstanceId,
                SubstanceCode = lineVm.SubstanceCode ?? string.Empty,
                ProductCode = lineVm.ProductCode,
                Quantity = lineVm.Quantity,
                UnitOfMeasure = lineVm.UnitOfMeasure ?? "EA",
                BaseUnitQuantity = lineVm.BaseUnitQuantity ?? lineVm.Quantity,
                BaseUnit = lineVm.BaseUnit ?? "g"
            });
        }

        transaction.TotalQuantity = transaction.Lines.Sum(l => l.BaseUnitQuantity);

        return transaction;
    }
}

/// <summary>
/// View model for transaction line.
/// </summary>
public class TransactionLineViewModel
{
    public Guid SubstanceId { get; set; }
    public string? SubstanceCode { get; set; }
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; } = 100;
    public string? UnitOfMeasure { get; set; } = "EA";
    public decimal? BaseUnitQuantity { get; set; } = 100;
    public string? BaseUnit { get; set; } = "g";
}

#endregion
