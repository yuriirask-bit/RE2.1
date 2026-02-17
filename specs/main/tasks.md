# Tasks: User Story 10 - GDP Certificates, Validity & Monitoring

**Input**: Design documents from `/specs/main/`
**Prerequisites**: plan.md, research.md, data-model.md, contracts/gdp-documents-api.yaml, quickstart.md
**Continues from**: specs/001-licence-management/tasks.md (replaces stale T231-T239 with detailed breakdown)

**Tests**: TDD approach — test tasks precede implementation tasks.

**Organization**: Single user story (US10) broken into implementation phases following established codebase patterns.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[US10]**: All tasks belong to User Story 10
- Include exact file paths in descriptions

---

## Phase A: Domain Model (GdpDocument)

**Purpose**: Create the GdpDocument domain model following the established LicenceDocument pattern.

### Tests (Write First)

- [X] T231 [US10] Write GdpDocument validation tests in tests/RE2.ComplianceCore.Tests/Models/GdpDocumentTests.cs — test Validate() for: missing OwnerEntityId, missing FileName, missing BlobStorageUrl, missing UploadedBy, invalid file extensions, valid document, GetFileExtension(), IsPdf(), IsImage() helper methods. Follow existing pattern from LicenceDocumentTests if present, or GdpInspectionTests.

### Implementation

- [X] T232 [US10] Create GdpDocument domain model in src/RE2.ComplianceCore/Models/GdpDocument.cs per data-model.md — include GdpDocumentEntityType enum (Credential, Site, Inspection, Provider, Customer), properties (DocumentId, OwnerEntityType, OwnerEntityId, DocumentType, FileName, BlobStorageUrl, UploadedDate, UploadedBy, ContentType, FileSizeBytes, Description), Validate() method with same rules as LicenceDocument, and helper methods (GetFileExtension, IsPdf, IsImage). Reuse existing DocumentType enum from LicenceDocument.cs.

**Checkpoint**: GdpDocument model compiles, all T231 tests pass.

---

## Phase B: Data Access (Repository Layer)

**Purpose**: Create repository interface and implementations for GdpDocument CRUD.

- [X] T233 [US10] Create IGdpDocumentRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpDocumentRepository.cs — methods: GetDocumentsByEntityAsync(GdpDocumentEntityType, Guid), GetDocumentAsync(Guid), CreateDocumentAsync(GdpDocument), DeleteDocumentAsync(Guid). Follow IGdpInspectionRepository pattern.

- [X] T234 [P] [US10] Create InMemoryGdpDocumentRepository in src/RE2.DataAccess/InMemory/InMemoryGdpDocumentRepository.cs — ConcurrentDictionary pattern with Clone helper, SeedDocuments method. Follow InMemoryGdpInspectionRepository pattern.

- [X] T235 [P] [US10] Create DataverseGdpDocumentRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpDocumentRepository.cs — Dataverse entity name "phr_gdpdocument", QueryExpression/ConditionExpression pattern, MapToDocumentDto/MapDocumentToEntity mappers. Follow DataverseGdpInspectionRepository pattern.

- [X] T236 [US10] Update InMemorySeedData.cs in src/RE2.DataAccess/InMemory/InMemorySeedData.cs — add SeedGdpDocumentData method with 3 sample documents: one GDP certificate attached to an existing credential (OwnerEntityType.Credential), one inspection report attached to an existing inspection (OwnerEntityType.Inspection), one WDA copy attached to an existing site (OwnerEntityType.Site). Add well-known document IDs. Update SeedAll overload to include InMemoryGdpDocumentRepository parameter.

- [X] T237 [US10] Update InfrastructureExtensions.cs in src/RE2.DataAccess/DependencyInjection/InfrastructureExtensions.cs — register DataverseGdpDocumentRepository in AddDataverseServices, register InMemoryGdpDocumentRepository in AddInMemoryRepositories with seed data call, add both as scoped/singleton DI registrations for IGdpDocumentRepository.

**Checkpoint**: All repository implementations compile, DI wiring complete, build succeeds with 0 errors.

---

## Phase C: Business Logic (Service Layer)

**Purpose**: Extend GdpComplianceService with document CRUD methods using IDocumentStorage for blob operations.

- [X] T238 [US10] Extend IGdpComplianceService interface in src/RE2.ComplianceCore/Interfaces/IGdpComplianceService.cs — add #region GDP Documents with methods: GetDocumentsByEntityAsync(GdpDocumentEntityType, Guid), GetDocumentAsync(Guid), UploadDocumentAsync(GdpDocument, Stream), GetDocumentDownloadUrlAsync(Guid, TimeSpan), DeleteDocumentAsync(Guid). Follow existing region pattern.

