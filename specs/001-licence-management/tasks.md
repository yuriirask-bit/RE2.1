# Tasks: Controlled Drug Licence & GDP Compliance Management System

**Feature**: 001-licence-management
**Branch**: `001-licence-management`
**Generated**: 2026-01-12

**Input**: Design documents from `/specs/001-licence-management/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: Tests are explicitly requested in the plan (TDD per Principle II). Test tasks are included and MUST be written before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

## Path Conventions

Project uses multi-project .NET solution structure per plan.md:
- Core logic: `src/RE2.ComplianceCore/`
- Data access: `src/RE2.DataAccess/`
- Web API: `src/RE2.ComplianceApi/`
- Web UI: `src/RE2.ComplianceWeb/`
- Functions: `src/RE2.ComplianceFunctions/`
- Shared: `src/RE2.Shared/`
- Tests: `tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create .NET 8 solution structure with projects per plan.md section "Project Structure"
- [X] T002 [P] Initialize RE2.ComplianceCore class library (.NET 8) in src/RE2.ComplianceCore/
- [X] T003 [P] Initialize RE2.DataAccess class library (.NET 8) in src/RE2.DataAccess/
- [X] T004 [P] Initialize RE2.ComplianceApi web API (.NET 8 ASP.NET Core) in src/RE2.ComplianceApi/
- [X] T005 [P] Initialize RE2.ComplianceWeb web UI (.NET 8 ASP.NET Core MVC) in src/RE2.ComplianceWeb/
- [X] T006 [P] Initialize RE2.ComplianceFunctions (Azure Functions .NET 8 Isolated) in src/RE2.ComplianceFunctions/
- [X] T007 [P] Initialize RE2.Shared class library (.NET 8) in src/RE2.Shared/
- [X] T008 [P] Initialize xUnit test projects in tests/ per plan.md structure
- [X] T009 Add NuGet packages to RE2.ComplianceCore (no external dependencies per library-first) - N/A per library-first principle
- [X] T010 [P] Add NuGet packages to RE2.DataAccess (Microsoft.PowerPlatform.Dataverse.Client, Azure.Identity, System.Net.Http.Json)
- [X] T011 [P] Add NuGet packages to RE2.ComplianceApi (ASP.NET Core, Asp.Versioning.Mvc, Microsoft.Identity.Web, Microsoft.Extensions.Http.Resilience)
- [X] T012 [P] Add NuGet packages to RE2.ComplianceWeb (ASP.NET Core MVC, Microsoft.Identity.Web)
- [X] T013 [P] Add NuGet packages to RE2.ComplianceFunctions (Microsoft.Azure.Functions.Worker, Azure.Identity)
- [X] T014 [P] Add test framework packages (xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers)
- [X] T015 Configure project references: ComplianceApi → ComplianceCore, DataAccess, Shared
- [X] T016 Configure project references: ComplianceWeb → ComplianceCore, DataAccess, Shared
- [X] T017 Configure project references: ComplianceFunctions → ComplianceCore, DataAccess, Shared
- [X] T018 [P] Setup .editorconfig for C# formatting and linting rules
- [X] T019 [P] Create appsettings.json templates per quickstart.md in RE2.ComplianceApi and RE2.ComplianceWeb
- [X] T020 [P] Create local.settings.json template per quickstart.md in RE2.ComplianceFunctions

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Authentication & Authorization Foundation

- [X] T021 Configure Azure AD authentication in RE2.ComplianceApi per research.md section 6 (stateless JWT)
- [X] T022 Configure Azure AD B2C authentication in RE2.ComplianceApi for external users per research.md section 6
- [X] T023 Configure multiple authentication schemes (AzureAd + AzureAdB2C) in RE2.ComplianceApi Program.cs
- [X] T024 Configure authorization policies (InternalUsers, ExternalUsers, AnyUser) in RE2.ComplianceApi
- [X] T025 Configure Azure AD authentication in RE2.ComplianceWeb Program.cs for compliance UI users
- [X] T026 Create User model in src/RE2.Shared/Constants/ with roles (ComplianceManager, QAUser, SalesAdmin, etc.) per data-model.md entity 28

### External System Integration Foundation

- [X] T027 Create IDataverseClient interface in src/RE2.ComplianceCore/Interfaces/IDataverseClient.cs
- [X] T028 Create ID365FoClient interface in src/RE2.ComplianceCore/Interfaces/ID365FoClient.cs
- [X] T029 Create IDocumentStorage interface in src/RE2.ComplianceCore/Interfaces/IDocumentStorage.cs
- [X] T030 Implement DataverseClient with ServiceClient and Managed Identity in src/RE2.DataAccess/Dataverse/DataverseClient.cs per research.md section 1
- [X] T031 Implement D365FoClient with HttpClient and OAuth2 in src/RE2.DataAccess/D365FinanceOperations/D365FoClient.cs per research.md section 2
- [X] T032 Implement DocumentStorageClient with Azure Blob Storage SDK in src/RE2.DataAccess/BlobStorage/DocumentStorageClient.cs
- [X] T033 Configure standard resilience handler for DataverseClient in RE2.DataAccess per research.md section 4 (retry, circuit breaker, timeout)
- [X] T034 Configure standard resilience handler for D365FoClient in RE2.DataAccess per research.md section 4
- [X] T035 Create InfrastructureExtensions.AddDataverseServices() in src/RE2.DataAccess/DependencyInjection/InfrastructureExtensions.cs per research.md section 3
- [X] T036 Create InfrastructureExtensions.AddD365FOServices() in src/RE2.DataAccess/DependencyInjection/InfrastructureExtensions.cs
- [X] T037 Create InfrastructureExtensions.AddBlobStorageServices() in src/RE2.DataAccess/DependencyInjection/InfrastructureExtensions.cs
- [X] T038 Register DI services in RE2.ComplianceApi Program.cs using extension methods
- [X] T039 Register DI services in RE2.ComplianceWeb Program.cs using extension methods
- [X] T040 Register DI services in RE2.ComplianceFunctions Program.cs using extension methods

### API Infrastructure Foundation

- [X] T041 Configure API versioning with Asp.Versioning.Mvc in RE2.ComplianceApi per research.md section 5 (URL path versioning)
- [X] T042 Configure Swagger/OpenAPI for v1 API documentation in RE2.ComplianceApi
- [X] T043 Configure OAuth2 security in Swagger for testing in RE2.ComplianceApi per research.md section 6
- [X] T044 Create error handling middleware in src/RE2.ComplianceApi/Middleware/ErrorHandlingMiddleware.cs
- [X] T045 Create request logging middleware in src/RE2.ComplianceApi/Middleware/RequestLoggingMiddleware.cs
- [X] T046 Configure Application Insights telemetry in RE2.ComplianceApi and RE2.ComplianceFunctions
- [X] T047 Create standardized error response DTOs in src/RE2.Shared/Models/ per transaction-validation-api.yaml ErrorResponse schema
- [X] T047g Implement health check endpoints (/health, /ready) in RE2.ComplianceApi per FR-056 with Dataverse/D365/Blob connectivity checks, degraded state detection for non-critical features per FR-054
- [X] T047h Implement graceful degradation middleware in src/RE2.ComplianceApi/Middleware/GracefulDegradationMiddleware.cs per FR-054/FR-055: categorize endpoints as critical (transaction validation, customer lookup, warehouse validation) or non-critical (reports, dashboards, document upload, workflow approvals); return HTTP 503 with Retry-After: 300 header for non-critical endpoints when dependent services (Dataverse, D365 F&O) are detected as unavailable via health check status

### Integration System Foundation (FR-061, data-model.md entity 27)

- [X] T047a [P] Create IntegrationSystem domain model in src/RE2.ComplianceCore/Models/IntegrationSystem.cs per data-model.md entity 27
- [X] T047b [P] Create IntegrationSystem DTO for Dataverse in src/RE2.DataAccess/D365FinanceOperations/Models/IntegrationSystemDto.cs
- [X] T047c Create IIntegrationSystemRepository interface in src/RE2.ComplianceCore/Interfaces/IIntegrationSystemRepository.cs
- [X] T047d Implement DataverseIntegrationSystemRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseIntegrationSystemRepository.cs
- [X] T047e Create IntegrationSystemsController v1 in src/RE2.ComplianceApi/Controllers/V1/IntegrationSystemsController.cs for managing API client registrations per data-model.md entity 27
- [X] T047f Configure authorization: only SystemAdmin role can manage integration system registrations

**Note**: T256e (extend TransactionValidationController to record calling system identity) remains in Phase 6 User Story 4 as it depends on TransactionValidationController implementation.

### Shared Domain Foundation

- [X] T048 [P] Create ErrorCodes constants in src/RE2.Shared/Constants/ErrorCodes.cs per FR-064 (LICENCE_EXPIRED, LICENCE_MISSING, etc.)
- [X] T049 [P] Create LicenceTypes constants in src/RE2.Shared/Constants/LicenceTypes.cs
- [X] T050 [P] Create SubstanceCategories constants in src/RE2.Shared/Constants/SubstanceCategories.cs
- [X] T051 [P] Create DateTimeExtensions in src/RE2.Shared/Extensions/DateTimeExtensions.cs
- [X] T052 [P] Create ValidationResult value object in src/RE2.ComplianceCore/Models/ValidationResult.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

### CLI Interface (Constitution Principle IV) ⚠️ REQUIRED FOR COMPLIANCE

**Priority**: Should complete alongside or immediately after Phase 2 Foundational tasks to achieve constitution compliance. Not blocking user story implementation but required for final sign-off.

