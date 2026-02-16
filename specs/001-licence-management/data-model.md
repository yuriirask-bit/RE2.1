# Data Model: Controlled Drug Licence & GDP Compliance Management System

**Feature**: 001-licence-management
**Created**: 2026-01-09
**Storage**: Dataverse virtual tables and D365 F&O virtual data entities (OData v4)

## Overview

This data model defines the domain entities for the compliance management system. All entities are stored in external systems (Dataverse or D365 F&O) accessed via API calls. No local database storage.

**Storage Strategy**:
- **Dataverse**: Compliance configuration and extensions (licence records, customer compliance extensions, GDP warehouse extensions, partners, qualifications, inspections, CAPAs, training, documentation)
- **D365 F&O (read-only master data)**: Customer master data (`CustomersV3`), warehouse master data (`Warehouses`), operational sites (`OperationalSitesV2`)
- **D365 F&O (transactional)**: Compliance validation requests/results, audit events, alerts/notifications
- **Composite Models**: Customer (D365 F&O `CustomersV3` + Dataverse `phr_customercomplianceextension`), GdpSite (D365 F&O `Warehouses` + Dataverse `phr_gdpwarehouseextension`)

## Core Entities

### 1. Licence

Represents a legal authorization (permit, exemption, certificate) held by the company or a customer.

**Attributes**:
- `LicenceId` (Guid, PK) - Unique identifier
- `LicenceNumber` (string, required, indexed) - Official licence/permit number from issuing authority
- `LicenceTypeId` (Guid, FK → LicenceType, required) - Category of authorization
- `LicenceType` (navigation property → LicenceType) - Related LicenceType entity providing `Name` and other type details
- `HolderType` (enum: Company, Customer, required) - Who holds this licence
- `HolderId` (Guid, FK → Company or Customer compliance extension, required) - Reference to holder entity. When `HolderType=Customer`, this is the `ComplianceExtensionId` (Guid) from `phr_customercomplianceextension`
- `IssuingAuthority` (string, required) - Name of authority (e.g., "IGJ", "Farmatec", "CBG-MEB")
- `IssueDate` (DateOnly, required) - Date licence was issued
- `ExpiryDate` (DateOnly, nullable) - Date licence expires (null = no expiry)
- `GracePeriodEndDate` (DateOnly, nullable) - End date of grace period allowing continued operation during licence renewal processing. When set and > Today, licence is treated as valid even if ExpiryDate has passed. Per Assumption 16: grace periods configured manually based on regulatory authority guidance.
- `Status` (enum: Valid, Expired, Suspended, Revoked, required) - Current status
- `Scope` (string, nullable) - Textual description of restrictions or conditions
- `PermittedActivities` (flags: Possess, Store, Distribute, Import, Export, Manufacture, HandlePrecursors) - What activities this licence allows
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Relationships**:
- Belongs to one `LicenceType` (many-to-one)
- Belongs to one `Company` or one `Customer` (polymorphic via HolderType/HolderId)
- Has many `LicenceSubstanceMapping` (one-to-many)
- Has many `LicenceDocument` (one-to-many)
- Has many `LicenceVerification` (one-to-many)
- Has many `LicenceScopeChange` (one-to-many)
- Referenced in many `TransactionLicenceUsage` (one-to-many)
- Generates many `Alert` (one-to-many)

**Storage**: Dataverse virtual table `phr_licence`

**Validation Rules**:
- `ExpiryDate` must be after `IssueDate` if specified
- `Status` automatically set to `Expired` when `ExpiryDate < Today`
- `LicenceNumber` + `IssuingAuthority` must be unique per `HolderId`

---

### 2. LicenceType

Represents a category of legal authorization with defined rules and requirements.

**Attributes**:
- `LicenceTypeId` (Guid, PK) - Unique identifier
- `Name` (string, required, unique) - Type name (e.g., "Wholesale Licence (WDA)", "Opium Act Exemption", "Import Permit")
- `IssuingAuthority` (string, required) - Typical authority for this type
- `TypicalValidityMonths` (int, nullable) - Standard validity period in months
- `PermittedActivities` (flags: Possess, Store, Distribute, Import, Export, Manufacture, HandlePrecursors) - Activities this type authorizes
- `IsActive` (bool, required, default: true) - Whether this type is still in use

**Relationships**:
- Has many `Licence` (one-to-many)
- Has many `CustomerRoleLicenceRequirement` (one-to-many) - Which customer roles require this licence type
- Has many `LicenceTypeSubstanceCategory` (one-to-many) - Which substance categories this type covers

**Storage**: Dataverse virtual table `phr_licencetype`

---

### 3. ControlledSubstance

Represents a regulated drug or precursor subject to compliance checks.

**Attributes**:
- `SubstanceId` (Guid, PK) - Unique identifier
- `SubstanceName` (string, required, indexed) - Common name
- `OpiumActList` (enum: None, ListI, ListII, nullable) - Dutch Opium Act classification
- `PrecursorCategory` (enum: None, Category1, Category2, Category3, nullable) - EU precursor regulation category
- `InternalCode` (string, required, unique, indexed) - Company's internal product/substance code
- `RegulatoryRestrictions` (string, nullable) - Additional restrictions or notes
- `IsActive` (bool, required, default: true) - Whether substance is still in use

