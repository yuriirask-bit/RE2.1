namespace RE2.ComplianceCore.Models;

/// <summary>
/// Licence instance (customer's specific authorization)
/// T063: Licence domain model (data-model.md entity 1)
/// </summary>
public class Licence
{
    public Guid Id { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;

    // Relationships
    public Guid LicenceTypeId { get; set; }
    public Guid? CustomerId { get; set; } // Nullable for company-wide licences

    // Validity
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }

    // Status
    public string Status { get; set; } = string.Empty; // Active, Expired, Suspended, Revoked
    public bool IsSuspended { get; set; }
    public DateTime? SuspensionDate { get; set; }
    public string? SuspensionReason { get; set; }

    // Scope
    public string Scope { get; set; } = string.Empty; // Description of authorized scope
    public List<string> AuthorizedActivities { get; set; } = new();
    public List<Guid> AuthorizedSubstances { get; set; } = new(); // Substance IDs

    // Issuing authority
    public string IssuingAuthority { get; set; } = string.Empty;
    public string? IssuingOfficer { get; set; }

    // Verification tracking (FR-009)
    public DateTime? LastVerifiedDate { get; set; }
    public string? VerificationMethod { get; set; } // IGJ website, email, Farmatec
    public string? VerifiedBy { get; set; }

    // Alert tracking (FR-007)
    public DateTime? NextReviewDate { get; set; }
    public bool AlertGenerated90Days { get; set; }
    public bool AlertGenerated60Days { get; set; }
    public bool AlertGenerated30Days { get; set; }

    // Optimistic concurrency (FR-027a)
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