- [X] T052a Create RE2.ComplianceCli console application (.NET 8) in src/RE2.ComplianceCli/ with CommandLineParser NuGet package
- [X] T052b [P] Implement `validate-transaction` command accepting JSON via stdin, returning ValidationResult to stdout per Constitution Principle IV
- [X] T052c [P] Implement `lookup-customer` command accepting customer ID via args, returning compliance status JSON to stdout
- [X] T052d [P] Implement `lookup-licence` command accepting licence number via args, returning licence details JSON to stdout
- [X] T052e [P] Implement `generate-report` command accepting report type and filters via args, returning report data to stdout
- [X] T052f Configure RE2.ComplianceCli project references to RE2.ComplianceCore and RE2.DataAccess (same dependencies as API layer)
- [X] T052g Add CLI to solution and configure build pipeline to produce standalone executable
- [X] T052h [P] Create CLI integration tests in tests/RE2.ComplianceCli.Tests/ verifying stdin/stdout protocol
- [X] T052i Update quickstart.md with CLI usage examples and stdin/stdout protocol documentation

**Constitution Compliance Note**: Tasks T052a-T052i implement Constitution Principle IV (CLI Interface Requirement). While Web APIs are the primary interface (justified violation in plan.md), completing these CLI tasks achieves full constitution compliance by providing text I/O protocol for debugging and scripting. These tasks SHOULD be completed before final constitution sign-off.


## Phase 3: User Story 1 - Manage Legal Licence Requirements (Priority: P1) 🎯 MVP

**Goal**: System stores and manages licence types (wholesale licence, Opium Act exemptions, permits, etc.) with activities and substance mappings, enabling transaction validation for legal authorization.

**Independent Test**: Create, configure, and maintain licence types with permitted activities (possess, store, distribute, etc.), map to controlled substances (Opium Act Lists I/II), and verify data retrieval accuracy.

### Tests for User Story 1 (TDD - Write First)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T053 [P] [US1] Unit tests for LicenceType model in tests/RE2.ComplianceCore.Tests/Models/LicenceTypeTests.cs
- [X] T054 [P] [US1] Unit tests for ControlledSubstance model in tests/RE2.ComplianceCore.Tests/Models/ControlledSubstanceTests.cs including validation rule: at least one of OpiumActList or PrecursorCategory must be specified (not both None) per data-model.md entity 3
- [X] T055 [P] [US1] Unit tests for LicenceSubstanceMapping model in tests/RE2.ComplianceCore.Tests/Models/LicenceSubstanceMappingTests.cs
- [X] T056 [P] [US1] Unit tests for Licence model in tests/RE2.ComplianceCore.Tests/Models/LicenceTests.cs
- [X] T057 [US1] Contract tests for Dataverse LicenceType entity in tests/RE2.Contract.Tests/DataverseLicenceTypeContractTests.cs
- [X] T058 [US1] Contract tests for Dataverse ControlledSubstance entity in tests/RE2.Contract.Tests/DataverseControlledSubstanceContractTests.cs
- [X] T059 [US1] Integration tests for GET /api/v1/licences in tests/RE2.ComplianceApi.Tests/Controllers/V1/LicencesControllerTests.cs

### Implementation for User Story 1

- [X] T060 [P] [US1] Create LicenceType domain model in src/RE2.ComplianceCore/Models/LicenceType.cs per data-model.md entity 2
- [X] T061 [P] [US1] Create ControlledSubstance domain model in src/RE2.ComplianceCore/Models/ControlledSubstance.cs per data-model.md entity 3
- [X] T062 [P] [US1] Create LicenceSubstanceMapping domain model in src/RE2.ComplianceCore/Models/LicenceSubstanceMapping.cs per data-model.md entity 4
- [X] T063 [US1] Create Licence domain model in src/RE2.ComplianceCore/Models/Licence.cs per data-model.md entity 1 (depends on T060)
- [X] T064 [P] [US1] Create LicenceType DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/LicenceTypeDto.cs
- [X] T065 [P] [US1] Create ControlledSubstance DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/ControlledSubstanceDto.cs
- [X] T066 [P] [US1] Create LicenceSubstanceMapping DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/LicenceSubstanceMappingDto.cs
- [X] T067 [US1] Create Licence DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/LicenceDto.cs
- [X] T068 [US1] Create ILicenceRepository interface in src/RE2.ComplianceCore/Interfaces/ILicenceRepository.cs
- [X] T069 [US1] Implement DataverseLicenceRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseLicenceRepository.cs with CRUD operations
- [X] T070 [US1] Create ILicenceTypeRepository interface in src/RE2.ComplianceCore/Interfaces/ILicenceTypeRepository.cs
- [X] T071 [US1] Implement DataverseLicenceTypeRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseLicenceTypeRepository.cs

### Licence Type Management (FR-001, FR-002)

- [X] T071a [US1] Create LicenceTypesController v1 in src/RE2.ComplianceApi/Controllers/V1/LicenceTypesController.cs with GET, POST, PUT, DELETE endpoints per FR-001
- [X] T071b [P] [US1] Create licence types web UI views in src/RE2.ComplianceWeb/Views/LicenceTypes/ (Index, Create, Edit, Details views) per FR-001
- [X] T071c [US1] Create LicenceTypesController for web UI in src/RE2.ComplianceWeb/Controllers/LicenceTypesController.cs
- [X] T071d [US1] Integration tests for LicenceTypesController API in tests/RE2.ComplianceApi.Tests/Controllers/V1/LicenceTypesControllerTests.cs
- [X] T071e [US1] Seed default licence types in InMemoryLicenceTypeRepository covering all FR-001 categories (Opium Act exemption, WDA, GDP certificate, import/export permits, pharmacy licences, manufacturer licence, precursor registration)
- [X] T071f [US1] Verify PermittedActivities flags support all FR-002 activities (possess, store, distribute, import, export, manufacture, handle precursors) with unit tests

- [X] T072 [US1] Create IControlledSubstanceRepository interface in src/RE2.ComplianceCore/Interfaces/IControlledSubstanceRepository.cs
- [X] T073 [US1] Implement DataverseControlledSubstanceRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseControlledSubstanceRepository.cs

### Controlled Substance Master List Management (FR-003)

- [X] T073a [US1] Create IControlledSubstanceService interface in src/RE2.ComplianceCore/Interfaces/IControlledSubstanceService.cs with CRUD and validation methods
- [X] T073b [US1] Implement ControlledSubstanceService in src/RE2.ComplianceCore/Services/SubstanceManagement/ControlledSubstanceService.cs with validation logic (unique InternalCode, valid OpiumActList/PrecursorCategory combinations)
- [X] T073c [US1] Create ControlledSubstancesController v1 in src/RE2.ComplianceApi/Controllers/V1/ControlledSubstancesController.cs with GET, POST, PUT, DELETE endpoints for substance master list management
- [X] T073d [P] [US1] Create controlled substances web UI views in src/RE2.ComplianceWeb/Views/Substances/ (Index, Create, Edit, Details views) per FR-003
- [X] T073e [US1] Create SubstancesController for web UI in src/RE2.ComplianceWeb/Controllers/SubstancesController.cs
- [X] T073f [US1] Integration tests for ControlledSubstances API in tests/RE2.ComplianceApi.Tests/Controllers/V1/ControlledSubstancesControllerTests.cs
- [X] T073g [US1] Unit tests for ControlledSubstanceService in tests/RE2.ComplianceCore.Tests/Services/ControlledSubstanceServiceTests.cs
- [X] T073h [US1] Configure route authorization: only ComplianceManager role can create/modify controlled substances per FR-031

- [X] T074 [US1] Create LicenceService in src/RE2.ComplianceCore/Services/LicenceValidation/LicenceService.cs with business logic for licence management
- [X] T075 [US1] Create LicenceController v1 in src/RE2.ComplianceApi/Controllers/V1/LicencesController.cs with GET, POST, PUT, DELETE endpoints
- [X] T076 [US1] Create licence management UI in src/RE2.ComplianceWeb/Views/Licences/ (Index, Create, Edit, Details views)
- [X] T077 [US1] Create LicencesController for web UI in src/RE2.ComplianceWeb/Controllers/LicencesController.cs
- [X] T078 [US1] Add validation for licence type permitted activities mapping per data-model.md validation rules
- [X] T079 [US1] Add validation for substance-to-licence-type mappings per data-model.md validation rules
- [X] T080 [US1] Configure route authorization: only ComplianceManager role can create/modify licence types

### Licence-to-Substance Mappings (FR-004)

- [X] T079a [US1] Create ILicenceSubstanceMappingRepository interface in src/RE2.ComplianceCore/Interfaces/ILicenceSubstanceMappingRepository.cs with CRUD and GetByLicenceId methods
- [X] T079b [US1] Implement DataverseLicenceSubstanceMappingRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseLicenceSubstanceMappingRepository.cs
- [X] T079c [US1] Create ILicenceSubstanceMappingService interface in src/RE2.ComplianceCore/Interfaces/ILicenceSubstanceMappingService.cs with validation logic
- [X] T079d [US1] Implement LicenceSubstanceMappingService in src/RE2.ComplianceCore/Services/LicenceValidation/LicenceSubstanceMappingService.cs with mapping validation per data-model.md (ExpiryDate ≤ Licence.ExpiryDate)
- [X] T079e [US1] Create LicenceSubstanceMappingsController v1 in src/RE2.ComplianceApi/Controllers/V1/LicenceSubstanceMappingsController.cs with GET, POST, PUT, DELETE endpoints per FR-004
- [X] T079f [P] [US1] Create substance mappings web UI partial view in src/RE2.ComplianceWeb/Views/Licences/_SubstanceMappings.cshtml for managing mappings within licence details
- [X] T079g [US1] Integration tests for LicenceSubstanceMappingsController API in tests/RE2.ComplianceApi.Tests/Controllers/V1/LicenceSubstanceMappingsControllerTests.cs
- [X] T079h [US1] Unit tests for LicenceSubstanceMappingService in tests/RE2.ComplianceCore.Tests/Services/LicenceSubstanceMappingServiceTests.cs
- [X] T079i [US1] Contract tests for Dataverse LicenceSubstanceMapping entity in tests/RE2.Contract.Tests/DataverseLicenceSubstanceMappingContractTests.cs

### Substance Reclassification Support (FR-066)

