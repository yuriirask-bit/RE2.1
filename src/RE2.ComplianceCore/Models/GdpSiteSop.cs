namespace RE2.ComplianceCore.Models;

/// <summary>
/// Join entity linking a GdpSop to a GdpSite.
/// T273: GdpSiteSop per US12 data-model.md entity 24 (FR-049).
/// Enables inspectors to confirm which SOPs apply to each site.
/// Stored in Dataverse phr_gdpsitesop table.
/// </summary>
public class GdpSiteSop
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid SiteSopId { get; set; }

    /// <summary>
    /// FK to GdpSite (GdpExtensionId).
    /// Required.
    /// </summary>
    public Guid SiteId { get; set; }

    /// <summary>
    /// FK to GdpSop.
    /// Required.
    /// </summary>
    public Guid SopId { get; set; }
}
