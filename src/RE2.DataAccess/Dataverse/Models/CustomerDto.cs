using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse.Models;

/// <summary>
/// Data transfer object for Dataverse phr_customer virtual table.
/// T087: Dataverse DTO for Customer.
/// Maps to data-model.md entity 5.
/// </summary>
public class CustomerDto
{
    public Guid phr_customerid { get; set; }
    public string? phr_businessname { get; set; }
    public string? phr_registrationnumber { get; set; }
    public int phr_businesscategory { get; set; }
    public string? phr_country { get; set; }
    public int phr_approvalstatus { get; set; }
    public DateTime? phr_onboardingdate { get; set; }
    public DateTime? phr_nextreverificationdate { get; set; }
    public int phr_gdpqualificationstatus { get; set; }
    public bool phr_issuspended { get; set; }
    public string? phr_suspensionreason { get; set; }
    public DateTime phr_createddate { get; set; }
    public DateTime phr_modifieddate { get; set; }
    public byte[]? phr_rowversion { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public Customer ToDomainModel()
    {
        return new Customer
        {
            CustomerId = phr_customerid,
            BusinessName = phr_businessname ?? string.Empty,
            RegistrationNumber = phr_registrationnumber,
            BusinessCategory = (BusinessCategory)phr_businesscategory,
            Country = phr_country ?? string.Empty,
            ApprovalStatus = (ApprovalStatus)phr_approvalstatus,
            OnboardingDate = phr_onboardingdate.HasValue
                ? DateOnly.FromDateTime(phr_onboardingdate.Value)
                : null,
            NextReVerificationDate = phr_nextreverificationdate.HasValue
                ? DateOnly.FromDateTime(phr_nextreverificationdate.Value)
                : null,
            GdpQualificationStatus = (GdpQualificationStatus)phr_gdpqualificationstatus,
            IsSuspended = phr_issuspended,
            SuspensionReason = phr_suspensionreason,
            CreatedDate = phr_createddate,
            ModifiedDate = phr_modifieddate,
            RowVersion = phr_rowversion ?? Array.Empty<byte>()
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    public static CustomerDto FromDomainModel(Customer model)
    {
        return new CustomerDto
        {
            phr_customerid = model.CustomerId,
            phr_businessname = model.BusinessName,
            phr_registrationnumber = model.RegistrationNumber,
            phr_businesscategory = (int)model.BusinessCategory,
            phr_country = model.Country,
            phr_approvalstatus = (int)model.ApprovalStatus,
            phr_onboardingdate = model.OnboardingDate.HasValue
                ? model.OnboardingDate.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            phr_nextreverificationdate = model.NextReVerificationDate.HasValue
                ? model.NextReVerificationDate.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            phr_gdpqualificationstatus = (int)model.GdpQualificationStatus,
            phr_issuspended = model.IsSuspended,
            phr_suspensionreason = model.SuspensionReason,
            phr_createddate = model.CreatedDate,
            phr_modifieddate = model.ModifiedDate,
            phr_rowversion = model.RowVersion.Length > 0 ? model.RowVersion : null
        };
    }
}