- [x] T080a [P] [US1] Create SubstanceReclassification domain model in src/RE2.ComplianceCore/Models/SubstanceReclassification.cs per FR-066
- [x] T080b [P] [US1] Create SubstanceReclassificationDto for Dataverse in src/RE2.DataAccess/Dataverse/Models/SubstanceReclassificationDto.cs
- [x] T080c [US1] Extend ControlledSubstance model with effective date tracking for classification changes
- [x] T080d [US1] Create ISubstanceReclassificationRepository interface in src/RE2.ComplianceCore/Interfaces/ISubstanceReclassificationRepository.cs
- [x] T080e [US1] Implement DataverseSubstanceReclassificationRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseSubstanceReclassificationRepository.cs
- [x] T080f [US1] Create SubstanceReclassificationService in src/RE2.ComplianceCore/Services/LicenceValidation/SubstanceReclassificationService.cs with customer impact analysis per FR-066
- [x] T080g [US1] Implement reclassification workflow: record new classification with effective date, identify affected customers, validate licence sufficiency
- [x] T080h [US1] Create SubstanceReclassificationController v1 in src/RE2.ComplianceApi/Controllers/V1/SubstanceReclassificationController.cs with POST /api/v1/substances/{id}/reclassify endpoint
- [x] T080i [US1] Create reclassification UI in src/RE2.ComplianceWeb/Views/Reclassifications/ with regulatory authority reference, effective date, affected customers preview
- [x] T080j [US1] Implement customer flagging logic per FR-066: set "Requires Re-Qualification" status for customers with insufficient licences
- [x] T080k [US1] Implement compliance team notification generation listing affected customers and required actions
- [x] T080l [US1] Extend TransactionComplianceService to prevent transactions with reclassified substances for flagged customers until licences updated per FR-066
- [x] T080m [US1] Add historical transaction validation support: maintain transaction compliance under classification at time of transaction per FR-066
- [x] T080n [P] [US1] Unit tests for SubstanceReclassificationService in tests/RE2.ComplianceCore.Tests/Services/SubstanceReclassificationServiceTests.cs
- [x] T080o [US1] Integration tests for reclassification API in tests/RE2.ComplianceApi.Tests/Controllers/V1/SubstanceReclassificationControllerTests.cs


**Checkpoint**: At this point, User Story 1 should be fully functional - compliance managers can manage licence types, controlled substances, and mappings via API and web UI

---

## Phase 4: User Story 2 - Customer Onboarding & Qualification (Priority: P1)

**Goal**: Create customer profiles capturing legal status and record all licences (wholesale, pharmacy, exemptions, permits) with verification details, enabling customer qualification and preventing sales to unauthorized entities.

**Independent Test**: Create customer profiles for different entity types (hospital, pharmacy, veterinarian, etc.), record licences with full details, set required licence types per role/country, and verify system flags missing or invalid licences.

### Tests for User Story 2 (TDD - Write First)

- [X] T081 [P] [US2] Unit tests for Customer model in tests/RE2.ComplianceCore.Tests/Models/CustomerTests.cs
- [X] T082 [P] [US2] Unit tests for QualificationReview model in tests/RE2.ComplianceCore.Tests/Models/QualificationReviewTests.cs
- [X] T083 [US2] Contract tests for Customer composite model: D365FoCustomerContractTests.cs (CustomersV3 OData entity) and DataverseCustomerComplianceExtensionContractTests.cs (phr_customercomplianceextension) in tests/RE2.Contract.Tests/
- [X] T084 [US2] Integration tests for GET /api/v1/customers/{id}/compliance-status in tests/RE2.ComplianceApi.Tests/Controllers/V1/CustomersControllerTests.cs per transaction-validation-api.yaml

### Implementation for User Story 2

- [X] T085 [P] [US2] Create Customer composite domain model in src/RE2.ComplianceCore/Models/Customer.cs per data-model.md entity 5 (D365 F&O CustomersV3 master data + Dataverse phr_customercomplianceextension compliance extensions, keyed by CustomerAccount + DataAreaId)
- [X] T086 [P] [US2] Create QualificationReview domain model in src/RE2.ComplianceCore/Models/QualificationReview.cs per data-model.md entity 29
- [X] T087 [P] [US2] Create Customer DTOs: D365FoCustomerDto in src/RE2.DataAccess/D365FinanceOperations/Models/D365FoCustomerDto.cs (read-only master data from CustomersV3) and CustomerComplianceExtensionDto in src/RE2.DataAccess/Dataverse/Models/CustomerComplianceExtensionDto.cs (compliance extensions from phr_customercomplianceextension)
- [X] T088 [US2] Create ICustomerRepository interface in src/RE2.ComplianceCore/Interfaces/ICustomerRepository.cs
- [X] T089 [US2] Implement CustomerRepository combining D365FoCustomerRepository (read-only master data from CustomersV3) and DataverseCustomerComplianceExtensionRepository (compliance extensions from phr_customercomplianceextension)
- [X] T090 [US2] Create CustomerService in src/RE2.ComplianceCore/Services/CustomerQualification/CustomerService.cs with qualification logic
- [X] T091 [US2] Create CustomersController v1 in src/RE2.ComplianceApi/Controllers/V1/CustomersController.cs with customer compliance status endpoint per transaction-validation-api.yaml
- [X] T092 [US2] Create customer management UI in src/RE2.ComplianceWeb/Views/Customers/ (Browse, Configure, Edit, Details views) - Browse.cshtml for browsing D365 F&O customers, Configure.cshtml for configuring compliance extensions
- [X] T093 [US2] Create CustomersController for web UI in src/RE2.ComplianceWeb/Controllers/CustomersController.cs
- [X] T094 [US2] Extend LicencesController web UI to support associating licences with customers
- [X] T095 [US2] Implement customer approval status validation per data-model.md validation rules (FR-016: prevent approval if required licences missing)
- [X] T096 [US2] Implement suspension logic per data-model.md (isSuspended blocks all transactions regardless of approval status)
- [X] T097 [US2] Add re-verification due date tracking and alert generation per FR-017
- [X] T097a [US2] Create PendingReVerification action in CustomersController web UI for dashboard integration per FR-017
- [X] T097b [US2] Create ReVerificationDue.cshtml view with filtering by days ahead, urgency indicators, and action buttons
- [X] T098 [US2] Configure route authorization: SalesAdmin and ComplianceManager can create/modify customers

**Checkpoint**: At this point, User Stories 1 AND 2 work independently - sales can onboard customers, record licences, and see compliance status

---

## Phase 5: User Story 3 - Licence Capture, Verification & Maintenance (Priority: P1)

**Goal**: Upload licence documents (PDFs, letters), record verification methods (authority website, email confirmation, Farmatec) with dates and verifiers, manage scope changes with effective dates, and maintain complete traceability for audit defense.

**Independent Test**: Upload various licence documents, record different verification methods with audit trails, set up expiry reminders (90/60/30 days), record scope changes over time, and verify all evidence is retrievable for audits.

### Tests for User Story 3 (TDD - Write First)

- [X] T099 [P] [US3] Unit tests for LicenceDocument model in tests/RE2.ComplianceCore.Tests/Models/LicenceDocumentTests.cs
- [X] T100 [P] [US3] Unit tests for LicenceVerification model in tests/RE2.ComplianceCore.Tests/Models/LicenceVerificationTests.cs
- [X] T101 [P] [US3] Unit tests for LicenceScopeChange model in tests/RE2.ComplianceCore.Tests/Models/LicenceScopeChangeTests.cs
- [X] T102 [US3] Integration tests for document upload in tests/RE2.ComplianceApi.Tests/Controllers/V1/LicencesControllerDocumentTests.cs
- [X] T103 [US3] Integration tests for Azure Blob Storage in tests/RE2.DataAccess.Tests/BlobStorage/DocumentStorageClientTests.cs

### Implementation for User Story 3

- [X] T104 [P] [US3] Create LicenceDocument domain model in src/RE2.ComplianceCore/Models/LicenceDocument.cs per data-model.md entity 12
- [X] T105 [P] [US3] Create LicenceVerification domain model in src/RE2.ComplianceCore/Models/LicenceVerification.cs per data-model.md entity 13 with properties: verificationMethod (enum: AuthorityWebsite, EmailConfirmation, FarmatecDatabase, PhysicalDocumentReview), verificationDate, verifierName, outcome, notes
- [X] T106 [P] [US3] Create LicenceScopeChange domain model in src/RE2.ComplianceCore/Models/LicenceScopeChange.cs per data-model.md entity 14
- [X] T107 [P] [US3] Create Alert domain model in src/RE2.ComplianceCore/Models/Alert.cs per data-model.md entity 11
- [X] T108 [P] [US3] Create LicenceDocument DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/LicenceDocumentDto.cs
- [X] T108a [P] [US3] Create LicenceVerification DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/LicenceVerificationDto.cs
- [X] T108b [US3] Extend ILicenceRepository interface with GetVerificationHistory(licenceId) method
- [X] T108c [US3] Implement verification history queries in DataverseLicenceRepository

- [X] T109 [P] [US3] Create Alert DTO for D365 F&O in src/RE2.DataAccess/D365FinanceOperations/Models/AlertDto.cs
- [X] T110 [US3] Extend ILicenceRepository with document upload methods
- [X] T111 [US3] Implement document upload in DataverseLicenceRepository using IDocumentStorage
- [X] T112 [US3] Extend LicenceService with verification recording methods (FR-009: method, date, verifier)
- [X] T113 [US3] Extend LicenceService with scope change recording methods (FR-010: effective dates)
- [X] T114 [US3] Extend LicencesController v1 API with POST /api/v1/licences/{id}/documents endpoint
- [X] T115 [US3] Extend LicencesController v1 API with POST /api/v1/licences/{id}/verifications endpoint
- [X] T116 [US3] Extend LicencesController v1 API with POST /api/v1/licences/{id}/scope-changes endpoint
- [X] T117 [US3] Add file upload UI in src/RE2.ComplianceWeb/Views/Licences/UploadDocument.cshtml
- [X] T118 [US3] Add verification recording UI in src/RE2.ComplianceWeb/Views/Licences/RecordVerification.cshtml
- [X] T119 [US3] Add scope change history UI in src/RE2.ComplianceWeb/Views/Licences/ScopeHistory.cshtml
- [X] T120 [US3] Create unified AlertGenerationService in src/RE2.ComplianceCore/Services/AlertGeneration/AlertGenerationService.cs supporting multiple entity types (Licence, GdpCredential, Customer re-verification)
- [X] T121 [US3] Create LicenceExpiryMonitor Azure Function in src/RE2.ComplianceFunctions/LicenceExpiryMonitor.cs (timer trigger, daily at 2 AM) using AlertGenerationService per FR-007: 90/60/30 day warnings for own company licences, 60/30 day warnings for customer licences (thresholds configurable per licence type)
- [X] T122 [US3] Add alert display dashboard in src/RE2.ComplianceWeb/Views/Dashboard/Index.cshtml

