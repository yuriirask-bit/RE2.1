# RE2 MVP - Quick Reference Guide

**Purpose**: Fast reference for common tasks and commands
**Generated**: 2026-01-12

---

## 📦 Project Status

| Component | Status | Files | Build |
|-----------|--------|-------|-------|
| **Phase 1: Setup** | ✅ Complete | 20/20 | ✅ |
| **Phase 2: Foundation** | 🏗️ Scaffolded | 32/32 | Partial |
| **Phase 3: US1 Licences** | 🏗️ Scaffolded | 28/28 | Partial |
| **Phase 4: US2 Customers** | 🏗️ Scaffolded | 18/18 | Partial |
| **Phase 5: US3 Documents** | 🏗️ Scaffolded | 23/23 | Partial |

**MVP Progress**: 28/121 tasks (23%) - Phase 1 complete, Phases 2-5 scaffolded

---

## ⚡ Quick Commands

### Build & Test
```bash
# Navigate to project
cd C:\src\RE2

# Restore packages (requires network)
dotnet restore

# Build entire solution
dotnet build

# Build specific project
dotnet build src/RE2.ComplianceApi/RE2.ComplianceApi.csproj

# Run tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Run Applications
```bash
# Run API (default: https://localhost:7001)
dotnet run --project src/RE2.ComplianceApi

# Run Web UI (default: https://localhost:5001)
dotnet run --project src/RE2.ComplianceWeb

# Run Azure Functions
cd src/RE2.ComplianceFunctions
func start
```

### Development Tools
```bash
# Watch mode (auto-rebuild on changes)
dotnet watch run --project src/RE2.ComplianceApi

# Generate migration (if using EF Core)
dotnet ef migrations add InitialCreate --project src/RE2.DataAccess

# Trust development certificate
dotnet dev-certs https --trust

# Clean build artifacts
dotnet clean
```

---

## 📁 Project Structure

```
RE2/
├── src/
│   ├── RE2.ComplianceCore/      ✅ Core business logic
│   ├── RE2.DataAccess/          ✅ External API clients
│   ├── RE2.ComplianceApi/       ✅ REST API
│   ├── RE2.ComplianceWeb/       🏗️ Web UI
│   ├── RE2.ComplianceFunctions/ 🏗️ Background jobs
│   └── RE2.Shared/              ✅ Constants & utilities
└── tests/
    ├── RE2.ComplianceCore.Tests/    ⚠️ NuGet restore needed
    ├── RE2.ComplianceApi.Tests/     ⚠️ NuGet restore needed
    ├── RE2.DataAccess.Tests/        ⚠️ NuGet restore needed
    └── RE2.Contract.Tests/          ⚠️ NuGet restore needed
```

**Legend**: ✅ Building | 🏗️ Scaffolded | ⚠️ Needs setup

---

## 🔧 Key Files

### Configuration
- `appsettings.json` (API) - Azure AD, Dataverse, D365 F&O config
- `appsettings.json` (Web) - Azure AD, API base URL
- `local.settings.json` (Functions) - Azure Functions settings
- `.gitignore` - Ignore patterns
- `.editorconfig` - C# formatting rules

### Documentation
- `README.md` - Project overview
- `IMPLEMENTATION_SUMMARY.md` - Detailed scaffolding summary
- `IMPLEMENTATION_CHECKLIST.md` - Task-by-task checklist (93 remaining)
- `specs/001-licence-management/` - Full specification docs

### Domain Models (RE2.ComplianceCore/Models/)
- `LicenceType.cs` - Licence type definitions
- `ControlledSubstance.cs` - Opium Act substances
- `Licence.cs` - Licence instances
- `Customer.cs` - Customer profiles
- `Transaction.cs` - Transactions for validation

### Interfaces (RE2.ComplianceCore/Interfaces/)
- `IDataverseClient.cs` - Virtual table access
- `ID365FoClient.cs` - Virtual entity access
- `IDocumentStorage.cs` - Blob storage
- `ILicenceRepository.cs` - Licence data access

### API Controllers (RE2.ComplianceApi/Controllers/V1/)
- `LicencesController.cs` - Licence CRUD
- `CustomersController.cs` - Customer compliance
- `TransactionValidationController.cs` - Transaction validation

### Constants (RE2.Shared/Constants/)
- `ErrorCodes.cs` - 20+ standardized error codes
- `LicenceTypes.cs` - 10+ licence types
- `SubstanceCategories.cs` - 10+ substance categories

---

## 🎯 Implementation Priority

### Phase 2: Foundation (BLOCKING) 🔴
**Must complete before any user story work**

Key tasks:
1. Authentication setup (Azure AD, Azure AD B2C)
2. External system integration (Dataverse, D365 F&O, Blob Storage)
3. API infrastructure (versioning, Swagger, middleware)
4. DI registration

**Estimated**: 6-8 hours

### Phase 3: User Story 1 (Licence Management) 🟡
After Phase 2 complete.

Key tasks:
1. Write tests first (TDD)
2. Implement repositories
3. Implement LicenceService
4. Complete API controllers
5. Complete web UI

**Estimated**: 4-6 hours

### Phase 4: User Story 2 (Customer Onboarding) 🟡
Can work in parallel with Phase 3 (different files).

Key tasks:
1. Write tests first (TDD)
2. Implement CustomerService
3. Complete API endpoints
4. Complete web UI

**Estimated**: 3-4 hours

### Phase 5: User Story 3 (Documents & Alerts) 🟡
Depends on Phases 3-4.

Key tasks:
1. Implement document upload
2. Create Azure Function for expiry monitoring
3. Complete dashboard

**Estimated**: 3-4 hours

**Total MVP**: 15-20 hours

---

## 🧪 Testing Strategy

### Test Types
1. **Unit Tests** - Core business logic (models, services)
2. **Integration Tests** - API endpoints with real HTTP
3. **Contract Tests** - External API compatibility
4. **Performance Tests** - Response time validation

### TDD Workflow (Required)
```
1. Write test (Red)
2. Verify it fails
3. Implement minimum code (Green)
4. Refactor (keep tests green)
```

### Test Commands
```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test tests/RE2.ComplianceCore.Tests

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~LicenceTypeTests"
```

---

## 🐛 Common Issues & Solutions

### Issue: NuGet packages won't restore
```bash
# Clear cache and retry
dotnet nuget locals all --clear
dotnet restore --force
```

### Issue: Build fails with "project not found"
```bash
# Rebuild solution file
dotnet new sln -n RE2 --force
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj
```

### Issue: Azure services unavailable locally
```bash
# Use Azurite for local blob storage
npm install -g azurite
azurite-blob --location C:\azurite