**Relationships**:
- Has many `LicenceSubstanceMapping` (one-to-many) - Which licences authorize this substance
- Has many `TransactionLine` (one-to-many) - Transaction lines involving this substance
- Has many `Threshold` (one-to-many) - Monitoring thresholds per customer/substance
- Belongs to many `LicenceType` via `LicenceTypeSubstanceCategory` (many-to-many)

**Storage**: Dataverse virtual table `phr_controlledsubstance`

**Validation Rules**:
- At least one of `OpiumActList` or `PrecursorCategory` must be specified (not both None)

---

### 4. LicenceSubstanceMapping

Maps which substances a specific licence authorizes (e.g., "Opium Act exemption X covers substances A, B, C").

**Attributes**:
- `MappingId` (Guid, PK) - Unique identifier
- `LicenceId` (Guid, FK → Licence, required) - Which licence
- `SubstanceId` (Guid, FK → ControlledSubstance, required) - Which substance
- `EffectiveDate` (DateOnly, required) - When this authorization became effective
- `ExpiryDate` (DateOnly, nullable) - When authorization ends (null = same as licence expiry)

**Relationships**:
- Belongs to one `Licence` (many-to-one)
- Belongs to one `ControlledSubstance` (many-to-one)

**Storage**: Dataverse virtual table `phr_licencesubstancemapping`

**Validation Rules**:
- `LicenceId` + `SubstanceId` + `EffectiveDate` must be unique (allows historical changes)
- `ExpiryDate` must not exceed licence's `ExpiryDate`

---

### 5. Customer (Composite: D365 F&O + Dataverse)

Represents a trading partner qualified to purchase controlled drugs or provide services. Uses a composite data model: master data from D365 F&O `CustomersV3` OData entity, compliance extensions stored in Dataverse.

**Composite Key**: `CustomerAccount` (string) + `DataAreaId` (string)

**D365 F&O Master Data** (read-only, from `CustomersV3` OData entity):
- `CustomerAccount` (string, PK part 1) - D365 F&O customer account number
- `DataAreaId` (string, PK part 2) - D365 F&O legal entity / data area
- `OrganizationName` (string) - Legal entity name (aliased as `BusinessName`)
- `AddressCountryRegionId` (string) - ISO country code (aliased as `Country`)

**Dataverse Compliance Extension** (stored in `phr_customercomplianceextension`):
- `ComplianceExtensionId` (Guid, Dataverse PK) - Unique Dataverse record identifier
- `CustomerAccount` (string, FK) - Links to D365 F&O customer
- `DataAreaId` (string, FK) - Links to D365 F&O data area
- `BusinessCategory` (enum: HospitalPharmacy, CommunityPharmacy, Veterinarian, Manufacturer, WholesalerEU, WholesalerNonEU, ResearchInstitution, required) - Type of entity
- `ApprovalStatus` (enum: Pending, Approved, ConditionallyApproved, Rejected, Suspended, required) - Current qualification status
- `GdpQualificationStatus` (enum: NotRequired, Pending, Approved, ConditionallyApproved, Rejected, UnderReview, required) - GDP qualification status
- `OnboardingDate` (DateOnly, nullable) - When customer was first qualified
- `NextReVerificationDate` (DateOnly, nullable) - When next periodic review is due
- `IsSuspended` (bool, required, default: false) - Whether sales are currently blocked
- `SuspensionReason` (string, nullable) - Why customer is suspended
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Computed Properties**:
- `BusinessName` => `OrganizationName` (convenience alias)
- `Country` => `AddressCountryRegionId` (convenience alias)
- `IsComplianceConfigured` => `ComplianceExtensionId != Guid.Empty`

**Relationships**:
- Has many `Licence` (one-to-many) - Licences held by this customer
- Has many `Transaction` (one-to-many) - Compliance validations for this customer
- Has many `Threshold` (one-to-many) - Monitoring thresholds for this customer
- Has many `QualificationReview` (one-to-many) - Qualification and re-qualification reviews
- Has many `GdpCredential` (one-to-many) - GDP credentials for this customer
- Has many `AuditEvent` (one-to-many) - Audit events related to this customer

**Storage**:
- D365 F&O: `CustomersV3` OData entity (read-only master data)
- Dataverse: `phr_customercomplianceextension` table (compliance extensions)

**Validation Rules**:
- `ApprovalStatus` must be `Approved` or `ConditionallyApproved` for transactions to be allowed
- `IsSuspended = true` blocks all transactions regardless of `ApprovalStatus`
- Compliance extension can only be created for customers that exist in D365 F&O

---

### 6. Transaction

Represents a compliance validation request/result for a sales order or transfer (originated from external order management system).

**Attributes**:
- `TransactionId` (Guid, PK) - Unique identifier
- `ExternalTransactionId` (string, required, unique, indexed) - Order/shipment ID from calling system
- `CustomerAccount` (string, FK → Customer, required) - Customer account number from D365 F&O
- `CustomerDataAreaId` (string, FK → Customer, required) - Customer data area from D365 F&O
- `TransactionDate` (DateTime, required) - When validation was performed
- `TransactionType` (enum: DomesticSale, EUCrossBorder, NonEUInternational, required) - Type of transaction
- `ComplianceStatus` (enum: Pass, Pending, Failed, OverrideApproved, required) - Validation result
- `ViolationSummary` (string, nullable) - Human-readable summary of violations
- `ApprovedBy` (Guid, FK → User, nullable) - Who approved override (if OverrideApproved)
- `ApprovalJustification` (string, nullable) - Why override was granted
- `CallingSystemId` (Guid, FK → IntegrationSystem, required) - Which external system called validation
- `ExternalUserId` (string, nullable) - User in external system who initiated transaction
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp

