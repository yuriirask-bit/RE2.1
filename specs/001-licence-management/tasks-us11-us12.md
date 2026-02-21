# Tasks: User Stories 11 & 12 - GDP Operational Checks, Documentation & Training

**Input**: Design documents from `/specs/main/` and `/specs/001-licence-management/`
**Prerequisites**: US7-US10 complete (GDP sites, providers, credentials, documents, inspections, CAPAs)
**Continues from**: US10 tasks (T231-T253 all complete)

**Tests**: TDD approach — test tasks precede implementation tasks.

**Organization**: Two user stories (US11 + US12) broken into implementation phases following established codebase patterns.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[US11]** / **[US12]**: Maps to specific user story
- Include exact file paths in descriptions

---

## Phase A: US11 Domain Model (GdpEquipmentQualification)

**Purpose**: Create new entity for FR-048 equipment/process qualification tracking. FR-046 and FR-047 use existing entities.

### Tests (Write First)

- [X] T254 [P] [US11] Write GdpEquipmentQualification model tests in tests/RE2.ComplianceCore.Tests/Models/GdpEquipmentQualificationTests.cs — test Validate() for: missing EquipmentName, missing QualifiedBy, future QualificationDate, valid record, IsExpired(), IsDueForRequalification() helpers. Follow GdpCredential test pattern.

### Implementation

- [X] T255 [US11] Create GdpEquipmentQualification domain model in src/RE2.ComplianceCore/Models/GdpEquipmentQualification.cs — include GdpEquipmentType enum (TemperatureControlledVehicle, MonitoringSystem, StorageEquipment, Other), properties (EquipmentQualificationId, EquipmentName, EquipmentType, ProviderId nullable FK, SiteId nullable FK, QualificationDate, RequalificationDueDate, QualificationStatus enum [Qualified, DueForRequalification, Expired, NotQualified], QualifiedBy, Notes, CreatedDate, ModifiedDate), Validate() method, IsExpired(), IsDueForRequalification() helpers. Dataverse table: phr_gdpequipmentqualification.

**Checkpoint**: Model compiles, T254 tests pass.

---

## Phase B: US11 Data Access & Service Layer

**Purpose**: Repository + service for equipment qualification and operational validation.

- [X] T256 [US11] Create IGdpEquipmentRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpEquipmentRepository.cs — methods: GetAllAsync, GetByIdAsync, GetByProviderAsync(Guid), GetBySiteAsync(Guid), GetDueForRequalificationAsync, CreateAsync, UpdateAsync, DeleteAsync. Follow IGdpInspectionRepository pattern.

- [X] T257 [P] [US11] Create InMemoryGdpEquipmentRepository in src/RE2.DataAccess/InMemory/InMemoryGdpEquipmentRepository.cs — ConcurrentDictionary pattern, Clone helper, SeedEquipment method. Follow InMemoryGdpInspectionRepository pattern.

- [X] T258 [P] [US11] Create DataverseGdpEquipmentRepository + GdpEquipmentQualificationDto in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpEquipmentRepository.cs and src/RE2.DataAccess/Dataverse/Models/GdpEquipmentQualificationDto.cs — entity name "phr_gdpequipmentqualification". Follow DataverseGdpInspectionRepository pattern.

- [X] T259 [US11] Update InMemorySeedData.cs — add SeedGdpEquipmentData method with 3 sample records (one qualified vehicle for a provider, one monitoring system at a site, one expired equipment). Add well-known IDs (84000000 range). Update SeedAll overload.

- [X] T260 [US11] Update InfrastructureExtensions.cs — register IGdpEquipmentRepository in both Dataverse and InMemory sections.

- [X] T261 [US11] Write GdpOperationalService tests in tests/RE2.ComplianceCore.Tests/Services/GdpOperationalServiceTests.cs — test ValidateSiteAssignmentAsync (FR-046: site must be GDP active with valid WDA coverage), ValidateProviderAssignmentAsync (FR-046: provider must have valid approved credentials), GetApprovedProvidersAsync (FR-047: filter by temp-controlled capability), GetEquipmentDueForRequalificationAsync (FR-048). Use in-memory repositories with seed data.

- [X] T262 [US11] Create IGdpOperationalService interface in src/RE2.ComplianceCore/Interfaces/IGdpOperationalService.cs and GdpOperationalService in src/RE2.ComplianceCore/Services/GdpCompliance/GdpOperationalService.cs — inject IGdpComplianceService, IGdpEquipmentRepository. Methods: ValidateSiteAssignmentAsync(string warehouseId, string dataAreaId) returns (bool IsAllowed, string Reason), ValidateProviderAssignmentAsync(Guid providerId) returns (bool IsAllowed, string Reason), GetApprovedProvidersAsync(bool? requireTempControl), GetApprovedRoutesAsync, GetAllEquipmentAsync, GetEquipmentAsync, CreateEquipmentAsync, UpdateEquipmentAsync, DeleteEquipmentAsync, GetEquipmentDueForRequalificationAsync.

