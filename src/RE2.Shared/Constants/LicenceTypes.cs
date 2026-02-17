namespace RE2.Shared.Constants;

/// <summary>
/// Licence type constants for controlled drug and GDP authorizations.
/// Represents categories of legal authorizations (permits, exemptions, certificates) as referenced in spec.md User Story 1.
/// These constants are used for display names and configuration - actual master data is stored in Dataverse LicenceType entity.
/// </summary>
public static class LicenceTypes
{
    #region Dutch Controlled Drug Licences (Opium Act & Medicines Act)

    /// <summary>
    /// Wholesale Licence under Dutch Medicines Act (Geneesmiddelenwet).
    /// Required for wholesale distribution of medicinal products in the Netherlands.
    /// </summary>
    public const string WHOLESALE_LICENCE = "Wholesale Licence (Medicines Act)";

    /// <summary>
    /// Opium Act Exemption (Vrijstelling Opiumwet).
    /// Required to handle substances on Dutch Opium Act Lists I or II.
    /// </summary>
    public const string OPIUM_ACT_EXEMPTION = "Opium Act Exemption";

    /// <summary>
    /// Import Permit for controlled substances under Opium Act.
    /// Required for cross-border import of Opium Act-listed drugs.
    /// </summary>
    public const string IMPORT_PERMIT_OPIUM = "Import Permit (Opium Act)";

    /// <summary>
    /// Export Permit for controlled substances under Opium Act.
    /// Required for cross-border export of Opium Act-listed drugs.
    /// </summary>
    public const string EXPORT_PERMIT_OPIUM = "Export Permit (Opium Act)";

    /// <summary>
    /// Precursor Registration under EU Regulation (EC) No 273/2004 and 111/2005.
    /// Required to handle chemical precursors (Category 1, 2, or 3).
    /// </summary>
    public const string PRECURSOR_REGISTRATION = "Precursor Registration";

    #endregion

    #region Pharmacy and Medical Practice Licences

    /// <summary>
    /// Community Pharmacy Licence (Apotheekvergunning).
    /// Required to operate a retail pharmacy in the Netherlands.
    /// </summary>
    public const string PHARMACY_LICENCE = "Community Pharmacy Licence";

    /// <summary>
    /// Hospital Pharmacy Licence (Ziekenhuisapotheek).
    /// Required to operate a hospital pharmacy.
    /// </summary>
    public const string HOSPITAL_PHARMACY_LICENCE = "Hospital Pharmacy Licence";

    /// <summary>
    /// Veterinary Practice Registration.
    /// Required for veterinarians to handle veterinary controlled drugs.
    /// </summary>
    public const string VETERINARY_LICENCE = "Veterinary Practice Registration";

    #endregion

    #region GDP (Good Distribution Practice) Authorizations

    /// <summary>
    /// Wholesale Distribution Authorization (WDA) under EU GDP guidelines.
    /// Required to distribute medicinal products under GDP compliance.
    /// </summary>
    public const string GDP_WHOLESALE_DISTRIBUTION = "Wholesale Distribution Authorization (WDA)";

    /// <summary>
    /// GDP Certificate issued by competent authority.
    /// Demonstrates compliance with EU Good Distribution Practice guidelines.
    /// </summary>
    public const string GDP_CERTIFICATE = "GDP Certificate";

    #endregion

    #region Manufacturing and Research Authorizations

    /// <summary>
    /// Manufacturing Licence for controlled substances.
    /// Required to produce or manufacture Opium Act-listed drugs.
    /// </summary>
    public const string MANUFACTURING_LICENCE = "Manufacturing Licence";

    /// <summary>
    /// Research Institution Licence for controlled substances.
    /// Required for scientific research involving Opium Act-listed drugs.
    /// </summary>
    public const string RESEARCH_LICENCE = "Research Institution Licence";

    #endregion

    #region Permitted Activities (Flags)

    /// <summary>
    /// Permitted activities that can be associated with licence types.
    /// Used in LicenceType.PermittedActivities and Licence.PermittedActivities (see data-model.md).
    /// </summary>
    [Flags]
    public enum PermittedActivity
    {
        /// <summary>No activities permitted.</summary>
        None = 0,

        /// <summary>Possess controlled substances.</summary>
        Possess = 1 << 0,

        /// <summary>Store controlled substances in warehouse/pharmacy.</summary>
        Store = 1 << 1,

        /// <summary>Distribute controlled substances to customers.</summary>
        Distribute = 1 << 2,

        /// <summary>Import controlled substances from foreign countries.</summary>
        Import = 1 << 3,

        /// <summary>Export controlled substances to foreign countries.</summary>
        Export = 1 << 4,

        /// <summary>Manufacture or produce controlled substances.</summary>
        Manufacture = 1 << 5,

        /// <summary>Handle chemical precursors (EU Regulation 273/2004, 111/2005).</summary>
        HandlePrecursors = 1 << 6
    }

    #endregion
}
