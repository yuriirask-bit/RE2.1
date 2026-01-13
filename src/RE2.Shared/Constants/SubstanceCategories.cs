namespace RE2.Shared.Constants;

/// <summary>
/// Controlled substance classification categories per Dutch and EU regulations.
/// Corresponds to ControlledSubstance entity fields in data-model.md.
/// </summary>
public static class SubstanceCategories
{
    #region Opium Act Classifications

    /// <summary>
    /// Dutch Opium Act List classification enum.
    /// Per data-model.md: ControlledSubstance.OpiumActList
    /// </summary>
    public enum OpiumActList
    {
        /// <summary>Not classified under Opium Act.</summary>
        None = 0,

        /// <summary>
        /// List I: Hard drugs with unacceptable health risks and no therapeutic value.
        /// Examples: Heroin, cocaine, MDMA (ecstasy), LSD.
        /// </summary>
        ListI = 1,

        /// <summary>
        /// List II: Substances with therapeutic applications (medical narcotics) or lower risk profile.
        /// Examples: Cannabis, morphine, methadone, fentanyl (medical use).
        /// </summary>
        ListII = 2
    }

    #endregion

    #region EU Precursor Classifications

    /// <summary>
    /// EU Precursor Category classification per Regulation (EC) No 273/2004 and 111/2005.
    /// Per data-model.md: ControlledSubstance.PrecursorCategory
    /// </summary>
    public enum PrecursorCategory
    {
        /// <summary>Not classified as precursor.</summary>
        None = 0,

        /// <summary>
        /// Category 1: Scheduled substances used in illicit drug manufacturing.
        /// Strictest controls - registration, licensing, and pre-export notification required.
        /// Examples: Ephedrine, pseudoephedrine, acetic anhydride.
        /// </summary>
        Category1 = 1,

        /// <summary>
        /// Category 2: Scheduled substances with legitimate uses but potential for drug manufacturing.
        /// Registration and documentation required for transactions.
        /// Examples: Acetone, toluene, sulfuric acid (above threshold quantities).
        /// </summary>
        Category2 = 2,

        /// <summary>
        /// Category 3: Scheduled substances requiring monitoring and record-keeping.
        /// Examples: Certain solvents and chemicals used in small-scale synthesis.
        /// </summary>
        Category3 = 3
    }

    #endregion

    #region Display Names for UI and Reporting

    // Opium Act display names
    public const string OPIUM_ACT_LIST_I_DISPLAY = "Opium Act List I";
    public const string OPIUM_ACT_LIST_II_DISPLAY = "Opium Act List II";

    // Precursor category display names
    public const string PRECURSOR_CATEGORY_1_DISPLAY = "EU Precursor Category 1";
    public const string PRECURSOR_CATEGORY_2_DISPLAY = "EU Precursor Category 2";
    public const string PRECURSOR_CATEGORY_3_DISPLAY = "EU Precursor Category 3";

    #endregion

    #region Business Category Classifications (for Customer entity)

    /// <summary>
    /// Customer business category classification.
    /// Per data-model.md: Customer.BusinessCategory
    /// Determines which licence types are required for the customer.
    /// </summary>
    public enum BusinessCategory
    {
        /// <summary>Hospital pharmacy (Ziekenhuisapotheek).</summary>
        HospitalPharmacy = 1,

        /// <summary>Community/retail pharmacy (Openbare apotheek).</summary>
        CommunityPharmacy = 2,

        /// <summary>Veterinary practice.</summary>
        Veterinarian = 3,

        /// <summary>Pharmaceutical manufacturer.</summary>
        Manufacturer = 4,

        /// <summary>Wholesaler within EU.</summary>
        WholesalerEU = 5,

        /// <summary>Wholesaler outside EU.</summary>
        WholesalerNonEU = 6,

        /// <summary>Research institution or laboratory.</summary>
        ResearchInstitution = 7
    }

    #endregion
}