**Checkpoint**: Build succeeds, T261 tests pass.

---

## Phase C: US11 API & Web UI

**Purpose**: API endpoints and web views for operational checks, equipment tracking, and dashboard.

### API

- [X] T263 [US11] Write GdpOperationsController tests in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpOperationsControllerTests.cs — test ValidateSiteAssignment (allowed/blocked), ValidateProviderAssignment (allowed/blocked), GetApprovedProviders (with/without temp filter), equipment CRUD. Use Mock<IGdpOperationalService>.

- [X] T264 [US11] Create GdpOperationsController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpOperationsController.cs — endpoints: POST validate/site-assignment (FR-046), POST validate/provider-assignment (FR-046), GET approved-providers?tempControlled=true (FR-047), GET approved-routes (FR-047), CRUD for equipment qualifications (FR-048). DTOs: SiteAssignmentValidationRequest/Response, ProviderAssignmentValidationRequest/Response, ApprovedProviderDto, EquipmentQualificationDto.

### Web UI

- [X] T265 [US11] Create GdpEquipmentController MVC in src/RE2.ComplianceWeb/Controllers/GdpEquipmentController.cs — inject IGdpOperationalService. Actions: Index (list all equipment with status badges), Details (equipment + provider/site info), Create GET/POST, Edit GET/POST, Delete POST. View models: EquipmentIndexViewModel, EquipmentCreateViewModel, EquipmentEditViewModel.

- [X] T266 [P] [US11] Create GdpEquipment views in src/RE2.ComplianceWeb/Views/GdpEquipment/ — Index.cshtml (table with status badges: Qualified=green, DueForRequalification=warning, Expired=danger; summary cards for total/qualified/due/expired), Details.cshtml (card layout), Create.cshtml (form with equipment type dropdown, provider/site selectors, date pickers), Edit.cshtml (same form pre-populated).

- [X] T267 [US11] Create GDP Operations dashboard in src/RE2.ComplianceWeb/Views/GdpOperations/Index.cshtml + GdpOperationsController MVC in src/RE2.ComplianceWeb/Controllers/GdpOperationsController.cs — dashboard showing: approved providers count (with temp-controlled filter), equipment qualification summary, site compliance status overview. Include quick-validate forms for site and provider assignments.

- [X] T268 [US11] Update _Layout.cshtml — add "GDP Equipment" and "Operations Dashboard" to GDP dropdown menu. Update InfrastructureExtensions for IGdpOperationalService DI registration.

**Checkpoint**: Build succeeds, all T263 tests pass, web views render.

---

## Phase D: US12 Domain Models

**Purpose**: Create GdpSop, GdpSiteSop, TrainingRecord, GdpChangeRecord domain models per data-model.md entities 23-26.

### Tests (Write First)

- [X] T269 [P] [US12] Write GdpSop model tests in tests/RE2.ComplianceCore.Tests/Models/GdpSopTests.cs — test Validate() for: missing SopNumber, missing Title, missing Version, invalid EffectiveDate, valid SOP. Follow GdpInspection test pattern.

- [X] T270 [P] [US12] Write TrainingRecord model tests in tests/RE2.ComplianceCore.Tests/Models/TrainingRecordTests.cs — test Validate() for: missing StaffMemberId, missing TrainingCurriculum, future CompletionDate, ExpiryDate before CompletionDate, valid record, IsExpired() helper.

- [X] T271 [P] [US12] Write GdpChangeRecord model tests in tests/RE2.ComplianceCore.Tests/Models/GdpChangeRecordTests.cs — test Validate() for: missing ChangeNumber, missing Description, valid record, IsPending(), IsApproved(), CanImplement() helpers.

### Implementation

- [X] T272 [P] [US12] Create GdpSop domain model in src/RE2.ComplianceCore/Models/GdpSop.cs — include GdpSopCategory enum (Returns, Recalls, Deviations, TemperatureExcursions, OutsourcedActivities, Other), properties (SopId, SopNumber, Title, Category, Version, EffectiveDate, DocumentUrl, IsActive), Validate() method. Dataverse: phr_gdpsop.

- [X] T273 [P] [US12] Create GdpSiteSop domain model in src/RE2.ComplianceCore/Models/GdpSiteSop.cs — join entity linking GdpSop to GdpSite. Properties: SiteSopId, SiteId (FK), SopId (FK). Dataverse: phr_gdpsitesop.

