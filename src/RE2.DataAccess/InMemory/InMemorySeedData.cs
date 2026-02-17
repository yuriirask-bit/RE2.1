using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// Provides seed data for in-memory repositories to enable local testing.
/// Contains realistic Dutch pharmaceutical licence management test data.
/// Extended for User Story 4 with customer and threshold data.
/// </summary>
public static class InMemorySeedData
{
    // Re-export well-known IDs for backward compatibility and convenience
    public static readonly Guid CompanyHolderId = WellKnownIds.CompanyHolderId;

    // ComplianceExtensionIds (repurposed from old CustomerId Guids for backward compatibility)
    public static readonly Guid CustomerHospitalComplianceExtensionId = WellKnownIds.CustomerHospitalId;
    public static readonly Guid CustomerPharmacyComplianceExtensionId = WellKnownIds.CustomerPharmacyId;
    public static readonly Guid CustomerVeterinarianComplianceExtensionId = WellKnownIds.CustomerVeterinarianId;
    public static readonly Guid CustomerSuspendedComplianceExtensionId = WellKnownIds.CustomerSuspendedId;

    public static readonly Guid WholesaleLicenceTypeId = WellKnownIds.WholesaleLicenceTypeId;
    public static readonly Guid OpiumExemptionTypeId = WellKnownIds.OpiumExemptionTypeId;
    public static readonly Guid ImportPermitTypeId = WellKnownIds.ImportPermitTypeId;
    public static readonly Guid ExportPermitTypeId = WellKnownIds.ExportPermitTypeId;
    public static readonly Guid PharmacyLicenceTypeId = WellKnownIds.PharmacyLicenceTypeId;
    public static readonly Guid PrecursorRegistrationTypeId = WellKnownIds.PrecursorRegistrationTypeId;

    // GDP Service Provider IDs for testing
    public static readonly Guid GdpProvider3PlId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    public static readonly Guid GdpProviderTransporterId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    public static readonly Guid GdpProviderWarehouseId = Guid.Parse("70000000-0000-0000-0000-000000000003");

    // GDP Credential IDs for testing
    public static readonly Guid GdpCredentialHospitalId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    public static readonly Guid GdpCredential3PlId = Guid.Parse("71000000-0000-0000-0000-000000000002");
    public static readonly Guid GdpCredentialTransporterId = Guid.Parse("71000000-0000-0000-0000-000000000003");

    // Threshold IDs for testing
    public static readonly Guid MorphineQuantityThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid FentanylQuantityThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    public static readonly Guid GlobalFrequencyThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000003");

    // GDP Inspection IDs for testing
    public static readonly Guid GdpInspectionIgjId = Guid.Parse("80000000-0000-0000-0000-000000000001");
    public static readonly Guid GdpInspectionInternalId = Guid.Parse("80000000-0000-0000-0000-000000000002");
    public static readonly Guid GdpInspectionSelfId = Guid.Parse("80000000-0000-0000-0000-000000000003");

    // GDP Inspection Finding IDs for testing
    public static readonly Guid GdpFindingCriticalId = Guid.Parse("81000000-0000-0000-0000-000000000001");
    public static readonly Guid GdpFindingMajorId = Guid.Parse("81000000-0000-0000-0000-000000000002");
    public static readonly Guid GdpFindingOtherId = Guid.Parse("81000000-0000-0000-0000-000000000003");

    // CAPA IDs for testing
    public static readonly Guid CapaOpenId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    public static readonly Guid CapaOverdueId = Guid.Parse("82000000-0000-0000-0000-000000000002");
    public static readonly Guid CapaCompletedId = Guid.Parse("82000000-0000-0000-0000-000000000003");

