namespace RE2.ComplianceCore.Models;

/// <summary>
/// Customer/trading partner profile with compliance status
/// T085: Customer domain model (data-model.md entity 5)
/// </summary>
public class Customer
{
    public Guid Id { get; set; }
    public string CustomerNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalEntityType { get; set; } = string.Empty; // Hospital, Pharmacy, Wholesaler, etc.

    // Location
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Address { get; set; }

    // Compliance status (FR-013, FR-014)
    public string ApprovalStatus { get; set; } = string.Empty; // Approved, Pending, Rejected, Suspended
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovedBy { get; set; }

    // Suspension (FR-015)
    public bool IsSuspended { get; set; }
    public DateTime? SuspensionDate { get; set; }
    public string? SuspensionReason { get; set; }

    // Required licences
    public List<Guid> RequiredLicenceTypes { get; set; } = new();
    public List<Guid> HeldLicences { get; set; } = new();

    // Re-verification tracking (FR-017)
    public DateTime? LastQualificationDate { get; set; }
    public DateTime? NextReQualificationDate { get; set; }
    public int ReQualificationFrequencyMonths { get; set; } = 36; // Default 3 years

    // GDP credentials
    public string? GdpStatus { get; set; }
    public string? WdaNumber { get; set; }
    public DateTime? GdpCertificateExpiry { get; set; }

    // Optimistic concurrency
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
