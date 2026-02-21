# RE2 MVP - Quick Reference Guide

**Purpose**: Fast reference for common tasks and commands
**Last Updated**: 2026-02-21

---

## Project Status

| User Story | Description | Status |
|------------|-------------|--------|
| **US1** | Licence Management | Implemented |
| **US2** | Customer Onboarding | Implemented |
| **US3** | Documents & Alerts | Implemented |
| **US4** | Transaction Validation | Implemented |
| **US5** | Substance Reclassification & Thresholds | Implemented |
| **US6** | Risk Management, Workflows & Access Control | Implemented |
| **US7** | GDP Sites Master Data & Navigation | Implemented |
| **US8** | GDP Provider Qualification | Implemented |
| **US9** | GDP Inspections & CAPA | Implemented |
| **US10** | GDP Certificates, Validity & Monitoring | Implemented |
| **US11** | GDP Operational Checks | Implemented |
| **US12** | GDP Documentation, Training & Change Control | Implemented |

**Tests**: 1,432 passing across 6 test projects. Build: 0 errors.
**Infrastructure**: Bicep IaC (8 modules) + Azure DevOps CI/CD pipeline.

---

## Quick Commands

### Build & Test
```bash
cd C:\src\RE2

dotnet restore
dotnet build
dotnet test
dotnet test /p:CollectCoverage=true
```

### Run Applications
```bash
# Run API (https://localhost:7001) — in-memory mode, no external services needed
dotnet run --project src/RE2.ComplianceApi

# Run Web UI (https://localhost:5001)
dotnet run --project src/RE2.ComplianceWeb

# Run CLI
dotnet run --project src/RE2.ComplianceCli -- <command> [options]
```

### Development Tools
```bash
# Watch mode (auto-rebuild on changes)
dotnet watch run --project src/RE2.ComplianceApi

# Trust development certificate
dotnet dev-certs https --trust

# Clean build artifacts
dotnet clean
```

### Infrastructure (Bicep)
```bash
# Validate Bicep templates
az bicep build --file infra/bicep/main.bicep

# Preview what would be deployed (what-if)
az deployment group what-if \
  --resource-group rg-re2-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/dev.bicepparam

# Deploy to Dev manually
az deployment group create \
  --resource-group rg-re2-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/dev.bicepparam
```

---

## Project Structure

```
RE2/
├── src/
│   ├── RE2.ComplianceCore/      Core business logic (35 models, 15 services, 36 interfaces)
│   ├── RE2.DataAccess/          External API clients (Dataverse, D365 F&O, Blob Storage, 23 InMemory repos)
│   ├── RE2.ComplianceApi/       REST API (19 controllers, Swagger, health checks)
│   ├── RE2.ComplianceWeb/       Web UI (MVC, 23 controllers, health checks, App Insights)
│   ├── RE2.ComplianceCli/       CLI tool (4 commands)
│   ├── RE2.ComplianceFunctions/ Azure Functions (background jobs, host.json)
│   └── RE2.Shared/              Constants & DTOs
├── infra/bicep/                 IaC (main.bicep + 8 modules + 3 env param files)
├── pipelines/                   Azure DevOps CI/CD (Build → Dev → UAT → Prod)
└── tests/
    ├── RE2.ComplianceCore.Tests/       890 tests
    ├── RE2.ComplianceApi.Tests/        364 tests
    ├── RE2.Contract.Tests/             125 tests
    ├── RE2.DataAccess.Tests/            32 tests
    ├── RE2.ComplianceCli.Tests/         14 tests
    └── RE2.ComplianceFunctions.Tests/    7 tests
```

All projects build and all tests pass.

---

## Key Files

### Configuration
- `appsettings.json` (API) — Azure AD, Dataverse, D365 F&O config
- `appsettings.json` (Web) — Azure AD, API base URL
- `.editorconfig` — C# formatting rules