**Relationships**:
- Belongs to one `Customer` (many-to-one)
- Belongs to one `IntegrationSystem` (many-to-one)
- Approved by one `User` (many-to-one, nullable)
- Has many `TransactionLine` (one-to-many) - Products/substances in transaction
- Has many `TransactionLicenceUsage` (one-to-many) - Which licences authorized transaction
- Has many `TransactionViolation` (one-to-many) - Specific compliance violations
- Generates many `AuditEvent` (one-to-many)

**Storage**: D365 F&O virtual data entity `PharmaComplianceTransactionEntity`

**Validation Rules**:
- `ComplianceStatus = OverrideApproved` requires `ApprovedBy` and `ApprovalJustification`
- `ExternalTransactionId` must be unique per `CallingSystemId`

---

### 7. TransactionLine

Represents a product/substance line within a transaction.

**Attributes**:
- `TransactionLineId` (Guid, PK) - Unique identifier
- `TransactionId` (Guid, FK → Transaction, required) - Parent transaction
- `SubstanceId` (Guid, FK → ControlledSubstance, required) - Substance involved
- `Quantity` (decimal, required) - Amount (units depend on substance)
- `UnitOfMeasure` (string, required) - Unit (e.g., "g", "kg", "pieces")
- `LineComplianceStatus` (enum: Pass, Fail, required) - Line-level validation result

**Relationships**:
- Belongs to one `Transaction` (many-to-one)
- Belongs to one `ControlledSubstance` (many-to-one)
- Has many `TransactionViolation` (one-to-many) - Violations specific to this line

**Storage**: D365 F&O virtual data entity `PharmaComplianceTransactionLineEntity`

---

### 8. TransactionLicenceUsage

Records which licences (customer + company) authorized a transaction.

**Attributes**:
- `UsageId` (Guid, PK) - Unique identifier
- `TransactionId` (Guid, FK → Transaction, required) - Which transaction
- `LicenceId` (Guid, FK → Licence, required) - Which licence
- `UsageType` (enum: CustomerLicence, CompanyLicence, required) - Whose licence

**Relationships**:
- Belongs to one `Transaction` (many-to-one)
- Belongs to one `Licence` (many-to-one)

**Storage**: D365 F&O virtual data entity `PharmaComplianceLicenceUsageEntity`

---

### 9. TransactionViolation

Records specific compliance violations found during transaction validation.

**Attributes**:
- `ViolationId` (Guid, PK) - Unique identifier
- `TransactionId` (Guid, FK → Transaction, required) - Parent transaction
- `TransactionLineId` (Guid, FK → TransactionLine, nullable) - Specific line with violation (null = transaction-level)
- `ViolationType` (enum: MissingLicence, ExpiredLicence, SuspendedLicence, SubstanceNotAuthorized, ThresholdExceeded, MissingPermit, required) - Type of violation
- `Description` (string, required) - Detailed violation message
- `RequiredLicenceTypeId` (Guid, FK → LicenceType, nullable) - Which licence type is missing

**Relationships**:
- Belongs to one `Transaction` (many-to-one)
- Belongs to one `TransactionLine` (many-to-one, nullable)
- References one `LicenceType` (many-to-one, nullable)

**Storage**: D365 F&O virtual data entity `PharmaComplianceViolationEntity`

---

### 10. Threshold

Defines quantity or frequency limits for suspicious-order monitoring.

**Attributes**:
- `ThresholdId` (Guid, PK) - Unique identifier
- `ComplianceExtensionId` (Guid, FK → CustomerComplianceExtension, required) - Which customer (references `ComplianceExtensionId` from `phr_customercomplianceextension`)
- `SubstanceId` (Guid, FK → ControlledSubstance, required) - Which substance
- `ThresholdType` (enum: MonthlyQuantity, AnnualFrequency, required) - Type of limit
- `LimitValue` (decimal, required) - Threshold value
- `MonitoringPeriodDays` (int, required) - Period for threshold calculation

**Relationships**:
- Belongs to one `Customer` via `ComplianceExtensionId` (many-to-one, references `phr_customercomplianceextension`)
- Belongs to one `ControlledSubstance` (many-to-one)
- Generates many `Alert` (one-to-many)

**Storage**: Dataverse virtual table `phr_threshold`

**Validation Rules**:
- `ComplianceExtensionId` + `SubstanceId` + `ThresholdType` must be unique

---

### 11. Alert

Represents a compliance warning or reminder.