- [X] T239 [US10] Extend GdpComplianceService in src/RE2.ComplianceCore/Services/GdpCompliance/GdpComplianceService.cs — add IGdpDocumentRepository and IDocumentStorage dependencies to constructor, implement document methods using container name "gdp-documents", blob path pattern "{entityType}/{entityId}/{documentId}/{filename}", metadata dictionary pattern from LicenceService. UploadDocumentAsync: validate document, upload blob, set BlobStorageUrl, save metadata. GetDocumentDownloadUrlAsync: get document, generate SAS URI with configurable expiry (default 15 minutes). DeleteDocumentAsync: delete blob, delete metadata.

**Checkpoint**: Build succeeds, service methods compile with proper dependencies.

---

## Phase D: Azure Function (GdpCertificateMonitor)

**Purpose**: Create timer-triggered Azure Function for automated GDP credential expiry monitoring per FR-043.

### Tests (Write First)

- [X] T240 [US10] Write GdpCertificateMonitor tests in tests/RE2.ComplianceFunctions.Tests/GdpCertificateMonitorTests.cs — test Run() calls GenerateGdpCredentialExpiryAlertsAsync(90), GenerateProviderRequalificationAlertsAsync(), GenerateCapaOverdueAlertsAsync() and logs results. Test GenerateGdpAlertsManual HTTP trigger returns GdpAlertGenerationResult with counts. Use Mock<AlertGenerationService> and Mock<ILogger>. Follow LicenceExpiryMonitorTests pattern.

### Implementation

- [X] T241 [US10] Create GdpCertificateMonitor Azure Function in src/RE2.ComplianceFunctions/GdpCertificateMonitor.cs — timer trigger at "0 0 3 * * *" (3 AM UTC daily, 1 hour after LicenceExpiryMonitor). Inject AlertGenerationService and ILogger. Run() calls three existing methods: GenerateGdpCredentialExpiryAlertsAsync(90), GenerateProviderRequalificationAlertsAsync(), GenerateCapaOverdueAlertsAsync(). Add GenerateGdpAlertsManual HTTP trigger (POST, AuthorizationLevel.Admin, route "gdp-alerts/generate") for manual execution. Create GdpAlertGenerationResult DTO with CredentialExpiryAlertsGenerated, RequalificationAlertsGenerated, CapaOverdueAlertsGenerated, TotalAlertsGenerated, GeneratedAt, Success, ErrorMessage. Follow LicenceExpiryMonitor pattern exactly.

**Checkpoint**: Azure Function compiles, tests pass.

---

## Phase E: API Layer (Document Endpoints)

**Purpose**: Add document CRUD REST endpoints to GdpProvidersController per contracts/gdp-documents-api.yaml.

### Tests (Write First)

- [X] T242 [US10] Write document API endpoint tests in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpProvidersControllerDocumentTests.cs — test GetDocuments (returns list), GetDocument (found/not found), UploadDocument (valid/invalid), DownloadDocument (returns SAS URL / not found), DeleteDocument (success/not found). Use Mock<IGdpComplianceService>. Follow GdpInspectionsControllerTests pattern.

### Implementation

- [X] T243 [US10] Extend GdpProvidersController in src/RE2.ComplianceApi/Controllers/V1/GdpProvidersController.cs — add #region Documents with endpoints: GET credentials/{credentialId}/documents (list), GET documents/{documentId} (metadata), POST credentials/{credentialId}/documents (upload via multipart/form-data), GET documents/{documentId}/download (SAS URL), DELETE documents/{documentId}. Add DTOs: GdpDocumentResponseDto (with FromDomain static method), UploadDocumentRequestDto, DocumentDownloadResponseDto. Upload endpoint accepts IFormFile, validates size (50 MB max), creates GdpDocument, calls service UploadDocumentAsync. Download endpoint calls GetDocumentDownloadUrlAsync with 15-minute expiry. Authorization: [Authorize(Roles = "QAUser,ComplianceManager")] on write operations.

**Checkpoint**: API endpoints compile, all T242 tests pass, build succeeds.

---

## Phase F: Web UI (Credential Management, Documents & Verifications)

**Purpose**: Create web UI for credential validity management (FR-043), document attachment (FR-044), and verification logging (FR-045).

### MVC Controller

