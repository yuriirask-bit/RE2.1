namespace RE2.ComplianceCore.Models;

/// <summary>
/// Transaction (order or shipment) for compliance validation
/// T128: Transaction domain model (data-model.md entity 6)
/// </summary>
public class Transaction
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty; // Order number from ERP
    public string TransactionType { get; set; } = string.Empty; // Order, Shipment, Return

    // Parties
    public Guid CustomerId { get; set; }
    public string? DestinationCountry { get; set; }

    // Transaction details
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Blocked, Completed

    // Compliance validation
    public DateTime? ValidationDate { get; set; }
    public string ValidationResult { get; set; } = string.Empty; // Compliant, NonCompliant, PendingReview
    public List<string> ComplianceWarnings { get; set; } = new();
    public List<string> ComplianceErrors { get; set; } = new();

    // Override tracking (FR-019a)
    public bool RequiresOverride { get; set; }
    public DateTime? OverrideApprovedDate { get; set; }
    public string? OverrideApprovedBy { get; set; }
    public string? OverrideJustification { get; set; }

    // Lines (controlled substances)
    public List<Guid> TransactionLineIds { get; set; } = new();

    // Licences used
    public List<Guid> LicencesUsed { get; set; } = new();

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