**Checkpoint**: At this point, User Stories 1-3 are complete - full licence lifecycle management with documents, verification, and expiry monitoring

---

## Phase 6: User Story 4 - Order & Shipment Transaction Checks (Priority: P2)

**Goal**: Automatically check at order entry and shipment release whether customer holds all required valid licences for each controlled product, including import/export permits for cross-border shipments, blocking or flagging non-compliant orders before warehouse processing.

**Independent Test**: Enter orders with various combinations (valid licences, expired, missing exemptions, cross-border shipments), set quantity/frequency thresholds, verify system correctly blocks/flags non-compliant orders with clear error messages per transaction-validation-api.yaml examples.

### Tests for User Story 4 (TDD - Write First)

- [X] T123 [P] [US4] Unit tests for TransactionComplianceService in tests/RE2.ComplianceCore.Tests/Services/TransactionComplianceServiceTests.cs
- [X] T124 [P] [US4] Unit tests for Transaction model in tests/RE2.ComplianceCore.Tests/Models/TransactionTests.cs
- [X] T125 [P] [US4] Unit tests for Threshold model in tests/RE2.ComplianceCore.Tests/Models/ThresholdTests.cs
- [X] T126 [US4] Contract tests for POST /api/v1/transactions/validate in tests/RE2.Contract.Tests/TransactionValidationContractTests.cs per transaction-validation-api.yaml
- [X] T127 [US4] Integration tests for transaction validation API in tests/RE2.ComplianceApi.Tests/Controllers/V1/TransactionsControllerTests.cs

### Implementation for User Story 4

- [X] T128 [P] [US4] Create Transaction domain model in src/RE2.ComplianceCore/Models/Transaction.cs per data-model.md entity 6
- [X] T129 [P] [US4] Create TransactionLine domain model in src/RE2.ComplianceCore/Models/TransactionLine.cs per data-model.md entity 7
- [X] T130 [P] [US4] Create TransactionViolation domain model in src/RE2.ComplianceCore/Models/TransactionViolation.cs per data-model.md entity 9
- [X] T131 [P] [US4] Create TransactionLicenceUsage domain model in src/RE2.ComplianceCore/Models/TransactionLicenceUsage.cs per data-model.md entity 8
- [X] T132 [P] [US4] Create Threshold domain model in src/RE2.ComplianceCore/Models/Threshold.cs per data-model.md entity 10

### Threshold Configuration (FR-022)

- [X] T132a [P] [US4] Create IThresholdRepository interface in src/RE2.ComplianceCore/Interfaces/IThresholdRepository.cs
- [X] T132b [US4] Implement InMemoryThresholdRepository in src/RE2.DataAccess/InMemory/InMemoryThresholdRepository.cs (in-memory for dev; Dataverse implementation pending)
- [X] T132c [US4] Create ThresholdService in src/RE2.ComplianceCore/Services/RiskMonitoring/ThresholdService.cs with CRUD operations and validation logic
- [X] T132d [US4] Create ThresholdsController v1 in src/RE2.ComplianceApi/Controllers/V1/ThresholdsController.cs with GET, POST, PUT, DELETE endpoints for customer-substance threshold configuration
- [X] T132e [US4] Create threshold configuration UI in src/RE2.ComplianceWeb/Views/Thresholds/ (Index, Create, Edit, Details views) with customer selector, substance selector, threshold type (monthly quantity, annual frequency), limit value fields
- [X] T132f [US4] Create ThresholdsController for web UI in src/RE2.ComplianceWeb/Controllers/ThresholdsController.cs
- [X] T132g [US4] Configure authorization: ComplianceManager role can manage thresholds per FR-031


- [X] T133 [P] [US4] Create Transaction DTOs for D365 F&O in src/RE2.DataAccess/D365FinanceOperations/Models/TransactionDto.cs
- [X] T134 [US4] Create ITransactionRepository interface in src/RE2.ComplianceCore/Interfaces/ITransactionRepository.cs
- [X] T135 [US4] Implement InMemoryTransactionRepository in src/RE2.DataAccess/InMemory/InMemoryTransactionRepository.cs (in-memory for dev; D365 F&O implementation pending)
- [X] T136 [US4] Create TransactionComplianceService in src/RE2.ComplianceCore/Services/TransactionValidation/TransactionComplianceService.cs with validation logic per FR-018
- [X] T137 [US4] Implement customer licence validation in TransactionComplianceService (FR-018, FR-019)
- [X] T138 [US4] Implement company licence validation in TransactionComplianceService (FR-024)
- [X] T139 [US4] Implement cross-border permit validation in TransactionComplianceService (FR-021)
- [X] T140 [US4] Implement threshold checking in TransactionComplianceService (FR-022) using IThresholdRepository to retrieve configured thresholds and evaluate transaction quantities against limits
- [X] T141 [US4] Create TransactionsController v1 in src/RE2.ComplianceApi/Controllers/V1/TransactionsController.cs per transaction-validation-api.yaml
- [X] T142 [US4] Implement POST /api/v1/transactions/validate endpoint per transaction-validation-api.yaml with <3 second response time (SC-005)
- [X] T143 [US4] Implement GET /api/v1/transactions/{externalId} endpoint per transaction-validation-api.yaml
- [X] T144 [US4] Implement POST /api/v1/warehouse/operations/validate endpoint per transaction-validation-api.yaml (FR-023)
- [X] T145 [US4] Create pending transactions dashboard in src/RE2.ComplianceWeb/Views/Transactions/PendingOverrides.cshtml
- [X] T146 [US4] Implement override approval UI in src/RE2.ComplianceWeb/Views/Transactions/Details.cshtml per FR-019a
- [X] T147 [US4] Create TransactionsController for web UI in src/RE2.ComplianceWeb/Controllers/TransactionsController.cs (override logic integrated)
- [X] T148 [US4] Implement POST /api/v1/transactions/{transactionId}/approve and /reject endpoints per transaction-validation-api.yaml (FR-019a)
- [X] T149 [US4] Configure authorization: implement role-based override approval per FR-019a with appsettings.json configuration (OverrideApprovalRoles array), validate approver in configured roles, enforce justification field requirements
- [X] T149a [US4] Add appsettings.json configuration section for override approval roles with default ["ComplianceManager"] and role precedence documentation (any configured role can approve)
- [X] T149b [US4] Extend TransactionsController to record calling system identity (IntegrationSystem ID from T047a-T047e) in transaction audit per FR-061

### Webhook/Callback Notifications (FR-059)

- [X] T149c [P] [US4] Create WebhookSubscription domain model in src/RE2.ComplianceCore/Models/WebhookSubscription.cs with properties: SubscriptionId, IntegrationSystemId, EventTypes (flags: ComplianceStatusChanged, OrderApproved, OrderRejected, LicenceExpiring), CallbackUrl, SecretKey, IsActive, CreatedDate
- [X] T149d [P] [US4] Create WebhookSubscription DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/WebhookSubscriptionDto.cs
- [X] T149e [US4] Create IWebhookSubscriptionRepository interface in src/RE2.ComplianceCore/Interfaces/IWebhookSubscriptionRepository.cs
- [X] T149f [US4] Implement DataverseWebhookSubscriptionRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseWebhookSubscriptionRepository.cs
- [X] T149g [US4] Create WebhookDispatchService in src/RE2.ComplianceCore/Services/Notifications/WebhookDispatchService.cs with methods: DispatchAsync(eventType, payload), GetSubscribersForEvent(eventType)
- [X] T149h [US4] Implement webhook dispatch with HMAC-SHA256 signature using subscriber's SecretKey per industry standards (X-Webhook-Signature header)
- [X] T149h2 [US4] Implement webhook retry logic in WebhookDispatchService: retry up to 3 times with exponential backoff (10s, 60s, 300s), mark subscription as unhealthy after 3 consecutive failures, generate SystemAdmin alert on webhook delivery failure per FR-059
- [X] T149i [US4] Integrate WebhookDispatchService into TransactionComplianceService to dispatch events on: ComplianceStatusChanged (pending→approved, pending→rejected), OverrideApproved
- [X] T149j [US4] Create WebhookSubscriptionsController v1 in src/RE2.ComplianceApi/Controllers/V1/WebhookSubscriptionsController.cs with GET, POST, DELETE endpoints for integration systems to manage their subscriptions
- [X] T149k [US4] Configure authorization: only SystemAdmin role can manage webhook subscriptions, or integration systems can manage their own subscriptions via API key auth
- [X] T149l [P] [US4] Unit tests for WebhookDispatchService in tests/RE2.ComplianceCore.Tests/Services/WebhookDispatchServiceTests.cs
- [X] T149m [US4] Integration tests for webhook dispatch in tests/RE2.ComplianceApi.Tests/Controllers/V1/WebhookSubscriptionsControllerTests.cs

**Checkpoint**: At this point, User Stories 1-4 are complete - real-time transaction validation API operational, blocking non-compliant orders

---

## Phase 7: User Story 5 - Declarations, Reporting & Audits (Priority: P2)

**Goal**: Store all licence/permit information supporting each controlled-drug transaction, generate periodic reports (by substance/customer/country) with distributed quantities and associated licences, and maintain audit logs of all data changes for regulatory evidence.