- [X] T274 [P] [US12] Create TrainingRecord domain model in src/RE2.ComplianceCore/Models/TrainingRecord.cs — include AssessmentResult enum (Pass, Fail, NotAssessed), properties (TrainingRecordId, StaffMemberId, StaffMemberName, TrainingCurriculum, SopId nullable FK, SiteId nullable FK, CompletionDate, ExpiryDate, TrainerName, AssessmentResult), Validate(), IsExpired(). Dataverse: phr_trainingrecord.

- [X] T275 [P] [US12] Create GdpChangeRecord domain model in src/RE2.ComplianceCore/Models/GdpChangeRecord.cs — include GdpChangeType enum (NewWarehouse, New3PL, NewProductType, StorageConditionChange, Other), ChangeApprovalStatus enum (Pending, Approved, Rejected), properties (ChangeRecordId, ChangeNumber, ChangeType, Description, RiskAssessment, ApprovalStatus, ApprovedBy, ApprovalDate, ImplementationDate, UpdatedDocumentationRefs, CreatedDate, ModifiedDate, RowVersion), Validate(), IsPending(), IsApproved(), CanImplement(). Dataverse: phr_gdpchangerecord.

**Checkpoint**: All models compile, T269-T271 tests pass.

---

## Phase E: US12 Data Access

**Purpose**: Repository interfaces and implementations for all US12 entities.

- [X] T276 [US12] Create IGdpSopRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpSopRepository.cs — methods: GetAllAsync, GetByIdAsync, GetByCategoryAsync, CreateAsync, UpdateAsync, DeleteAsync, GetSiteSopsAsync(Guid siteId), LinkSopToSiteAsync, UnlinkSopFromSiteAsync.

- [X] T277 [P] [US12] Create InMemoryGdpSopRepository in src/RE2.DataAccess/InMemory/InMemoryGdpSopRepository.cs — ConcurrentDictionary for SOPs and SiteSops, seed method.

- [X] T278 [P] [US12] Create DataverseGdpSopRepository + GdpSopDto + GdpSiteSopDto in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpSopRepository.cs and src/RE2.DataAccess/Dataverse/Models/GdpSopDto.cs.

- [X] T279 [US12] Create ITrainingRepository interface in src/RE2.ComplianceCore/Interfaces/ITrainingRepository.cs — methods: GetAllAsync, GetByIdAsync, GetByStaffAsync(Guid), GetBySiteAsync(Guid), GetBySopAsync(Guid), GetExpiredAsync, CreateAsync, UpdateAsync, DeleteAsync.

- [X] T280 [P] [US12] Create InMemoryTrainingRepository in src/RE2.DataAccess/InMemory/InMemoryTrainingRepository.cs.

- [X] T281 [P] [US12] Create DataverseTrainingRepository + TrainingRecordDto in src/RE2.DataAccess/Dataverse/Repositories/DataverseTrainingRepository.cs and src/RE2.DataAccess/Dataverse/Models/TrainingRecordDto.cs.

- [X] T282 [US12] Create IGdpChangeRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpChangeRepository.cs — methods: GetAllAsync, GetByIdAsync, GetPendingAsync, CreateAsync, UpdateAsync, ApproveAsync(Guid, Guid approvedBy), RejectAsync(Guid, Guid rejectedBy).

- [X] T283 [P] [US12] Create InMemoryGdpChangeRepository in src/RE2.DataAccess/InMemory/InMemoryGdpChangeRepository.cs.

- [X] T284 [P] [US12] Create DataverseGdpChangeRepository + GdpChangeRecordDto in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpChangeRepository.cs and src/RE2.DataAccess/Dataverse/Models/GdpChangeRecordDto.cs.

- [X] T285 [US12] Update InMemorySeedData.cs — add SeedGdpSopData (3 SOPs with site links), SeedTrainingData (3 training records), SeedGdpChangeData (2 change records: 1 pending, 1 approved). Add well-known IDs (85000000, 86000000, 87000000 ranges). Update SeedAll overload.

- [X] T286 [US12] Update InfrastructureExtensions.cs — register all US12 repositories in both Dataverse and InMemory sections.

**Checkpoint**: All repositories compile, DI wiring complete.

---

## Phase F: US12 Service & API Layer

**Purpose**: Extend service layer and create API endpoints for SOPs, training, and change control.

- [X] T287 [US12] Extend IGdpComplianceService and GdpComplianceService — add #region GDP SOPs, #region Training Records, #region Change Control with CRUD methods + approval workflow for change records. Inject IGdpSopRepository, ITrainingRepository, IGdpChangeRepository.

- [X] T288 [US12] Write API controller tests in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpSopsControllerTests.cs and tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpChangeControllerTests.cs — test CRUD for SOPs, site linking, change record creation, approval/rejection workflow.