### Domain Models (RE2.ComplianceCore/Models/) — 35 models
- `Licence.cs`, `LicenceType.cs`, `LicenceDocument.cs`, `LicenceVerification.cs`, `LicenceScopeChange.cs`
- `ControlledSubstance.cs`, `SubstanceReclassification.cs`, `LicenceSubstanceMapping.cs`
- `Customer.cs` — Composite key: CustomerAccount + DataAreaId
- `Product.cs` — D365 F&O product with substance attributes
- `Transaction.cs`, `TransactionLine.cs`, `TransactionViolation.cs`, `TransactionLicenceUsage.cs`
- `Threshold.cs`, `Alert.cs`, `AuditEvent.cs`
- `GdpSite.cs`, `GdpSiteWdaCoverage.cs` — Composite key: WarehouseId + DataAreaId
- `GdpServiceProvider.cs`, `GdpCredential.cs`, `GdpCredentialVerification.cs`, `GdpDocument.cs`
- `GdpInspection.cs`, `Capa.cs`
- `GdpEquipmentQualification.cs`
- `GdpSop.cs`, `GdpSiteSop.cs`, `TrainingRecord.cs`, `GdpChangeRecord.cs`
- `WebhookSubscription.cs`, `IntegrationSystem.cs`, `RegulatoryInspection.cs`
- `ValidationResult.cs`, `QualificationReview.cs`

### Interfaces (RE2.ComplianceCore/Interfaces/) — 36 interfaces

Repositories (23 total, all with InMemory implementations):
- `ILicenceRepository`, `ILicenceTypeRepository`, `ILicenceSubstanceMappingRepository`
- `IControlledSubstanceRepository`, `ISubstanceReclassificationRepository`
- `ICustomerRepository`, `IProductRepository`, `ITransactionRepository`
- `IThresholdRepository`, `IAlertRepository`, `IAuditRepository`
- `IGdpSiteRepository`, `IRegulatoryInspectionRepository`
- `IGdpCredentialRepository`, `IGdpDocumentRepository`
- `IGdpInspectionRepository`, `ICapaRepository`, `IGdpEquipmentRepository`
- `IGdpSopRepository`, `ITrainingRepository`, `IGdpChangeRepository`
- `IWebhookSubscriptionRepository`, `IIntegrationSystemRepository`
- `IDocumentStorage`, `IDataverseClient`, `ID365FoClient`

Services:
- `ILicenceService`, `ILicenceSubstanceMappingService`
- `IControlledSubstanceService`, `ISubstanceReclassificationService`
- `ICustomerService`, `ITransactionComplianceService`
- `IThresholdService`, `IGdpComplianceService`, `IGdpOperationalService`
- `IReportingService`

### API Controllers (RE2.ComplianceApi/Controllers/V1/) — 19 total
- `LicencesController.cs` — CRUD, documents, verifications, scope changes
- `LicenceTypesController.cs` — Licence type reference data
- `LicenceSubstanceMappingsController.cs` — Substance authorization per licence
- `ControlledSubstancesController.cs` — Substance registry
- `CustomersController.cs` — Customer compliance management
- `TransactionsController.cs` — Validation, overrides, warehouse operations
- `ProductsController.cs` — Product browsing with substance info
- `ThresholdsController.cs` — Threshold CRUD
- `ReportsController.cs` — Audit reports, licence usage, customer compliance
- `GdpSitesController.cs` — GDP site management, WDA coverage
- `GdpProvidersController.cs` — Provider qualification, credentials, documents, reviews
- `GdpInspectionsController.cs` — Inspections, findings, CAPAs
- `GdpOperationsController.cs` — Site/provider validation, equipment qualifications
- `GdpSopsController.cs` — SOP management, site linking
- `GdpChangeControlController.cs` — Change records, approval/rejection workflow
- `SubstanceReclassificationController.cs` — Reclassification workflow
- `WebhookSubscriptionsController.cs` — Webhook CRUD
- `IntegrationSystemsController.cs` — API client registration
- `ApprovalWorkflowController.cs` — Approval workflows (FR-030)