**Independent Test**: Complete transactions with various licence/permit combinations, generate reports filtering by substance/customer/country, review audit logs showing who changed what data and when, record inspection findings with corrective actions.

### Tests for User Story 5 (TDD - Write First)

- [X] T150 [P] [US5] Unit tests for AuditEvent model in tests/RE2.ComplianceCore.Tests/Models/AuditEventTests.cs
- [X] T151 [P] [US5] Unit tests for reporting service in tests/RE2.ComplianceCore.Tests/Services/ReportingServiceTests.cs
- [X] T152 [US5] Integration tests for audit report generation in tests/RE2.ComplianceApi.Tests/Controllers/V1/ReportsControllerTests.cs

### Implementation for User Story 5

- [X] T153 [P] [US5] Create AuditEvent domain model in src/RE2.ComplianceCore/Models/AuditEvent.cs per data-model.md entity 15
- [X] T154 [US5] Create IAuditRepository interface in src/RE2.ComplianceCore/Interfaces/IAuditRepository.cs
- [X] T155 [US5] Implement D365FoAuditRepository in src/RE2.DataAccess/D365FinanceOperations/Repositories/D365FoAuditRepository.cs
- [X] T156 [US5] Create audit logging interceptor for all data modification operations (FR-027)
- [X] T157 [US5] Implement optimistic locking with RowVersion/ETag in all Dataverse and D365 F&O repositories per research.md section 7: catch ConcurrencyVersionMismatch exceptions (Dataverse), handle PreconditionFailed responses (D365), throw custom ConcurrencyException for upper layers
- [X] T157a [US5] Create ConcurrencyException custom exception in src/RE2.ComplianceCore/Exceptions/ with properties: EntityType, EntityId, LocalVersion, RemoteVersion, ConflictingFields
- [X] T158 [US5] Create conflict resolution UI component in src/RE2.ComplianceWeb/Views/Shared/_ConflictResolution.cshtml per FR-027b
- [X] T159 [US5] Implement conflict detection and resolution workflow per FR-027b
- [X] T160 [US5] Create ReportingService in src/RE2.ComplianceCore/Services/Reporting/ReportingService.cs
- [X] T161 [US5] Implement transaction audit report generation (FR-026: by substance, customer, country)
- [X] T162 [US5] Implement licence usage report generation
- [X] T163 [US5] Implement customer compliance history report generation (FR-029)
- [X] T163a [US5] Create LicenceCorrectionImpactService in src/RE2.ComplianceCore/Services/Reporting/LicenceCorrectionImpactService.cs implementing SC-038 historical validation report
- [X] T163b [US5] Implement impact analysis logic: query transactions where licence effective dates overlap transaction dates, re-validate each transaction under corrected licence data, return list of transactions with original vs. corrected compliance status
- [X] T163c [US5] Add GET /api/v1/reports/licence-correction-impact endpoint to ReportsController v1 accepting licenceId and correctionDate parameters
- [X] T163d [US5] Create licence correction impact report UI in src/RE2.ComplianceWeb/Views/Reports/LicenceCorrectionImpact.cshtml showing affected transactions table with columns: TransactionID, Date, Customer, OriginalStatus, CorrectedStatus, ImpactSeverity per SC-038
- [X] T164 [US5] Create ReportsController v1 in src/RE2.ComplianceApi/Controllers/V1/ReportsController.cs
- [X] T165 [US5] Create reports UI in src/RE2.ComplianceWeb/Views/Reports/ (Index, TransactionAudit, LicenceUsage, CustomerCompliance views)
- [X] T166 [US5] Create ReportsController for web UI in src/RE2.ComplianceWeb/Controllers/ReportsController.cs
- [X] T167 [US5] Implement inspection recording UI in src/RE2.ComplianceWeb/Views/Inspections/ (Create, Index views) per FR-028
- [X] T168 [US5] Create ComplianceReportGenerator Azure Function in src/RE2.ComplianceFunctions/ComplianceReportGenerator.cs (timer trigger, weekly)
- [X] T169 [US5] Configure authorization: ComplianceManager and QAUser roles can access reports

**Checkpoint**: At this point, User Stories 1-5 are complete - full audit trail and reporting capabilities operational for regulatory compliance

---

## Phase 8: User Story 6 - Risk Management, Workflows & Access Control (Priority: P3)

**Goal**: Configurable workflows for high-risk events (adding controlled substances to customer scope, approving exception sales, adding new countries) requiring review and approval by designated roles, with role-based access control restricting who can create/modify licences or override blocks.

**Independent Test**: Configure approval workflows for specific high-risk events, attempt to perform restricted actions with different user roles, verify only authorized users can approve exceptions, modify licence data, or override system blocks.

### Tests for User Story 6 (TDD - Write First)

- [X] T170 [P] [US6] Unit tests for authorization policies in tests/RE2.ComplianceApi.Tests/Authorization/AuthorizationPolicyTests.cs
- [X] T171 [US6] Integration tests for role-based access control in tests/RE2.ComplianceApi.Tests/Controllers/RoleBasedAccessTests.cs

### Implementation for User Story 6

- [X] T172 [US6] Create custom authorization requirement ActiveEmployeeRequirement in src/RE2.ComplianceApi/Authorization/ActiveEmployeeRequirement.cs per research.md section 6
- [X] T173 [US6] Create authorization handler ActiveEmployeeHandler in src/RE2.ComplianceApi/Authorization/ActiveEmployeeHandler.cs
- [X] T174 [US6] Configure custom authorization policies (CanManageLicences, InternalTenantOnly, ActiveEmployeeOnly) in RE2.ComplianceApi Program.cs
- [X] T175 [US6] Implement Azure Logic App workflow definition for high-risk event approvals in infra/logic-apps/approval-workflow.json per FR-030 with HTTP trigger, approval action, and callback to ComplianceApi
- [X] T176 [US6] Create ApprovalWorkflowController v1 in src/RE2.ComplianceApi/Controllers/V1/ApprovalWorkflowController.cs with:
  - POST /api/v1/workflows/trigger (trigger Logic App for high-risk events)
  - POST /api/v1/workflows/callback (receive approval/rejection from Logic App)
  - GET /api/v1/workflows/{workflowId}/status (check workflow state)

- [X] T176a Implement workflow state synchronization: update internal approval status when Logic App callback received, generate audit events per FR-030
- [X] T176b Configure Azure Logic App deployment in infra/bicep/logic-apps.bicep with Managed Identity authentication to ComplianceApi callback endpoints

- [X] T177 [US6] Create compliance dashboard in src/RE2.ComplianceWeb/Views/Dashboard/ComplianceRisks.cshtml per FR-032
- [X] T178 [US6] Implement dashboard highlighting: customers with expiring licences, blocked orders, abnormal order volumes
- [X] T179 [US6] Add role-based UI hiding/showing in Razor views (e.g., hide "Create Licence" button if not ComplianceManager)
- [X] T180 [US6] Configure Azure API Management policies for rate limiting per FR-063

**Checkpoint**: At this point, User Stories 1-6 are complete - governance layer and risk controls operational on top of compliance functionality

---

## Phase 9: User Story 7 - GDP Master Data & Authorisations (Priority: P1)

**Goal**: Register Wholesale Distribution Authorisation (WDA) and GDP certificate (scope, sites, activities, inspection dates), maintain master list of GDP-relevant sites (warehouses, cross-docks, transport hubs) linked to WDAs, configure GDP activities per site (storage >72h, temperature-controlled, outsourced, transport-only).

**Independent Test**: Register WDA and GDP certificates with scope, create site master data linking sites to WDAs, configure activities per site, verify system can validate whether a given activity at a given site is covered by appropriate authorization.

### Tests for User Story 7 (TDD - Write First)

- [X] T181 [P] [US7] Unit tests for GdpSite model in tests/RE2.ComplianceCore.Tests/Models/GdpSiteTests.cs
- [X] T182 [P] [US7] Unit tests for GdpSiteWdaCoverage model in tests/RE2.ComplianceCore.Tests/Models/GdpSiteWdaCoverageTests.cs
- [X] T183 [US7] Contract tests for Dataverse GdpSite entity in tests/RE2.Contract.Tests/DataverseGdpSiteContractTests.cs
- [X] T184 [US7] Integration tests for GDP site management API in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpSitesControllerTests.cs

### Implementation for User Story 7

- [X] T185 [P] [US7] Create GdpSite domain model in src/RE2.ComplianceCore/Models/GdpSite.cs per data-model.md entity 16
- [X] T186 [P] [US7] Create GdpSiteWdaCoverage domain model in src/RE2.ComplianceCore/Models/GdpSiteWdaCoverage.cs per data-model.md entity 17
- [X] T187 [P] [US7] Create GdpSite DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/GdpSiteDto.cs
- [X] T188 [US7] Create IGdpSiteRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpSiteRepository.cs
- [X] T189 [US7] Implement DataverseGdpSiteRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpSiteRepository.cs
- [X] T190 [US7] Create GdpComplianceService in src/RE2.ComplianceCore/Services/GdpCompliance/GdpComplianceService.cs
- [X] T191 [US7] Create GdpSitesController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpSitesController.cs
- [X] T192 [US7] Create GDP sites management UI in src/RE2.ComplianceWeb/Views/GdpSites/ (Index, Create, Edit, Details views)
- [X] T193 [US7] Create GdpSitesController for web UI in src/RE2.ComplianceWeb/Controllers/GdpSitesController.cs
- [X] T194 [US7] Add WDA coverage validation per data-model.md (FR-033: ensure sites linked to valid WDA)
- [X] T195 [US7] Configure route authorization: QAUser and ComplianceManager can manage GDP sites

**Checkpoint**: At this point, User Story 7 is complete - GDP site and WDA master data operational

---

## Phase 10: User Story 8 - Supplier, Customer & Service-Provider GDP Qualification (Priority: P2)

**Goal**: Record GDP status and WDA/GMP authorisations of suppliers, customers, and service providers (3PLs, transporters, external warehouses) including EudraGMDP entries, track qualification status (approved/conditionally approved/rejected/under review), set review frequencies with reminders (every 2-3 years).

