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
    public static readonly Guid CustomerHospitalId = WellKnownIds.CustomerHospitalId;
    public static readonly Guid CustomerPharmacyId = WellKnownIds.CustomerPharmacyId;
    public static readonly Guid CustomerVeterinarianId = WellKnownIds.CustomerVeterinarianId;
    public static readonly Guid CustomerSuspendedId = WellKnownIds.CustomerSuspendedId;

    public static readonly Guid WholesaleLicenceTypeId = WellKnownIds.WholesaleLicenceTypeId;
    public static readonly Guid OpiumExemptionTypeId = WellKnownIds.OpiumExemptionTypeId;
    public static readonly Guid ImportPermitTypeId = WellKnownIds.ImportPermitTypeId;
    public static readonly Guid ExportPermitTypeId = WellKnownIds.ExportPermitTypeId;
    public static readonly Guid PharmacyLicenceTypeId = WellKnownIds.PharmacyLicenceTypeId;
    public static readonly Guid PrecursorRegistrationTypeId = WellKnownIds.PrecursorRegistrationTypeId;

    // Substance IDs for testing
    public static readonly Guid MorphineId = WellKnownIds.MorphineId;
    public static readonly Guid FentanylId = WellKnownIds.FentanylId;
    public static readonly Guid OxycodoneId = WellKnownIds.OxycodoneId;
    public static readonly Guid CodeineId = WellKnownIds.CodeineId;
    public static readonly Guid DiazepamId = WellKnownIds.DiazepamId;
    public static readonly Guid EphedrineId = WellKnownIds.EphedrineId;

    // Threshold IDs for testing
    public static readonly Guid MorphineQuantityThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid FentanylQuantityThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    public static readonly Guid GlobalFrequencyThresholdId = Guid.Parse("40000000-0000-0000-0000-000000000003");

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
        customerRepo.Seed(GetCustomers());
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
        customerRepo.Seed(GetCustomers());
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

    /// <summary>
    /// Gets sample customers for User Story 4 transaction validation testing.
    /// </summary>
    public static IEnumerable<Customer> GetCustomers()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new List<Customer>
        {
            // Hospital - approved, GDP qualified
            new()
            {
                CustomerId = CustomerHospitalId,
                BusinessName = "Amsterdam Medical Center",
                RegistrationNumber = "NL-AMC-12345",
                BusinessCategory = BusinessCategory.HospitalPharmacy,
                Country = "NL",
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
                CustomerId = CustomerPharmacyId,
                BusinessName = "Utrecht Central Pharmacy",
                RegistrationNumber = "NL-UCP-67890",
                BusinessCategory = BusinessCategory.CommunityPharmacy,
                Country = "NL",
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
                CustomerId = CustomerVeterinarianId,
                BusinessName = "Rotterdam Veterinary Clinic",
                RegistrationNumber = "NL-RVC-11111",
                BusinessCategory = BusinessCategory.Veterinarian,
                Country = "NL",
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
                CustomerId = CustomerSuspendedId,
                BusinessName = "Suspended Wholesale BV",
                RegistrationNumber = "NL-SWB-99999",
                BusinessCategory = BusinessCategory.WholesalerEU,
                Country = "NL",
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
                SubstanceId = MorphineId,
                SubstanceCode = "MORPH-001",
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
                SubstanceId = FentanylId,
                SubstanceCode = "FENT-001",
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
                SubstanceId = null, // Applies to all substances
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
                SubstanceId = OxycodoneId,
                SubstanceCode = "OXY-001",
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
                SubstanceId = EphedrineId,
                SubstanceCode = "EPH-001",
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
}
