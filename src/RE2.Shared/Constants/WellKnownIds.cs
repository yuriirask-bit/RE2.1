namespace RE2.Shared.Constants;

/// <summary>
/// Well-known GUIDs for testing and seed data reference.
/// These are stable IDs used across the system for referencing seed data entities.
/// </summary>
public static class WellKnownIds
{
    #region Holder IDs

    /// <summary>
    /// Company (wholesaler) holder ID for internal licences.
    /// </summary>
    public static readonly Guid CompanyHolderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Hospital customer ID for testing.
    /// </summary>
    public static readonly Guid CustomerHospitalId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    /// <summary>
    /// Community pharmacy customer ID for testing.
    /// </summary>
    public static readonly Guid CustomerPharmacyId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    /// <summary>
    /// Veterinarian customer ID for testing.
    /// </summary>
    public static readonly Guid CustomerVeterinarianId = Guid.Parse("00000000-0000-0000-0000-000000000012");

    /// <summary>
    /// Suspended customer ID for testing blocked transactions.
    /// </summary>
    public static readonly Guid CustomerSuspendedId = Guid.Parse("00000000-0000-0000-0000-000000000013");

    #endregion

    #region Licence Type IDs

    /// <summary>
    /// Wholesale Distribution Authorization (WDA) licence type.
    /// </summary>
    public static readonly Guid WholesaleLicenceTypeId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Opium Act Exemption licence type.
    /// </summary>
    public static readonly Guid OpiumExemptionTypeId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Import Permit (Opium Act) licence type.
    /// </summary>
    public static readonly Guid ImportPermitTypeId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    /// <summary>
    /// Export Permit (Opium Act) licence type.
    /// </summary>
    public static readonly Guid ExportPermitTypeId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    /// <summary>
    /// Pharmacy licence type.
    /// </summary>
    public static readonly Guid PharmacyLicenceTypeId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    /// <summary>
    /// Precursor Registration licence type.
    /// </summary>
    public static readonly Guid PrecursorRegistrationTypeId = Guid.Parse("10000000-0000-0000-0000-000000000006");

    #endregion

    #region Substance IDs

    /// <summary>
    /// Morphine substance ID.
    /// </summary>
    public static readonly Guid MorphineId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Fentanyl substance ID.
    /// </summary>
    public static readonly Guid FentanylId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Oxycodone substance ID.
    /// </summary>
    public static readonly Guid OxycodoneId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    /// <summary>
    /// Codeine substance ID.
    /// </summary>
    public static readonly Guid CodeineId = Guid.Parse("20000000-0000-0000-0000-000000000010");

    /// <summary>
    /// Diazepam substance ID.
    /// </summary>
    public static readonly Guid DiazepamId = Guid.Parse("20000000-0000-0000-0000-000000000011");

    /// <summary>
    /// Ephedrine substance ID.
    /// </summary>
    public static readonly Guid EphedrineId = Guid.Parse("20000000-0000-0000-0000-000000000020");

    #endregion
}
