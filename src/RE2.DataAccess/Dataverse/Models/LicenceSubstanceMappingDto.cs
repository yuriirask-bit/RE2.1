using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licencesubstancemapping virtual table.
/// T066: Dataverse DTO for LicenceSubstanceMapping.
/// Maps to data-model.md entity 4.
/// </summary>
public class LicenceSubstanceMappingDto
{
    public Guid phr_licencesubstancemappingid { get; set; }
    public Guid phr_licenceid { get; set; }
    public Guid phr_substanceid { get; set; }
    public decimal? phr_maxquantitypertransaction { get; set; }
    public decimal? phr_maxquantityperperiod { get; set; }
    public string? phr_periodtype { get; set; }
    public string? phr_restrictions { get; set; }

    public LicenceSubstanceMapping ToDomainModel()
    {
        return new LicenceSubstanceMapping
        {
            MappingId = phr_licencesubstancemappingid,
            LicenceId = phr_licenceid,
            SubstanceId = phr_substanceid,
            MaxQuantityPerTransaction = phr_maxquantitypertransaction,
            MaxQuantityPerPeriod = phr_maxquantityperperiod,
            PeriodType = phr_periodtype,
            Restrictions = phr_restrictions
        };
    }

    public static LicenceSubstanceMappingDto FromDomainModel(LicenceSubstanceMapping model)
    {
        return new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = model.MappingId,
            phr_licenceid = model.LicenceId,
            phr_substanceid = model.SubstanceId,
            phr_maxquantitypertransaction = model.MaxQuantityPerTransaction,
            phr_maxquantityperperiod = model.MaxQuantityPerPeriod,
            phr_periodtype = model.PeriodType,
            phr_restrictions = model.Restrictions
        };
    }
}