### Web Controllers (RE2.ComplianceWeb/Controllers/) — 23 total
- `HomeController`, `DashboardController`, `LicencesController`, `LicenceTypesController`
- `CustomersController`, `SubstancesController`, `TransactionsController`
- `ThresholdsController`, `ReportsController`, `GdpSitesController`
- `AlertsController`, `InspectionsController`, `ConflictsController`
- `ReclassificationsController`, `LicenceMappingsController`
- `GdpProvidersController`, `GdpCredentialsController`, `GdpInspectionsController`
- `GdpEquipmentController`, `GdpOperationsController`, `GdpSopsController`
- `TrainingController`, `ChangeControlController`

### Constants (RE2.Shared/Constants/)
- `ErrorCodes.cs` — Standardized error codes
- `LicenceTypes.cs` — Licence type definitions
- `SubstanceCategories.cs` — Opium Act / Precursor categories
- `TransactionTypes.cs` — Transaction type, direction enums
- `UserRoles.cs` — Role constants

---

## API Endpoints

All routes under `/api/v1/`. Authorize header required (JWT).

### Licences (`/api/v1/licences`)
- `GET /api/v1/licences` — List (filter: holderId, holderType, status)
- `GET /api/v1/licences/{id}` — Get by ID
- `GET /api/v1/licences/by-number/{licenceNumber}` — Get by number
- `GET /api/v1/licences/expiring` — Expiring licences
- `PUT /api/v1/licences/{id}` — Update
- `DELETE /api/v1/licences/{id}` — Delete
- `GET /api/v1/licences/{id}/documents` — List documents
- `POST /api/v1/licences/{id}/documents` — Upload document
- `GET /api/v1/licences/{id}/documents/{documentId}/download` — Download
- `DELETE /api/v1/licences/{id}/documents/{documentId}` — Delete document
- `GET /api/v1/licences/{id}/verifications` — List verifications
- `POST /api/v1/licences/{id}/verifications` — Record verification
- `GET /api/v1/licences/{id}/scope-changes` — List scope changes
- `POST /api/v1/licences/{id}/scope-changes` — Request scope change

### Licence Types (`/api/v1/licencetypes`)
- `GET /api/v1/licencetypes` — List all
- `GET /api/v1/licencetypes/{id}` — Get by ID
- `GET /api/v1/licencetypes/by-name/{name}` — Get by name
- `PUT /api/v1/licencetypes/{id}` — Update
- `DELETE /api/v1/licencetypes/{id}` — Delete

### Licence-Substance Mappings (`/api/v1/licencesubstancemappings`)
- `GET /api/v1/licencesubstancemappings` — List all
- `GET /api/v1/licencesubstancemappings/{id}` — Get by ID
- `GET /api/v1/licencesubstancemappings/check-authorization` — Check substance authorization
- `PUT /api/v1/licencesubstancemappings/{id}` — Update
- `DELETE /api/v1/licencesubstancemappings/{id}` — Delete

### Controlled Substances (`/api/v1/controlledsubstances`)
- `GET /api/v1/controlledsubstances` — List (filter: activeOnly, opiumActList, precursorCategory, search)
- `GET /api/v1/controlledsubstances/{substanceCode}` — Get by substance code

### Customers (`/api/v1/customers`)
- `GET /api/v1/customers` — List (filter: status, category, country)
- `GET /api/v1/customers/d365` — Browse D365 F&O customers
- `GET /api/v1/customers/search?q=` — Search by name
- `GET /api/v1/customers/reverification-due` — Due for re-verification
- `GET /api/v1/customers/{customerAccount}?dataAreaId=` — Get by composite key
- `GET /api/v1/customers/{customerAccount}/compliance-status?dataAreaId=` — Compliance status
- `POST /api/v1/customers` — Configure compliance (SalesAdmin/ComplianceManager)
- `PUT /api/v1/customers/{customerAccount}?dataAreaId=` — Update compliance
- `DELETE /api/v1/customers/{customerAccount}?dataAreaId=` — Remove compliance
- `POST /api/v1/customers/{customerAccount}/suspend?dataAreaId=` — Suspend (ComplianceManager)
- `POST /api/v1/customers/{customerAccount}/reinstate?dataAreaId=` — Reinstate (ComplianceManager)

