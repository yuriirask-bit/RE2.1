# RE2 MVP Implementation Checklist

**Generated**: 2026-01-12
**Scope**: Phases 2-5 (Foundation through User Story 3)
**Status**: Scaffolding Complete - Ready for Implementation

---

## 📋 How to Use This Checklist

1. **Check off tasks** as you complete them
2. **Follow the order** - Phase 2 blocks all other phases
3. **TDD Required** - Write tests BEFORE implementation
4. **Tasks marked [P]** can run in parallel
5. **Reference task IDs** (T###) for details in `specs/001-licence-management/tasks.md`

---

## 🚧 Phase 2: Foundation (BLOCKING - Must Complete First)

**Purpose**: Core infrastructure that blocks all user story development
**Status**: 0/32 tasks complete
**Priority**: CRITICAL

### 2.1 Environment Setup (Prerequisites)

- [ ] Install Azure Storage Emulator (Azurite) for local blob storage
- [ ] Install Azure Functions Core Tools (for timer triggers)
- [ ] Install Docker Desktop (for TestContainers)
- [ ] Configure NuGet restore (network required)
  ```bash
  dotnet restore
  ```

---

### 2.2 Authentication & Authorization (T021-T026)

**Status**: 0/6 tasks

- [ ] **T021** [P] Add NuGet packages to ComplianceApi:
  ```bash
  dotnet add src/RE2.ComplianceApi package Microsoft.Identity.Web
  dotnet add src/RE2.ComplianceApi package Microsoft.Identity.Web.UI
  ```
- [ ] **T021** Configure Azure AD authentication in `Program.cs`
  - Add Azure AD authentication scheme
  - Configure JWT bearer token validation
  - Set up audience and issuer validation
  - Reference: `research.md` section 6

- [ ] **T022** [P] Configure Azure AD B2C authentication for external users
  - Add B2C authentication scheme
  - Configure B2C tenant settings
  - Set up sign-up/sign-in policy

- [ ] **T023** Configure multiple authentication schemes in `Program.cs`
  - Add policy to accept either AzureAd OR AzureAdB2C tokens
  - Set default authentication scheme
  - Configure challenge behavior

- [ ] **T024** [P] Configure authorization policies
  - Create `InternalUsers` policy (requires Azure AD)
  - Create `ExternalUsers` policy (requires B2C)
  - Create `AnyUser` policy (either scheme)
  - Apply policies to controllers with `[Authorize(Policy = "...")]`

- [ ] **T025** [P] Configure authentication in ComplianceWeb `Program.cs`
  - Add cookie authentication for web UI
  - Configure OIDC for Azure AD sign-in
  - Set up callback paths
  - Add sign-out support

- [ ] **T026** Create User model with roles in `RE2.Shared/Constants/`
  ```csharp
  public static class UserRoles
  {
      public const string ComplianceManager = "ComplianceManager";
      public const string QAUser = "QAUser";
      public const string SalesAdmin = "SalesAdmin";
      public const string TrainingCoordinator = "TrainingCoordinator";
      // etc. per data-model.md entity 28
  }
  ```

---

### 2.3 External System Integration (T027-T040)

**Status**: 3/14 tasks (interfaces created)

#### Already Complete ✅
- [x] T027: IDataverseClient interface created
- [x] T028: ID365FoClient interface created
- [x] T029: IDocumentStorage interface created

#### Implementation Needed

- [ ] **T030** Implement DataverseClient with ServiceClient and Managed Identity
  ```bash
  cd src/RE2.DataAccess
  dotnet add package Microsoft.PowerPlatform.Dataverse.Client
  ```
  - Initialize `ServiceClient` with connection string
  - Configure Managed Identity authentication
  - Add methods: `ExecuteAsync`, `RetrieveAsync`, `QueryAsync`
  - Implement error handling and logging
  - Reference: `research.md` section 1

- [ ] **T031** Implement D365FoClient with HttpClient and OAuth2
  ```bash
  dotnet add package Microsoft.Extensions.Http
  ```
  - Create `HttpClient` with base address from config
  - Add OAuth2 token acquisition (Azure AD client credentials)
  - Implement OData query methods
  - Add retry policy for transient failures
  - Reference: `research.md` section 2

- [ ] **T032** [P] Implement DocumentStorageClient with Azure Blob Storage SDK
  ```bash
  dotnet add package Azure.Storage.Blobs
  dotnet add package Azure.Identity
  ```
  - Initialize `BlobServiceClient`
  - Create container if not exists
  - Implement: `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `ExistsAsync`
  - Use Managed Identity or connection string

- [ ] **T033** [P] Configure resilience handler for DataverseClient
  ```bash
  cd src/RE2.DataAccess
  dotnet add package Microsoft.Extensions.Http.Resilience
  ```
  - Add standard resilience handler (retry, circuit breaker, timeout)
  - Configure retry: 3 attempts with exponential backoff
  - Configure circuit breaker: 5 failures in 30 seconds
  - Configure timeout: 30 seconds per request
  - Reference: `research.md` section 4

- [ ] **T034** [P] Configure resilience handler for D365FoClient
  - Same configuration as T033
  - Apply to HttpClient registration

- [ ] **T035** Create `InfrastructureExtensions.AddDataverseServices()` in DataAccess
  - File: `src/RE2.DataAccess/DependencyInjection/InfrastructureExtensions.cs`
  - Register `IDataverseClient` → `DataverseClient` (scoped)
  - Configure ServiceClient with connection string from IConfiguration
  - Add resilience policies
  - Reference: `research.md` section 3

- [ ] **T036** [P] Create `InfrastructureExtensions.AddD365FOServices()`
  - Register `ID365FoClient` → `D365FoClient` (scoped)
  - Configure HttpClient with base URL
  - Add OAuth2 token handler
  - Add resilience policies

- [ ] **T037** [P] Create `InfrastructureExtensions.AddBlobStorageServices()`
  - Register `IDocumentStorage` → `DocumentStorageClient` (scoped)
  - Configure BlobServiceClient with connection string or Managed Identity
  - Set container name from configuration

- [ ] **T038** Register DI services in ComplianceApi `Program.cs`
  ```csharp
  builder.Services.AddDataverseServices(builder.Configuration);
  builder.Services.AddD365FOServices(builder.Configuration);
  builder.Services.AddBlobStorageServices(builder.Configuration);
  ```

- [ ] **T039** [P] Register DI services in ComplianceWeb `Program.cs`
  - Same as T038

- [ ] **T040** [P] Register DI services in ComplianceFunctions `Program.cs`
  - Same as T038
  - Note: May need to convert console app to Azure Functions project first

---

### 2.4 API Infrastructure (T041-T047)

**Status**: 0/7 tasks

- [ ] **T041** Configure API versioning with Asp.Versioning.Mvc
  ```bash
  cd src/RE2.ComplianceApi
  dotnet add package Asp.Versioning.Mvc
  ```
  - Configure in `Program.cs`:
    ```csharp
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });
    ```
  - Reference: `research.md` section 5

- [ ] **T042** Configure Swagger/OpenAPI for v1 API documentation
  ```bash
  dotnet add package Swashbuckle.AspNetCore
  ```
  - Add Swagger in `Program.cs`
  - Configure multiple API versions
  - Add XML comments for documentation
  - Set up operation filters for common parameters

- [ ] **T043** [P] Configure OAuth2 security in Swagger
  - Add OAuth2 authorization to Swagger UI
  - Configure Azure AD token endpoint
  - Enable "Try it out" with real authentication
  - Reference: `research.md` section 6

- [ ] **T044** Implement error handling middleware
  - Complete: `src/RE2.ComplianceApi/Middleware/ErrorHandlingMiddleware.cs`
  - Map exception types to HTTP status codes:
    - `ValidationException` → 400 Bad Request
    - `NotFoundException` → 404 Not Found
    - `UnauthorizedException` → 401 Unauthorized
    - `ForbiddenException` → 403 Forbidden
    - `Exception` → 500 Internal Server Error
  - Return standardized ErrorResponse per `transaction-validation-api.yaml`
  - Include correlation ID for tracing

- [ ] **T045** [P] Create request logging middleware
  - File: `src/RE2.ComplianceApi/Middleware/RequestLoggingMiddleware.cs`
  - Log: HTTP method, path, query string, request ID, user identity
  - Log response: status code, duration
  - Use structured logging (JSON format)
  - Exclude sensitive data (Authorization header)

- [ ] **T046** Configure Application Insights telemetry
  ```bash
  dotnet add package Microsoft.ApplicationInsights.AspNetCore
  ```
  - Add Application Insights in `Program.cs`
  - Configure instrumentation key from configuration
  - Enable request tracking
  - Enable dependency tracking
  - Add custom telemetry for business events

- [ ] **T047** [P] Create standardized error response DTOs
  - File: `src/RE2.Shared/Models/ErrorResponse.cs`
  - Match schema in `transaction-validation-api.yaml`:
    ```csharp
    public class ErrorResponse
    {
        public ErrorDetail Error { get; set; }
    }

    public class ErrorDetail
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ValidationError>? Details { get; set; }
    }
    ```

---

### 2.5 Shared Domain Foundation (T048-T052)

**Status**: 3/5 tasks (constants created)

#### Already Complete ✅
- [x] T048: ErrorCodes constants created
- [x] T049: LicenceTypes constants created
- [x] T050: SubstanceCategories constants created

#### Implementation Needed

- [ ] **T051** [P] Create DateTimeExtensions
  - File: `src/RE2.Shared/Extensions/DateTimeExtensions.cs`
  ```csharp
  public static class DateTimeExtensions
  {
      public static bool IsExpired(this DateTime expiryDate)
          => expiryDate < DateTime.UtcNow;

      public static bool IsExpiringWithin(this DateTime expiryDate, int days)
          => expiryDate < DateTime.UtcNow.AddDays(days);

      public static bool IsBetween(this DateTime date, DateTime start, DateTime end)
          => date >= start && date <= end;
  }
  ```

- [ ] **T052** [P] Create ValidationResult value object
  - File: `src/RE2.ComplianceCore/Models/ValidationResult.cs`
  ```csharp
  public class ValidationResult
  {
      public bool IsValid { get; set; }
      public List<string> Errors { get; set; } = new();
      public List<string> Warnings { get; set; } = new();

      public static ValidationResult Success()
          => new ValidationResult { IsValid = true };

      public static ValidationResult Failure(params string[] errors)
          => new ValidationResult { IsValid = false, Errors = errors.ToList() };
  }
  ```

**✅ Phase 2 Checkpoint**: Foundation complete - user story implementation can now begin

---

## 📦 Phase 3: User Story 1 - Licence Management (P1 - MVP)

**Purpose**: Manage legal licence requirements
**Status**: 5/28 tasks (models and controllers scaffolded)
**Priority**: HIGH (MVP required)

### Already Complete ✅
- [x] T060: LicenceType model created
- [x] T061: ControlledSubstance model created
- [x] T063: Licence model created
- [x] T075: LicencesController scaffolded
- [x] ILicenceRepository interface created

---

### 3.1 Tests for User Story 1 (TDD - Write First) (T053-T059)

**IMPORTANT**: Write these tests FIRST, verify they FAIL, then implement

- [ ] **T053** [P] Unit tests for LicenceType model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceTypeTests.cs`
  - Test: Valid licence type creation
  - Test: Permitted activities validation
  - Test: Audit fields are set correctly

