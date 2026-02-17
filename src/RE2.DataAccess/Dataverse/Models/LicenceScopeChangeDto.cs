using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_licencescopechange virtual table.
/// T108: DTO for LicenceScopeChange mapping to data-model.md entity 14.
/// Tracks historical changes to licence scope and authorized substances.
/// </summary>
public class LicenceScopeChangeDto
{
    /// <summary>
    /// Primary key in Dataverse.
    /// </summary>
    public Guid phr_changeid { get; set; }

    /// <summary>
    /// Foreign key to licence.
    /// </summary>
    public Guid phr_licenceid { get; set; }

    /// <summary>
    /// When the change took effect.
    /// </summary>
    public DateTime phr_effectivedate { get; set; }

    /// <summary>
    /// Description of what changed.
    /// </summary>
    public string? phr_changedescription { get; set; }

    /// <summary>
    /// Type of change as integer per ScopeChangeType enum.
    /// </summary>
    public int phr_changetype { get; set; }

    /// <summary>
    /// Who recorded the change.
    /// </summary>
    public Guid phr_recordedby { get; set; }

    /// <summary>
    /// Name of the recorder (for display).
    /// </summary>
    public string? phr_recordername { get; set; }

    /// <summary>
    /// When the change was recorded in the system.
    /// </summary>
    public DateTime phr_recordeddate { get; set; }

    /// <summary>
    /// Reference to supporting document.
    /// </summary>
    public Guid? phr_supportingdocumentid { get; set; }

    /// <summary>
    /// Substances added (comma-separated internal codes).
    /// </summary>
    public string? phr_substancesadded { get; set; }

    /// <summary>
    /// Substances removed (comma-separated internal codes).
    /// </summary>
    public string? phr_substancesremoved { get; set; }

    /// <summary>
    /// Activities added.
    /// </summary>
    public string? phr_activitiesadded { get; set; }

    /// <summary>
    /// Activities removed.
    /// </summary>
    public string? phr_activitiesremoved { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>LicenceScopeChange domain model.</returns>
    public LicenceScopeChange ToDomainModel()
    {
        return new LicenceScopeChange
        {
            ChangeId = phr_changeid,
            LicenceId = phr_licenceid,
            EffectiveDate = DateOnly.FromDateTime(phr_effectivedate),
            ChangeDescription = phr_changedescription ?? string.Empty,
            ChangeType = (ScopeChangeType)phr_changetype,
            RecordedBy = phr_recordedby,
            RecorderName = phr_recordername,
            RecordedDate = phr_recordeddate,
            SupportingDocumentId = phr_supportingdocumentid,
            SubstancesAdded = phr_substancesadded,
            SubstancesRemoved = phr_substancesremoved,
            ActivitiesAdded = phr_activitiesadded,
            ActivitiesRemoved = phr_activitiesremoved
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>LicenceScopeChangeDto for Dataverse persistence.</returns>
    public static LicenceScopeChangeDto FromDomainModel(LicenceScopeChange model)
    {
        return new LicenceScopeChangeDto
        {
            phr_changeid = model.ChangeId,
            phr_licenceid = model.LicenceId,
            phr_effectivedate = model.EffectiveDate.ToDateTime(TimeOnly.MinValue),
            phr_changedescription = model.ChangeDescription,
            phr_changetype = (int)model.ChangeType,
            phr_recordedby = model.RecordedBy,
            phr_recordername = model.RecorderName,
            phr_recordeddate = model.RecordedDate,
            phr_supportingdocumentid = model.SupportingDocumentId,
            phr_substancesadded = model.SubstancesAdded,
            phr_substancesremoved = model.SubstancesRemoved,
            phr_activitiesadded = model.ActivitiesAdded,
            phr_activitiesremoved = model.ActivitiesRemoved
        };
    }
}

/// <summary>
/// OData response wrapper for LicenceScopeChange queries.
/// </summary>
public class LicenceScopeChangeODataResponse
{
    /// <summary>
    /// Collection of licence scope change DTOs.
    /// </summary>
    public List<LicenceScopeChangeDto> value { get; set; } = new();
}