### Transactions (`/api/v1/transactions`)
- `POST /api/v1/transactions/validate` — Validate transaction (<3s target)
- `GET /api/v1/transactions` — List (filter: status, customerAccount, customerDataAreaId, fromDate, toDate)
- `GET /api/v1/transactions/{id}` — Get by ID
- `GET /api/v1/transactions/by-external/{externalId}` — Get by external ID
- `GET /api/v1/transactions/pending` — Pending overrides (ComplianceManager)
- `GET /api/v1/transactions/pending/count` — Pending override count
- `POST /api/v1/transactions/{id}/approve` — Approve override (ComplianceManager)
- `POST /api/v1/transactions/{id}/reject` — Reject override (ComplianceManager)
- `POST /api/v1/warehouse/operations/validate` — Validate warehouse operation

### Products (`/api/v1/products`)
- `GET /api/v1/products` — List (filter: controlled)
- `GET /api/v1/products/{itemNumber}?dataAreaId=` — Get by item number
- `GET /api/v1/products/by-substance/{substanceCode}` — Products by substance

### Thresholds (`/api/v1/thresholds`)
- `GET /api/v1/thresholds` — List (filter: activeOnly, type, substanceCode, search)
- CRUD operations (POST, PUT, DELETE)

### Reports (`/api/v1/reports`) — ComplianceManager/QAUser
- `GET /api/v1/reports/transaction-audit` — Transaction audit report
- `POST /api/v1/reports/transaction-audit` — Transaction audit (complex criteria)
- `GET /api/v1/reports/licence-usage` — Licence usage report
- `POST /api/v1/reports/licence-usage` — Licence usage (complex criteria)
- `GET /api/v1/reports/customer-compliance/{customerAccount}/{dataAreaId}` — Customer compliance report
- `POST /api/v1/reports/customer-compliance` — Customer compliance (complex criteria)
- `GET /api/v1/reports/licence-correction-impact` — Licence correction impact
- `POST /api/v1/reports/licence-correction-impact` — Licence correction impact (complex criteria)

### GDP Sites (`/api/v1/gdpsites`)
- `GET /api/v1/gdpsites` — List GDP-configured sites
- `GET /api/v1/gdpsites/warehouses` — Browse D365 F&O warehouses
- `GET /api/v1/gdpsites/warehouses/{warehouseId}?dataAreaId=` — Get warehouse
- `GET /api/v1/gdpsites/{warehouseId}?dataAreaId=` — Get GDP site
- `PUT /api/v1/gdpsites/{warehouseId}?dataAreaId=` — Configure GDP site
- `DELETE /api/v1/gdpsites/{warehouseId}?dataAreaId=` — Remove GDP config
- `GET /api/v1/gdpsites/{warehouseId}/wda-coverage` — WDA coverage
- `POST /api/v1/gdpsites/{warehouseId}/wda-coverage` — Add WDA coverage
- `DELETE /api/v1/gdpsites/{warehouseId}/wda-coverage/{coverageId}` — Remove WDA coverage

### GDP Providers (`/api/v1/gdp-providers`)
- `GET /api/v1/gdp-providers` — List providers
- `GET /api/v1/gdp-providers/{providerId}` — Get provider
- `POST /api/v1/gdp-providers` — Create (QAUser/ComplianceManager)
- `PUT /api/v1/gdp-providers/{providerId}` — Update (QAUser/ComplianceManager)
- `DELETE /api/v1/gdp-providers/{providerId}` — Delete (ComplianceManager)
- `GET /api/v1/gdp-providers/requiring-review` — Providers due for review
- `GET /api/v1/gdp-providers/{providerId}/credentials` — Provider credentials
- `POST /api/v1/gdp-providers/credentials` — Create credential
- `GET /api/v1/gdp-providers/credentials/expiring` — Expiring credentials
- `POST /api/v1/gdp-providers/{providerId}/reviews` — Record review
- `POST /api/v1/gdp-providers/credentials/{credentialId}/verifications` — Record verification
- `POST /api/v1/gdp-providers/credentials/{credentialId}/documents` — Upload document
- `GET /api/v1/gdp-providers/check-qualification` — Check GDP qualification

