using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpwarehouseextension table.
/// T188: Dataverse GDP extension data transfer object.
/// </summary>
public class GdpWarehouseExtensionDto
{
    public Guid phr_gdpextensionid { get; set; }
    public string? phr_warehouseid { get; set; }
    public string? phr_dataareaid { get; set; }
    public int phr_gdpsitetype { get; set; }
    public int phr_permittedactivities { get; set; }
    public bool phr_isgdpactive { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }
    public byte[]? phr_rowversion { get; set; }

    /// <summary>
    /// Maps GDP extension DTO fields onto an existing GdpSite domain model.
    /// D365FO warehouse fields must already be set on the target.
    /// </summary>
    public void ApplyToDomainModel(GdpSite site)
    {
        site.GdpExtensionId = phr_gdpextensionid;
        site.GdpSiteType = (GdpSiteType)phr_gdpsitetype;
        site.PermittedActivities = (GdpSiteActivity)phr_permittedactivities;
        site.IsGdpActive = phr_isgdpactive;
        site.CreatedDate = phr_createddate;
        site.ModifiedDate = phr_modifieddate;
        site.RowVersion = phr_rowversion ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Creates a domain model with only GDP extension fields populated.
    /// D365FO warehouse fields will need to be merged separately.
    /// </summary>
    public GdpSite ToDomainModel()
    {
        return new GdpSite
        {
            WarehouseId = phr_warehouseid ?? string.Empty,
            DataAreaId = phr_dataareaid ?? string.Empty,
            GdpExtensionId = phr_gdpextensionid,
            GdpSiteType = (GdpSiteType)phr_gdpsitetype,
            PermittedActivities = (GdpSiteActivity)phr_permittedactivities,
            IsGdpActive = phr_isgdpactive,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate,
            RowVersion = phr_rowversion ?? Array.Empty<byte>()
        };
    }

    public static GdpWarehouseExtensionDto FromDomainModel(GdpSite site)
    {
        return new GdpWarehouseExtensionDto
        {
            phr_gdpextensionid = site.GdpExtensionId,
            phr_warehouseid = site.WarehouseId,
            phr_dataareaid = site.DataAreaId,
            phr_gdpsitetype = (int)site.GdpSiteType,
            phr_permittedactivities = (int)site.PermittedActivities,
            phr_isgdpactive = site.IsGdpActive,
            phr_createddate = site.CreatedDate,
            phr_modifieddate = site.ModifiedDate,
            phr_rowversion = site.RowVersion
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_gdpsitewdacoverage table.
/// T188: WDA coverage data transfer object.
/// </summary>
public class GdpSiteWdaCoverageDto
{
    public Guid phr_coverageid { get; set; }
    public string? phr_warehouseid { get; set; }
    public string? phr_dataareaid { get; set; }
    public Guid phr_licenceid { get; set; }
    public DateTime phr_effectivedate { get; set; }
    public DateTime? phr_expirydate { get; set; }

    public GdpSiteWdaCoverage ToDomainModel()
    {
        return new GdpSiteWdaCoverage
        {
            CoverageId = phr_coverageid,
            WarehouseId = phr_warehouseid ?? string.Empty,
            DataAreaId = phr_dataareaid ?? string.Empty,
            LicenceId = phr_licenceid,
            EffectiveDate = DateOnly.FromDateTime(phr_effectivedate),
            ExpiryDate = phr_expirydate.HasValue ? DateOnly.FromDateTime(phr_expirydate.Value) : null
        };
    }

    public static GdpSiteWdaCoverageDto FromDomainModel(GdpSiteWdaCoverage coverage)
    {
        return new GdpSiteWdaCoverageDto
        {
            phr_coverageid = coverage.CoverageId,
            phr_warehouseid = coverage.WarehouseId,
            phr_dataareaid = coverage.DataAreaId,
            phr_licenceid = coverage.LicenceId,
            phr_effectivedate = coverage.EffectiveDate.ToDateTime(TimeOnly.MinValue),
            phr_expirydate = coverage.ExpiryDate?.ToDateTime(TimeOnly.MinValue)
        };
    }
}