**Attributes**:
- `AlertId` (Guid, PK) - Unique identifier
- `AlertType` (enum: LicenceExpiring, LicenceExpired, MissingDocumentation, ThresholdExceeded, ReVerificationDue, GdpCertificateExpiring, required) - Type of alert
- `Severity` (enum: Critical, Warning, Info, required) - Urgency level
- `TargetEntityType` (enum: Customer, Licence, Threshold, GdpSite, GdpCredential, required) - What entity this alert is about
- `TargetEntityId` (Guid, required) - Reference to entity. When `TargetEntityType=Customer`, this is the `ComplianceExtensionId` from `phr_customercomplianceextension`
- `GeneratedDate` (DateTime, required) - When alert was created
- `AcknowledgedDate` (DateTime, nullable) - When alert was acknowledged
- `AcknowledgedBy` (Guid, FK → User, nullable) - Who acknowledged alert
- `Message` (string, required) - Alert message

**Relationships**:
- May reference `Customer`, `Licence`, `Threshold`, `GdpSite`, or `GdpCredential` (polymorphic via TargetEntityType/TargetEntityId)
- Acknowledged by one `User` (many-to-one, nullable)

**Storage**: D365 F&O virtual data entity `PharmaComplianceAlertEntity`

---

### 12. LicenceDocument

Stores metadata and reference to supporting documentation for licences.

**Attributes**:
- `DocumentId` (Guid, PK) - Unique identifier
- `LicenceId` (Guid, FK → Licence, required) - Which licence
- `DocumentType` (enum: Certificate, Letter, InspectionReport, Other, required) - Type of document
- `FileName` (string, required) - Original filename
- `BlobStorageUrl` (string, required) - Azure Blob Storage URL (SAS token not stored here)
- `UploadedDate` (DateTime, required) - When document was uploaded
- `UploadedBy` (Guid, FK → User, required) - Who uploaded document

**Relationships**:
- Belongs to one `Licence` (many-to-one)
- Uploaded by one `User` (many-to-one)

**Storage**: Dataverse virtual table `phr_licencedocument`

**Note**: Actual document files stored in Azure Blob Storage, not in Dataverse/D365.

---

### 13. LicenceVerification

Records verification of a licence's authenticity and validity.

**Attributes**:
- `VerificationId` (Guid, PK) - Unique identifier
- `LicenceId` (Guid, FK → Licence, required) - Which licence
- `VerificationMethod` (enum: AuthorityWebsite, EmailConfirmation, PhoneConfirmation, PhysicalInspection, required) - How verified
- `VerificationDate` (DateOnly, required) - When verification performed
- `VerifiedBy` (Guid, FK → User, required) - Who performed verification
- `Outcome` (enum: Valid, Invalid, Pending, required) - Verification result
- `Notes` (string, nullable) - Additional details

**Relationships**:
- Belongs to one `Licence` (many-to-one)
- Verified by one `User` (many-to-one)

**Storage**: Dataverse virtual table `phr_licenceverification`

---

### 14. LicenceScopeChange

Records historical changes to a licence's scope or authorized substances.

**Attributes**:
- `ChangeId` (Guid, PK) - Unique identifier
- `LicenceId` (Guid, FK → Licence, required) - Which licence
- `EffectiveDate` (DateOnly, required) - When change took effect
- `ChangeDescription` (string, required) - What changed
- `RecordedBy` (Guid, FK → User, required) - Who recorded change
- `RecordedDate` (DateTime, required) - When change was recorded

**Relationships**:
- Belongs to one `Licence` (many-to-one)
- Recorded by one `User` (many-to-one)

**Storage**: Dataverse virtual table `phr_licencescopechange`

---

### 15. AuditEvent

Records compliance-related actions, verifications, or changes for audit trail.

**Attributes**:
- `EventId` (Guid, PK) - Unique identifier
- `EventType` (enum: LicenceCreated, LicenceModified, CustomerApproved, CustomerSuspended, TransactionValidated, InspectionRecorded, OverrideApproved, required) - Type of event
- `EventDate` (DateTime, required) - When event occurred
- `PerformedBy` (Guid, FK → User, required) - Who performed action
- `EntityType` (enum: Customer, Licence, Transaction, GdpSite, Inspection, required) - What entity affected
- `EntityId` (Guid, required) - Reference to affected entity. When `EntityType=Customer`, this is the `ComplianceExtensionId` from `phr_customercomplianceextension`
- `Details` (string, nullable) - Event details (JSON serialized)
- `SupportingEvidenceUrl` (string, nullable) - Reference to evidence (document, screenshot, etc.)

**Relationships**:
- May reference `Customer`, `Licence`, `Transaction`, `GdpSite`, `Inspection`, etc. (polymorphic via EntityType/EntityId)
- Performed by one `User` (many-to-one)

**Storage**: D365 F&O virtual data entity `PharmaComplianceAuditEntity`

---

## GDP Entities

### 16. GdpSite (Composite Model)

Represents a GDP-relevant physical location. This is a **composite domain model** combining read-only warehouse master data from D365 F&O with GDP-specific extensions stored in Dataverse. This avoids duplicating master data and ensures the compliance system stays in sync with D365FO.

**D365 F&O Warehouse Attributes** (read-only, from OData entity `Warehouses`):
- `WarehouseId` (string, composite PK part 1) - D365FO warehouse identifier
- `DataAreaId` (string, composite PK part 2) - Legal entity / data area
- `WarehouseName` (string) - Warehouse display name
- `OperationalSiteId` (string) - Parent operational site ID
- `OperationalSiteName` (string) - Parent site name (resolved from `OperationalSitesV2` entity)
- `WarehouseType` (string) - D365FO type: Standard, Quarantine, Transit
- `FormattedAddress` (string) - Pre-formatted address from D365FO
- `Street` (string), `StreetNumber` (string), `City` (string), `ZipCode` (string)
- `CountryRegionId` (string), `StateId` (string)
- `Latitude` (decimal?), `Longitude` (decimal?)

