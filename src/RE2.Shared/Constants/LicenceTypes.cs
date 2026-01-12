namespace RE2.Shared.Constants;

/// <summary>
/// Licence type constants for controlled drug authorizations
/// T049: Licence types constants
/// </summary>
public static class LicenceTypes
{
    // Dutch Opium Act licences
    public const string WHOLESALE_LICENCE = "WholesaleLicence";
    public const string OPIUM_ACT_EXEMPTION = "OpiumActExemption";
    public const string IMPORT_PERMIT = "ImportPermit";
    public const string EXPORT_PERMIT = "ExportPermit";

    // Pharmacy and medical licences
    public const string PHARMACY_LICENCE = "PharmacyLicence";
    public const string HOSPITAL_PHARMACY = "HospitalPharmacy";
    public const string VETERINARY_LICENCE = "VeterinaryLicence";

    // GDP authorizations
    public const string GDP_WHOLESALE_DISTRIBUTION = "GdpWholesaleDistribution";
    public const string GDP_CERTIFICATE = "GdpCertificate";

    // Special authorizations
    public const string RESEARCH_LICENCE = "ResearchLicence";
    public const string MANUFACTURING_LICENCE = "ManufacturingLicence";
}
