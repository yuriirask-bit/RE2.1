using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// DTO for Dataverse phr_gdpsop entity.
/// T278: Dataverse DTO for GDP SOP.
/// </summary>
public class GdpSopDto
{
    public Guid phr_gdpsopid { get; set; }
    public string? phr_sopnumber { get; set; }
    public string? phr_title { get; set; }
    public int phr_category { get; set; }
    public string? phr_version { get; set; }
    public DateTime? phr_effectivedate { get; set; }
    public string? phr_documenturl { get; set; }
    public bool phr_isactive { get; set; }

    public GdpSop ToDomainModel()
    {
        return new GdpSop
        {
            SopId = phr_gdpsopid,
            SopNumber = phr_sopnumber ?? string.Empty,
            Title = phr_title ?? string.Empty,
            Category = (GdpSopCategory)phr_category,
            Version = phr_version ?? string.Empty,
            EffectiveDate = phr_effectivedate.HasValue ? DateOnly.FromDateTime(phr_effectivedate.Value) : default,
            DocumentUrl = phr_documenturl,
            IsActive = phr_isactive
        };
    }
}

/// <summary>
/// DTO for Dataverse phr_gdpsitesop entity.
/// T278: Dataverse DTO for GDP site-SOP link.
/// </summary>
public class GdpSiteSopDto
{
    public Guid phr_gdpsitesopid { get; set; }
    public Guid phr_siteid { get; set; }
    public Guid phr_sopid { get; set; }

    public GdpSiteSop ToDomainModel()
    {
        return new GdpSiteSop
        {
            SiteSopId = phr_gdpsitesopid,
            SiteId = phr_siteid,
            SopId = phr_sopid
        };
    }
}
