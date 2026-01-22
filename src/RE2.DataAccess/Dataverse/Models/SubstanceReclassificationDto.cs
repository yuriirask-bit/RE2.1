using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Dataverse DTO for SubstanceReclassification entity.
/// T080b: Maps to phr_substancereclassification virtual table.
/// </summary>
public class SubstanceReclassificationDto
{
    /// <summary>
    /// Primary key - maps to phr_substancereclassificationid.
    /// </summary>
    public Guid phr_substancereclassificationid { get; set; }

    /// <summary>
    /// Foreign key to substance - maps to phr_substanceid.
    /// </summary>
    public Guid phr_substanceid { get; set; }

    /// <summary>
    /// Previous Opium Act list value.
    /// </summary>
    public int? phr_previousopiumactlist { get; set; }

    /// <summary>
    /// New Opium Act list value.
    /// </summary>
    public int? phr_newopiumactlist { get; set; }

    /// <summary>
    /// Previous precursor category value.
    /// </summary>
    public int? phr_previousprecursorcategory { get; set; }

    /// <summary>
    /// New precursor category value.
    /// </summary>
    public int? phr_newprecursorcategory { get; set; }

    /// <summary>
    /// Effective date of reclassification.
    /// </summary>
    public DateTime? phr_effectivedate { get; set; }

    /// <summary>
    /// Regulatory reference (e.g., gazette number).
    /// </summary>
    public string? phr_regulatoryreference { get; set; }

    /// <summary>
    /// Regulatory authority name.
    /// </summary>
    public string? phr_regulatoryauthority { get; set; }

    /// <summary>
    /// Reason for reclassification.
    /// </summary>
    public string? phr_reason { get; set; }

    /// <summary>
    /// Processing status.
    /// </summary>
    public int phr_status { get; set; }

    /// <summary>
    /// Number of affected customers.
    /// </summary>
    public int phr_affectedcustomercount { get; set; }

    /// <summary>
    /// Number of flagged customers.
    /// </summary>
    public int phr_flaggedcustomercount { get; set; }