**GDP Extension Attributes** (stored in Dataverse table `phr_gdpwarehouseextension`):
- `GdpExtensionId` (Guid, PK) - Unique identifier for the GDP extension record
- `phr_warehouseid` (string, FK → D365FO Warehouse) - Links to D365FO warehouse
- `phr_dataareaid` (string) - Legal entity context
- `GdpSiteType` (enum: Warehouse, CrossDock, TransportHub, required) - GDP classification
- `PermittedActivities` (flags: StorageOver72h=1, TemperatureControlled=2, Outsourced=4, TransportOnly=8) - Activities allowed at this site
- `IsGdpActive` (bool, required, default: true) - Whether GDP configuration is active
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Domain Logic**:
- `IsConfiguredForGdp` (bool) - True when GdpExtensionId != Guid.Empty
- `HasActivity(GdpSiteActivity)` - Checks if a specific activity flag is set
- `Validate()` - Validates GdpSiteType, PermittedActivities (at least one required), WarehouseId, DataAreaId

**Relationships**:
- Has many `GdpSiteWdaCoverage` (one-to-many, via WarehouseId + DataAreaId) - Which WDAs cover this site
- Has many `GdpInspection` (one-to-many) - Inspections at this site
- Has many `GdpSiteSop` (one-to-many) - SOPs applicable to this site
- Has many `TrainingRecord` (one-to-many) - Training for staff at this site

**Storage**: D365 F&O `Warehouses` entity (read-only) + Dataverse table `phr_gdpwarehouseextension` (GDP extensions)

---

### 17. GdpSiteWdaCoverage

Maps which Wholesale Distribution Authorisations (WDAs) cover which GDP sites (warehouses).

**Attributes**:
- `CoverageId` (Guid, PK) - Unique identifier
- `WarehouseId` (string, FK → D365FO Warehouse, required) - Which warehouse
- `DataAreaId` (string, required) - Legal entity context
- `LicenceId` (Guid, FK → Licence, required) - Which WDA licence (must be type "Wholesale Distribution Authorisation (WDA)")
- `EffectiveDate` (DateOnly, required) - When coverage started
- `ExpiryDate` (DateOnly, nullable) - When coverage ends

**Domain Logic**:
- `IsActive()` - True when EffectiveDate <= today and (ExpiryDate is null or ExpiryDate >= today)
- `Validate()` - Checks required fields, ExpiryDate > EffectiveDate

**Relationships**:
- Belongs to one `GdpSite` (many-to-one, via WarehouseId + DataAreaId)
- Belongs to one `Licence` (many-to-one, constrained to WDA type per FR-033)

**Storage**: Dataverse virtual table `phr_gdpsitewdacoverage`

---

### 18. GdpCredential

Represents a partner's GDP compliance status and credentials.

**Attributes**:
- `CredentialId` (Guid, PK) - Unique identifier
- `EntityType` (enum: Supplier, ServiceProvider, required) - Type of partner
- `EntityId` (Guid, FK → Customer or GdpServiceProvider, required) - Which partner. When `EntityType=Supplier` (customer), this is the `ComplianceExtensionId` from `phr_customercomplianceextension`
- `WdaNumber` (string, nullable) - WDA number if applicable
- `GdpCertificateNumber` (string, nullable) - GDP certificate number
- `EudraGmdpEntryUrl` (string, nullable) - Link to EudraGMDP entry
- `ValidityStartDate` (DateOnly, nullable) - When credentials became valid
- `ValidityEndDate` (DateOnly, nullable) - When credentials expire
- `QualificationStatus` (enum: Approved, ConditionallyApproved, Rejected, UnderReview, required) - Current status
- `LastVerificationDate` (DateOnly, nullable) - Last verification via EudraGMDP
- `NextReviewDate` (DateOnly, nullable) - Next periodic review due date
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Relationships**:
- Belongs to one `Customer` or one `GdpServiceProvider` (polymorphic via EntityType/EntityId)
- Has many `GdpCredentialVerification` (one-to-many) - Verification history
- Has many `QualificationReview` (one-to-many) - Qualification reviews
- Generates many `Alert` (one-to-many) - Expiry alerts

**Storage**: Dataverse virtual table `phr_gdpcredential`

---

### 19. GdpServiceProvider

Represents 3PLs, transporters, external warehouses providing GDP services.

**Attributes**:
- `ProviderId` (Guid, PK) - Unique identifier
- `ProviderName` (string, required, indexed) - Provider name
- `ServiceType` (enum: ThirdPartyLogistics, Transporter, ExternalWarehouse, required) - Type of service
- `TemperatureControlledCapability` (bool, required) - Whether temperature-controlled transport available
- `ApprovedRoutes` (string, nullable) - Approved routes/lanes (free text or JSON)
- `QualificationStatus` (enum: Approved, ConditionallyApproved, Rejected, UnderReview, required) - Current status
- `ReviewFrequencyMonths` (int, required) - How often re-qualification needed (e.g., 24 or 36 months)
- `LastReviewDate` (DateOnly, nullable) - Last qualification review
- `NextReviewDate` (DateOnly, nullable) - Next review due
- `IsActive` (bool, required, default: true) - Whether provider can be selected