- [ ] **T054** [P] Unit tests for ControlledSubstance model
  - File: `tests/RE2.ComplianceCore.Tests/Models/ControlledSubstanceTests.cs`
  - Test: Substance categorization (List I vs List II)
  - Test: Precursor category validation
  - Test: Reclassification tracking

- [ ] **T055** [P] Unit tests for LicenceSubstanceMapping model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceSubstanceMappingTests.cs`
  - Test: Mapping creation with effective dates
  - Test: Expiry date validation

- [ ] **T056** [P] Unit tests for Licence model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceTests.cs`
  - Test: Licence validity checks (expired, suspended)
  - Test: Scope validation
  - Test: Alert generation flags
  - Test: Optimistic concurrency (RowVersion)

- [ ] **T057** Contract tests for Dataverse LicenceType entity
  - File: `tests/RE2.Contract.Tests/DataverseLicenceTypeContractTests.cs`
  - Test: Can retrieve LicenceType from Dataverse
  - Test: Response matches expected schema
  - Use TestContainers or mock server

- [ ] **T058** Contract tests for Dataverse ControlledSubstance entity
  - File: `tests/RE2.Contract.Tests/DataverseControlledSubstanceContractTests.cs`
  - Test: Can retrieve ControlledSubstance
  - Test: Category mapping is correct

