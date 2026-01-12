namespace RE2.ComplianceCore.Models;

/// <summary>
/// Controlled substance (Opium Act Lists I/II, precursors)
/// T061: ControlledSubstance domain model (data-model.md entity 3)
/// </summary>
public class ControlledSubstance
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // OpiumActListI, OpiumActListII, etc.

    // Classification details
    public string OpiumActList { get; set; } = string.Empty; // List I, List II, or empty
    public bool IsPrecursor { get; set; }
    public string? PrecursorCategory { get; set; } // 1, 2, 3, or null

    // Regulatory thresholds
    public decimal? ThresholdQuantity { get; set; }
    public string? ThresholdUnit { get; set; }

    // Active status
    public bool IsActive { get; set; }

    // Reclassification tracking (FR-066)
    public DateTime? ReclassificationDate { get; set; }
    public string? PreviousCategory { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