- [X] T244 [US10] Create GdpCredentialsController MVC controller in src/RE2.ComplianceWeb/Controllers/GdpCredentialsController.cs — inject IGdpComplianceService and ILogger. Actions: Index (list all credentials with validity status), Details (credential + documents + verifications), Expiring (credentials expiring within configurable days, default 90), RecordVerification GET/POST (verification form), UploadDocument GET/POST (document upload form), DeleteDocument POST (delete with redirect). View models: CredentialIndexViewModel (list with computed IsExpiring/IsExpired), CredentialDetailsViewModel (credential + documents list + verifications list), RecordVerificationViewModel (CredentialId, VerificationDate, VerificationMethod, VerifiedBy, Outcome, Notes), UploadDocumentViewModel (OwnerEntityType, OwnerEntityId, DocumentType, Description, File as IFormFile). Helper methods: PopulateVerificationMethodSelectList, PopulateOutcomeSelectList, PopulateDocumentTypeSelectList.

### Views

- [X] T245 [P] [US10] Create GdpCredentials/Index.cshtml in src/RE2.ComplianceWeb/Views/GdpCredentials/Index.cshtml — table listing all credentials with columns: Entity Type, Entity Name, WDA/Certificate Number, Validity Start, Validity End, Status badge (Valid=green, Expiring=warning, Expired=danger), Last Verified. Summary cards at top: total credentials, valid, expiring (within 90 days), expired. Link to Details and Expiring views. Follow GdpInspections/Index.cshtml pattern.

- [X] T246 [P] [US10] Create GdpCredentials/Details.cshtml in src/RE2.ComplianceWeb/Views/GdpCredentials/Details.cshtml — card layout showing credential details (WDA number, GDP certificate number, EudraGMDP URL, validity dates, qualification status, last verification date). Two tabbed sections: (1) Documents tab — table of attached documents with download/delete buttons, "Upload Document" action button; (2) Verifications tab — table of verification history (date, method, verifier, outcome, notes), "Record Verification" action button. Follow GdpProviders/Details.cshtml pattern.

- [X] T247 [P] [US10] Create GdpCredentials/Expiring.cshtml in src/RE2.ComplianceWeb/Views/GdpCredentials/Expiring.cshtml — dashboard showing credentials expiring within configurable window. Alert banner for expired credentials. Table sorted by expiry date (soonest first) with days remaining column. Filter controls for entity type and days ahead. Follow Capas.cshtml dashboard pattern.

- [X] T248 [P] [US10] Create GdpCredentials/RecordVerification.cshtml in src/RE2.ComplianceWeb/Views/GdpCredentials/RecordVerification.cshtml — form to record EudraGMDP/national DB verification. Breadcrumb: GDP Credentials > [Credential] > Record Verification. Card showing credential summary (WDA/certificate number, entity name, last verified date). Form fields: Verification Date (date picker, required), Verification Method (dropdown: EudraGMDP, NationalDatabase, Other), Verified By (text, required), Outcome (dropdown: Valid, Invalid, NotFound), Notes (textarea). Submit + Cancel buttons. Follow CompleteCapa.cshtml pattern.

- [X] T249 [P] [US10] Create GdpCredentials/UploadDocument.cshtml in src/RE2.ComplianceWeb/Views/GdpCredentials/UploadDocument.cshtml — form to upload document. Breadcrumb: GDP Credentials > [Credential] > Upload Document. Form fields: Document Type (dropdown: Certificate, Letter, InspectionReport, Other), Description (textarea, optional), File (file input with accept=".pdf,.doc,.docx,.jpg,.jpeg,.png,.tiff", max 50 MB). File type and size validation hint text. Submit + Cancel buttons. Use enctype="multipart/form-data". Follow CreateFinding.cshtml pattern.

### Navigation & Integration

- [X] T250 [US10] Update _Layout.cshtml in src/RE2.ComplianceWeb/Views/Shared/_Layout.cshtml — add "GDP Credentials" and "Expiring Credentials" links to GDP dropdown menu section (between GDP Providers and GDP Inspections links, with divider).

- [X] T251 [US10] Update GdpProviders/Details.cshtml in src/RE2.ComplianceWeb/Views/GdpProviders/Details.cshtml — add Documents section below existing credentials section. Show table of documents attached to this provider (OwnerEntityType.Provider). Include upload document link that routes to GdpCredentials/UploadDocument with pre-set entity type and ID.

**Checkpoint**: All web views render correctly, forms submit and redirect properly, navigation links work. Build succeeds with 0 errors.

---

## Phase G: Polish & Cross-Cutting

**Purpose**: Final validation and cleanup.

- [X] T252 [US10] Verify all existing tests still pass — run full test suite (`dotnet test RE2.sln`) and confirm 0 regressions from US10 changes. All new tests must also pass.