- [ ] **T059** Integration tests for GET /api/v1/licences
  - File: `tests/RE2.ComplianceApi.Tests/Controllers/V1/LicencesControllerTests.cs`
  - Test: Authorized user can list licences
  - Test: Returns 401 for unauthenticated request
  - Use `WebApplicationFactory<Program>`

---

### 3.2 Missing Models (T062, T064-T067)

- [ ] **T062** [P] Create LicenceSubstanceMapping model
  - File: `src/RE2.ComplianceCore/Models/LicenceSubstanceMapping.cs`
  - Properties: Id, LicenceId, SubstanceId, EffectiveDate, ExpiryDate
  - Per data-model.md entity 4

- [ ] **T064** [P] Create LicenceType DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/LicenceTypeDto.cs`
  - Map Dataverse column names to C# properties
  - Add conversion methods: `ToEntity()`, `FromEntity()`

- [ ] **T065** [P] Create ControlledSubstance DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/ControlledSubstanceDto.cs`

- [ ] **T066** [P] Create LicenceSubstanceMapping DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/LicenceSubstanceMappingDto.cs`

- [ ] **T067** Create Licence DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/LicenceDto.cs`

---

### 3.3 Repository Layer (T068-T073)

- [ ] **T068** Create ILicenceRepository interface (extend existing)
  - Add methods:
    ```csharp
    Task<Licence?> GetByIdAsync(Guid id);
    Task<Licence?> GetByLicenceNumberAsync(string licenceNumber);
    Task<IEnumerable<Licence>> GetByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<Licence>> GetExpiringLicencesAsync(DateTime beforeDate);
    Task<Licence> CreateAsync(Licence licence);
    Task<Licence> UpdateAsync(Licence licence);
    Task<bool> DeleteAsync(Guid id);
    ```