    /// <summary>
    /// User who initiated.
    /// </summary>
    public Guid? phr_initiatedbyuserid { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime phr_createdon { get; set; }

    /// <summary>
    /// Processing completion timestamp.
    /// </summary>
    public DateTime? phr_processeddate { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime phr_modifiedon { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public SubstanceReclassification ToDomainModel()
    {
        return new SubstanceReclassification
        {
            ReclassificationId = phr_substancereclassificationid,
            SubstanceId = phr_substanceid,
            PreviousOpiumActList = phr_previousopiumactlist.HasValue
                ? (SubstanceCategories.OpiumActList)phr_previousopiumactlist.Value
                : SubstanceCategories.OpiumActList.None,
            NewOpiumActList = phr_newopiumactlist.HasValue
                ? (SubstanceCategories.OpiumActList)phr_newopiumactlist.Value
                : SubstanceCategories.OpiumActList.None,
            PreviousPrecursorCategory = phr_previousprecursorcategory.HasValue
                ? (SubstanceCategories.PrecursorCategory)phr_previousprecursorcategory.Value
                : SubstanceCategories.PrecursorCategory.None,
            NewPrecursorCategory = phr_newprecursorcategory.HasValue
                ? (SubstanceCategories.PrecursorCategory)phr_newprecursorcategory.Value
                : SubstanceCategories.PrecursorCategory.None,
            EffectiveDate = phr_effectivedate.HasValue
                ? DateOnly.FromDateTime(phr_effectivedate.Value)
                : default,
            RegulatoryReference = phr_regulatoryreference ?? string.Empty,
            RegulatoryAuthority = phr_regulatoryauthority ?? string.Empty,
            Reason = phr_reason,
            Status = (ReclassificationStatus)phr_status,
            AffectedCustomerCount = phr_affectedcustomercount,
            FlaggedCustomerCount = phr_flaggedcustomercount,
            InitiatedByUserId = phr_initiatedbyuserid,
            CreatedDate = phr_createdon,
            ProcessedDate = phr_processeddate,
            ModifiedDate = phr_modifiedon
        };
    }

    /// <summary>
    /// Creates DTO from domain model.
    /// </summary>
    public static SubstanceReclassificationDto FromDomainModel(SubstanceReclassification model)
    {
        return new SubstanceReclassificationDto
        {
            phr_substancereclassificationid = model.ReclassificationId,
            phr_substanceid = model.SubstanceId,
            phr_previousopiumactlist = (int)model.PreviousOpiumActList,
            phr_newopiumactlist = (int)model.NewOpiumActList,
            phr_previousprecursorcategory = (int)model.PreviousPrecursorCategory,
            phr_newprecursorcategory = (int)model.NewPrecursorCategory,
            phr_effectivedate = model.EffectiveDate.ToDateTime(TimeOnly.MinValue),
            phr_regulatoryreference = model.RegulatoryReference,
            phr_regulatoryauthority = model.RegulatoryAuthority,
            phr_reason = model.Reason,
            phr_status = (int)model.Status,
            phr_affectedcustomercount = model.AffectedCustomerCount,
            phr_flaggedcustomercount = model.FlaggedCustomerCount,
            phr_initiatedbyuserid = model.InitiatedByUserId,
            phr_createdon = model.CreatedDate,
            phr_processeddate = model.ProcessedDate,
            phr_modifiedon = model.ModifiedDate
        };
    }
}

/// <summary>
/// Dataverse DTO for ReclassificationCustomerImpact entity.
/// Maps to phr_reclassificationcustomerimpact virtual table.
/// </summary>
public class ReclassificationCustomerImpactDto
{
    public Guid phr_impactid { get; set; }
    public Guid phr_reclassificationid { get; set; }
    public Guid phr_customerid { get; set; }
    public string? phr_customername { get; set; }
    public bool phr_hassufficientlicence { get; set; }
    public bool phr_requiresrequalification { get; set; }
    public string? phr_relevantlicenceids { get; set; }
    public string? phr_licencegapsummary { get; set; }
    public bool phr_notificationsent { get; set; }
    public DateTime? phr_notificationdate { get; set; }
    public DateTime? phr_requalificationdate { get; set; }
    public DateTime phr_createdon { get; set; }

    public ReclassificationCustomerImpact ToDomainModel()
    {
        return new ReclassificationCustomerImpact
        {
            ImpactId = phr_impactid,
            ReclassificationId = phr_reclassificationid,
            CustomerId = phr_customerid,
            CustomerName = phr_customername,
            HasSufficientLicence = phr_hassufficientlicence,
            RequiresReQualification = phr_requiresrequalification,
            RelevantLicenceIds = string.IsNullOrEmpty(phr_relevantlicenceids)
                ? null
                : phr_relevantlicenceids.Split(',').Select(Guid.Parse).ToList(),
            LicenceGapSummary = phr_licencegapsummary,
            NotificationSent = phr_notificationsent,
            NotificationDate = phr_notificationdate,
            ReQualificationDate = phr_requalificationdate,
            CreatedDate = phr_createdon
        };
    }

    public static ReclassificationCustomerImpactDto FromDomainModel(ReclassificationCustomerImpact model)
    {
        return new ReclassificationCustomerImpactDto
        {
            phr_impactid = model.ImpactId,
            phr_reclassificationid = model.ReclassificationId,
            phr_customerid = model.CustomerId,
            phr_customername = model.CustomerName,
            phr_hassufficientlicence = model.HasSufficientLicence,
            phr_requiresrequalification = model.RequiresReQualification,
            phr_relevantlicenceids = model.RelevantLicenceIds != null
                ? string.Join(",", model.RelevantLicenceIds)
                : null,
            phr_licencegapsummary = model.LicenceGapSummary,
            phr_notificationsent = model.NotificationSent,
            phr_notificationdate = model.NotificationDate,
            phr_requalificationdate = model.ReQualificationDate,
            phr_createdon = model.CreatedDate
        };
    }
}