**Independent Test**: Record GDP credentials for various partners, conduct qualification reviews with different outcomes, set review frequencies, verify system prevents selection of non-approved partners while generating re-qualification reminders.

### Tests for User Story 8 (TDD - Write First)

- [X] T196 [P] [US8] Unit tests for GdpCredential model in tests/RE2.ComplianceCore.Tests/Models/GdpCredentialTests.cs
- [X] T197 [P] [US8] Unit tests for GdpServiceProvider model in tests/RE2.ComplianceCore.Tests/Models/GdpServiceProviderTests.cs
- [X] T198 [US8] Integration tests for GDP qualification API in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpProvidersControllerTests.cs

### Implementation for User Story 8

- [X] T199 [P] [US8] Create GdpCredential domain model in src/RE2.ComplianceCore/Models/GdpCredential.cs per data-model.md entity 18
- [X] T200 [P] [US8] Create GdpServiceProvider domain model in src/RE2.ComplianceCore/Models/GdpServiceProvider.cs per data-model.md entity 19
- [X] T201 [P] [US8] Create GdpCredentialVerification domain model in src/RE2.ComplianceCore/Models/GdpCredentialVerification.cs per data-model.md entity 30
- [X] T202 [P] [US8] Create GdpCredential DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/GdpCredentialDto.cs
- [X] T203 [US8] Create IGdpCredentialRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpCredentialRepository.cs
- [X] T204 [US8] Implement DataverseGdpCredentialRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpCredentialRepository.cs
- [X] T205 [US8] Extend GdpComplianceService with partner qualification methods (FR-037, FR-038)
- [X] T206 [US8] Create GdpProvidersController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpProvidersController.cs
- [X] T207 [US8] Create GDP providers management UI in src/RE2.ComplianceWeb/Views/GdpProviders/ (Index, Create, Edit, QualificationReview views)
- [X] T208 [US8] Create GdpProvidersController for web UI in src/RE2.ComplianceWeb/Controllers/GdpProvidersController.cs
- [X] T209 [US8] Extend CustomersController web UI to include GDP credentials tab
- [X] T210 [US8] Implement EudraGMDP verification recording UI per FR-045
- [X] T211 [US8] Implement re-qualification reminder logic per FR-039

**Checkpoint**: At this point, User Stories 7-8 are complete - full GDP partner qualification operational

---

## Phase 11: User Story 9 - GDP Inspections, Audits & CAPA Tracking (Priority: P2)

**Goal**: Register GDP inspections by authorities (IGJ/NVWA) and internal/self-inspections against each site and WDA (findings with classification: critical/major/other), create and track CAPAs linked to specific findings with owners and due dates, view dashboards summarizing open/overdue CAPAs and upcoming inspections.

**Independent Test**: Record various inspection types (authority, internal) with findings at different sites, create CAPAs linked to findings with ownership and due dates, verify dashboards correctly summarize open/overdue CAPAs and inspection schedules.

### Tests for User Story 9 (TDD - Write First)

- [X] T213 [P] [US9] Unit tests for GdpInspection model in tests/RE2.ComplianceCore.Tests/Models/GdpInspectionTests.cs
- [X] T214 [P] [US9] Unit tests for Capa model in tests/RE2.ComplianceCore.Tests/Models/CapaTests.cs
- [X] T215 [US9] Integration tests for CAPA management API in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpInspectionsControllerTests.cs

### Implementation for User Story 9

- [X] T216 [P] [US9] Create GdpInspection domain model in src/RE2.ComplianceCore/Models/GdpInspection.cs per data-model.md entity 20
- [X] T217 [P] [US9] Create GdpInspectionFinding domain model in src/RE2.ComplianceCore/Models/GdpInspectionFinding.cs per data-model.md entity 21
- [X] T218 [P] [US9] Create Capa domain model in src/RE2.ComplianceCore/Models/Capa.cs per data-model.md entity 22
- [X] T219 [P] [US9] Create GdpInspection DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/GdpInspectionDto.cs
- [X] T220 [US9] Create IGdpInspectionRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpInspectionRepository.cs
- [X] T221 [US9] Implement DataverseGdpInspectionRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpInspectionRepository.cs
- [X] T222 [US9] Create ICapaRepository interface in src/RE2.ComplianceCore/Interfaces/ICapaRepository.cs
- [X] T223 [US9] Implement DataverseCapaRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseCapaRepository.cs
- [X] T224 [US9] Extend GdpComplianceService with inspection and CAPA management methods
- [X] T225 [US9] Create GdpInspectionsController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpInspectionsController.cs
- [X] T226 [US9] Create GDP inspections management UI in src/RE2.ComplianceWeb/Views/GdpInspections/ (Index, Create, Details, CreateCapa views)
- [X] T227 [US9] Create GdpInspectionsController for web UI in src/RE2.ComplianceWeb/Controllers/GdpInspectionsController.cs
- [X] T228 [US9] Create CAPA dashboard UI in src/RE2.ComplianceWeb/Views/GdpInspections/Capas.cshtml per FR-042
- [X] T229 [US9] Implement CAPA status tracking (Open, Overdue, Completed) per data-model.md validation rules
- [X] T230 [US9] Configure authorization: QAUser role can manage inspections and CAPAs

**Checkpoint**: At this point, User Stories 7-9 are complete - full GDP inspection and CAPA management operational

---

## Phase 12: User Story 10 - GDP Certificates, Validity & Monitoring (Priority: P2)

> **DETAILED TASK BREAKDOWN**: See [specs/main/tasks.md](../main/tasks.md) for the complete 23-task breakdown (T231-T253) generated from detailed gap analysis. Key insight: FR-043 (validity) and FR-045 (verification) core logic already exists from US8 — US10 focuses on GdpDocument model (FR-044), GdpCertificateMonitor Azure Function, and web UI.

**Goal**: Attach GDP certificates/WDA copies/inspection reports to entity records, automate GDP credential expiry monitoring via Azure Function, and provide web UI for credential validity management and verification logging.

**Independent Test**: Upload documents to GDP credentials/sites/inspections, verify GdpCertificateMonitor generates expiry alerts, manage credentials through web UI, record EudraGMDP verifications through web form.

### Phase A: Domain Model
- [X] T231 [US10] Write GdpDocument validation tests in tests/RE2.ComplianceCore.Tests/Models/GdpDocumentTests.cs
- [X] T232 [US10] Create GdpDocument domain model + GdpDocumentEntityType enum in src/RE2.ComplianceCore/Models/GdpDocument.cs

### Phase B: Data Access
- [X] T233 [US10] Create IGdpDocumentRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpDocumentRepository.cs
- [X] T234 [P] [US10] Create InMemoryGdpDocumentRepository in src/RE2.DataAccess/InMemory/InMemoryGdpDocumentRepository.cs
- [X] T235 [P] [US10] Create DataverseGdpDocumentRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpDocumentRepository.cs
- [X] T236 [US10] Update InMemorySeedData.cs with sample GDP documents and new SeedAll overload
- [X] T237 [US10] Update InfrastructureExtensions.cs with IGdpDocumentRepository DI registrations

### Phase C: Business Logic
- [X] T238 [US10] Extend IGdpComplianceService with document CRUD methods (GetDocuments, Upload, Download, Delete)
- [X] T239 [US10] Extend GdpComplianceService with document methods using IDocumentStorage (container: "gdp-documents")

### Phase D: Azure Function
- [X] T240 [US10] Write GdpCertificateMonitor tests in tests/RE2.ComplianceFunctions.Tests/GdpCertificateMonitorTests.cs
- [X] T241 [US10] Create GdpCertificateMonitor Azure Function in src/RE2.ComplianceFunctions/GdpCertificateMonitor.cs (3 AM UTC daily)

### Phase E: API Layer
- [X] T242 [US10] Write document API endpoint tests in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpProvidersControllerDocumentTests.cs
- [X] T243 [US10] Extend GdpProvidersController with document endpoints (upload/download/list/delete) + DTOs

### Phase F: Web UI
- [X] T244 [US10] Create GdpCredentialsController MVC in src/RE2.ComplianceWeb/Controllers/GdpCredentialsController.cs
- [X] T245 [P] [US10] Create GdpCredentials/Index.cshtml — credential listing with validity status badges
- [X] T246 [P] [US10] Create GdpCredentials/Details.cshtml — credential details with documents + verifications tabs
- [X] T247 [P] [US10] Create GdpCredentials/Expiring.cshtml — expiring credentials dashboard
- [X] T248 [P] [US10] Create GdpCredentials/RecordVerification.cshtml — EudraGMDP verification form (FR-045)
- [X] T249 [P] [US10] Create GdpCredentials/UploadDocument.cshtml — document upload form (FR-044)
- [X] T250 [US10] Update _Layout.cshtml with GDP Credentials navigation links
- [X] T251 [US10] Update GdpProviders/Details.cshtml with documents section

### Phase G: Polish
- [X] T252 [US10] Verify all tests pass — full regression test (`dotnet test RE2.sln`)
- [X] T253 [US10] Final validation — build clean, all US10 features functional

**Checkpoint**: At this point, User Stories 7-10 are complete - full GDP certificate lifecycle management with monitoring

---

## Phase 13: User Story 11 - GDP Operational Checks & Distribution Controls (Priority: P2)

**Goal**: Prevent assigning product storage or distribution to sites or 3PLs not covered by appropriate WDA/GDP certificates, show which routes and transport providers are GDP-approved (including temperature-controlled capability), record qualification status of GDP equipment/processes (temperature-controlled vehicles, monitoring systems) with re-qualification due dates.

**Independent Test**: Attempt to assign storage/distribution to various sites and 3PLs (approved vs. not approved), select transport providers for different shipment types, record equipment qualification status with re-qualification tracking.

### Tests for User Story 11 (TDD - Write First)

- [X] T240 [P] [US11] Unit tests for GDP operational validation in tests/RE2.ComplianceCore.Tests/Services/GdpOperationalServiceTests.cs
- [X] T241 [US11] Integration tests for GDP site assignment validation in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpOperationsControllerTests.cs