- [ ] **T069** Implement DataverseLicenceRepository
  - File: `src/RE2.DataAccess/Dataverse/Repositories/DataverseLicenceRepository.cs`
  - Implement all ILicenceRepository methods
  - Use IDataverseClient for queries
  - Handle optimistic concurrency with RowVersion

- [ ] **T070** Create ILicenceTypeRepository interface
  - File: `src/RE2.ComplianceCore/Interfaces/ILicenceTypeRepository.cs`
  - Methods: GetAll, GetById, GetByCode, Create, Update, Delete

- [ ] **T071** Implement DataverseLicenceTypeRepository
  - File: `src/RE2.DataAccess/Dataverse/Repositories/DataverseLicenceTypeRepository.cs`

- [ ] **T072** Create IControlledSubstanceRepository interface
  - File: `src/RE2.ComplianceCore/Interfaces/IControlledSubstanceRepository.cs`
  - Methods: GetAll, GetById, GetByCategory, GetByCodes

- [ ] **T073** Implement DataverseControlledSubstanceRepository
  - File: `src/RE2.DataAccess/Dataverse/Repositories/DataverseControlledSubstanceRepository.cs`

---

### 3.4 Business Logic Layer (T074)

- [ ] **T074** Create LicenceService
  - File: `src/RE2.ComplianceCore/Services/LicenceValidation/LicenceService.cs`
  ```csharp
  public class LicenceService
  {
      private readonly ILicenceRepository _licenceRepo;
      private readonly ILicenceTypeRepository _licenceTypeRepo;

      public async Task<ValidationResult> ValidateLicenceAsync(Guid licenceId);
      public async Task<bool> IsLicenceValidAsync(Guid licenceId);
      public async Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead);
      public async Task<Licence> CreateLicenceAsync(Licence licence);
      // etc.
  }
  ```
  - Implement business validation rules from data-model.md
  - Check permitted activities mapping
  - Validate substance-to-licence-type mappings

---

### 3.5 API Implementation (T075, T078-T080)

- [ ] **T075** Complete LicencesController v1 implementation
  - Implement GET /api/v1/licences (list with filtering)
  - Implement GET /api/v1/licences/{id} (get by ID)
  - Implement POST /api/v1/licences (create)
  - Implement PUT /api/v1/licences/{id} (update)
  - Implement DELETE /api/v1/licences/{id} (soft delete)
  - Add validation and error handling
  - Return proper HTTP status codes

- [ ] **T078** Add validation for licence type permitted activities
  - Validate activities are from allowed set (possess, store, distribute, etc.)
  - Ensure activities match licence type capabilities

- [ ] **T079** Add validation for substance-to-licence-type mappings
  - Ensure substances can be authorized by selected licence type
  - Check Opium Act List compatibility

- [ ] **T080** Configure route authorization
  - Add `[Authorize(Policy = "InternalUsers")]` to controller
  - Restrict POST/PUT/DELETE to ComplianceManager role:
    ```csharp
    [Authorize(Roles = "ComplianceManager")]
    [HttpPost]
    public async Task<IActionResult> CreateLicence(...)
    ```