**Relationships**:
- Has many `GdpCredential` (one-to-many) - GDP credentials
- Has many `QualificationReview` (one-to-many) - Qualification reviews

**Storage**: Dataverse virtual table `phr_gdpserviceprovider`

---

### 20. GdpInspection

Represents a regulatory or internal GDP audit.

**Attributes**:
- `InspectionId` (Guid, PK) - Unique identifier
- `InspectionDate` (DateOnly, required) - When inspection occurred
- `InspectorName` (string, required) - Inspector or authority name (e.g., "IGJ", "NVWA", "Internal QA")
- `InspectionType` (enum: RegulatoryAuthority, Internal, SelfInspection, required) - Type of inspection
- `SiteId` (Guid, FK → GdpSite, required) - Which site was inspected
- `WdaLicenceId` (Guid, FK → Licence, nullable) - Which WDA was inspected (if applicable)
- `FindingsSummary` (string, nullable) - Overall findings
- `ReportReferenceUrl` (string, nullable) - Link to inspection report document

**Relationships**:
- Applies to one `GdpSite` (many-to-one)
- May reference one `Licence` (many-to-one, WDA type)
- Has many `GdpInspectionFinding` (one-to-many) - Individual findings
- Has many `Capa` (one-to-many) - CAPAs from findings

**Storage**: Dataverse virtual table `phr_gdpinspection`

---

### 21. GdpInspectionFinding

Represents an individual finding from a GDP inspection.

**Attributes**:
- `FindingId` (Guid, PK) - Unique identifier
- `InspectionId` (Guid, FK → GdpInspection, required) - Parent inspection
- `FindingDescription` (string, required) - What deficiency was found
- `Classification` (enum: Critical, Major, Other, required) - Severity
- `FindingNumber` (string, nullable) - Official finding reference number

**Relationships**:
- Belongs to one `GdpInspection` (many-to-one)
- Has many `Capa` (one-to-many) - CAPAs addressing this finding

**Storage**: Dataverse virtual table `phr_gdpinspectionfinding`

---

### 22. Capa (Corrective and Preventive Action)

Represents an action to address a GDP deficiency.

**Attributes**:
- `CapaId` (Guid, PK) - Unique identifier
- `CapaNumber` (string, required, unique, indexed) - Unique CAPA reference
- `FindingId` (Guid, FK → GdpInspectionFinding, required) - Which finding this addresses
- `Description` (string, required) - CAPA description
- `OwnerId` (Guid, FK → User, required) - Person responsible
- `DueDate` (DateOnly, required) - When CAPA must be completed
- `CompletionDate` (DateOnly, nullable) - When CAPA was actually completed
- `Status` (enum: Open, Overdue, Completed, required) - Current status
- `VerificationNotes` (string, nullable) - Verification of effectiveness
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Relationships**:
- Originates from one `GdpInspectionFinding` (many-to-one)
- Assigned to one `User` as owner (many-to-one)

**Storage**: Dataverse virtual table `phr_capa`

**Validation Rules**:
- `Status` automatically set to `Overdue` when `DueDate < Today` and `CompletionDate` is null
- `Status = Completed` requires `CompletionDate`

---

### 23. GdpSop

Represents a documented procedure for GDP compliance.

**Attributes**:
- `SopId` (Guid, PK) - Unique identifier
- `SopNumber` (string, required, unique, indexed) - SOP reference number
- `Title` (string, required) - SOP title
- `Category` (enum: Returns, Recalls, Deviations, TemperatureExcursions, OutsourcedActivities, Other, required) - SOP category
- `Version` (string, required) - SOP version
- `EffectiveDate` (DateOnly, required) - When this version became effective
- `DocumentUrl` (string, nullable) - Link to SOP document
- `IsActive` (bool, required, default: true) - Whether SOP is current

**Relationships**:
- Has many `GdpSiteSop` (one-to-many) - Which sites this SOP applies to
- Has many `TrainingRecord` (one-to-many) - Training on this SOP

**Storage**: Dataverse virtual table `phr_gdpsop`

---

### 24. GdpSiteSop

Maps which SOPs apply to which sites.

**Attributes**:
- `SiteSopId` (Guid, PK) - Unique identifier
- `SiteId` (Guid, FK → GdpSite, required) - Which site
- `SopId` (Guid, FK → GdpSop, required) - Which SOP

**Relationships**:
- Belongs to one `GdpSite` (many-to-one)
- Belongs to one `GdpSop` (many-to-one)

**Storage**: Dataverse virtual table `phr_gdpsitesop`

---

### 25. TrainingRecord

Represents completion of GDP training by staff.

