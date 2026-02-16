using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_substancecomplianceextension table.
/// Stores compliance-specific metadata only — classification comes from D365 F&O product attributes.
/// </summary>
public class SubstanceComplianceExtensionDto
{
    public Guid phr_complianceextensionid { get; set; }
    public string? phr_substancecode { get; set; }
    public string? phr_substancename { get; set; }
    public string? phr_regulatoryrestrictions { get; set; }
    public bool phr_isactive { get; set; }
    public DateTime? phr_classificationeffectivedate { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }
    public string? phr_rowversion { get; set; }

    /// <summary>
    /// Applies compliance extension data onto an existing domain model
    /// (which already has D365 classification data populated).
    /// </summary>
    public void ApplyToDomainModel(ControlledSubstance substance)
    {
        substance.ComplianceExtensionId = phr_complianceextensionid;
        substance.RegulatoryRestrictions = phr_regulatoryrestrictions;
        substance.IsActive = phr_isactive;
        substance.ClassificationEffectiveDate = phr_classificationeffectivedate.HasValue
            ? DateOnly.FromDateTime(phr_classificationeffectivedate.Value)
            : null;
        substance.CreatedDate = phr_createddate;
        substance.ModifiedDate = phr_modifieddate;
    }

    /// <summary>
    /// Creates a domain model from compliance extension data only (for discovery).
    /// Classification fields will be defaults until merged with D365 data.
    /// </summary>
    public ControlledSubstance ToDomainModel()
    {
        return new ControlledSubstance
        {
            SubstanceCode = phr_substancecode ?? string.Empty,
            SubstanceName = phr_substancename ?? string.Empty,
            ComplianceExtensionId = phr_complianceextensionid,
            RegulatoryRestrictions = phr_regulatoryrestrictions,
            IsActive = phr_isactive,
            ClassificationEffectiveDate = phr_classificationeffectivedate.HasValue
                ? DateOnly.FromDateTime(phr_classificationeffectivedate.Value)
                : null,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate
        };
    }

    /// <summary>
    /// Creates a DTO from domain model for writing to Dataverse.
    /// </summary>
    public static SubstanceComplianceExtensionDto FromDomainModel(ControlledSubstance model)
    {
        return new SubstanceComplianceExtensionDto
        {
            phr_complianceextensionid = model.ComplianceExtensionId,
            phr_substancecode = model.SubstanceCode,
            phr_substancename = model.SubstanceName,
            phr_regulatoryrestrictions = model.RegulatoryRestrictions,
            phr_isactive = model.IsActive,
            phr_classificationeffectivedate = model.ClassificationEffectiveDate?.ToDateTime(TimeOnly.MinValue),
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate
        };
    }
}