---

### 3.6 Web UI Implementation (T076-T077)

- [ ] **T076** Create licence management UI views (complete existing)
  - Implement Index.cshtml (connect to API, display data)
  - Implement Create.cshtml (form submission)
  - Add Edit.cshtml (update form)
  - Add Details.cshtml (view licence details)

- [ ] **T077** Create LicencesController for web UI
  - File: `src/RE2.ComplianceWeb/Controllers/LicencesController.cs`
  - Implement actions: Index, Create, Edit, Details, Delete
  - Call ComplianceApi via HttpClient
  - Handle errors and display user-friendly messages

**✅ Phase 3 Checkpoint**: User Story 1 complete - licence management operational

---

## 👥 Phase 4: User Story 2 - Customer Onboarding (P1 - MVP)

**Purpose**: Create customer profiles and manage qualification
**Status**: 2/18 tasks (Customer model and controller scaffolded)
**Priority**: HIGH (MVP required)

### Already Complete ✅
- [x] T085: Customer model created
- [x] T091: CustomersController scaffolded

---

### 4.1 Tests for User Story 2 (TDD - Write First) (T081-T084)

- [ ] **T081** [P] Unit tests for Customer model
  - File: `tests/RE2.ComplianceCore.Tests/Models/CustomerTests.cs`
  - Test: Approval status validation
  - Test: Suspension logic (blocks transactions)
  - Test: Re-verification date calculation

- [ ] **T082** [P] Unit tests for QualificationReview model
  - File: `tests/RE2.ComplianceCore.Tests/Models/QualificationReviewTests.cs`
  - Test: Review outcome tracking
  - Test: Next review date calculation

- [ ] **T083** Contract tests for Dataverse Customer entity
  - File: `tests/RE2.Contract.Tests/DataverseCustomerContractTests.cs`
  - Test: Can retrieve customer data
  - Test: Licence associations work correctly

- [ ] **T084** Integration tests for GET /api/v1/customers/{id}/compliance-status
  - File: `tests/RE2.ComplianceApi.Tests/Controllers/V1/CustomersControllerTests.cs`
  - Test: Returns compliance status within 1 second (SC-033)
  - Test: Includes held licences and warnings
  - Use mock data or TestContainers

---

### 4.2 Missing Models (T086-T087)

- [ ] **T086** [P] Create QualificationReview model
  - File: `src/RE2.ComplianceCore/Models/QualificationReview.cs`
  - Properties: Id, CustomerId, ReviewDate, Reviewer, Outcome, NextReviewDate
  - Per data-model.md entity 29