**Attributes**:
- `TrainingRecordId` (Guid, PK) - Unique identifier
- `StaffMemberId` (Guid, FK → User, required) - Who was trained
- `TrainingCurriculum` (string, required) - Training topic/curriculum
- `SopId` (Guid, FK → GdpSop, nullable) - Which SOP training covered (if applicable)
- `SiteId` (Guid, FK → GdpSite, nullable) - Which site training applies to (if site-specific)
- `CompletionDate` (DateOnly, required) - When training completed
- `ExpiryDate` (DateOnly, nullable) - When retraining required (null = no expiry)
- `TrainerName` (string, nullable) - Who delivered training
- `AssessmentResult` (enum: Pass, Fail, NotAssessed, required) - Assessment outcome

**Relationships**:
- Belongs to one `User` as staff member (many-to-one)
- May reference one `GdpSop` (many-to-one, nullable)
- May reference one `GdpSite` (many-to-one, nullable)

**Storage**: Dataverse virtual table `phr_trainingrecord`

---

### 26. GdpChangeRecord

Represents a change impacting GDP compliance.

**Attributes**:
- `ChangeRecordId` (Guid, PK) - Unique identifier
- `ChangeNumber` (string, required, unique, indexed) - Change control reference
- `ChangeType` (enum: NewWarehouse, New3PL, NewProductType, StorageConditionChange, Other, required) - Type of change
- `Description` (string, required) - Change description
- `RiskAssessment` (string, nullable) - Risk assessment results
- `ApprovalStatus` (enum: Pending, Approved, Rejected, required) - Current status
- `ApprovedBy` (Guid, FK → User, nullable) - Who approved change
- `ApprovalDate` (DateOnly, nullable) - When approved
- `ImplementationDate` (DateOnly, nullable) - When change implemented
- `UpdatedDocumentationRefs` (string, nullable) - References to updated SOPs, training, etc.
- `CreatedDate` (DateTime, required) - Record creation timestamp
- `ModifiedDate` (DateTime, required) - Last modification timestamp
- `RowVersion` (byte[], required) - Optimistic concurrency token

**Relationships**:
- May reference `GdpSite`, `GdpServiceProvider`, etc. (polymorphic, not enforced by FK)
- Approved by one `User` (many-to-one, nullable)
- Generates `AuditEvent` (one-to-many)

**Storage**: Dataverse virtual table `phr_gdpchangerecord`

---

## Supporting Entities

### 27. IntegrationSystem

Represents an external system authorized to call compliance APIs.

**Attributes**:
- `IntegrationSystemId` (Guid, PK) - Unique identifier
- `SystemName` (string, required, unique, indexed) - System name (e.g., "SAP ERP", "WMS")
- `SystemType` (enum: ERP, OrderManagement, WMS, CustomSystem, required) - Type of system
- `ApiKeyHash` (string, nullable) - Hashed API key for authentication (if using API key auth)
- `OAuthClientId` (string, nullable) - OAuth client ID (if using OAuth)
- `AuthorizedEndpoints` (string, nullable) - Comma-separated list of authorized API endpoints
- `IpWhitelist` (string, nullable) - Comma-separated IP addresses allowed (optional)
- `IsActive` (bool, required, default: true) - Whether system can call APIs
- `ContactPerson` (string, nullable) - Technical contact for this integration

**Relationships**:
- Has many `Transaction` (one-to-many) - Transactions originated by this system

**Storage**: Dataverse virtual table `phr_integrationsystem`

---

### 28. User

Represents an application user (internal staff or external partner).

**Attributes**:
- `UserId` (Guid, PK) - Unique identifier
- `UserName` (string, required, unique, indexed) - Username for login
- `Email` (string, required, unique, indexed) - Email address
- `FullName` (string, required) - Display name
- `AuthenticationMethod` (enum: AzureAD, LocalCredentials, required) - How user authenticates
- `AzureAdObjectId` (Guid, nullable) - Azure AD object ID (if SSO user)
- `Roles` (string, required) - Comma-separated roles (ComplianceManager, QAUser, SalesAdmin, etc.)
- `IsActive` (bool, required, default: true) - Whether user can login

**Relationships**:
- Has many `LicenceDocument` uploaded (one-to-many)
- Has many `LicenceVerification` performed (one-to-many)
- Has many `LicenceScopeChange` recorded (one-to-many)
- Has many `AuditEvent` performed (one-to-many)
- Has many `Alert` acknowledged (one-to-many)
- Has many `Transaction` overrides approved (one-to-many)
- Has many `Capa` owned (one-to-many)
- Has many `TrainingRecord` (one-to-many)
- Has many `GdpChangeRecord` approved (one-to-many)

**Storage**: Dataverse virtual table `phr_user` (synced with Azure AD)

---

### 29. QualificationReview

Represents a qualification or re-qualification review of a customer/partner.

**Attributes**:
- `ReviewId` (Guid, PK) - Unique identifier
- `EntityType` (enum: Customer, ServiceProvider, required) - What is being reviewed
- `EntityId` (Guid, FK → Customer or GdpServiceProvider, required) - Which entity. When `EntityType=Customer`, this is the `ComplianceExtensionId` from `phr_customercomplianceextension`
- `ReviewDate` (DateOnly, required) - When review occurred
- `ReviewMethod` (enum: OnSiteAudit, Questionnaire, DocumentReview, required) - How reviewed
- `ReviewOutcome` (enum: Approved, ConditionallyApproved, Rejected, required) - Result
- `ReviewerName` (string, required) - Who performed review
- `Notes` (string, nullable) - Review notes
- `NextReviewDate` (DateOnly, nullable) - When next review due