### GDP Inspections (`/api/v1/gdp-inspections`)
- `GET /api/v1/gdp-inspections` — List inspections
- `POST /api/v1/gdp-inspections` — Create (QAUser/ComplianceManager)
- `GET /api/v1/gdp-inspections/by-site/{siteId}` — By site
- `POST /api/v1/gdp-inspections/findings` — Create finding
- `GET /api/v1/gdp-inspections/capas` — List CAPAs
- `GET /api/v1/gdp-inspections/capas/overdue` — Overdue CAPAs
- `POST /api/v1/gdp-inspections/capas/{capaId}/complete` — Complete CAPA

### GDP Operations (`/api/v1/gdp-operations`)
- `POST /api/v1/gdp-operations/validate/site-assignment` — Validate site
- `POST /api/v1/gdp-operations/validate/provider-assignment` — Validate provider
- `GET /api/v1/gdp-operations/approved-providers` — Approved providers
- `GET /api/v1/gdp-operations/equipment` — List equipment
- `GET /api/v1/gdp-operations/equipment/due-for-requalification` — Equipment due
- CRUD operations for equipment qualifications

### GDP SOPs (`/api/v1/gdp-sops`)
- CRUD operations + site linking/unlinking (QAUser/ComplianceManager)

### GDP Change Control (`/api/v1/gdp-changes`)
- `GET /api/v1/gdp-changes` — List change records
- `GET /api/v1/gdp-changes/pending` — Pending changes
- `POST /api/v1/gdp-changes` — Create change record
- `POST /api/v1/gdp-changes/{changeId}/approve` — Approve (ComplianceManager)
- `POST /api/v1/gdp-changes/{changeId}/reject` — Reject (ComplianceManager)

### Substance Reclassification (`/api/v1`)
- `POST /api/v1/substances/{substanceCode}/reclassify` — Create reclassification (ComplianceManager)
- `GET /api/v1/substances/{substanceCode}/reclassifications` — List for substance
- `GET /api/v1/substances/{substanceCode}/classification` — Current classification
- `GET /api/v1/reclassifications/{id}` — Get by ID
- `GET /api/v1/reclassifications/pending` — Pending reclassifications
- `POST /api/v1/reclassifications/{id}/process` — Process reclassification
- `GET /api/v1/reclassifications/{id}/impact-analysis` — Impact analysis
- `GET /api/v1/reclassifications/{id}/notification` — Notification preview
- `POST /api/v1/reclassifications/{id}/customers/{customerId}/requalify` — Requalify customer
- `GET /api/v1/customers/{customerId}/reclassification-status` — Customer reclassification status

### Webhook Subscriptions (`/api/v1/webhooksubscriptions`)
- `GET /api/v1/webhooksubscriptions` — List (SystemAdmin)
- `GET /api/v1/webhooksubscriptions/{id}` — Get by ID
- `PUT /api/v1/webhooksubscriptions/{id}` — Update
- `DELETE /api/v1/webhooksubscriptions/{id}` — Delete
- `POST /api/v1/webhooksubscriptions/{id}/reactivate` — Reactivate
- `POST /api/v1/webhooksubscriptions/{id}/deactivate` — Deactivate
- `GET /api/v1/webhooksubscriptions/event-types` — List event types

### Integration Systems (`/api/v1/integrationsystems`)
- CRUD operations for API client registrations (FR-061)