- [ ] **T087** [P] Create Customer DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/CustomerDto.cs`

---

### 4.3 Repository Layer (T088-T089)

- [ ] **T088** Create ICustomerRepository interface
  - File: `src/RE2.ComplianceCore/Interfaces/ICustomerRepository.cs`
  - Methods: GetById, GetAll, GetByStatus, GetByCountry, Create, Update, Delete

- [ ] **T089** Implement DataverseCustomerRepository
  - File: `src/RE2.DataAccess/Dataverse/Repositories/DataverseCustomerRepository.cs`
  - Implement all ICustomerRepository methods
  - Include related licences in queries

---

### 4.4 Business Logic Layer (T090, T095-T097)

- [ ] **T090** Create CustomerService
  - File: `src/RE2.ComplianceCore/Services/CustomerQualification/CustomerService.cs`
  ```csharp
  public class CustomerService
  {
      public async Task<Customer> GetCustomerWithComplianceStatusAsync(Guid customerId);
      public async Task<ValidationResult> ValidateCustomerQualificationAsync(Guid customerId);
      public async Task<bool> CanCustomerReceiveSubstanceAsync(Guid customerId, Guid substanceId);
      // etc.
  }
  ```

- [ ] **T095** Implement customer approval status validation
  - Per data-model.md validation rules
  - FR-016: Prevent approval if required licences missing
  - Check all required licence types are held

- [ ] **T096** Implement suspension logic
  - Per data-model.md: isSuspended blocks all transactions
  - Suspended customers cannot receive shipments regardless of approval

- [ ] **T097** Add re-verification due date tracking
  - FR-017: Calculate next re-verification date
  - Generate alerts for upcoming re-verification
  - Default: 3 years (configurable per customer)

---

### 4.5 API Implementation (T091, T098)

- [ ] **T091** Complete CustomersController v1 implementation
  - Implement GET /api/v1/customers/{id}/compliance-status
    - **Performance requirement**: <1 second response (SC-033)
    - Return: approval status, held licences, missing licences, warnings
  - Implement GET /api/v1/customers (list)
  - Implement GET /api/v1/customers/{id} (details)
  - Implement POST /api/v1/customers (create)
  - Implement PUT /api/v1/customers/{id} (update)

- [ ] **T098** Configure route authorization
  - SalesAdmin and ComplianceManager can create/modify customers
  ```csharp
  [Authorize(Roles = "SalesAdmin,ComplianceManager")]
  [HttpPost]
  public async Task<IActionResult> CreateCustomer(...)
  ```

---

### 4.6 Web UI Implementation (T092-T094)

- [ ] **T092** Create customer management UI views (complete existing)
  - Implement Index.cshtml (customer list)
  - Add Create.cshtml (new customer form)
  - Add Edit.cshtml (update customer)
  - Add Details.cshtml (customer details with compliance status)

- [ ] **T093** Create CustomersController for web UI
  - File: `src/RE2.ComplianceWeb/Controllers/CustomersController.cs`
  - Implement actions: Index, Create, Edit, Details
  - Display compliance status prominently
  - Show held licences and warnings

- [ ] **T094** Extend LicencesController web UI for customer associations
  - Add ability to link licences to customers
  - Show customer's licences in customer details view
  - Allow assigning/removing licences

**✅ Phase 4 Checkpoint**: User Story 2 complete - customer onboarding operational

---

## 📄 Phase 5: User Story 3 - Document Management & Alerts (P1 - MVP)

**Purpose**: Upload documents, track verification, monitor expiry
**Status**: 1/23 tasks (Dashboard view created)
**Priority**: HIGH (MVP required)

### Already Complete ✅
- [x] T122: Dashboard view scaffolded

---

### 5.1 Tests for User Story 3 (TDD - Write First) (T099-T103)

- [ ] **T099** [P] Unit tests for LicenceDocument model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceDocumentTests.cs`
  - Test: Document metadata creation
  - Test: Blob URL validation