**Relationships**:
- Belongs to one `Customer` or one `GdpServiceProvider` (polymorphic via EntityType/EntityId)

**Storage**: Dataverse virtual table `phr_qualificationreview`

---

### 30. GdpCredentialVerification

Records verification of a partner's GDP status via EudraGMDP or national databases.

**Attributes**:
- `VerificationId` (Guid, PK) - Unique identifier
- `CredentialId` (Guid, FK → GdpCredential, required) - Which credential
- `VerificationDate` (DateOnly, required) - When verified
- `VerificationMethod` (enum: EudraGMDP, NationalDatabase, Other, required) - How verified
- `VerifiedBy` (Guid, FK → User, required) - Who performed verification
- `Outcome` (enum: Valid, Invalid, NotFound, required) - Verification result
- `Notes` (string, nullable) - Additional details

**Relationships**:
- Belongs to one `GdpCredential` (many-to-one)
- Performed by one `User` (many-to-one)

**Storage**: Dataverse virtual table `phr_gdpcredentialverification`

---

## Entity Storage Summary

### Dataverse Virtual Tables (Master Data)
- Licence, LicenceType, ControlledSubstance, LicenceSubstanceMapping
- CustomerComplianceExtension (`phr_customercomplianceextension`), IntegrationSystem, User
- Threshold
- LicenceDocument, LicenceVerification, LicenceScopeChange
- GdpWarehouseExtension (`phr_gdpwarehouseextension`), GdpSiteWdaCoverage, GdpCredential, GdpServiceProvider
- GdpInspection, GdpInspectionFinding, Capa
- GdpSop, GdpSiteSop, TrainingRecord, GdpChangeRecord
- QualificationReview, GdpCredentialVerification

### D365 F&O Virtual Data Entities (Read-Only Master Data)
- CustomersV3 (OData: `CustomersV3`) - Customer master data, keyed by CustomerAccount + dataAreaId
- Warehouses (OData: `Warehouses`) - Physical warehouse locations, keyed by WarehouseId + dataAreaId
- OperationalSitesV2 (OData: `OperationalSitesV2`) - Organizational sites, keyed by SiteId + dataAreaId

### D365 F&O Virtual Data Entities (Transactional Data)
- Transaction, TransactionLine, TransactionLicenceUsage, TransactionViolation
- AuditEvent, Alert

### Composite Domain Models (Multi-Source)
- Customer = D365FO `CustomersV3` (read-only master data) + Dataverse `phr_customercomplianceextension` (compliance config)
- GdpSite = D365FO `Warehouses` (read-only) + Dataverse `phr_gdpwarehouseextension` (GDP config)

---

## Optimistic Concurrency Control

All entities with mutable state include a `RowVersion` (byte[]) attribute for optimistic locking:
- Licence, Customer, GdpSite, GdpCredential, Capa, GdpChangeRecord

When updating these entities, the application MUST:
1. Include current `RowVersion` in update request
2. Dataverse/D365 verifies `RowVersion` matches current value
3. If mismatch detected (concurrent modification), operation fails with 409 Conflict
4. Application presents conflict resolution UI showing both versions (FR-027b)
5. User resolves conflict by choosing version or merging fields
6. Resolution logged in `AuditEvent` (FR-027c)

---

## Key Relationships Diagram (High-Level)

```text
D365FO CustomersV3 ----[compliance extensions]---- Dataverse phr_customercomplianceextension = Customer (composite)
Customer [CustomerAccount+DataAreaId] (1) ----< (M) Licence (via ComplianceExtensionId) ----< (M) LicenceSubstanceMapping >---- (M) ControlledSubstance
   |                                                                                                        |
   | (1:M via CustomerAccount+DataAreaId)                                                                   | (1:M)
   v                                                                                                        v
Transaction ----< (1:M) TransactionLine ----< (M:1) ControlledSubstance
   |
   | (1:M)
   v
TransactionViolation

Customer [ComplianceExtensionId] (1) ----< (M) GdpCredential
Customer [ComplianceExtensionId] (1) ----< (M) Threshold >---- (M:1) ControlledSubstance

D365FO Warehouse ----[GDP extensions]---- Dataverse phr_gdpwarehouseextension = GdpSite (composite)
GdpSite [WarehouseId+DataAreaId] (1) ----< (M) GdpInspection ----< (1:M) GdpInspectionFinding ----< (1:M) Capa
GdpSite [WarehouseId+DataAreaId] (1) ----< (M) GdpSiteWdaCoverage >---- (M:1) Licence (WDA type)
GdpSite [WarehouseId+DataAreaId] (1) ----< (M) GdpSiteSop >---- (M:1) GdpSop

GdpServiceProvider (1) ----< (M) GdpCredential
GdpServiceProvider (1) ----< (M) QualificationReview

User (1) ----< (M) LicenceVerification
User (1) ----< (M) AuditEvent
User (1) ----< (M) Capa (as owner)
User (1) ----< (M) TrainingRecord
```

---

## Next Steps

- **Phase 1 (continued)**: Generate API contracts in `/contracts/` directory
- **Phase 1 (continued)**: Generate quickstart.md with development setup instructions
- **Phase 1 (final)**: Update agent context file with technology stack