# Mock Dataverse/D365 with TestContainers in tests
# Or create stub implementations for local dev
```

### Issue: Authentication fails locally
```bash
# Trust development certificate
dotnet dev-certs https --trust

# Use mock authentication for local dev
# Configure in appsettings.Development.json
```

### Issue: Azure Function won't start
```bash
# Install Azure Functions Core Tools
winget install Microsoft.Azure.FunctionsCoreTools

# Initialize function app
func init --worker-runtime dotnet-isolated
```

---

## 📊 API Endpoints (Planned)

### Licences
- `GET /api/v1/licences` - List licences
- `GET /api/v1/licences/{id}` - Get licence
- `POST /api/v1/licences` - Create licence
- `PUT /api/v1/licences/{id}` - Update licence
- `DELETE /api/v1/licences/{id}` - Delete licence
- `POST /api/v1/licences/{id}/documents` - Upload document
- `POST /api/v1/licences/{id}/verifications` - Record verification

### Customers
- `GET /api/v1/customers` - List customers
- `GET /api/v1/customers/{id}` - Get customer
- `GET /api/v1/customers/{id}/compliance-status` - Check compliance (<1s)
- `POST /api/v1/customers` - Create customer
- `PUT /api/v1/customers/{id}` - Update customer

### Transactions
- `POST /api/v1/transactions/validate` - Validate transaction (<3s)
- `GET /api/v1/transactions/{externalId}/status` - Get status
- `POST /api/v1/transactions/{id}/override` - Approve override

---

## 🔒 Security & Roles

### Authentication
- **Internal**: Azure AD (JWT tokens)
- **External**: Azure AD B2C (SSO)

### Roles
- **ComplianceManager**: Full access (licences, approvals, overrides)
- **QAUser**: GDP management (sites, inspections, reports)
- **SalesAdmin**: Customer onboarding
- **TrainingCoordinator**: Training records

### Authorization Example
```csharp
[Authorize(Policy = "InternalUsers")]
[Authorize(Roles = "ComplianceManager")]
[HttpPost]
public async Task<IActionResult> CreateLicence(...)
```

---

## 📈 Performance Targets

| Metric | Target | Requirement |
|--------|--------|-------------|
| Transaction validation | <3s | SC-005 |
| Customer compliance lookup | <1s | SC-033 |
| Audit report generation | <2m | SC-009 |
| Concurrent requests | 50 | SC-032 |
| System availability | 99.5% | FR-052 |
| MTTR | <30m | SC-031 |

---

## 🔗 Useful Links

### Documentation
- Specification: `specs/001-licence-management/spec.md`
- Technical Plan: `specs/001-licence-management/plan.md`
- Data Model: `specs/001-licence-management/data-model.md`
- API Contracts: `specs/001-licence-management/contracts/`
- Quickstart: `specs/001-licence-management/quickstart.md`
- Task List: `specs/001-licence-management/tasks.md` (298 tasks)

### External Resources
- .NET 8 Docs: https://docs.microsoft.com/dotnet
- ASP.NET Core: https://docs.microsoft.com/aspnet/core
- Azure SDK: https://docs.microsoft.com/azure/developer
- xUnit: https://xunit.net
- Moq: https://github.com/moq/moq4

---

## 💡 Tips

### Development Workflow
1. Start with Phase 2 (Foundation) - it blocks everything
2. Use TDD approach (tests first, always)
3. Work on user stories in parallel (different developers)
4. Commit frequently with clear messages
5. Run tests before committing

### Code Organization
- Core logic in `RE2.ComplianceCore` (no dependencies)
- External integrations in `RE2.DataAccess`
- HTTP concerns in `RE2.ComplianceApi`
- UI concerns in `RE2.ComplianceWeb`
- Shared code in `RE2.Shared`

### Performance
- Use async/await throughout
- Implement caching for frequently-accessed data
- Add resilience patterns (retry, circuit breaker)
- Monitor with Application Insights

### Testing
- Unit tests for business logic (fast, no dependencies)
- Integration tests for API endpoints (with TestContainers)
- Contract tests for external APIs
- Performance tests for critical paths

---

**Last Updated**: 2026-01-12
**Quick Reference Version**: 1.0
