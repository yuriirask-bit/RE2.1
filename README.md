# RE2 - Controlled Drug Licence & GDP Compliance Management System

**MVP Scaffold**: Phases 1-5 (Setup through User Story 3)
**Status**: 🏗️ **Structure Created** - Ready for Implementation
**Generated**: 2026-01-12

---

## 📋 Project Overview

A comprehensive licence management and GDP compliance system for Dutch pharmaceutical wholesalers handling controlled drugs. The system provides:

- **Real-time compliance validation** for controlled drug transactions
- **Licence lifecycle management** (capture, verification, monitoring)
- **Customer qualification tracking** with approval workflows
- **GDP compliance** (sites, credentials, inspections, CAPA)
- **Audit trails and reporting** for regulatory compliance

### Key Features

- ✅ Stateless architecture (no local data storage for business data)
- ✅ Azure cloud-native (App Service, Functions, Blob Storage, API Management)
- ✅ RESTful APIs for ERP/WMS integration
- ✅ Web UI for compliance staff
- ✅ Automated expiry monitoring and alerts
- ✅ TDD approach with comprehensive test coverage

---

## 🏗️ Architecture

### Technology Stack

- **.NET 8 LTS** (C# 12) - November 2026 EOL
- **ASP.NET Core 8.0** - Web API and MVC
- **Azure SDK** - Identity, Blob Storage, Service Bus
- **Entity Framework Core 8.0** - Internal state only (NOT for business data)
- **xUnit + Moq + FluentAssertions** - Testing framework
- **TestContainers** - External service mocking

### Project Structure

```
RE2/
├── src/
│   ├── RE2.ComplianceCore/          # Core domain logic (library-first)
│   │   ├── Models/                  # Domain entities
│   │   ├── Services/                # Business logic
│   │   └── Interfaces/              # Abstractions
│   │
│   ├── RE2.DataAccess/              # External API clients
│   │   ├── Dataverse/               # Virtual tables (licences, customers)
│   │   ├── D365FinanceOperations/   # Virtual entities (transactions)
│   │   └── BlobStorage/             # Document storage
│   │
│   ├── RE2.ComplianceApi/           # REST API (Azure App Service)
│   │   ├── Controllers/V1/          # Versioned endpoints
│   │   ├── Middleware/              # Error handling, logging
│   │   └── Authorization/           # Custom policies
│   │
│   ├── RE2.ComplianceWeb/           # Web UI (ASP.NET MVC)
│   │   ├── Controllers/             # MVC controllers
│   │   └── Views/                   # Razor views
│   │
│   ├── RE2.ComplianceFunctions/     # Background jobs (Azure Functions)
│   │
│   └── RE2.Shared/                  # Constants and utilities
│       └── Constants/               # ErrorCodes, LicenceTypes, etc.
│
└── tests/
    ├── RE2.ComplianceCore.Tests/    # Unit tests
    ├── RE2.ComplianceApi.Tests/     # Integration tests
    ├── RE2.DataAccess.Tests/        # Data access tests
    └── RE2.Contract.Tests/          # Contract tests
```

---

## ✅ Phase 1 Complete: Setup (T001-T020)

### Created Artifacts

- [x] .NET 8 solution structure (RE2.sln)
- [x] 6 source projects initialized
- [x] 4 test projects initialized
- [x] Project references configured (dependency graph)
- [x] .gitignore (C#/.NET patterns)
- [x] .editorconfig (C# formatting rules)
- [x] appsettings.json templates (API, Web, Functions)

---

## 🏗️ Phases 2-5: Scaffolded Structure

### Phase 2: Foundation (T021-T052) - BLOCKING

**Status**: Directory structure and interface placeholders created

#### Core Interfaces Created
- ✅ `IDataverseClient` - Virtual table access (licences, customers, GDP)
- ✅ `ID365FoClient` - Virtual entity access (transactions, audit events)
- ✅ `IDocumentStorage` - Azure Blob Storage for documents

#### Placeholder Implementations
- ✅ `DataverseClient` - To implement with ServiceClient + Managed Identity
- ✅ `D365FoClient` - To implement with HttpClient + OAuth2
- ✅ `DocumentStorageClient` - To implement with Azure.Storage.Blobs

#### Constants & Shared Code
- ✅ `ErrorCodes` - Standardized error codes (FR-064)
- ✅ `LicenceTypes` - Controlled drug licence categories
- ✅ `SubstanceCategories` - Opium Act classifications

#### TODO: Implementation Needed
- [ ] Authentication configuration (Azure AD, Azure AD B2C)
- [ ] API versioning setup (Asp.Versioning.Mvc)
- [ ] Swagger/OpenAPI documentation
- [ ] Error handling middleware implementation
- [ ] Request logging middleware
- [ ] Application Insights telemetry
- [ ] Resilience patterns (retry, circuit breaker, timeout)
- [ ] Dependency injection registration

---

### Phase 3: User Story 1 - Licence Management (T053-T080)

**Status**: Core domain models and API scaffolding created

#### Domain Models Created
- ✅ `LicenceType` - Licence type definitions with permitted activities
- ✅ `ControlledSubstance` - Opium Act substances (List I/II, precursors)
- ✅ `Licence` - Licence instance with validity, status, verification tracking

#### API Controllers Created
- ✅ `LicencesController` (v1) - CRUD endpoints for licence management

#### Web UI Views Created
- ✅ `Views/Licences/Index.cshtml` - Licence listing page
- ✅ `Views/Licences/Create.cshtml` - Licence creation form

#### TODO: Implementation Needed
- [ ] Repository interfaces (ILicenceRepository, ILicenceTypeRepository, etc.)
- [ ] Repository implementations (DataverseLicenceRepository, etc.)
- [ ] LicenceService business logic
- [ ] Controller action implementations (GET, POST, PUT, DELETE)
- [ ] Web UI controller implementations
- [ ] Validation rules for licence types and substance mappings
- [ ] Authorization: ComplianceManager role restrictions

---

### Phase 4: User Story 2 - Customer Onboarding (T081-T098)

**Status**: Domain models and API scaffolding created

#### Domain Models Created
- ✅ `Customer` - Customer profile with compliance status and licences

#### API Controllers Created
- ✅ `CustomersController` (v1) - Compliance status and CRUD endpoints

#### Web UI Views Created
- ✅ `Views/Customers/Index.cshtml` - Customer listing page

#### TODO: Implementation Needed
- [ ] QualificationReview model
- [ ] Customer repository interface and implementation
- [ ] CustomerService business logic
- [ ] Compliance status API endpoint (<1 second response time - SC-033)
- [ ] Customer CRUD operations
- [ ] Approval status validation (FR-016: prevent approval if licences missing)
- [ ] Suspension logic (FR-015)
- [ ] Re-verification tracking (FR-017)

---

### Phase 5: User Story 3 - Document & Alert Management (T099-T122)

**Status**: Partially scaffolded (models pending, views created)

#### Web UI Views Created
- ✅ `Views/Dashboard/Index.cshtml` - Compliance dashboard with alert summary

#### TODO: Models to Create
- [ ] `LicenceDocument` - Document metadata with blob storage URL
- [ ] `LicenceVerification` - Verification activity tracking
- [ ] `LicenceScopeChange` - Scope change history
- [ ] `Alert` - Alert/notification entity

#### TODO: Implementation Needed
- [ ] Document upload API endpoints
- [ ] Verification recording API
- [ ] Scope change history API
- [ ] LicenceExpiryMonitor Azure Function (timer trigger, daily at 2 AM)
- [ ] Alert generation logic (90/60/30 day warnings - FR-007)
- [ ] Alert dashboard implementation
- [ ] Blob storage integration for document uploads

---

## 🚀 Quick Start

### Prerequisites

- ✅ **.NET 8 SDK** (8.0.416 installed)
- [ ] **Visual Studio 2022** (17.8+) or **VS Code** with C# extension
- [ ] **Azure CLI** (for deployment)
- [ ] **Docker Desktop** (for TestContainers)
- [ ] **Azurite** (Azure Storage Emulator for local development)

### Build the Solution

```bash
cd C:\src\RE2
dotnet restore
dotnet build
```

### Run the API

```bash
cd src/RE2.ComplianceApi
dotnet run
```

API will be available at: `https://localhost:7001/api/v1`

### Run the Web UI

```bash
cd src/RE2.ComplianceWeb
dotnet run
```

Web UI will be available at: `https://localhost:5001`

### Run Tests

```bash
dotnet test
```

---

## 📝 Development Workflow

### TDD Approach (Constitutional Requirement)

1. **Write tests first** (Red)
2. **Get user/stakeholder approval** on tests
3. **Verify tests fail**
4. **Implement minimum code** to pass tests (Green)
5. **Refactor** while keeping tests green

### Implementation Order (MVP)

```
Phase 1: Setup ✅ COMPLETE
  ↓
Phase 2: Foundation (BLOCKING) 🏗️ SCAFFOLDED
  ↓
Phase 3: User Story 1 (Licence Management) 🏗️ SCAFFOLDED
  ↓
Phase 4: User Story 2 (Customer Onboarding) 🏗️ SCAFFOLDED
  ↓
Phase 5: User Story 3 (Document & Alerts) 🏗️ SCAFFOLDED
```

**MVP Scope**: 121 tasks total
- Phase 1: 20 tasks ✅ Complete
- Phase 2: 32 tasks 🏗️ Scaffolded (BLOCKING)
- Phase 3-5: 69 tasks 🏗️ Scaffolded

---

## 🔑 Key API Endpoints (To Be Implemented)

### Licence Management
- `GET /api/v1/licences` - List licences
- `GET /api/v1/licences/{id}` - Get licence details
- `POST /api/v1/licences` - Create licence
- `PUT /api/v1/licences/{id}` - Update licence
- `DELETE /api/v1/licences/{id}` - Delete licence

### Customer Compliance
- `GET /api/v1/customers/{id}/compliance-status` - Check customer compliance (<1s)

### Transaction Validation
- `POST /api/v1/transactions/validate` - Validate transaction (<3s)
- `GET /api/v1/transactions/{externalId}/status` - Get transaction status
- `POST /api/v1/transactions/{transactionId}/override` - Approve override

---

## 📊 Performance Targets

| Operation | Target | Requirement |
|-----------|--------|-------------|
| Transaction validation | <3 seconds | SC-005 |
| Customer compliance lookup | <1 second | SC-033 |
| Audit report generation | <2 minutes | SC-009 |
| Concurrent validation requests | 50 requests | SC-032 |
| System availability | 99.5% uptime | FR-052 |
| Mean time to recovery | <30 minutes | SC-031 |

---

## 🔒 Security & Compliance

### Authentication
- **Internal users**: Azure AD with JWT tokens
- **External users**: Azure AD B2C with SSO
- **API authentication**: OAuth2 bearer tokens

### Authorization
- **ComplianceManager**: Full access to licences, approvals, overrides
- **QAUser**: GDP site management, inspections, reports
- **SalesAdmin**: Customer onboarding, qualification
- **TrainingCoordinator**: Training records management

### Compliance Features
- **Audit trail**: All data changes logged (FR-027)
- **Optimistic concurrency**: RowVersion fields prevent conflicts (FR-027a)
- **Standardized error codes**: Consistent API responses (FR-064)
- **API versioning**: 6-month backward compatibility (FR-062)

---

## 📚 Documentation

- **Specification**: `specs/001-licence-management/spec.md`
- **Technical Plan**: `specs/001-licence-management/plan.md`
- **Data Model**: `specs/001-licence-management/data-model.md`
- **API Contracts**: `specs/001-licence-management/contracts/`
- **Quickstart Guide**: `specs/001-licence-management/quickstart.md`
- **Research**: `specs/001-licence-management/research.md`
- **Tasks**: `specs/001-licence-management/tasks.md` (298 tasks total)

---

## 🎯 Next Steps

### Immediate Actions

1. **Phase 2 Implementation** (Foundation - BLOCKING):
   - Install Azure SDK packages (NuGet restore)
   - Implement authentication configuration (Azure AD, B2C)
   - Implement Dataverse and D365 F&O clients
   - Set up API versioning and Swagger
   - Implement error handling and logging middleware
   - Configure resilience patterns
   - Register dependency injection services

2. **Phase 3 Implementation** (User Story 1):
   - Write unit tests for domain models (TDD)
   - Implement repository interfaces
   - Implement Dataverse repositories
   - Implement LicenceService business logic
   - Complete API controller actions
   - Complete web UI controllers and views

3. **Environment Setup**:
   - Configure Azure AD tenant (or use mock tokens)
   - Start Azurite for local blob storage
   - Set up TestContainers for integration tests
   - Configure Application Insights (or disable locally)

### Commands

```bash
# Restore NuGet packages (may require network connection)
dotnet restore

# Build solution
dotnet build

# Run tests (after implementation)
dotnet test

# Run API locally
dotnet run --project src/RE2.ComplianceApi

# Run Web UI locally
dotnet run --project src/RE2.ComplianceWeb
```

---

## 🛠️ Troubleshooting

### Common Issues

1. **NuGet restore fails**:
   - Check network connectivity
   - Clear NuGet cache: `dotnet nuget locals all --clear`
   - Retry restore: `dotnet restore --force`

2. **Azure services unavailable**:
   - Use Azurite for local blob storage
   - Mock Dataverse/D365 F&O with TestContainers
   - Disable Application Insights in development

3. **Authentication issues**:
   - Configure mock JWT tokens for local development
   - Use development certificates: `dotnet dev-certs https --trust`

---

## 📞 Support

- **GitHub Issues**: [Report issues](https://github.com/anthropics/claude-code/issues)
- **Documentation**: See `specs/001-licence-management/` directory
- **Constitution**: `.specify/memory/constitution.md`

---

**Status**: 🏗️ Scaffold complete, ready for implementation
**Version**: MVP (Phases 1-5)
**Last Updated**: 2026-01-12
