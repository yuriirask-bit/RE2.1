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
    public string phr_substancecode { get; set; } = string.Empty;
    public decimal? phr_maxquantitypertransaction { get; set; }
    public decimal? phr_maxquantityperperiod { get; set; }
    public string? phr_periodtype { get; set; }
    public string? phr_restrictions { get; set; }
    public DateTime phr_effectivedate { get; set; }
    public DateTime? phr_expirydate { get; set; }

    public LicenceSubstanceMapping ToDomainModel()
    {
        return new LicenceSubstanceMapping
        {
            MappingId = phr_licencesubstancemappingid,
            LicenceId = phr_licenceid,
            SubstanceCode = phr_substancecode,
            MaxQuantityPerTransaction = phr_maxquantitypertransaction,
            MaxQuantityPerPeriod = phr_maxquantityperperiod,
            PeriodType = phr_periodtype,
            Restrictions = phr_restrictions,
            EffectiveDate = DateOnly.FromDateTime(phr_effectivedate),
            ExpiryDate = phr_expirydate.HasValue ? DateOnly.FromDateTime(phr_expirydate.Value) : null
        };
    }

    public static LicenceSubstanceMappingDto FromDomainModel(LicenceSubstanceMapping model)
    {
        return new LicenceSubstanceMappingDto
        {
            phr_licencesubstancemappingid = model.MappingId,
            phr_licenceid = model.LicenceId,
            phr_substancecode = model.SubstanceCode,
            phr_maxquantitypertransaction = model.MaxQuantityPerTransaction,
            phr_maxquantityperperiod = model.MaxQuantityPerPeriod,
            phr_periodtype = model.PeriodType,
            phr_restrictions = model.Restrictions,
            phr_effectivedate = model.EffectiveDate.ToDateTime(TimeOnly.MinValue),
            phr_expirydate = model.ExpiryDate?.ToDateTime(TimeOnly.MinValue)
        };
    }
}