### Approval Workflows (`/api/v1/workflows`)
- `POST /api/v1/workflows/trigger` — Trigger workflow (ComplianceManager)
- `POST /api/v1/workflows/callback` — Logic Apps callback
- `GET /api/v1/workflows/{workflowId}/status` — Workflow status

---

## CLI Commands (RE2.ComplianceCli)

```bash
# Validate a transaction (reads JSON from stdin or file)
dotnet run --project src/RE2.ComplianceCli -- validate-transaction [-f file.json] [-v]

# Lookup customer compliance status
dotnet run --project src/RE2.ComplianceCli -- lookup-customer -a <account> [-d <dataAreaId>] [-n <name>] [--include-licences] [-v]

# Lookup licence details
dotnet run --project src/RE2.ComplianceCli -- lookup-licence [-i <id>] [-n <number>] [--include-substances] [--include-documents] [-v]

# Generate compliance report
dotnet run --project src/RE2.ComplianceCli -- generate-report -t <type> [--days-ahead 90] [--customer-account <acct>] [--from-date yyyy-MM-dd] [--to-date yyyy-MM-dd] [-o file.json] [-v]
#   Report types: expiring-licences, customer-compliance, alerts-summary, transaction-history
```

All commands output JSON to stdout. Use `-v` for verbose logging to stderr.

---

## Architecture Notes

### Composite Models (D365 F&O + Dataverse pattern)
- **ControlledSubstance**: keyed by `SubstanceCode`
- **Customer**: keyed by `CustomerAccount` + `DataAreaId`
- **GdpSite**: keyed by `WarehouseId` + `DataAreaId`
- **Product**: keyed by `ItemNumber` + `DataAreaId`

### Product-based Transactions
- `TransactionLine` uses `ItemNumber`/`DataAreaId` — substance is resolved server-side via product attributes
- External systems never need to know substance codes

### Data Access
- 23 repository interfaces, each with an InMemory implementation
- In-memory mode works for all local development (no external services needed)
- Production uses Dataverse virtual tables + D365 F&O virtual entities

---

## Testing

### Test Counts (1,432 total)
| Project | Tests |
|---------|-------|
| RE2.ComplianceCore.Tests | 890 |
| RE2.ComplianceApi.Tests | 364 |
| RE2.Contract.Tests | 125 |
| RE2.DataAccess.Tests | 32 |
| RE2.ComplianceCli.Tests | 14 |
| RE2.ComplianceFunctions.Tests | 7 |

### Test Commands
```bash
dotnet test                                                    # Run all
dotnet test tests/RE2.ComplianceCore.Tests                     # Specific project
dotnet test --logger "console;verbosity=detailed"              # Verbose
dotnet test --filter "FullyQualifiedName~LicenceTypeTests"     # Specific test
```

---

## Security & Roles

### Authentication
- **Internal**: Azure AD (JWT tokens)
- **External**: Azure AD B2C (SSO)

### Roles
- **ComplianceManager**: Full access (licences, approvals, overrides, reclassification, GDP change approval)
- **QAUser**: GDP management (sites, providers, inspections, equipment, SOPs, reports)
- **SalesAdmin**: Customer onboarding
- **SystemAdmin**: Integration systems, webhook subscriptions
- **TrainingCoordinator**: Training records management

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Transaction validation | <3s |
| Customer compliance lookup | <1s |
| Audit report generation | <2m |
| Concurrent requests | 50 |

---

## Common Issues & Solutions

### NuGet packages won't restore
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

### Build fails with "project not found"
```bash
dotnet new sln -n RE2 --force
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj
```

### Azure services unavailable locally
Not needed for development. All repositories have InMemory implementations that are registered by default. No external services required for local dev or testing.

---

## Documentation

- Deployment Guide: `docs/deployment/README.md`
- API Reference: `docs/api/README.md`
- User Guide: `docs/user-guide/README.md`
- Integration Guide: `docs/integration/README.md`
- Specification: `specs/001-licence-management/spec.md`
- Data Model: `specs/001-licence-management/data-model.md`
- API Contracts: `specs/001-licence-management/contracts/`
