using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// Provides seed data for in-memory repositories to enable local testing of User Story 1.
/// Contains realistic Dutch pharmaceutical licence management test data.
/// </summary>
public static class InMemorySeedData
{
    // Well-known IDs for testing
    public static readonly Guid CompanyHolderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid CustomerHospitalId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid CustomerPharmacyId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    public static readonly Guid CustomerVeterinarianId = Guid.Parse("00000000-0000-0000-0000-000000000012");

    public static readonly Guid WholesaleLicenceTypeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid OpiumExemptionTypeId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid ImportPermitTypeId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid ExportPermitTypeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid PharmacyLicenceTypeId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid PrecursorRegistrationTypeId = Guid.Parse("10000000-0000-0000-0000-000000000006");

    /// <summary>
    /// Seeds all repositories with test data for User Story 1.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
    }

    /// <summary>
    /// Gets sample licence types per Dutch regulatory framework.
    /// </summary>
    public static IEnumerable<LicenceType> GetLicenceTypes()
    {
        return new List<LicenceType>
        {
            new()
            {
                LicenceTypeId = WholesaleLicenceTypeId,
                Name = "Wholesale Distribution Authorisation (WDA)",
                IssuingAuthority = "IGJ",
                TypicalValidityMonths = null, // Permanent until revoked
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute,
                IsActive = true
            },
            new()
            {
                LicenceTypeId = OpiumExemptionTypeId,
                Name = "Opium Act Exemption",
                IssuingAuthority = "Farmatec/CIBG",
                TypicalValidityMonths = 60, // 5 years typical
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute,
                IsActive = true
            },
            new()
            {
                LicenceTypeId = ImportPermitTypeId,
                Name = "Import Permit (Opium Act)",
                IssuingAuthority = "Farmatec/CIBG",
                TypicalValidityMonths = 12,
                PermittedActivities = LicenceTypes.PermittedActivity.Import,
                IsActive = true
            },
            new()
            {
                LicenceTypeId = ExportPermitTypeId,
                Name = "Export Permit (Opium Act)",
                IssuingAuthority = "Farmatec/CIBG",
                TypicalValidityMonths = 12,
                PermittedActivities = LicenceTypes.PermittedActivity.Export,
                IsActive = true
            },
            new()
            {
                LicenceTypeId = PharmacyLicenceTypeId,
                Name = "Pharmacy Licence",
                IssuingAuthority = "IGJ",
                TypicalValidityMonths = null, // Permanent
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute,
                IsActive = true
            },
            new()
            {
                LicenceTypeId = PrecursorRegistrationTypeId,
                Name = "Precursor Registration",
                IssuingAuthority = "Farmatec/CIBG",
                TypicalValidityMonths = 36,
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.HandlePrecursors,
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Gets sample controlled substances per Dutch Opium Act.
    /// </summary>
    public static IEnumerable<ControlledSubstance> GetControlledSubstances()
    {
        return new List<ControlledSubstance>
        {
            // Opium Act List I (high-risk narcotics)
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                SubstanceName = "Morphine",
                InternalCode = "MORPH-001",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                IsActive = true
            },
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                SubstanceName = "Fentanyl",
                InternalCode = "FENT-001",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                IsActive = true
            },
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                SubstanceName = "Oxycodone",
                InternalCode = "OXY-001",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                IsActive = true
            },
            // Opium Act List II (moderate-risk narcotics)
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000010"),
                SubstanceName = "Codeine",
                InternalCode = "COD-001",
                OpiumActList = SubstanceCategories.OpiumActList.ListII,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                IsActive = true
            },
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000011"),
                SubstanceName = "Diazepam",
                InternalCode = "DIAZ-001",
                OpiumActList = SubstanceCategories.OpiumActList.ListII,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                IsActive = true
            },
            // Precursors
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000020"),
                SubstanceName = "Ephedrine",
                InternalCode = "EPH-001",
                OpiumActList = SubstanceCategories.OpiumActList.None,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
                IsActive = true
            },
            new()
            {
                SubstanceId = Guid.Parse("20000000-0000-0000-0000-000000000021"),
                SubstanceName = "Pseudoephedrine",
                InternalCode = "PSE-001",
                OpiumActList = SubstanceCategories.OpiumActList.None,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Gets sample licences for both company and customers.
    /// </summary>
    public static IEnumerable<Licence> GetLicences()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new List<Licence>
        {
            // Company (wholesaler) licences
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                LicenceNumber = "WDA-NL-2024-001",
                LicenceTypeId = WholesaleLicenceTypeId,
                HolderType = "Company",
                HolderId = CompanyHolderId,
                IssuingAuthority = "IGJ",
                IssueDate = today.AddYears(-2),
                ExpiryDate = null, // No expiry
                Status = "Valid",
                Scope = "Wholesale distribution of pharmaceutical products in Netherlands",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute
            },
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                LicenceNumber = "OPW-NL-2023-4521",
                LicenceTypeId = OpiumExemptionTypeId,
                HolderType = "Company",
                HolderId = CompanyHolderId,
                IssuingAuthority = "Farmatec/CIBG",
                IssueDate = today.AddYears(-1),
                ExpiryDate = today.AddYears(4), // 5 year validity
                Status = "Valid",
                Scope = "Opium Act List I and II substances",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute
            },
            // Licence expiring soon (within 60 days) - for testing expiry alerts
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                LicenceNumber = "IMP-NL-2024-789",
                LicenceTypeId = ImportPermitTypeId,
                HolderType = "Company",
                HolderId = CompanyHolderId,
                IssuingAuthority = "Farmatec/CIBG",
                IssueDate = today.AddMonths(-10),
                ExpiryDate = today.AddDays(45), // Expiring in 45 days
                Status = "Valid",
                Scope = "Import of controlled substances from Germany",
                PermittedActivities = LicenceTypes.PermittedActivity.Import
            },
            // Customer licences - Hospital
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000010"),
                LicenceNumber = "PHARM-AMC-2022-001",
                LicenceTypeId = PharmacyLicenceTypeId,
                HolderType = "Customer",
                HolderId = CustomerHospitalId,
                IssuingAuthority = "IGJ",
                IssueDate = today.AddYears(-3),
                ExpiryDate = null,
                Status = "Valid",
                Scope = "Hospital pharmacy - Amsterdam Medical Center",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute
            },
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000011"),
                LicenceNumber = "OPW-AMC-2023-112",
                LicenceTypeId = OpiumExemptionTypeId,
                HolderType = "Customer",
                HolderId = CustomerHospitalId,
                IssuingAuthority = "Farmatec/CIBG",
                IssueDate = today.AddYears(-1),
                ExpiryDate = today.AddYears(4),
                Status = "Valid",
                Scope = "Opium Act List I and II for hospital use",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store
            },
            // Customer licences - Community Pharmacy
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000020"),
                LicenceNumber = "PHARM-UTR-2021-045",
                LicenceTypeId = PharmacyLicenceTypeId,
                HolderType = "Customer",
                HolderId = CustomerPharmacyId,
                IssuingAuthority = "IGJ",
                IssueDate = today.AddYears(-4),
                ExpiryDate = null,
                Status = "Valid",
                Scope = "Community pharmacy - Utrecht Central",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.Distribute
            },
            // Expired licence - for testing expired status
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000030"),
                LicenceNumber = "OPW-VET-2020-033",
                LicenceTypeId = OpiumExemptionTypeId,
                HolderType = "Customer",
                HolderId = CustomerVeterinarianId,
                IssuingAuthority = "Farmatec/CIBG",
                IssueDate = today.AddYears(-5),
                ExpiryDate = today.AddDays(-30), // Expired 30 days ago
                Status = "Expired",
                Scope = "Opium Act List II for veterinary use",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store
            },
            // Expiring very soon (30 days) - for urgent alert testing
            new()
            {
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000040"),
                LicenceNumber = "PREC-NL-2024-101",
                LicenceTypeId = PrecursorRegistrationTypeId,
                HolderType = "Company",
                HolderId = CompanyHolderId,
                IssuingAuthority = "Farmatec/CIBG",
                IssueDate = today.AddYears(-3),
                ExpiryDate = today.AddDays(25), // Expiring in 25 days
                Status = "Valid",
                Scope = "Category 1 precursors",
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                      LicenceTypes.PermittedActivity.Store |
                                      LicenceTypes.PermittedActivity.HandlePrecursors
            }
        };
    }
}
