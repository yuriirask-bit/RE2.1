using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Transaction (order or shipment) for compliance validation.
/// T128: Transaction domain model (data-model.md entity 6).
/// Per FR-018: Real-time transaction compliance validation.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Internal unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// External order/shipment number from ERP system.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Type of transaction (Order, Shipment, Return, Transfer).
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// Direction for cross-border validation (Internal, Inbound, Outbound).
    /// </summary>
    public TransactionDirection Direction { get; set; }

    #region Parties

    /// <summary>
    /// Customer account number (D365FO composite key part 1).
    /// </summary>
    public string CustomerAccount { get; set; } = string.Empty;

    /// <summary>
    /// Customer data area ID (D365FO composite key part 2).
    /// </summary>
    public string CustomerDataAreaId { get; set; } = string.Empty;

    /// <summary>
    /// Customer name (denormalized for display).
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Origin country (ISO 3166-1 alpha-2).
    /// </summary>
    public string OriginCountry { get; set; } = "NL";

    /// <summary>
    /// Destination country (ISO 3166-1 alpha-2).
    /// </summary>
    public string? DestinationCountry { get; set; }

    #endregion

    #region Transaction Details

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Total quantity (all lines combined, in base unit).
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total value (all lines combined).
    /// </summary>
    public decimal? TotalValue { get; set; }

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// ERP system status (Draft, Confirmed, Shipped, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    #endregion

    #region Compliance Validation

    /// <summary>
    /// Compliance validation status.
    /// </summary>
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;

    /// <summary>
    /// Date/time when validation was performed.
    /// </summary>
    public DateTime? ValidationDate { get; set; }

    /// <summary>
    /// List of compliance warnings (non-blocking).
    /// </summary>
    public List<string> ComplianceWarnings { get; set; } = new();

    /// <summary>
    /// List of compliance errors (blocking).
    /// </summary>
    public List<string> ComplianceErrors { get; set; } = new();

    /// <summary>
    /// Detailed validation violations.
    /// </summary>
    public List<TransactionViolation> Violations { get; set; } = new();

    #endregion

    #region Override Tracking (FR-019a)

    /// <summary>
    /// Whether this transaction requires override approval.
    /// </summary>
    public bool RequiresOverride { get; set; }

    /// <summary>
    /// Current override status.
    /// </summary>
    public OverrideStatus OverrideStatus { get; set; } = OverrideStatus.None;

    /// <summary>
    /// Date when override was approved/rejected.
    /// </summary>
    public DateTime? OverrideDecisionDate { get; set; }

    /// <summary>
    /// User who approved/rejected the override.
    /// </summary>
    public string? OverrideDecisionBy { get; set; }

    /// <summary>
    /// Justification for override approval.
    /// </summary>
    public string? OverrideJustification { get; set; }

    /// <summary>
    /// Reason for override rejection.
    /// </summary>
    public string? OverrideRejectionReason { get; set; }

    #endregion

    #region Line Items

    /// <summary>
    /// Transaction line items with controlled substances.
    /// </summary>
    public List<TransactionLine> Lines { get; set; } = new();

    /// <summary>
    /// Line IDs (for lazy loading scenarios).
    /// </summary>
    public List<Guid> TransactionLineIds { get; set; } = new();

    #endregion

    #region Licence Coverage

    /// <summary>
    /// Licences used to cover this transaction.
    /// </summary>
    public List<TransactionLicenceUsage> LicenceUsages { get; set; } = new();

    /// <summary>
    /// Licence IDs used (denormalized).
    /// </summary>
    public List<Guid> LicencesUsed { get; set; } = new();

    #endregion

    #region Integration System Tracking (T149b/FR-061)

    /// <summary>
    /// Integration system ID that submitted the transaction.
    /// T149b/FR-061: Record calling system identity in transaction audit.
    /// Examples: "D365FO", "WMS", "OrderManagement", "Portal"
    /// </summary>
    public string? IntegrationSystemId { get; set; }

    /// <summary>
    /// External user ID from the calling system who initiated the transaction.
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// Warehouse site ID for warehouse-related transactions.
    /// </summary>
    public string? WarehouseSiteId { get; set; }

    #endregion

    #region Audit Fields

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the record.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the record.
    /// </summary>
    public string? ModifiedBy { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// Checks if this transaction is cross-border.
    /// </summary>
    public bool IsCrossBorder()
    {
        if (string.IsNullOrEmpty(DestinationCountry))
        {
            return false;
        }

        return !OriginCountry.Equals(DestinationCountry, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this transaction requires import permit.
    /// Per FR-021: Cross-border import validation.
    /// </summary>
    public bool RequiresImportPermit()
    {
        return IsCrossBorder() && Direction == TransactionDirection.Inbound;
    }

    /// <summary>
    /// Checks if this transaction requires export permit.
    /// Per FR-021: Cross-border export validation.
    /// </summary>
    public bool RequiresExportPermit()
    {
        return IsCrossBorder() && Direction == TransactionDirection.Outbound;
    }

    /// <summary>
    /// Checks if this transaction can proceed based on validation status.
    /// </summary>
    public bool CanProceed()
    {
        return ValidationStatus == ValidationStatus.Passed ||
               ValidationStatus == ValidationStatus.ApprovedWithOverride;
    }

    /// <summary>
    /// Checks if this transaction is blocked.
    /// </summary>
    public bool IsBlocked()
    {
        return ValidationStatus == ValidationStatus.Failed ||
               ValidationStatus == ValidationStatus.RejectedOverride;
    }

    /// <summary>
    /// Checks if override is pending approval.
    /// </summary>
    public bool IsAwaitingOverride()
    {
        return RequiresOverride && OverrideStatus == OverrideStatus.Pending;
    }

    /// <summary>
    /// Marks the transaction as validated with the given result.
    /// </summary>
    public void SetValidationResult(ValidationResult result, DateTime validationTime)
    {
        ValidationDate = validationTime;

        if (result.IsValid)
        {
            ValidationStatus = ValidationStatus.Passed;
            RequiresOverride = false;
            ComplianceErrors.Clear();
        }
        else
        {
            ValidationStatus = ValidationStatus.Failed;
            RequiresOverride = result.CanOverride;
            OverrideStatus = result.CanOverride ? OverrideStatus.Pending : OverrideStatus.None;

            ComplianceErrors = result.Violations
                .Where(v => v.Severity == ViolationSeverity.Critical)
                .Select(v => v.Message)
                .ToList();

            ComplianceWarnings = result.Violations
                .Where(v => v.Severity == ViolationSeverity.Warning)
                .Select(v => v.Message)
                .ToList();
        }
    }

    /// <summary>
    /// Approves override for this transaction.
    /// </summary>
    public void ApproveOverride(string approverUserId, string justification, DateTime approvalTime)
    {
        if (!RequiresOverride)
        {
            throw new InvalidOperationException("Transaction does not require override");
        }

        OverrideStatus = OverrideStatus.Approved;
        OverrideDecisionDate = approvalTime;
        OverrideDecisionBy = approverUserId;
        OverrideJustification = justification;
        ValidationStatus = ValidationStatus.ApprovedWithOverride;
        ModifiedAt = approvalTime;
        ModifiedBy = approverUserId;
    }

    /// <summary>
    /// Rejects override for this transaction.
    /// </summary>
    public void RejectOverride(string rejecterUserId, string reason, DateTime rejectionTime)
    {
        if (!RequiresOverride)
        {
            throw new InvalidOperationException("Transaction does not require override");
        }

        OverrideStatus = OverrideStatus.Rejected;
        OverrideDecisionDate = rejectionTime;
        OverrideDecisionBy = rejecterUserId;
        OverrideRejectionReason = reason;
        ValidationStatus = ValidationStatus.RejectedOverride;
        ModifiedAt = rejectionTime;
        ModifiedBy = rejecterUserId;
    }

    #endregion
}
