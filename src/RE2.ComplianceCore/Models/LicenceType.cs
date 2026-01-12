namespace RE2.ComplianceCore.Models;

/// <summary>
/// Licence type definition (wholesale, exemptions, permits)
/// with permitted activities and substance mappings
/// T060: LicenceType domain model (data-model.md entity 2)
/// </summary>
public class LicenceType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Permitted activities (possess, store, distribute, manufacture, import, export)
    public List<string> PermittedActivities { get; set; } = new();

    // Regulatory authority (IGJ, NVWA, Ministry)
    public string IssuingAuthority { get; set; } = string.Empty;

    // Validity period defaults
    public int DefaultValidityMonths { get; set; }

    // Active status
    public bool IsActive { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
