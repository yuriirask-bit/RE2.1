using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_customercomplianceextension table.
/// Stores compliance-specific extensions for customers whose master data lives in D365FO.
/// Composite key: phr_customeraccount + phr_dataareaid links to D365FO CustomersV3.
/// </summary>
public class CustomerComplianceExtensionDto
{
    public Guid phr_complianceextensionid { get; set; }
    public string? phr_customeraccount { get; set; }
    public string? phr_dataareaid { get; set; }
    public int phr_businesscategory { get; set; }
    public int phr_approvalstatus { get; set; }
    public int phr_gdpqualificationstatus { get; set; }
    public DateTime? phr_onboardingdate { get; set; }
    public DateTime? phr_nextreverificationdate { get; set; }
    public bool phr_issuspended { get; set; }
    public string? phr_suspensionreason { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }
    public byte[]? phr_rowversion { get; set; }

    /// <summary>
    /// Applies compliance extension fields onto an existing Customer domain model
    /// (which already has D365FO fields populated).
    /// </summary>
    public void ApplyToDomainModel(Customer customer)
    {
        customer.ComplianceExtensionId = phr_complianceextensionid;
        customer.BusinessCategory = (BusinessCategory)phr_businesscategory;
        customer.ApprovalStatus = (ApprovalStatus)phr_approvalstatus;
        customer.GdpQualificationStatus = (GdpQualificationStatus)phr_gdpqualificationstatus;
        customer.OnboardingDate = phr_onboardingdate.HasValue
            ? DateOnly.FromDateTime(phr_onboardingdate.Value)
            : null;
        customer.NextReVerificationDate = phr_nextreverificationdate.HasValue
            ? DateOnly.FromDateTime(phr_nextreverificationdate.Value)
            : null;
        customer.IsSuspended = phr_issuspended;
        customer.SuspensionReason = phr_suspensionreason;
        customer.CreatedDate = phr_createddate;
        customer.ModifiedDate = phr_modifieddate;
        customer.RowVersion = phr_rowversion ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Converts DTO to domain model (compliance extension fields only; D365FO fields will be defaults).
    /// </summary>
    public Customer ToDomainModel()
    {
        var customer = new Customer
        {
            CustomerAccount = phr_customeraccount ?? string.Empty,
            DataAreaId = phr_dataareaid ?? string.Empty
        };
        ApplyToDomainModel(customer);
        return customer;
    }

    /// <summary>
    /// Converts domain model to DTO (compliance extension fields only).
    /// </summary>
    public static CustomerComplianceExtensionDto FromDomainModel(Customer model)
    {
        return new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = model.ComplianceExtensionId,
            phr_customeraccount = model.CustomerAccount,
            phr_dataareaid = model.DataAreaId,
            phr_businesscategory = (int)model.BusinessCategory,
            phr_approvalstatus = (int)model.ApprovalStatus,
            phr_gdpqualificationstatus = (int)model.GdpQualificationStatus,
            phr_onboardingdate = model.OnboardingDate.HasValue
                ? model.OnboardingDate.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            phr_nextreverificationdate = model.NextReVerificationDate.HasValue
                ? model.NextReVerificationDate.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            phr_issuspended = model.IsSuspended,
            phr_suspensionreason = model.SuspensionReason,
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate,
            phr_rowversion = model.RowVersion.Length > 0 ? model.RowVersion : null
        };
    }
}