- [X] T253 [US10] Update specs/001-licence-management/tasks.md — replace stale T231-T239 placeholders with reference to specs/main/tasks.md, or mark them as superseded by this detailed task list.

**Checkpoint**: Full build clean (0 errors, 0 warnings), all tests pass, US10 feature complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase A (Domain Model)**: No dependencies on other US10 phases — can start immediately
- **Phase B (Data Access)**: Depends on Phase A (GdpDocument model must exist)
- **Phase C (Business Logic)**: Depends on Phase B (IGdpDocumentRepository must exist)
- **Phase D (Azure Function)**: No dependencies on Phases B/C — uses existing AlertGenerationService methods. Can run in parallel with B/C.
- **Phase E (API Layer)**: Depends on Phase C (service document methods must exist)
- **Phase F (Web UI)**: Depends on Phase C (service methods) and Phase E (for consistency). Can partially parallelize view creation.
- **Phase G (Polish)**: Depends on all other phases

### Within-Phase Parallel Opportunities

- **Phase B**: T234 and T235 can run in parallel (different files, same interface)
- **Phase D**: T240 and T241 can run sequentially (TDD), but T240 can be written while Phase B/C run
- **Phase F**: T245, T246, T247, T248, T249 can all run in parallel (different view files)

### Critical Path

```text
T231 → T232 → T233 → T234/T235 → T236 → T237 → T238 → T239 → T243 → T244 → T245-T251 → T252
                                                    ↘ T240 → T241 (parallel with B/C) ↗
```

---

## Parallel Example: Phase F Views

```text
# Launch all view creation tasks in parallel (different files):
Task: T245 "Create GdpCredentials/Index.cshtml"
Task: T246 "Create GdpCredentials/Details.cshtml"
Task: T247 "Create GdpCredentials/Expiring.cshtml"
Task: T248 "Create GdpCredentials/RecordVerification.cshtml"
Task: T249 "Create GdpCredentials/UploadDocument.cshtml"
```

---

## Implementation Strategy

### Task Count Summary

| Phase | Tasks | New Files | Modified Files |
|-------|-------|-----------|----------------|
| A: Domain Model | 2 (T231-T232) | 2 | 0 |
| B: Data Access | 5 (T233-T237) | 3 | 2 |
| C: Business Logic | 2 (T238-T239) | 0 | 2 |
| D: Azure Function | 2 (T240-T241) | 2 | 0 |
| E: API Layer | 2 (T242-T243) | 1 | 1 |
| F: Web UI | 8 (T244-T251) | 6 | 2 |
| G: Polish | 2 (T252-T253) | 0 | 1 |
| **Total** | **23** | **14** | **8** |

### MVP Scope

All tasks are US10 — implement sequentially in phase order for a single developer, or parallelize Phases D and B/C for faster delivery.

### Key Patterns to Follow

| Component | Pattern Source | Key File |
|-----------|---------------|----------|
| GdpDocument model | LicenceDocument | src/RE2.ComplianceCore/Models/LicenceDocument.cs |
| InMemory repository | InMemoryGdpInspectionRepository | src/RE2.DataAccess/InMemory/InMemoryGdpInspectionRepository.cs |
| Dataverse repository | DataverseGdpInspectionRepository | src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpInspectionRepository.cs |
| Service document methods | LicenceService.UploadDocumentAsync | src/RE2.ComplianceCore/Services/LicenceValidation/LicenceService.cs |
| Azure Function | LicenceExpiryMonitor | src/RE2.ComplianceFunctions/LicenceExpiryMonitor.cs |
| API controller DTOs | GdpProvidersController | src/RE2.ComplianceApi/Controllers/V1/GdpProvidersController.cs |
| MVC controller + views | GdpInspectionsController | src/RE2.ComplianceWeb/Controllers/GdpInspectionsController.cs |
| View templates | CompleteCapa.cshtml, Capas.cshtml | src/RE2.ComplianceWeb/Views/GdpInspections/ |

---

## Notes

- [P] tasks = different files, no dependencies
- FR-043 core (validity periods, expiry alerts) already implemented in US8 — US10 adds the Azure Function timer trigger and web UI
- FR-044 (document attachment) is the primary new capability — follows LicenceDocument pattern
- FR-045 core (verification logging) already implemented in US8 — US10 adds the web UI forms
- GdpDocument reuses existing DocumentType enum — no new enum file needed
- GdpCertificateMonitor calls existing AlertGenerationService methods — no new alert logic
- Company's own WDAs tracked via Licence model (HolderType=Company) — not GdpCredential