### Implementation for User Story 11

- [X] T242 [US11] Create GdpOperationalService in src/RE2.ComplianceCore/Services/GdpCompliance/GdpOperationalService.cs
- [X] T243 [US11] Implement site assignment validation per FR-046 (prevent non-GDP-compliant assignments)
- [X] T244 [US11] Implement transport provider GDP approval checking per FR-047
- [X] T245 [US11] Create GdpOperationsController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpOperationsController.cs
- [X] T246 [US11] Implement equipment qualification tracking UI in src/RE2.ComplianceWeb/Views/GdpEquipment/ per FR-048
- [X] T247 [US11] Create GDP-approved routes/providers lookup API endpoint
- [X] T248 [US11] Add GDP operational checks dashboard in src/RE2.ComplianceWeb/Views/Dashboard/GdpOperations.cshtml

**Checkpoint**: At this point, User Stories 7-11 are complete - GDP operational controls enforce compliance at execution level

---

## Phase 14: User Story 12 - GDP Documentation, Training & Change Control (Priority: P3)

**Goal**: Maintain index of GDP-relevant SOPs (returns, recalls, deviations, temperature excursions, outsourced activities) linked to sites and activities, record GDP-specific training completion for distribution staff linked to functions, manage changes impacting GDP (new warehouse, new 3PL, new product type, change in storage condition) via controlled change records with approvals.

**Independent Test**: Maintain SOP indices linked to sites/activities, record training completion for staff in distribution roles, process change records for GDP-impacting changes with required approvals.

### Tests for User Story 12 (TDD - Write First)

- [X] T249 [P] [US12] Unit tests for GdpSop model in tests/RE2.ComplianceCore.Tests/Models/GdpSopTests.cs
- [X] T250 [P] [US12] Unit tests for TrainingRecord model in tests/RE2.ComplianceCore.Tests/Models/TrainingRecordTests.cs
- [X] T251 [P] [US12] Unit tests for GdpChangeRecord model in tests/RE2.ComplianceCore.Tests/Models/GdpChangeRecordTests.cs
- [X] T252 [US12] Integration tests for change control workflow in tests/RE2.ComplianceApi.Tests/Controllers/V1/GdpChangeControllerTests.cs

### Implementation for User Story 12

- [X] T253 [P] [US12] Create GdpSop domain model in src/RE2.ComplianceCore/Models/GdpSop.cs per data-model.md entity 23
- [X] T254 [P] [US12] Create GdpSiteSop domain model in src/RE2.ComplianceCore/Models/GdpSiteSop.cs per data-model.md entity 24
- [X] T255 [P] [US12] Create TrainingRecord domain model in src/RE2.ComplianceCore/Models/TrainingRecord.cs per data-model.md entity 25
- [X] T256 [P] [US12] Create GdpChangeRecord domain model in src/RE2.ComplianceCore/Models/GdpChangeRecord.cs per data-model.md entity 26

- [X] T257 [P] [US12] Create GdpSop DTO for Dataverse in src/RE2.DataAccess/Dataverse/Models/GdpSopDto.cs
- [X] T258 [US12] Create IGdpSopRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpSopRepository.cs
- [X] T259 [US12] Implement DataverseGdpSopRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpSopRepository.cs
- [X] T260 [US12] Create ITrainingRepository interface in src/RE2.ComplianceCore/Interfaces/ITrainingRepository.cs
- [X] T261 [US12] Implement DataverseTrainingRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseTrainingRepository.cs
- [X] T262 [US12] Create IGdpChangeRepository interface in src/RE2.ComplianceCore/Interfaces/IGdpChangeRepository.cs
- [X] T263 [US12] Implement DataverseGdpChangeRepository in src/RE2.DataAccess/Dataverse/Repositories/DataverseGdpChangeRepository.cs
- [X] T264 [US12] Create GdpSopsController v1 in src/RE2.ComplianceApi/Controllers/V1/GdpSopsController.cs
- [X] T265 [US12] Create GDP SOPs management UI in src/RE2.ComplianceWeb/Views/GdpSops/ (Index, Create, Edit views)
- [X] T266 [US12] Create GdpSopsController for web UI in src/RE2.ComplianceWeb/Controllers/GdpSopsController.cs
- [X] T267 [US12] Create training records UI in src/RE2.ComplianceWeb/Views/Training/ (Index, Create, StaffReport views) per FR-050
- [X] T268 [US12] Create TrainingController for web UI in src/RE2.ComplianceWeb/Controllers/TrainingController.cs
- [X] T269 [US12] Create change control UI in src/RE2.ComplianceWeb/Views/ChangeControl/ (Index, Create, Approve views) per FR-051
- [X] T270 [US12] Create ChangeControlController for web UI in src/RE2.ComplianceWeb/Controllers/ChangeControlController.cs
- [X] T271 [US12] Implement change approval workflow per FR-051 (GDP risk assessment before implementation)
- [X] T272 [US12] Configure authorization: QAUser and TrainingCoordinator roles for respective functions

**Checkpoint**: At this point, all User Stories 1-12 are complete - full system functionality operational

---

## Phase 15: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T273 [P] Add comprehensive XML documentation comments to public APIs in RE2.ComplianceCore
- [x] T274 [P] Add comprehensive XML documentation comments to public APIs in RE2.ComplianceApi
- [x] T275 [P] Create API documentation in docs/api/ from XML comments
- [x] T276 [P] Create user guide in docs/user-guide/ for compliance UI
- [x] T277 [P] Create integration guide in docs/integration/ for external systems
- [x] T278 [P] Update README.md at repository root with project overview
- [x] T279 Code cleanup and refactoring: remove unused usings, apply consistent naming
- [x] T280 [P] Performance optimization: add caching for frequently-accessed external data per plan.md (Azure Cache for Redis)
- [x] T281 [P] Implement Redis caching layer for Dataverse licence lookups
- [x] T282 [P] Implement Redis caching for customer compliance status lookups
- [x] T283 [P] Add unit tests for LicenceValidationService in tests/RE2.ComplianceCore.Tests/Services/LicenceValidationServiceTests.cs
- [x] T284 [P] Add unit tests for CustomerService in tests/RE2.ComplianceCore.Tests/Services/CustomerServiceTests.cs
- [x] T285 [P] Add unit tests for GdpComplianceService in tests/RE2.ComplianceCore.Tests/Services/GdpComplianceServiceTests.cs
- [x] T286 [P] Add unit tests for ReportingService in tests/RE2.ComplianceCore.Tests/Services/ReportingServiceTests.cs
- [x] T287 Security hardening: implement rate limiting per transaction-validation-api.yaml headers
- [ ] T288 Security hardening: integrate OWASP dependency check and SAST scanning (SonarQube or similar) in .azure/pipelines/ci-build.yml per NFR-005 with build failure on high/critical severity
- [x] T289 Security hardening: implement comprehensive input validation in RE2.ComplianceApi using FluentValidation for all DTOs, validating against OWASP injection patterns per NFR-002, with unit tests in tests/RE2.ComplianceApi.Tests/Validation/
- [x] T290 Security hardening: enforce HTTPS-only in production via middleware (RE2.ComplianceApi/Middleware/HttpsEnforcementMiddleware.cs) returning HTTP 403 for non-HTTPS requests per NFR-003
- [ ] T290a Security testing: perform DAST scanning with OWASP ZAP against deployed staging environment per NFR-005, verify zero high-severity findings per SC-039
- [ ] T290b Security testing: verify API rate limiting implementation per NFR-004 using load testing tool, confirm HTTP 429 responses at threshold per SC-042
- [ ] T290c Security testing: verify TLS configuration using SSL Labs scan, target A- or higher grade per SC-043
- [ ] T291 Run quickstart.md validation: verify local development setup works end-to-end
- [ ] T292 Create Azure Bicep infrastructure templates in infra/bicep/ per plan.md project structure
- [ ] T293 [P] Create Azure DevOps CI pipeline in .azure/pipelines/ci-build.yml
- [ ] T294 [P] Create Azure DevOps CD pipelines (staging and production) in .azure/pipelines/
- [ ] T295 Performance testing: verify transaction validation response times meet targets using load testing tool (JMeter or k6): p50 <1s, p95 <3s, p99 <5s per FR-058 and SC-005 with test scenarios for simple validation (single product, valid licence) and complex validation (multiple products, cross-border, threshold checks)
- [ ] T296 Performance testing: verify <1 second customer compliance status lookup (SC-033)
- [ ] T297 Load testing: verify 50 concurrent validation requests supported (SC-032)
- [ ] T298 Availability testing: verify 99.5% uptime target achievable (SC-028)
- [ ] T300 [P] Implement API version deprecation middleware in RE2.ComplianceApi adding X-API-Deprecated, X-API-Sunset-Date, Link headers per FR-062, with version routing enforcement: return HTTP 410 Gone for versions beyond sunset date (6 months after deprecation notice), redirect to latest version documentation

- [ ] T300a Implement API version compatibility layer in RE2.ComplianceApi: maintain previous API version (v1 initially) for minimum 6 months after v2 release per FR-062, transform v1 requests to v2 internal format
- [ ] T300b Create version migration guide template in docs/api/version-migration/ with breaking changes, upgrade path, code examples per FR-062

- [ ] T301 Configure Azure Monitor alerts for critical path availability (transaction validation, customer lookup), response time thresholds (3s for validation per SC-005), health check failures, and document recovery playbooks targeting MTTR <30 min per SC-031
- [ ] T301a Configure Azure Monitor metrics and alerts for resource utilization (CPU, memory, App Service plan capacity) per FR-056; set warning thresholds at 70% and critical at 85% with auto-scaling rules for App Service


---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-14)**: All depend on Foundational phase completion
  - User Stories 1-3 (P1): Can proceed in parallel after Foundation
  - User Story 4 (P2): Can proceed in parallel, integrates with Stories 1-2
  - User Story 5 (P2): Depends on Story 4 (transactions must exist to report on)
  - User Story 6 (P3): Can proceed in parallel, adds governance to existing features
  - User Story 7 (P1): Can proceed in parallel (independent GDP domain)
  - User Stories 8-12 (P2-P3): Depend on Story 7 (GDP foundation)