    // GDP Document IDs for testing
    public static readonly Guid GdpDocumentCredentialCertId = Guid.Parse("83000000-0000-0000-0000-000000000001");
    public static readonly Guid GdpDocumentInspectionReportId = Guid.Parse("83000000-0000-0000-0000-000000000002");
    public static readonly Guid GdpDocumentSiteWdaCopyId = Guid.Parse("83000000-0000-0000-0000-000000000003");

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 10 including GDP document data.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo,
        InMemoryGdpSiteRepository gdpSiteRepo,
        InMemoryProductRepository productRepo,
        InMemoryGdpCredentialRepository gdpCredentialRepo,
        InMemoryGdpInspectionRepository gdpInspectionRepo,
        InMemoryCapaRepository capaRepo,
        InMemoryGdpDocumentRepository gdpDocumentRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
        productRepo.Seed(GetProducts());
        SeedGdpData(gdpSiteRepo);
        SeedGdpCredentialData(gdpCredentialRepo);
        SeedGdpInspectionData(gdpInspectionRepo, capaRepo);
        SeedGdpDocumentData(gdpDocumentRepo);
    }

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 9 including GDP inspection and CAPA data.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo,
        InMemoryGdpSiteRepository gdpSiteRepo,
        InMemoryProductRepository productRepo,
        InMemoryGdpCredentialRepository gdpCredentialRepo,
        InMemoryGdpInspectionRepository gdpInspectionRepo,
        InMemoryCapaRepository capaRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
        productRepo.Seed(GetProducts());
        SeedGdpData(gdpSiteRepo);
        SeedGdpCredentialData(gdpCredentialRepo);
        SeedGdpInspectionData(gdpInspectionRepo, capaRepo);
    }

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 8 including GDP credential data.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo,
        InMemoryGdpSiteRepository gdpSiteRepo,
        InMemoryProductRepository productRepo,
        InMemoryGdpCredentialRepository gdpCredentialRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
        productRepo.Seed(GetProducts());
        SeedGdpData(gdpSiteRepo);
        SeedGdpCredentialData(gdpCredentialRepo);
    }

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 7 including GDP site data and product data.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo,
        InMemoryGdpSiteRepository gdpSiteRepo,
        InMemoryProductRepository productRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
        productRepo.Seed(GetProducts());
        SeedGdpData(gdpSiteRepo);
    }

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 7 including GDP site data.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo,
        InMemoryGdpSiteRepository gdpSiteRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
        SeedGdpData(gdpSiteRepo);
    }

    /// <summary>
    /// Seeds all repositories with test data.
    /// Extended overload for User Story 4 including customers and thresholds.
    /// </summary>
    public static void SeedAll(
        InMemoryLicenceTypeRepository licenceTypeRepo,
        InMemoryControlledSubstanceRepository substanceRepo,
        InMemoryLicenceRepository licenceRepo,
        InMemoryCustomerRepository customerRepo,
        InMemoryThresholdRepository thresholdRepo)
    {
        licenceTypeRepo.Seed(GetLicenceTypes());
        substanceRepo.Seed(GetControlledSubstances());
        licenceRepo.Seed(GetLicences());
        customerRepo.SeedD365Customers(GetD365Customers());
        customerRepo.SeedComplianceExtensions(GetCustomerComplianceExtensions());
        thresholdRepo.Seed(GetThresholds());
    }

    /// <summary>
    /// Seeds all repositories with test data for User Story 1.
    /// Legacy overload for backward compatibility.
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
    /// Keyed by SubstanceCode (string business key). No SubstanceId (Guid).
    /// </summary>
    public static IEnumerable<ControlledSubstance> GetControlledSubstances()
    {
        return new List<ControlledSubstance>
        {
            // Opium Act List I (high-risk narcotics)
            new()
            {
                SubstanceCode = "Morphine",
                SubstanceName = "Morphine",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                CreatedDate = DateTime.UtcNow.AddYears(-5),
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                SubstanceCode = "Fentanyl",
                SubstanceName = "Fentanyl",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                CreatedDate = DateTime.UtcNow.AddYears(-5),
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                SubstanceCode = "Oxycodone",
                SubstanceName = "Oxycodone",
                OpiumActList = SubstanceCategories.OpiumActList.ListI,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                CreatedDate = DateTime.UtcNow.AddYears(-5),
                ModifiedDate = DateTime.UtcNow
            },
            // Opium Act List II (moderate-risk narcotics)
            new()
            {
                SubstanceCode = "Codeine",
                SubstanceName = "Codeine",
                OpiumActList = SubstanceCategories.OpiumActList.ListII,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000010"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                CreatedDate = DateTime.UtcNow.AddYears(-5),
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                SubstanceCode = "Diazepam",
                SubstanceName = "Diazepam",
                OpiumActList = SubstanceCategories.OpiumActList.ListII,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000011"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
                CreatedDate = DateTime.UtcNow.AddYears(-5),
                ModifiedDate = DateTime.UtcNow
            },
            // Precursors
            new()
            {
                SubstanceCode = "Ephedrine",
                SubstanceName = "Ephedrine",
                OpiumActList = SubstanceCategories.OpiumActList.None,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000020"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
                CreatedDate = DateTime.UtcNow.AddYears(-3),
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                SubstanceCode = "Pseudoephedrine",
                SubstanceName = "Pseudoephedrine",
                OpiumActList = SubstanceCategories.OpiumActList.None,
                PrecursorCategory = SubstanceCategories.PrecursorCategory.Category1,
                ComplianceExtensionId = Guid.Parse("20000000-0000-0000-0000-000000000021"),
                IsActive = true,
                ClassificationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
                CreatedDate = DateTime.UtcNow.AddYears(-3),
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Gets sample products from D365 F&O with resolved substance attributes.
    /// </summary>
    public static IEnumerable<Product> GetProducts()
    {
        return new List<Product>
        {
            new()
            {
                ItemNumber = "ITEM-MORPH-10MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-MORPH-10MG",
                ProductName = "Morphine Sulfate 10mg/ml",
                ProductDescription = "Morphine Sulfate solution for injection, 10mg/ml",
                SubstanceCode = "Morphine",
                OpiumActListValue = "ListI",
                PrecursorCategoryValue = null
            },
            new()
            {
                ItemNumber = "ITEM-FENT-50MCG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-FENT-50MCG",
                ProductName = "Fentanyl Citrate 50mcg/ml",
                ProductDescription = "Fentanyl Citrate solution for injection, 50mcg/ml",
                SubstanceCode = "Fentanyl",
                OpiumActListValue = "ListI",
                PrecursorCategoryValue = null
            },
            new()
            {
                ItemNumber = "ITEM-OXY-20MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-OXY-20MG",
                ProductName = "Oxycodone HCl 20mg",
                ProductDescription = "Oxycodone Hydrochloride tablets, 20mg",
                SubstanceCode = "Oxycodone",
                OpiumActListValue = "ListI",
                PrecursorCategoryValue = null
            },
            new()
            {
                ItemNumber = "ITEM-COD-30MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-COD-30MG",
                ProductName = "Codeine Phosphate 30mg",
                ProductDescription = "Codeine Phosphate tablets, 30mg",
                SubstanceCode = "Codeine",
                OpiumActListValue = "ListII",
                PrecursorCategoryValue = null
            },
            new()
            {
                ItemNumber = "ITEM-DIAZ-5MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-DIAZ-5MG",
                ProductName = "Diazepam 5mg",
                ProductDescription = "Diazepam tablets, 5mg",
                SubstanceCode = "Diazepam",
                OpiumActListValue = "ListII",
                PrecursorCategoryValue = null
            },
            new()
            {
                ItemNumber = "ITEM-EPH-25MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-EPH-25MG",
                ProductName = "Ephedrine HCl 25mg",
                ProductDescription = "Ephedrine Hydrochloride tablets, 25mg",
                SubstanceCode = "Ephedrine",
                OpiumActListValue = null,
                PrecursorCategoryValue = "Category1"
            },
            new()
            {
                ItemNumber = "ITEM-PSE-60MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-PSE-60MG",
                ProductName = "Pseudoephedrine 60mg",
                ProductDescription = "Pseudoephedrine tablets, 60mg",
                SubstanceCode = "Pseudoephedrine",
                OpiumActListValue = null,
                PrecursorCategoryValue = "Category1"
            },
            // Non-controlled product
            new()
            {
                ItemNumber = "ITEM-PARA-500MG",
                DataAreaId = "nlpd",
                ProductNumber = "PROD-PARA-500MG",
                ProductName = "Paracetamol 500mg",
                ProductDescription = "Paracetamol tablets, 500mg",
                SubstanceCode = null,
                OpiumActListValue = null,
                PrecursorCategoryValue = null
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
                HolderId = CustomerHospitalComplianceExtensionId,
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
                HolderId = CustomerHospitalComplianceExtensionId,
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
                HolderId = CustomerPharmacyComplianceExtensionId,
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
                HolderId = CustomerVeterinarianComplianceExtensionId,
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

    /// <summary>
    /// Gets mock D365FO customer master data (read-only fields).
    /// Contains CustomerAccount, DataAreaId, OrganizationName, and AddressCountryRegionId.
    /// </summary>
    public static IEnumerable<Customer> GetD365Customers()
    {
        return new List<Customer>
        {
            // Hospital
            new()
            {
                CustomerAccount = "CUST-001",
                DataAreaId = "nlpd",
                OrganizationName = "Amsterdam Medical Center",
                AddressCountryRegionId = "NL"
            },
            // Community Pharmacy
            new()
            {
                CustomerAccount = "CUST-002",
                DataAreaId = "nlpd",
                OrganizationName = "Utrecht Central Pharmacy",
                AddressCountryRegionId = "NL"
            },
            // Veterinary Clinic
            new()
            {
                CustomerAccount = "CUST-003",
                DataAreaId = "nlpd",
                OrganizationName = "Rotterdam Veterinary Clinic",
                AddressCountryRegionId = "NL"
            },
            // Suspended Wholesale
            new()
            {
                CustomerAccount = "CUST-004",
                DataAreaId = "nlpd",
                OrganizationName = "Suspended Wholesale BV",
                AddressCountryRegionId = "NL"
            }
        };
    }

    /// <summary>
    /// Gets compliance extension data for customers (Dataverse-side).
    /// Contains ComplianceExtensionId, BusinessCategory, ApprovalStatus, etc.
    /// CustomerAccount/DataAreaId must match D365FO customers for merging.
    /// </summary>
    public static IEnumerable<Customer> GetCustomerComplianceExtensions()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new List<Customer>
        {
            // Hospital - approved, GDP qualified
            new()
            {
                CustomerAccount = "CUST-001",
                DataAreaId = "nlpd",
                ComplianceExtensionId = CustomerHospitalComplianceExtensionId,
                BusinessCategory = BusinessCategory.HospitalPharmacy,
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.Approved,
                OnboardingDate = today.AddYears(-2),
                NextReVerificationDate = today.AddMonths(10),
                IsSuspended = false,
                CreatedDate = DateTime.UtcNow.AddYears(-2),
                ModifiedDate = DateTime.UtcNow
            },
            // Community Pharmacy - approved, GDP qualified
            new()
            {
                CustomerAccount = "CUST-002",
                DataAreaId = "nlpd",
                ComplianceExtensionId = CustomerPharmacyComplianceExtensionId,
                BusinessCategory = BusinessCategory.CommunityPharmacy,
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.Approved,
                OnboardingDate = today.AddYears(-3),
                NextReVerificationDate = today.AddMonths(6),
                IsSuspended = false,
                CreatedDate = DateTime.UtcNow.AddYears(-3),
                ModifiedDate = DateTime.UtcNow
            },
            // Veterinarian - approved but GDP not required
            new()
            {
                CustomerAccount = "CUST-003",
                DataAreaId = "nlpd",
                ComplianceExtensionId = CustomerVeterinarianComplianceExtensionId,
                BusinessCategory = BusinessCategory.Veterinarian,
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.NotRequired,
                OnboardingDate = today.AddYears(-1),
                NextReVerificationDate = today.AddMonths(11),
                IsSuspended = false,
                CreatedDate = DateTime.UtcNow.AddYears(-1),
                ModifiedDate = DateTime.UtcNow
            },
            // Suspended customer - for testing blocked transactions
            new()
            {
                CustomerAccount = "CUST-004",
                DataAreaId = "nlpd",
                ComplianceExtensionId = CustomerSuspendedComplianceExtensionId,
                BusinessCategory = BusinessCategory.WholesalerEU,
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.Approved,
                OnboardingDate = today.AddYears(-4),
                NextReVerificationDate = today.AddMonths(-1), // Overdue
                IsSuspended = true,
                SuspensionReason = "Compliance audit findings - pending remediation",
                CreatedDate = DateTime.UtcNow.AddYears(-4),
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Gets sample thresholds for User Story 4 transaction validation testing.
    /// Per FR-020/FR-022: Quantity and frequency threshold configuration.
    /// Uses SubstanceCode (string) instead of SubstanceId (Guid).
    /// </summary>
    public static IEnumerable<Threshold> GetThresholds()
    {
        return new List<Threshold>
        {
            // Morphine quantity threshold - 1000g per month per customer
            new()
            {
                Id = MorphineQuantityThresholdId,
                Name = "Morphine Monthly Quantity Limit",
                Description = "Maximum monthly morphine quantity per customer (hospitals exempt)",
                ThresholdType = ThresholdType.Quantity,
                Period = ThresholdPeriod.Monthly,
                SubstanceCode = "Morphine",
                SubstanceName = "Morphine",
                LimitValue = 1000m,
                LimitUnit = "g",
                WarningThresholdPercent = 80m,
                AllowOverride = true,
                MaxOverridePercent = 150m, // Can override up to 150%
                IsActive = true,
                RegulatoryReference = "Opium Act Article 6",
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Fentanyl quantity threshold - 100g per month (stricter)
            new()
            {
                Id = FentanylQuantityThresholdId,
                Name = "Fentanyl Monthly Quantity Limit",
                Description = "Maximum monthly fentanyl quantity per customer - strict limit for List I high-potency",
                ThresholdType = ThresholdType.Quantity,
                Period = ThresholdPeriod.Monthly,
                SubstanceCode = "Fentanyl",
                SubstanceName = "Fentanyl",
                LimitValue = 100m,
                LimitUnit = "g",
                WarningThresholdPercent = 70m,
                AllowOverride = true,
                MaxOverridePercent = 120m, // Stricter override limit
                IsActive = true,
                RegulatoryReference = "Opium Act Article 6, INCB Guidelines",
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Global frequency threshold - max 10 transactions per day per customer
            new()
            {
                Id = GlobalFrequencyThresholdId,
                Name = "Daily Transaction Frequency Limit",
                Description = "Maximum daily transactions per customer for all controlled substances",
                ThresholdType = ThresholdType.Frequency,
                Period = ThresholdPeriod.Daily,
                SubstanceCode = null, // Applies to all substances
                LimitValue = 10m,
                LimitUnit = "count",
                WarningThresholdPercent = 80m,
                AllowOverride = true,
                MaxOverridePercent = 200m,
                IsActive = true,
                RegulatoryReference = "Internal Policy P-2024-001",
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Per-transaction quantity limit for pharmacies
            new()
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                Name = "Pharmacy Per-Transaction Oxycodone Limit",
                Description = "Maximum oxycodone per transaction for community pharmacies",
                ThresholdType = ThresholdType.Quantity,
                Period = ThresholdPeriod.PerTransaction,
                SubstanceCode = "Oxycodone",
                SubstanceName = "Oxycodone",
                CustomerCategory = BusinessCategory.CommunityPharmacy,
                LimitValue = 500m,
                LimitUnit = "g",
                WarningThresholdPercent = 90m,
                AllowOverride = true,
                MaxOverridePercent = 125m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-3)
            },
            // Precursor Category 1 annual limit
            new()
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                Name = "Ephedrine Annual Limit",
                Description = "Annual ephedrine quantity limit per EU precursor regulations",
                ThresholdType = ThresholdType.CumulativeQuantity,
                Period = ThresholdPeriod.Yearly,
                SubstanceCode = "Ephedrine",
                SubstanceName = "Ephedrine",
                OpiumActList = "Precursor Cat 1",
                LimitValue = 5000m,
                LimitUnit = "g",
                WarningThresholdPercent = 75m,
                AllowOverride = false, // Cannot override EU regulation
                IsActive = true,
                RegulatoryReference = "EU Regulation 273/2004",
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            }
        };
    }

    /// <summary>
    /// Seeds GDP site data for User Story 7 testing.
    /// Creates mock D365FO warehouses and GDP extensions.
    /// </summary>
    public static void SeedGdpData(InMemoryGdpSiteRepository gdpSiteRepo)
    {
        // Mock D365FO warehouses
        var warehouses = new List<GdpSite>
        {
            new()
            {
                WarehouseId = "WH-AMS-01",
                WarehouseName = "Amsterdam Central Warehouse",
                OperationalSiteId = "SITE-AMS",
                OperationalSiteName = "Amsterdam Operations",
                DataAreaId = "nlpd",
                WarehouseType = "Standard",
                Street = "Pharmaweg",
                StreetNumber = "42",
                City = "Amsterdam",
                ZipCode = "1012 AB",
                CountryRegionId = "NL",
                StateId = "NH",
                FormattedAddress = "Pharmaweg 42, 1012 AB Amsterdam, Netherlands"
            },
            new()
            {
                WarehouseId = "WH-RTD-01",
                WarehouseName = "Rotterdam Distribution Hub",
                OperationalSiteId = "SITE-RTD",
                OperationalSiteName = "Rotterdam Operations",
                DataAreaId = "nlpd",
                WarehouseType = "Standard",
                Street = "Havenstraat",
                StreetNumber = "15",
                City = "Rotterdam",
                ZipCode = "3011 AA",
                CountryRegionId = "NL",
                StateId = "ZH",
                FormattedAddress = "Havenstraat 15, 3011 AA Rotterdam, Netherlands"
            },
            new()
            {
                WarehouseId = "WH-UTR-01",
                WarehouseName = "Utrecht Cold Storage",
                OperationalSiteId = "SITE-UTR",
                OperationalSiteName = "Utrecht Operations",
                DataAreaId = "nlpd",
                WarehouseType = "Standard",
                Street = "Koelweg",
                StreetNumber = "8",
                City = "Utrecht",
                ZipCode = "3500 AA",
                CountryRegionId = "NL",
                StateId = "UT",
                FormattedAddress = "Koelweg 8, 3500 AA Utrecht, Netherlands"
            },
            new()
            {
                WarehouseId = "WH-QRN-01",
                WarehouseName = "Amsterdam Quarantine Area",
                OperationalSiteId = "SITE-AMS",
                OperationalSiteName = "Amsterdam Operations",
                DataAreaId = "nlpd",
                WarehouseType = "Quarantine",
                Street = "Pharmaweg",
                StreetNumber = "42B",
                City = "Amsterdam",
                ZipCode = "1012 AB",
                CountryRegionId = "NL",
                StateId = "NH",
                FormattedAddress = "Pharmaweg 42B, 1012 AB Amsterdam, Netherlands"
            },
            new()
            {
                WarehouseId = "WH-TRN-01",
                WarehouseName = "Schiphol Transit Hub",
                OperationalSiteId = "SITE-AMS",
                OperationalSiteName = "Amsterdam Operations",
                DataAreaId = "nlpd",
                WarehouseType = "Transit",
                Street = "Schipholweg",
                StreetNumber = "1",
                City = "Schiphol",
                ZipCode = "1118 AA",
                CountryRegionId = "NL",
                StateId = "NH",
                FormattedAddress = "Schipholweg 1, 1118 AA Schiphol, Netherlands"
            }
        };

        gdpSiteRepo.SeedWarehouses(warehouses);

        // GDP extensions for some warehouses
        var gdpExtensions = new List<GdpSite>
        {
            new()
            {
                WarehouseId = "WH-AMS-01",
                DataAreaId = "nlpd",
                GdpExtensionId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                GdpSiteType = GdpSiteType.Warehouse,
                PermittedActivities = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled,
                IsGdpActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-6),
                ModifiedDate = DateTime.UtcNow.AddMonths(-1)
            },
            new()
            {
                WarehouseId = "WH-RTD-01",
                DataAreaId = "nlpd",
                GdpExtensionId = Guid.Parse("50000000-0000-0000-0000-000000000002"),
                GdpSiteType = GdpSiteType.CrossDock,
                PermittedActivities = GdpSiteActivity.TransportOnly,
                IsGdpActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-1)
            },
            new()
            {
                WarehouseId = "WH-TRN-01",
                DataAreaId = "nlpd",
                GdpExtensionId = Guid.Parse("50000000-0000-0000-0000-000000000003"),
                GdpSiteType = GdpSiteType.TransportHub,
                PermittedActivities = GdpSiteActivity.TransportOnly,
                IsGdpActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-2),
                ModifiedDate = DateTime.UtcNow
            }
        };

        gdpSiteRepo.SeedGdpExtensions(gdpExtensions);

        // WDA coverage for Amsterdam warehouse (linked to the company WDA licence)
        var wdaCoverages = new List<GdpSiteWdaCoverage>
        {
            new()
            {
                CoverageId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                WarehouseId = "WH-AMS-01",
                DataAreaId = "nlpd",
                LicenceId = Guid.Parse("30000000-0000-0000-0000-000000000001"), // Company WDA licence
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
                ExpiryDate = null // No expiry
            }
        };

        gdpSiteRepo.SeedWdaCoverages(wdaCoverages);
    }

    /// <summary>
    /// Seeds GDP credential data for User Story 8 testing.
    /// Creates GDP service providers, credentials, reviews, and verifications.
    /// </summary>
    public static void SeedGdpCredentialData(InMemoryGdpCredentialRepository gdpCredentialRepo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // GDP Service Providers
        var providers = new List<GdpServiceProvider>
        {
            new()
            {
                ProviderId = GdpProvider3PlId,
                ProviderName = "MedLogistics NL B.V.",
                ServiceType = GdpServiceType.ThirdPartyLogistics,
                TemperatureControlledCapability = true,
                ApprovedRoutes = "Amsterdam-Rotterdam, Amsterdam-Utrecht, Rotterdam-Eindhoven",
                QualificationStatus = GdpQualificationStatus.Approved,
                ReviewFrequencyMonths = 24,
                LastReviewDate = today.AddMonths(-6),
                NextReviewDate = today.AddMonths(18),
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-2),
                ModifiedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                ProviderId = GdpProviderTransporterId,
                ProviderName = "PharmaTransport EU GmbH",
                ServiceType = GdpServiceType.Transporter,
                TemperatureControlledCapability = true,
                ApprovedRoutes = "NL-DE, NL-BE, NL-FR",
                QualificationStatus = GdpQualificationStatus.Approved,
                ReviewFrequencyMonths = 36,
                LastReviewDate = today.AddMonths(-12),
                NextReviewDate = today.AddMonths(24),
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-12)
            },
            new()
            {
                ProviderId = GdpProviderWarehouseId,
                ProviderName = "ColdStore Benelux B.V.",
                ServiceType = GdpServiceType.ExternalWarehouse,
                TemperatureControlledCapability = true,
                QualificationStatus = GdpQualificationStatus.UnderReview,
                ReviewFrequencyMonths = 24,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                ModifiedDate = DateTime.UtcNow
            }
        };

        gdpCredentialRepo.SeedProviders(providers);

        // GDP Credentials
        var credentials = new List<GdpCredential>
        {
            // Hospital customer credential
            new()
            {
                CredentialId = GdpCredentialHospitalId,
                EntityType = GdpCredentialEntityType.Supplier,
                EntityId = CustomerHospitalComplianceExtensionId,
                WdaNumber = "WDA-NL-2023-H001",
                GdpCertificateNumber = "GDP-NL-AMC-2023",
                EudraGmdpEntryUrl = "https://eudragmdp.ema.europa.eu/inspections/view/gdp/NL-AMC-2023",
                ValidityStartDate = today.AddYears(-2),
                ValidityEndDate = today.AddYears(3),
                QualificationStatus = GdpQualificationStatus.Approved,
                LastVerificationDate = today.AddMonths(-3),
                NextReviewDate = today.AddMonths(9),
                CreatedDate = DateTime.UtcNow.AddYears(-2),
                ModifiedDate = DateTime.UtcNow.AddMonths(-3)
            },
            // 3PL provider credential
            new()
            {
                CredentialId = GdpCredential3PlId,
                EntityType = GdpCredentialEntityType.ServiceProvider,
                EntityId = GdpProvider3PlId,
                WdaNumber = "WDA-NL-2022-3PL001",
                GdpCertificateNumber = "GDP-NL-ML-2022",
                EudraGmdpEntryUrl = "https://eudragmdp.ema.europa.eu/inspections/view/gdp/NL-ML-2022",
                ValidityStartDate = today.AddYears(-3),
                ValidityEndDate = today.AddYears(2),
                QualificationStatus = GdpQualificationStatus.Approved,
                LastVerificationDate = today.AddMonths(-6),
                NextReviewDate = today.AddMonths(18),
                CreatedDate = DateTime.UtcNow.AddYears(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Transporter credential
            new()
            {
                CredentialId = GdpCredentialTransporterId,
                EntityType = GdpCredentialEntityType.ServiceProvider,
                EntityId = GdpProviderTransporterId,
                GdpCertificateNumber = "GDP-DE-PT-2021",
                EudraGmdpEntryUrl = "https://eudragmdp.ema.europa.eu/inspections/view/gdp/DE-PT-2021",
                ValidityStartDate = today.AddYears(-4),
                ValidityEndDate = today.AddYears(1),
                QualificationStatus = GdpQualificationStatus.Approved,
                LastVerificationDate = today.AddMonths(-12),
                NextReviewDate = today.AddMonths(24),
                CreatedDate = DateTime.UtcNow.AddYears(-4),
                ModifiedDate = DateTime.UtcNow.AddMonths(-12)
            }
        };

        gdpCredentialRepo.SeedCredentials(credentials);

        // Qualification Reviews
        var reviews = new List<QualificationReview>
        {
            QualificationReview.CreateForServiceProvider(
                GdpProvider3PlId,
                today.AddMonths(-6),
                ReviewMethod.OnSiteAudit,
                ReviewOutcome.Approved,
                "Jan de Vries",
                "Full GDP audit passed. Temperature monitoring systems verified."),
            QualificationReview.CreateForServiceProvider(
                GdpProviderTransporterId,
                today.AddMonths(-12),
                ReviewMethod.DocumentReview,
                ReviewOutcome.Approved,
                "Marie Jansen",
                "GDP certificate and transport qualification documents reviewed and approved.")
        };

        // Set next review dates
        reviews[0].SetNextReviewDate(24);
        reviews[1].SetNextReviewDate(36);

        gdpCredentialRepo.SeedReviews(reviews);

        // Credential Verifications
        var verifications = new List<GdpCredentialVerification>
        {
            new()
            {
                VerificationId = Guid.Parse("72000000-0000-0000-0000-000000000001"),
                CredentialId = GdpCredential3PlId,
                VerificationDate = today.AddMonths(-6),
                VerificationMethod = GdpVerificationMethod.EudraGMDP,
                VerifiedBy = "Jan de Vries",
                Outcome = GdpVerificationOutcome.Valid,
                Notes = "EudraGMDP entry confirmed valid. GDP certificate active."
            },
            new()
            {
                VerificationId = Guid.Parse("72000000-0000-0000-0000-000000000002"),
                CredentialId = GdpCredentialTransporterId,
                VerificationDate = today.AddMonths(-12),
                VerificationMethod = GdpVerificationMethod.NationalDatabase,
                VerifiedBy = "Marie Jansen",
                Outcome = GdpVerificationOutcome.Valid,
                Notes = "German national database confirms valid GDP certification."
            }
        };

        gdpCredentialRepo.SeedVerifications(verifications);
    }

    /// <summary>
    /// Seeds GDP inspection, finding, and CAPA data for User Story 9.
    /// </summary>
    public static void SeedGdpInspectionData(
        InMemoryGdpInspectionRepository gdpInspectionRepo,
        InMemoryCapaRepository capaRepo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var amsterdamSiteId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var rotterdamSiteId = Guid.Parse("50000000-0000-0000-0000-000000000002");

        // GDP Inspections
        var inspections = new List<GdpInspection>
        {
            new()
            {
                InspectionId = GdpInspectionIgjId,
                InspectionDate = today.AddMonths(-3),
                InspectorName = "IGJ - Inspectie Gezondheidszorg en Jeugd",
                InspectionType = GdpInspectionType.RegulatoryAuthority,
                SiteId = amsterdamSiteId,
                FindingsSummary = "Routine GDP inspection. Two findings identified: temperature deviation in cold storage zone B and incomplete batch documentation.",
                ReportReferenceUrl = "https://igj.nl/inspections/2025/AMS-GDP-001",
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-3)
            },
            new()
            {
                InspectionId = GdpInspectionInternalId,
                InspectionDate = today.AddMonths(-1),
                InspectorName = "Internal QA - Dr. van der Berg",
                InspectionType = GdpInspectionType.Internal,
                SiteId = rotterdamSiteId,
                FindingsSummary = "Internal audit of cross-dock operations. One minor observation on documentation practices.",
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                ModifiedDate = DateTime.UtcNow.AddMonths(-1)
            },
            new()
            {
                InspectionId = GdpInspectionSelfId,
                InspectionDate = today.AddMonths(-6),
                InspectorName = "Self-Inspection Team",
                InspectionType = GdpInspectionType.SelfInspection,
                SiteId = amsterdamSiteId,
                FindingsSummary = "Annual self-inspection. No critical or major findings. All GDP procedures followed correctly.",
                CreatedDate = DateTime.UtcNow.AddMonths(-6),
                ModifiedDate = DateTime.UtcNow.AddMonths(-6)
            }
        };

        gdpInspectionRepo.SeedInspections(inspections);

        // GDP Inspection Findings
        var findings = new List<GdpInspectionFinding>
        {
            new()
            {
                FindingId = GdpFindingCriticalId,
                InspectionId = GdpInspectionIgjId,
                FindingDescription = "Temperature excursion detected in cold storage zone B. Temperature recorded at 12°C for 4 hours, exceeding the 2-8°C range for stored medicinal products.",
                Classification = FindingClassification.Critical,
                FindingNumber = "IGJ-2025-AMS-F001"
            },
            new()
            {
                FindingId = GdpFindingMajorId,
                InspectionId = GdpInspectionIgjId,
                FindingDescription = "Batch documentation for shipment SH-2025-0142 incomplete. Missing Responsible Person sign-off on temperature monitoring records.",
                Classification = FindingClassification.Major,
                FindingNumber = "IGJ-2025-AMS-F002"
            },
            new()
            {
                FindingId = GdpFindingOtherId,
                InspectionId = GdpInspectionInternalId,
                FindingDescription = "Cross-dock receiving log entries occasionally lack timestamp precision (minutes omitted).",
                Classification = FindingClassification.Other,
                FindingNumber = "INT-2025-RTD-F001"
            }
        };

        gdpInspectionRepo.SeedFindings(findings);

        // CAPAs linked to findings
        var capas = new List<Capa>
        {
            new()
            {
                CapaId = CapaOpenId,
                CapaNumber = "CAPA-2025-001",
                FindingId = GdpFindingCriticalId,
                Description = "Install redundant temperature monitoring system in cold storage zone B. Implement automatic alert escalation when temperature exceeds threshold for more than 30 minutes.",
                OwnerName = "Jan de Vries",
                DueDate = today.AddMonths(1),
                Status = CapaStatus.Open,
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-2)
            },
            new()
            {
                CapaId = CapaOverdueId,
                CapaNumber = "CAPA-2025-002",
                FindingId = GdpFindingMajorId,
                Description = "Update batch documentation procedure to include mandatory Responsible Person sign-off checklist. Retrain warehouse staff on documentation requirements.",
                OwnerName = "Marie Jansen",
                DueDate = today.AddMonths(-1),
                Status = CapaStatus.Open,
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                ModifiedDate = DateTime.UtcNow.AddMonths(-2)
            },
            new()
            {
                CapaId = CapaCompletedId,
                CapaNumber = "CAPA-2025-003",
                FindingId = GdpFindingOtherId,
                Description = "Update cross-dock receiving log template to include mandatory timestamp fields with HH:MM format. Train receiving staff on correct timestamp entry.",
                OwnerName = "Pieter Bakker",
                DueDate = today.AddMonths(-1),
                CompletionDate = today.AddDays(-20),
                Status = CapaStatus.Completed,
                VerificationNotes = "New log template implemented and verified. Staff training completed on 2025-12-15. Random audit of 50 entries showed 100% compliance.",
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                ModifiedDate = DateTime.UtcNow.AddDays(-20)
            }
        };

        capaRepo.SeedCapas(capas);
    }

    /// <summary>
    /// Seeds GDP document data for User Story 10 testing.
    /// Creates sample documents attached to existing credentials, inspections, and sites.
    /// </summary>
    public static void SeedGdpDocumentData(InMemoryGdpDocumentRepository gdpDocumentRepo)
    {
        var documents = new List<GdpDocument>
        {
            // GDP certificate attached to 3PL provider credential
            new()
            {
                DocumentId = GdpDocumentCredentialCertId,
                OwnerEntityType = GdpDocumentEntityType.Credential,
                OwnerEntityId = GdpCredential3PlId,
                DocumentType = DocumentType.Certificate,
                FileName = "gdp-certificate-medlogistics-2022.pdf",
                BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/credential/" + GdpCredential3PlId + "/" + GdpDocumentCredentialCertId + "/gdp-certificate-medlogistics-2022.pdf",
                UploadedDate = DateTime.UtcNow.AddMonths(-6),
                UploadedBy = "Jan de Vries",
                ContentType = "application/pdf",
                FileSizeBytes = 245_000,
                Description = "GDP certificate for MedLogistics NL B.V. — issued 2022, valid until 2027."
            },
            // Inspection report attached to IGJ inspection
            new()
            {
                DocumentId = GdpDocumentInspectionReportId,
                OwnerEntityType = GdpDocumentEntityType.Inspection,
                OwnerEntityId = GdpInspectionIgjId,
                DocumentType = DocumentType.InspectionReport,
                FileName = "igj-inspection-report-ams-2025.pdf",
                BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/inspection/" + GdpInspectionIgjId + "/" + GdpDocumentInspectionReportId + "/igj-inspection-report-ams-2025.pdf",
                UploadedDate = DateTime.UtcNow.AddMonths(-3),
                UploadedBy = "Marie Jansen",
                ContentType = "application/pdf",
                FileSizeBytes = 1_250_000,
                Description = "IGJ GDP inspection report for Amsterdam Central Warehouse — 2 findings identified."
            },
            // WDA copy attached to Amsterdam site
            new()
            {
                DocumentId = GdpDocumentSiteWdaCopyId,
                OwnerEntityType = GdpDocumentEntityType.Site,
                OwnerEntityId = Guid.Parse("50000000-0000-0000-0000-000000000001"), // Amsterdam GDP extension
                DocumentType = DocumentType.Letter,
                FileName = "wda-licence-copy-amsterdam.pdf",
                BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/site/50000000-0000-0000-0000-000000000001/" + GdpDocumentSiteWdaCopyId + "/wda-licence-copy-amsterdam.pdf",
                UploadedDate = DateTime.UtcNow.AddMonths(-12),
                UploadedBy = "Pieter Bakker",
                ContentType = "application/pdf",
                FileSizeBytes = 180_000,
                Description = "Copy of WDA licence covering Amsterdam Central Warehouse."
            }
        };

        gdpDocumentRepo.SeedDocuments(documents);
    }
}