- [X] T289 [US12] Create GdpSopsController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpSopsController.cs — endpoints: CRUD for SOPs, GET sops/{id}/sites (linked sites), POST sops/{id}/sites/{siteId} (link), DELETE sops/{id}/sites/{siteId} (unlink). DTOs: GdpSopResponseDto, CreateGdpSopRequestDto.

- [X] T290 [US12] Create GdpChangeControlController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpChangeControlController.cs — endpoints: CRUD for change records, POST changes/{id}/approve, POST changes/{id}/reject. DTOs: GdpChangeRecordResponseDto, CreateChangeRecordRequestDto.

**Checkpoint**: API endpoints compile, T288 tests pass.

---

## Phase G: US12 Web UI

**Purpose**: MVC controllers and views for SOPs, training records, and change control management.

- [X] T291 [US12] Create GdpSopsController MVC in src/RE2.ComplianceWeb/Controllers/GdpSopsController.cs — actions: Index, Details (SOP + linked sites), Create GET/POST, Edit GET/POST, LinkSite POST, UnlinkSite POST. View models: SopIndexViewModel, SopCreateViewModel, SopEditViewModel.

- [X] T292 [P] [US12] Create GdpSops views in src/RE2.ComplianceWeb/Views/GdpSops/ — Index.cshtml (table with category badges, active filter), Details.cshtml (SOP details + linked sites table with link/unlink), Create.cshtml (form with category dropdown), Edit.cshtml.

- [X] T293 [US12] Create TrainingController MVC in src/RE2.ComplianceWeb/Controllers/TrainingController.cs — actions: Index (all records), StaffReport(Guid staffId) (records for one person), Create GET/POST. View models: TrainingIndexViewModel, TrainingCreateViewModel, StaffTrainingReportViewModel. [Authorize(Roles = "QAUser,TrainingCoordinator")].

- [X] T294 [P] [US12] Create Training views in src/RE2.ComplianceWeb/Views/Training/ — Index.cshtml (table with assessment result badges, expiry warnings), StaffReport.cshtml (per-staff view), Create.cshtml (form with SOP dropdown, site dropdown, assessment result, dates).

- [X] T295 [US12] Create ChangeControlController MVC in src/RE2.ComplianceWeb/Controllers/ChangeControlController.cs — actions: Index (all changes with status filter), Details, Create GET/POST, Approve POST, Reject POST. View models: ChangeIndexViewModel, ChangeCreateViewModel. [Authorize(Roles = "QAUser,ComplianceManager")] for approval actions.

- [X] T296 [P] [US12] Create ChangeControl views in src/RE2.ComplianceWeb/Views/ChangeControl/ — Index.cshtml (table with approval status badges, pending count), Details.cshtml (change details + risk assessment + approval actions), Create.cshtml (form with change type dropdown, risk assessment textarea).

- [X] T297 [US12] Update _Layout.cshtml navigation — add "GDP SOPs", "Training Records", and "Change Control" links under appropriate menu sections. Configure authorization for TrainingCoordinator role.

**Checkpoint**: All web views render, forms work, navigation links present.

---

## Phase H: Polish & Cross-Cutting

- [X] T298 [US11+US12] Verify all tests pass — run full test suite (`dotnet test RE2.sln`) and confirm 0 regressions.

- [X] T299 [US11+US12] Update specs/001-licence-management/tasks.md — mark Phase 13 (US11) and Phase 14 (US12) tasks as complete.

**Checkpoint**: Full build clean, all tests pass, US11+US12 complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase A (US11 Model)**: No dependencies — start immediately
- **Phase B (US11 Data+Service)**: Depends on Phase A
- **Phase C (US11 API+Web)**: Depends on Phase B
- **Phase D (US12 Models)**: No dependencies — can run in parallel with Phases A-C
- **Phase E (US12 Data)**: Depends on Phase D
- **Phase F (US12 Service+API)**: Depends on Phase E
- **Phase G (US12 Web)**: Depends on Phase F
- **Phase H (Polish)**: Depends on all

### Critical Path

```text
Phase A → Phase B → Phase C → Phase H
Phase D → Phase E → Phase F → Phase G → Phase H
```

---

## Task Count Summary

| Phase | Tasks | Story |
|-------|-------|-------|
| A: US11 Domain Model | 2 (T254-T255) | US11 |
| B: US11 Data+Service | 7 (T256-T262) | US11 |
| C: US11 API+Web | 6 (T263-T268) | US11 |
| D: US12 Domain Models | 8 (T269-T275) | US12 |
| E: US12 Data Access | 11 (T276-T286) | US12 |
| F: US12 Service+API | 4 (T287-T290) | US12 |
| G: US12 Web UI | 7 (T291-T297) | US12 |
| H: Polish | 2 (T298-T299) | Both |
| **Total** | **47** | |