- **Polish (Phase 15)**: Depends on all user stories being complete

### User Story Dependencies

#### Licence Management Track (Stories 1-6)
- **User Story 1 (P1)**: No dependencies - foundational licence types and substance mappings
- **User Story 2 (P1)**: No dependencies - can start in parallel with US1, shares Licence entity
- **User Story 3 (P1)**: Depends on US1 (needs Licence entity) - adds document management
- **User Story 4 (P2)**: Depends on US1 and US2 (needs licences and customers) - adds transaction validation
- **User Story 5 (P2)**: Depends on US4 (needs transactions to report on) - adds audit trail
- **User Story 6 (P3)**: Can start after US1-4 - adds governance and workflows

#### GDP Compliance Track (Stories 7-12)
- **User Story 7 (P1)**: No dependencies - foundational GDP sites and WDA
- **User Story 8 (P2)**: Depends on US7 (needs GdpSite) - adds partner qualification
- **User Story 9 (P2)**: Depends on US7 (needs GdpSite for inspections) - adds inspections and CAPA
- **User Story 10 (P2)**: Depends on US7 and US8 (needs GDP entities) - adds certificate monitoring
- **User Story 11 (P2)**: Depends on US7 and US8 (needs sites and providers) - adds operational checks
- **User Story 12 (P3)**: Depends on US7-11 (needs all GDP entities) - adds documentation and training

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD per Principle II)
- DTOs before repositories
- Repositories before services
- Services before controllers
- API controllers before web UI controllers
- Core implementation before integration
- Story complete and independently tested before moving to next priority

### Parallel Opportunities

#### Phase 1 (Setup)
- T002-T007: All project initializations can run in parallel (different directories)
- T008-T014: All NuGet package additions can run in parallel (different projects)
- T018-T020: All configuration files can run in parallel

#### Phase 2 (Foundational)
- T021-T026: Authentication configurations can run in parallel (different projects)
- T027-T032: Interface and client implementations can run in parallel (different files)
- T033-T034: Resilience handlers can run in parallel
- T048-T051: Shared constants can run in parallel (different files)

#### User Story Tests
- All tests within a story marked [P] can run in parallel (e.g., T053-T056 for US1)

#### User Story Models
- All domain models within a story marked [P] can run in parallel (e.g., T060-T063 for US1)
- All DTOs within a story marked [P] can run in parallel (e.g., T064-T067 for US1)

#### Between User Stories (After Foundation Complete)
- **Parallel Track 1**: US1 → US3 → US4 (Licence Management)
- **Parallel Track 2**: US2 (Customer Management)
- **Parallel Track 3**: US7 → US8 → US9 (GDP Compliance)
- All three tracks can proceed simultaneously with different team members

#### Phase 15 (Polish)
- T273-T278: Documentation tasks can run in parallel
- T281-T282: Caching implementations can run in parallel
- T283-T286: Unit tests can run in parallel
- T293-T294: CI/CD pipelines can run in parallel

---

## Parallel Example: User Story 1 (MVP)

```bash
# After Foundation Phase 2 completes, launch User Story 1:

# Step 1: Launch all tests together (TDD - write first):
Task T053: "Unit tests for LicenceType model"
Task T054: "Unit tests for ControlledSubstance model"
Task T055: "Unit tests for LicenceSubstanceMapping model"
Task T056: "Unit tests for Licence model"
# (Tests will FAIL - expected)

# Step 2: Launch all domain models together:
Task T060: "Create LicenceType domain model"
Task T061: "Create ControlledSubstance domain model"
Task T062: "Create LicenceSubstanceMapping domain model"
# T063 waits for T060 (Licence depends on LicenceType)

# Step 3: Launch all DTOs together:
Task T064: "Create LicenceType DTO for Dataverse"
Task T065: "Create ControlledSubstance DTO for Dataverse"
Task T066: "Create LicenceSubstanceMapping DTO for Dataverse"
Task T067: "Create Licence DTO for Dataverse"
```

---

## Parallel Example: Multiple Stories After Foundation

```bash
# After Foundation Phase 2 completes:

# Team Member A works on User Story 1:
Tasks T053-T080: Licence Type Management

# Team Member B works on User Story 2 (in parallel):
Tasks T081-T098: Customer Onboarding

# Team Member C works on User Story 7 (in parallel):
Tasks T181-T195: GDP Sites and WDA

# All three stories proceed independently without conflicts
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup (T001-T020)
2. Complete Phase 2: Foundational (T021-T052) - CRITICAL, blocks all stories
3. Complete Phase 3: User Story 1 (T053-T080) - Licence type management
4. Complete Phase 4: User Story 2 (T081-T098) - Customer onboarding
5. Complete Phase 5: User Story 3 (T099-T122) - Document management and expiry monitoring
6. **STOP and VALIDATE**: Test stories 1-3 independently
7. Deploy/demo if ready - system can manage licences, customers, and documents

### Incremental Delivery (Full P1 Features)

1. Complete Setup + Foundational → Foundation ready
2. Add User Stories 1-3 (P1) → Test independently → Deploy/Demo (MVP!)
3. Add User Story 4 (P2) → Transaction validation operational → Deploy/Demo
4. Add User Story 5 (P2) → Audit trail and reporting → Deploy/Demo
5. Add User Story 7 (P1) → GDP sites management → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy (Full System)

With 3+ developers:

1. **Week 1-2**: Team completes Setup + Foundational together (T001-T052)
2. **Week 3-6**: Once Foundational is done:
   - Developer A: User Stories 1, 3 (Licence management)
   - Developer B: User Stories 2, 4 (Customer and transaction validation)
   - Developer C: User Stories 7, 8 (GDP sites and partners)
3. **Week 7-9**:
   - Developer A: User Story 5 (Reporting)
   - Developer B: User Story 6 (Workflows)
   - Developer C: User Stories 9, 10 (GDP inspections and monitoring)
4. **Week 10-11**:
   - Developer A: User Story 11 (GDP operations)
   - Developer B: User Story 12 (GDP documentation)
   - Developer C: Phase 15 Polish (testing, docs)
5. Stories complete and integrate independently

---

## Task Count Summary

- **Phase 1 (Setup)**: 20 tasks
- **Phase 2 (Foundational)**: 40 tasks (BLOCKING) - increased: added T047a-T047f for IntegrationSystem foundation, T047g for health checks (moved from Phase 15 T299), T047h for graceful degradation middleware
- **Phase 3 (User Story 1 - P1)**: 51 tasks
- **Phase 4 (User Story 2 - P1)**: 18 tasks
- **Phase 5 (User Story 3 - P1)**: 23 tasks
- **Phase 6 (User Story 4 - P2)**: 41 tasks - increased: added T149b for FR-061 integration audit, T149c-T149m for FR-059 webhook dispatch, T149h2 for webhook retry logic
- **Phase 7 (User Story 5 - P2)**: 20 tasks
- **Phase 8 (User Story 6 - P3)**: 11 tasks
- **Phase 9 (User Story 7 - P1)**: 15 tasks
- **Phase 10 (User Story 8 - P2)**: 16 tasks
- **Phase 11 (User Story 9 - P2)**: 18 tasks
- **Phase 12 (User Story 10 - P2)**: 9 tasks
- **Phase 13 (User Story 11 - P2)**: 9 tasks
- **Phase 14 (User Story 12 - P3)**: 17 tasks - reduced: moved IntegrationSystem tasks to Phase 2
- **Phase 15 (Polish)**: 29 tasks - T299 moved to Phase 2 as T047g; added T301a for FR-056 resource monitoring
- **TOTAL**: 339 tasks

**MVP Scope** (User Stories 1-3): 75 + 20 (Setup) + 40 (Foundation) = **135 tasks**

**Parallel Opportunities**:
- Phase 1: 16 parallelizable tasks (80%)
- Phase 2: 12 parallelizable tasks (37.5%)
- User Story phases: All tests, models, DTOs within story can parallelize
- Between stories: 3-4 stories can proceed in parallel after Foundation

---

## Notes

- **[P] tasks**: Different files, no dependencies, can run in parallel
- **[Story] label**: Maps task to specific user story for traceability (US1-US12)
- **TDD Required**: All test tasks MUST be written before implementation per Principle II
- **Test Failures Expected**: Tests will FAIL initially - this is correct TDD workflow
- **Independent Stories**: Each user story should be independently completable and testable
- **Checkpoint Validation**: Stop at each checkpoint to validate story works independently
- **Project Structure**: Multi-project .NET solution per plan.md (not single project)
- **External Data**: No local data storage for business entities - all via Dataverse/D365 APIs
- **Stateless Architecture**: No session state, supports horizontal scaling per FR-052
- **Performance Targets**: <3s transaction validation (SC-005), <1s customer lookup (SC-033)
- **Commit Strategy**: Commit after each task or logical group
- **Avoid**: Vague tasks, same file conflicts, cross-story dependencies that break independence

---

## Out of Scope (v1.0)

The following items are explicitly **NOT** included in the current task list per spec.md Assumptions and Edge Cases:

| Item | Reference | Rationale |
|------|-----------|-----------|
| Inventory recall workflows | Assumption 9 | Managed by separate WMS/QMS systems |
| Real-time temperature monitoring | Assumption 12 | Handled by WMS/QMS integration |
| Real-time shipment tracking | Edge Case: 3PL suspension mid-shipment | Deferred to v2.0 |
| Site availability scheduling | Edge Case: Sites temporarily closed | Deferred to v2.0 |
| Advanced logistics routing | Edge Case: Split shipments | Single route per shipment assumed |
| Batch integration patterns | Assumption 22 | RESTful sync APIs only for v1.0 |
| SMS notifications | Assumption 8 | Email and in-app only for v1.0 |
| Automated EudraGMDP API integration | Assumption 13 | Manual verification with logging |

These items may be considered for future versions based on operational feedback.
