using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_controlledsubstance virtual table.
/// T065: Dataverse DTO for ControlledSubstance.
/// Maps to data-model.md entity 3.
/// </summary>
public class ControlledSubstanceDto
{
    public Guid phr_controlledsubstanceid { get; set; }
    public string? phr_substancename { get; set; }
    public int? phr_opiumactlist { get; set; }
    public int? phr_precursorcategory { get; set; }
    public string? phr_internalcode { get; set; }
    public string? phr_regulatoryrestrictions { get; set; }
    public bool phr_isactive { get; set; }

    public ControlledSubstance ToDomainModel()
    {
        return new ControlledSubstance
        {
            SubstanceId = phr_controlledsubstanceid,
            SubstanceName = phr_substancename ?? string.Empty,
            OpiumActList = (SubstanceCategories.OpiumActList)(phr_opiumactlist ?? 0),
            PrecursorCategory = (SubstanceCategories.PrecursorCategory)(phr_precursorcategory ?? 0),
            InternalCode = phr_internalcode ?? string.Empty,
            RegulatoryRestrictions = phr_regulatoryrestrictions,
            IsActive = phr_isactive
        };
    }

    public static ControlledSubstanceDto FromDomainModel(ControlledSubstance model)
    {
        return new ControlledSubstanceDto
        {
            phr_controlledsubstanceid = model.SubstanceId,
            phr_substancename = model.SubstanceName,
            phr_opiumactlist = (int)model.OpiumActList,
            phr_precursorcategory = (int)model.PrecursorCategory,
            phr_internalcode = model.InternalCode,
            phr_regulatoryrestrictions = model.RegulatoryRestrictions,
            phr_isactive = model.IsActive
        };
    }
}