- [ ] **T100** [P] Unit tests for LicenceVerification model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceVerificationTests.cs`
  - Test: Verification tracking (method, date, verifier)
  - Test: Verification methods (IGJ website, email, Farmatec)

- [ ] **T101** [P] Unit tests for LicenceScopeChange model
  - File: `tests/RE2.ComplianceCore.Tests/Models/LicenceScopeChangeTests.cs`
  - Test: Scope change tracking with effective dates
  - Test: Historical scope retrieval

- [ ] **T102** Integration tests for document upload
  - File: `tests/RE2.ComplianceApi.Tests/Controllers/V1/LicencesControllerDocumentTests.cs`
  - Test: Can upload PDF document
  - Test: Document metadata is stored correctly
  - Test: Blob storage integration works

- [ ] **T103** Integration tests for Azure Blob Storage
  - File: `tests/RE2.DataAccess.Tests/BlobStorage/DocumentStorageClientTests.cs`
  - Test: Upload returns blob URL
  - Test: Download retrieves correct content
  - Test: Delete removes blob
  - Use TestContainers or Azurite

---

### 5.2 Missing Models (T104-T109)

- [ ] **T104** [P] Create LicenceDocument model
  - File: `src/RE2.ComplianceCore/Models/LicenceDocument.cs`
  - Properties: Id, LicenceId, DocumentType, FileName, BlobUrl, UploadDate, UploadedBy
  - Per data-model.md entity 12

- [ ] **T105** [P] Create LicenceVerification model
  - File: `src/RE2.ComplianceCore/Models/LicenceVerification.cs`
  - Properties: Id, LicenceId, VerificationMethod, VerificationDate, VerifiedBy, Outcome
  - Per data-model.md entity 13

- [ ] **T106** [P] Create LicenceScopeChange model
  - File: `src/RE2.ComplianceCore/Models/LicenceScopeChange.cs`
  - Properties: Id, LicenceId, ChangeType, EffectiveDate, PreviousScope, NewScope
  - Per data-model.md entity 14

- [ ] **T107** [P] Create Alert model
  - File: `src/RE2.ComplianceCore/Models/Alert.cs`
  - Properties: Id, AlertType, EntityId, EntityType, Message, Severity, CreatedDate, AcknowledgedDate
  - Per data-model.md entity 11

- [ ] **T108** [P] Create LicenceDocument DTO for Dataverse
  - File: `src/RE2.DataAccess/Dataverse/Models/LicenceDocumentDto.cs`

- [ ] **T109** [P] Create Alert DTO for D365 F&O
  - File: `src/RE2.DataAccess/D365FinanceOperations/Models/AlertDto.cs`

---

### 5.3 Repository Extensions (T110-T111)

- [ ] **T110** Extend ILicenceRepository with document methods
  - Add: `Task<LicenceDocument> AddDocumentAsync(Guid licenceId, Stream content, string fileName)`
  - Add: `Task<IEnumerable<LicenceDocument>> GetDocumentsAsync(Guid licenceId)`
  - Add: `Task<Stream> DownloadDocumentAsync(Guid documentId)`

- [ ] **T111** Implement document upload in DataverseLicenceRepository
  - Use IDocumentStorage to upload to blob storage
  - Store metadata in Dataverse
  - Return blob URL

---

### 5.4 Business Logic Extensions (T112-T113, T121)

- [ ] **T112** Extend LicenceService with verification recording
  - FR-009: Record verification method, date, verifier
  - Methods: `RecordVerificationAsync(Guid licenceId, VerificationMethod, string verifiedBy)`
  - Store in LicenceVerification entity

- [ ] **T113** Extend LicenceService with scope change recording
  - FR-010: Track scope changes with effective dates
  - Methods: `RecordScopeChangeAsync(Guid licenceId, string newScope, DateTime effectiveDate)`
  - Maintain history of all scope changes

- [ ] **T121** Implement alert generation logic
  - Create AlertService in `src/RE2.ComplianceCore/Services/AlertService.cs`
  - Generate alerts for:
    - Licences expiring in 90 days (warning)
    - Licences expiring in 60 days (warning)
    - Licences expiring in 30 days (critical)
  - Store alerts in Alert entity (D365 F&O)

---

### 5.5 API Extensions (T114-T116)

- [ ] **T114** Add POST /api/v1/licences/{id}/documents
  - Accept multipart/form-data with file upload
  - Validate file type (PDF, images)
  - Maximum file size: 10MB
  - Return document metadata with blob URL

- [ ] **T115** Add POST /api/v1/licences/{id}/verifications
  - Accept: `{ method: "IGJ_Website", verifiedBy: "John Doe", outcome: "Valid" }`
  - Record verification in LicenceVerification
  - Return verification record

- [ ] **T116** Add POST /api/v1/licences/{id}/scope-changes
  - Accept: `{ effectiveDate: "2026-02-01", newScope: "Extended to List II substances" }`
  - Record scope change with effective date
  - Return scope change record

---

### 5.6 Web UI Extensions (T117-T119)

- [ ] **T117** Add file upload UI
  - File: `src/RE2.ComplianceWeb/Views/Licences/UploadDocument.cshtml`
  - Form with file input and document type selection
  - Display uploaded documents list with download links
  - Show upload progress

- [ ] **T118** Add verification recording UI
  - File: `src/RE2.ComplianceWeb/Views/Licences/RecordVerification.cshtml`
  - Form with verification method dropdown (IGJ website, Email, Farmatec)
  - Verifier name input
  - Outcome selection
  - Display verification history

- [ ] **T119** Add scope change history UI
  - File: `src/RE2.ComplianceWeb/Views/Licences/ScopeHistory.cshtml`
  - Timeline of scope changes
  - Show effective dates
  - Display previous and new scope for each change

---

### 5.7 Azure Function Implementation (T120-T121)

**Note**: ComplianceFunctions project needs Azure Functions SDK

- [ ] **Setup** Convert console app to Azure Functions project
  ```bash
  cd src/RE2.ComplianceFunctions
  dotnet add package Microsoft.Azure.Functions.Worker
  dotnet add package Microsoft.Azure.Functions.Worker.Sdk
  ```
  - Update project file to use Azure Functions SDK
  - Add `host.json` configuration

- [ ] **T120** Create LicenceExpiryMonitor Azure Function
  - File: `src/RE2.ComplianceFunctions/LicenceExpiryMonitor.cs`
  ```csharp
  [Function("LicenceExpiryMonitor")]
  public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
  {
      // Runs daily at 2:00 AM
      // Query licences expiring in 90, 60, 30 days
      // Generate alerts
  }
  ```
  - FR-007: Daily expiry checks at 2 AM
  - Timer trigger: `0 0 2 * * *` (cron expression)

- [ ] **T121** Implement alert generation in LicenceExpiryMonitor
  - Query licences using ILicenceRepository
  - For each expiring licence:
    - Check if 90-day alert already generated
    - Check if 60-day alert already generated
    - Check if 30-day alert already generated
  - Create Alert entities in D365 F&O
  - Update licence alert flags (AlertGenerated90Days, etc.)

---

### 5.8 Dashboard Implementation (T122)

- [ ] **T122** Complete alert display dashboard
  - Complete: `src/RE2.ComplianceWeb/Views/Dashboard/Index.cshtml`
  - Connect to API to fetch real alert data
  - Display:
    - Count of expiring licences (90/60/30 days)
    - Count of blocked transactions
    - Count of active licences
    - Count of qualified customers
  - Show recent alerts list
  - Show pending approvals list
  - Refresh data automatically (polling or SignalR)

**✅ Phase 5 Checkpoint**: User Story 3 complete - document management and alert monitoring operational

---

## 🎯 MVP Complete Validation

After completing Phases 2-5, verify the following:

### Build & Test
- [ ] Solution builds without errors: `dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] No compiler warnings

