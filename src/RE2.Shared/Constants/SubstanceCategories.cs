namespace RE2.Shared.Constants;

/// <summary>
/// Controlled substance category constants (Dutch Opium Act classification)
/// T050: Substance categories constants
/// </summary>
public static class SubstanceCategories
{
    // Dutch Opium Act Lists
    public const string OPIUM_ACT_LIST_I = "OpiumActListI";      // Hard drugs (cocaine, heroin, etc.)
    public const string OPIUM_ACT_LIST_II = "OpiumActListII";    // Soft drugs, medical narcotics

    // Medicine Act categories
    public const string PRESCRIPTION_MEDICINE = "PrescriptionMedicine";
    public const string CONTROLLED_MEDICINE = "ControlledMedicine";
    public const string PHARMACY_ONLY = "PharmacyOnly";

    // EU Precursor categories
    public const string PRECURSOR_CATEGORY_1 = "PrecursorCategory1";
    public const string PRECURSOR_CATEGORY_2 = "PrecursorCategory2";
    public const string PRECURSOR_CATEGORY_3 = "PrecursorCategory3";

    // Special categories
    public const string PSYCHOTROPIC_SUBSTANCE = "PsychotropicSubstance";
    public const string NARCOTIC_RAW_MATERIAL = "NarcoticRawMaterial";
    public const string VETERINARY_CONTROLLED = "VeterinaryControlled";
}