### API Functionality
- [ ] Can authenticate with Azure AD
- [ ] Can create licence: `POST /api/v1/licences`
- [ ] Can list licences: `GET /api/v1/licences`
- [ ] Can upload document: `POST /api/v1/licences/{id}/documents`
- [ ] Can check customer compliance: `GET /api/v1/customers/{id}/compliance-status`
- [ ] Swagger UI accessible at `/swagger`

### Web UI Functionality
- [ ] Can navigate to Licence Management page
- [ ] Can create new licence via web form
- [ ] Can view licence details
- [ ] Can upload licence document
- [ ] Dashboard displays alerts

### Performance
- [ ] Customer compliance lookup: <1 second (SC-033)
- [ ] API responds within acceptable time under load

### Azure Functions
- [ ] LicenceExpiryMonitor runs on schedule
- [ ] Alerts are generated correctly
- [ ] Alerts appear in dashboard

---

## 📊 Progress Tracking

Use this to track your progress:

| Phase | Tasks | Complete | % |
|-------|-------|----------|---|
| Phase 1: Setup | 20 | 20 | 100% ✅ |
| Phase 2: Foundation | 32 | 0 | 0% |
| Phase 3: User Story 1 | 28 | 5 | 18% |
| Phase 4: User Story 2 | 18 | 2 | 11% |
| Phase 5: User Story 3 | 23 | 1 | 4% |
| **TOTAL MVP** | **121** | **28** | **23%** |

---

## 🚀 Quick Start Commands

### Install Prerequisites
```bash
# Install Azurite (Azure Storage Emulator)
npm install -g azurite

# Start Azurite
azurite-blob --location C:\azurite --debug C:\azurite\debug.log

# Install Azure Functions Core Tools
winget install Microsoft.Azure.FunctionsCoreTools
```

### Restore Packages
```bash
cd C:\src\RE2
dotnet restore
```

### Build Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run API Locally
```bash
cd src/RE2.ComplianceApi
dotnet run
# API available at: https://localhost:7001
```

### Run Web UI Locally
```bash
cd src/RE2.ComplianceWeb
dotnet run
# Web UI available at: https://localhost:5001
```

### Run Azure Function Locally
```bash
cd src/RE2.ComplianceFunctions
func start
```

---

## 📝 Notes

### Parallel Execution
Tasks marked **[P]** can be executed in parallel by different developers. Example:
- Developer A: T053-T056 (unit tests)
- Developer B: T064-T067 (DTOs)
- Developer C: T041-T043 (API infrastructure)

### TDD Requirement
Per Constitution Principle II, **Test-Driven Development is MANDATORY**:
1. ✅ Write tests first
2. ✅ Verify tests fail (Red)
3. ✅ Implement minimum code (Green)
4. ✅ Refactor (while keeping tests green)

### Constitutional Compliance
- **Library-First**: Core logic in RE2.ComplianceCore (justified violation for multi-service architecture)
- **CLI Interface**: Not implemented (justified violation for web/API architecture)
- **Simplicity**: Avoid premature optimization
- **Independent Stories**: Each user story is independently testable

---

**Last Updated**: 2026-01-12
**Total Remaining Tasks**: 93
**Estimated Effort**: 15-20 hours for MVP completion
**Next Checkpoint**: Phase 2 Foundation (32 tasks)
