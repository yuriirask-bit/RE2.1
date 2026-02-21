# RE2 - Controlled Drug Licence & GDP Compliance Management System

**Status**: **Implemented** - User Stories 1-12 complete, 1,432 tests passing
**Build**: 0 errors | **Infrastructure**: Bicep IaC + Azure DevOps CI/CD
**Last Updated**: 2026-02-21

---

## Project Overview

A comprehensive licence management and GDP compliance system for Dutch pharmaceutical wholesalers handling controlled drugs. The system provides:

- **Real-time compliance validation** for controlled drug transactions
- **Licence lifecycle management** (capture, verification, monitoring)
- **Customer qualification tracking** with approval workflows
- **GDP compliance** (sites, credentials, inspections, CAPA, equipment qualifications)
- **GDP documentation management** (SOPs, training records, change control)
- **GDP operational validation** (site/provider eligibility, equipment requalification)
- **Audit trails and reporting** for regulatory compliance
- **CLI tooling** for transaction validation, customer/licence lookup, and report generation

### Key Features

- Stateless architecture (no local data storage for business data)
- **Composite model architecture**: domain entities span D365 F&O (master data) + Dataverse (compliance extensions)
- Azure cloud-native (App Service, Functions, Blob Storage, API Management)
- RESTful APIs for ERP/WMS integration
- Web UI for compliance staff
- CLI tool for debugging, scripting, and automation
- Automated expiry monitoring and alerts
- In-memory repositories for local development without external dependencies
- Full Infrastructure as Code (Bicep) with CI/CD via Azure DevOps Pipelines
- TDD approach with comprehensive test coverage (1,432 tests across 6 projects)

---

## Architecture

### Technology Stack

- **.NET 8 LTS** (C# 12) - November 2026 EOL
- **ASP.NET Core 8.0** - Web API and MVC
- **Azure SDK** - Identity, Blob Storage, Service Bus
- **Entity Framework Core 8.0** - Internal state only (NOT for business data)
- **xUnit + Moq + FluentAssertions** - Testing framework
- **CommandLineParser** - CLI argument parsing

### Composite Model Architecture

Key domain entities follow a **composite model** pattern where master data originates from D365 Finance & Operations and compliance extensions are stored in Dataverse:

| Model | D365 F&O Source | Key | Compliance Extension |
|-------|----------------|-----|---------------------|
| **ControlledSubstance** | Product attributes | `SubstanceCode` (string) | Regulatory restrictions, classification |
| **Customer** | Customer master | `CustomerAccount` + `DataAreaId` | Approval status, GDP qualification |
| **GdpSite** | Warehouse master | `WarehouseId` + `DataAreaId` | GDP site type, permitted activities |
| **Product** | Released products | `ItemNumber` + `DataAreaId` | Substance code mapping |
| **TransactionLine** | Sales order lines | `ItemNumber` + `DataAreaId` | Substance resolved server-side |

### Project Structure

```
RE2/
├── src/
│   ├── RE2.ComplianceCore/          # Core domain logic (library-first)
│   │   ├── Models/                  # Domain entities (35 models)
│   │   ├── Services/                # Business logic (15 services)
│   │   └── Interfaces/              # Abstractions (36 interfaces)
│   │
│   ├── RE2.DataAccess/              # External API clients & repositories
│   │   ├── Dataverse/               # Virtual tables (licences, customers)
│   │   ├── D365FinanceOperations/   # Virtual entities (transactions)
│   │   ├── InMemory/                # In-memory repositories (23 repos + seed data)
│   │   └── BlobStorage/             # Document storage
│   │
│   ├── RE2.ComplianceApi/           # REST API (Azure App Service)
│   │   ├── Controllers/V1/          # Versioned endpoints (19 controllers)
│   │   ├── HealthChecks/            # Liveness + readiness probes
│   │   ├── Middleware/              # Error handling, logging, graceful degradation
│   │   └── Authorization/           # Custom policies
│   │
│   ├── RE2.ComplianceWeb/           # Web UI (ASP.NET MVC)
│   │   ├── Controllers/             # MVC controllers (23 controllers)
│   │   ├── HealthChecks/            # Liveness + readiness probes
│   │   └── Views/                   # Razor views
│   │
│   ├── RE2.ComplianceCli/           # CLI tool (console application)
│   │   └── Commands/                # validate-transaction, lookup-customer,
│   │                                # lookup-licence, generate-report
│   │
│   ├── RE2.ComplianceFunctions/     # Background jobs (Azure Functions)
│   │
│   └── RE2.Shared/                  # Constants and utilities
│       └── Constants/               # ErrorCodes, LicenceTypes, etc.
│
├── infra/bicep/                     # Infrastructure as Code
│   ├── main.bicep                   # Orchestrator (wires all modules)
│   ├── dev.bicepparam               # Dev environment parameters
│   ├── uat.bicepparam               # UAT environment parameters
│   ├── prod.bicepparam              # Prod environment parameters
│   └── modules/                     # Reusable Bicep modules
│       ├── monitoring.bicep         # Log Analytics + Application Insights
│       ├── storage-account.bicep    # Blob Storage (documents + Functions runtime)
│       ├── redis-cache.bicep        # Azure Cache for Redis
│       ├── key-vault.bicep          # Key Vault + secrets
│       ├── app-service-plan.bicep   # Shared plan for API + Web
│       ├── app-service.bicep        # Reusable App Service (API / Web)
│       ├── function-app.bicep       # Azure Functions + dedicated plan
│       ├── logic-app.bicep          # Approval workflow Logic App
│       └── role-assignments.bicep   # RBAC (Managed Identity → Storage, Key Vault)
│
├── pipelines/
│   └── azure-pipelines.yml          # CI/CD: Build → Dev → UAT → Prod
│
├── tests/
│   ├── RE2.ComplianceCore.Tests/    # Unit tests (890)
│   ├── RE2.ComplianceApi.Tests/     # Integration tests (364)
│   ├── RE2.Contract.Tests/          # Contract tests (125)
│   ├── RE2.DataAccess.Tests/        # Data access tests (32)
│   ├── RE2.ComplianceCli.Tests/     # CLI tests (14)
│   └── RE2.ComplianceFunctions.Tests/ # Functions tests (7)
│
├── docs/                            # Documentation
│   ├── api/                         # API reference
│   ├── user-guide/                  # Web UI walkthrough
│   ├── integration/                 # ERP/WMS integration guide
│   └── deployment/                  # Deployment & operations guide
│
└── specs/                           # Feature specifications
    └── 001-licence-management/      # US1-US12 specs, data model, contracts
```

---

## Domain Models

### Core Entities (35 models)

| Model | Description |
|-------|-------------|
| `Licence` | Licence instance with validity, status, verification tracking |
| `LicenceType` | Licence type definitions with permitted activities |
| `LicenceDocument` | Document metadata with blob storage URL |
| `LicenceVerification` | Verification activity tracking |
| `LicenceScopeChange` | Scope change history |
| `LicenceSubstanceMapping` | Licence-to-substance authorization mapping |
| `ControlledSubstance` | **Composite**: D365 product attributes + Dataverse compliance extension |
| `Customer` | **Composite**: D365 F&O customer + Dataverse compliance extension |
| `QualificationReview` | Customer/provider qualification review tracking |
| `Product` | D365 F&O released product with substance classification |
| `Transaction` | Compliance validation transaction |
| `TransactionLine` | Transaction line items (ItemNumber/DataAreaId-based) |
| `TransactionViolation` | Compliance violation details |
| `TransactionLicenceUsage` | Licence usage per transaction |
| `Threshold` | Quantity/value threshold rules per FR-022 |
| `SubstanceReclassification` | Substance reclassification records per FR-066 |
| `Alert` | Alert/notification entity |
| `AuditEvent` | Audit trail entry |
| `GdpSite` | **Composite**: D365 warehouse + Dataverse GDP extension |
| `GdpSiteWdaCoverage` | WDA licence coverage for GDP sites |
| `GdpServiceProvider` | GDP service provider with qualification status (FR-038) |
| `GdpCredential` | GDP credential (WDA licence, GDP certificate, ISO cert) |
| `GdpCredentialVerification` | Credential verification records (EudraGMDP checks) |
| `GdpDocument` | GDP document metadata with blob storage URL |
| `GdpInspection` | GDP inspection records with findings (FR-040) |
| `Capa` | Corrective and Preventive Actions (FR-041, FR-042) |
| `GdpEquipmentQualification` | Equipment qualification tracking (FR-048) |
| `GdpSop` | GDP Standard Operating Procedure (FR-049) |
| `GdpSiteSop` | SOP-to-site linkage |
| `TrainingRecord` | Staff training records with assessment (FR-050) |
| `GdpChangeRecord` | Change control records with approval workflow (FR-051) |
| `RegulatoryInspection` | Regulatory inspection records |
| `WebhookSubscription` | Webhook event subscriptions |
| `IntegrationSystem` | Registered API client systems |
| `ValidationResult` | Validation result with violations |

---

## API Endpoints

All endpoints are under `/api/v1/`.

### Licence Management (`/api/v1/licences`)
- `GET /api/v1/licences` - List licences (filter by holderId, holderType, status)
- `GET /api/v1/licences/{id}` - Get licence by ID
- `GET /api/v1/licences/by-number/{licenceNumber}` - Get licence by number
- `GET /api/v1/licences/expiring` - Get expiring licences (query: daysAhead)
- `POST /api/v1/licences` - Create licence [ComplianceManager]
- `PUT /api/v1/licences/{id}` - Update licence [ComplianceManager]
- `DELETE /api/v1/licences/{id}` - Delete licence [ComplianceManager]
- `GET /api/v1/licences/{id}/documents` - List documents
- `POST /api/v1/licences/{id}/documents` - Upload document [ComplianceManager]
- `GET /api/v1/licences/{id}/documents/{documentId}/download` - Download document
- `DELETE /api/v1/licences/{id}/documents/{documentId}` - Delete document [ComplianceManager]
- `GET /api/v1/licences/{id}/verifications` - List verifications
- `POST /api/v1/licences/{id}/verifications` - Record verification [ComplianceManager]
- `GET /api/v1/licences/{id}/scope-changes` - List scope changes
- `POST /api/v1/licences/{id}/scope-changes` - Record scope change [ComplianceManager]

### Licence Types (`/api/v1/licencetypes`)
- `GET /api/v1/licencetypes` - List licence types
- `GET /api/v1/licencetypes/{id}` - Get licence type by ID
- `POST /api/v1/licencetypes` - Create licence type [ComplianceManager]
- `PUT /api/v1/licencetypes/{id}` - Update licence type [ComplianceManager]
- `DELETE /api/v1/licencetypes/{id}` - Delete licence type [ComplianceManager]

### Licence-Substance Mappings (`/api/v1/licencesubstancemappings`)
- `GET /api/v1/licencesubstancemappings` - List mappings (filter by licenceId, substanceCode, activeOnly)
- `GET /api/v1/licencesubstancemappings/{id}` - Get mapping by ID
- `GET /api/v1/licencesubstancemappings/check-authorization` - Check substance authorization
- `POST /api/v1/licencesubstancemappings` - Create mapping [ComplianceManager]
- `PUT /api/v1/licencesubstancemappings/{id}` - Update mapping [ComplianceManager]
- `DELETE /api/v1/licencesubstancemappings/{id}` - Delete mapping [ComplianceManager]

### Customer Compliance (`/api/v1/customers`)
- `GET /api/v1/customers` - List compliance-configured customers (filter by status, category, country)
- `GET /api/v1/customers/d365` - Browse all D365 F&O customers
- `GET /api/v1/customers/search?q=` - Search customers by name
- `GET /api/v1/customers/reverification-due` - Get re-verification due customers
- `GET /api/v1/customers/{customerAccount}?dataAreaId=` - Get customer by composite key
- `GET /api/v1/customers/{customerAccount}/compliance-status?dataAreaId=` - Compliance status (<1s)
- `POST /api/v1/customers` - Configure compliance extension [SalesAdmin, ComplianceManager]
- `PUT /api/v1/customers/{customerAccount}?dataAreaId=` - Update compliance [SalesAdmin, ComplianceManager]
- `DELETE /api/v1/customers/{customerAccount}?dataAreaId=` - Remove compliance [SalesAdmin, ComplianceManager]
- `POST /api/v1/customers/{customerAccount}/suspend?dataAreaId=` - Suspend [ComplianceManager]
- `POST /api/v1/customers/{customerAccount}/reinstate?dataAreaId=` - Reinstate [ComplianceManager]

### Transaction Validation (`/api/v1/transactions`)
- `POST /api/v1/transactions/validate` - Validate transaction (<3s)
- `GET /api/v1/transactions` - List transactions (filter by status, customerAccount, dates)
- `GET /api/v1/transactions/{id}` - Get transaction by ID
- `GET /api/v1/transactions/by-external/{externalId}` - Get by external ID (ERP order number)
- `GET /api/v1/transactions/pending` - Get pending overrides [ComplianceManager]
- `GET /api/v1/transactions/pending/count` - Get pending override count
- `POST /api/v1/transactions/{id}/approve` - Approve override [ComplianceManager]
- `POST /api/v1/transactions/{id}/reject` - Reject override [ComplianceManager]
- `POST /api/v1/warehouse/operations/validate` - Validate warehouse operation

### Controlled Substances (`/api/v1/controlledsubstances`)
- `GET /api/v1/controlledsubstances` - List substances (filter by activeOnly, opiumActList, precursorCategory, search)
- `GET /api/v1/controlledsubstances/{substanceCode}` - Get by substance code
- `POST /api/v1/controlledsubstances/configure-compliance` - Configure compliance [ComplianceManager]
- `PUT /api/v1/controlledsubstances/{substanceCode}/compliance` - Update compliance [ComplianceManager]
- `POST /api/v1/controlledsubstances/{substanceCode}/deactivate` - Deactivate [ComplianceManager]
- `POST /api/v1/controlledsubstances/{substanceCode}/reactivate` - Reactivate [ComplianceManager]

### Products (`/api/v1/products`)
- `GET /api/v1/products` - List products (query: controlled=true)
- `GET /api/v1/products/{itemNumber}?dataAreaId=` - Get product by item number
- `GET /api/v1/products/by-substance/{substanceCode}` - Get products by substance

### Thresholds (`/api/v1/thresholds`)
- `GET /api/v1/thresholds` - List thresholds (filter by activeOnly, type, substanceCode, search)
- `GET /api/v1/thresholds/{id}` - Get threshold by ID
- `GET /api/v1/thresholds/by-substance/{substanceCode}` - Get by substance
- `GET /api/v1/thresholds/by-category/{category}` - Get by customer category
- `POST /api/v1/thresholds` - Create threshold [ComplianceManager]
- `PUT /api/v1/thresholds/{id}` - Update threshold [ComplianceManager]
- `DELETE /api/v1/thresholds/{id}` - Delete threshold [ComplianceManager]

### Substance Reclassifications
- `POST /api/v1/substances/{substanceCode}/reclassify` - Create reclassification [ComplianceManager]
- `GET /api/v1/substances/{substanceCode}/reclassifications` - Get reclassification history
- `GET /api/v1/substances/{substanceCode}/classification` - Get effective classification at date
- `GET /api/v1/reclassifications/{id}` - Get reclassification by ID
- `GET /api/v1/reclassifications/pending` - Get pending reclassifications
- `POST /api/v1/reclassifications/{id}/process` - Process reclassification [ComplianceManager]
- `GET /api/v1/reclassifications/{id}/impact-analysis` - Get impact analysis
- `GET /api/v1/reclassifications/{id}/notification` - Get compliance notification
- `POST /api/v1/reclassifications/{id}/customers/{customerId}/requalify` - Mark re-qualified [ComplianceManager]
- `GET /api/v1/customers/{customerId}/reclassification-status` - Check reclassification blocking

### Reports (`/api/v1/reports`)
- `GET /api/v1/reports/transaction-audit` - Transaction audit report (FR-026)
- `POST /api/v1/reports/transaction-audit` - Transaction audit report (complex criteria)
- `GET /api/v1/reports/licence-usage` - Licence usage report (FR-026)
- `POST /api/v1/reports/licence-usage` - Licence usage report (complex criteria)
- `GET /api/v1/reports/customer-compliance/{customerAccount}/{dataAreaId}` - Customer compliance history (FR-029)
- `POST /api/v1/reports/customer-compliance` - Customer compliance history (complex criteria)
- `GET /api/v1/reports/licence-correction-impact` - Licence correction impact analysis (SC-038)
- `POST /api/v1/reports/licence-correction-impact` - Licence correction impact (complex criteria)

### GDP Sites (`/api/v1/gdpsites`)
- `GET /api/v1/gdpsites/warehouses` - Browse D365 F&O warehouses
- `GET /api/v1/gdpsites/warehouses/{warehouseId}?dataAreaId=` - Get warehouse
- `GET /api/v1/gdpsites` - List GDP-configured sites
- `GET /api/v1/gdpsites/{warehouseId}?dataAreaId=` - Get GDP site
- `POST /api/v1/gdpsites` - Configure GDP [QAUser, ComplianceManager]
- `PUT /api/v1/gdpsites/{warehouseId}` - Update GDP config [QAUser, ComplianceManager]
- `DELETE /api/v1/gdpsites/{warehouseId}?dataAreaId=` - Remove GDP config [ComplianceManager]
- `GET /api/v1/gdpsites/{warehouseId}/wda-coverage?dataAreaId=` - Get WDA coverage
- `POST /api/v1/gdpsites/{warehouseId}/wda-coverage` - Add WDA coverage [ComplianceManager]
- `DELETE /api/v1/gdpsites/{warehouseId}/wda-coverage/{coverageId}` - Remove WDA coverage [ComplianceManager]

### GDP Providers (`/api/v1/gdp-providers`)
- `GET /api/v1/gdp-providers` - List all GDP service providers
- `GET /api/v1/gdp-providers/{providerId}` - Get provider by ID
- `POST /api/v1/gdp-providers` - Create provider [QAUser, ComplianceManager]
- `PUT /api/v1/gdp-providers/{providerId}` - Update provider [QAUser, ComplianceManager]
- `DELETE /api/v1/gdp-providers/{providerId}` - Delete provider [ComplianceManager]
- `GET /api/v1/gdp-providers/requiring-review` - Providers due for re-qualification (FR-039)
- `GET /api/v1/gdp-providers/{providerId}/credentials` - Provider credentials
- `GET /api/v1/gdp-providers/credentials/{credentialId}` - Get credential
- `POST /api/v1/gdp-providers/credentials` - Create credential [QAUser, ComplianceManager]
- `GET /api/v1/gdp-providers/credentials/expiring` - Expiring credentials (query: daysAhead)
- `GET /api/v1/gdp-providers/{providerId}/reviews` - Provider qualification reviews
- `POST /api/v1/gdp-providers/{providerId}/reviews` - Record review [QAUser, ComplianceManager]
- `GET /api/v1/gdp-providers/credentials/{credentialId}/verifications` - Credential verifications
- `POST /api/v1/gdp-providers/credentials/{credentialId}/verifications` - Record verification [QAUser, ComplianceManager]
- `GET /api/v1/gdp-providers/credentials/{credentialId}/documents` - Credential documents (FR-044)
- `POST /api/v1/gdp-providers/credentials/{credentialId}/documents` - Upload document [QAUser, ComplianceManager]
- `GET /api/v1/gdp-providers/documents/{documentId}/download` - Download document
- `DELETE /api/v1/gdp-providers/documents/{documentId}` - Delete document [QAUser, ComplianceManager]
- `GET /api/v1/gdp-providers/check-qualification` - Check partner GDP qualification (FR-038)

### GDP Inspections (`/api/v1/gdp-inspections`)
- `GET /api/v1/gdp-inspections` - List all GDP inspections
- `GET /api/v1/gdp-inspections/{inspectionId}` - Get inspection by ID
- `GET /api/v1/gdp-inspections/by-site/{siteId}` - Inspections for a site
- `POST /api/v1/gdp-inspections` - Create inspection [QAUser, ComplianceManager]
- `PUT /api/v1/gdp-inspections/{inspectionId}` - Update inspection [QAUser, ComplianceManager]
- `GET /api/v1/gdp-inspections/{inspectionId}/findings` - Inspection findings
- `GET /api/v1/gdp-inspections/findings/{findingId}` - Get finding by ID
- `POST /api/v1/gdp-inspections/findings` - Create finding [QAUser, ComplianceManager]
- `DELETE /api/v1/gdp-inspections/findings/{findingId}` - Delete finding [QAUser, ComplianceManager]
- `GET /api/v1/gdp-inspections/capas` - List all CAPAs
- `GET /api/v1/gdp-inspections/capas/{capaId}` - Get CAPA by ID
- `GET /api/v1/gdp-inspections/capas/by-finding/{findingId}` - CAPAs for a finding
- `GET /api/v1/gdp-inspections/capas/overdue` - Overdue CAPAs (FR-042)
- `POST /api/v1/gdp-inspections/capas` - Create CAPA [QAUser, ComplianceManager]
- `PUT /api/v1/gdp-inspections/capas/{capaId}` - Update CAPA [QAUser, ComplianceManager]
- `POST /api/v1/gdp-inspections/capas/{capaId}/complete` - Complete CAPA [QAUser, ComplianceManager]

### GDP Operations (`/api/v1/gdp-operations`)
- `POST /api/v1/gdp-operations/validate/site-assignment` - Validate site eligibility (FR-046)
- `POST /api/v1/gdp-operations/validate/provider-assignment` - Validate provider eligibility (FR-047)
- `GET /api/v1/gdp-operations/approved-providers` - Approved providers (query: tempControlled)
- `GET /api/v1/gdp-operations/equipment` - List equipment qualifications
- `GET /api/v1/gdp-operations/equipment/{equipmentId}` - Get equipment by ID
- `GET /api/v1/gdp-operations/equipment/due-for-requalification` - Equipment due (query: daysAhead)
- `POST /api/v1/gdp-operations/equipment` - Create equipment [QAUser, ComplianceManager]
- `PUT /api/v1/gdp-operations/equipment/{equipmentId}` - Update equipment [QAUser, ComplianceManager]
- `DELETE /api/v1/gdp-operations/equipment/{equipmentId}` - Delete equipment [QAUser, ComplianceManager]

### GDP SOPs (`/api/v1/gdp-sops`)
- `GET /api/v1/gdp-sops` - List all SOPs
- `GET /api/v1/gdp-sops/{sopId}` - Get SOP by ID
- `POST /api/v1/gdp-sops` - Create SOP [QAUser, ComplianceManager]
- `PUT /api/v1/gdp-sops/{sopId}` - Update SOP [QAUser, ComplianceManager]
- `DELETE /api/v1/gdp-sops/{sopId}` - Delete SOP [QAUser, ComplianceManager]
- `GET /api/v1/gdp-sops/{sopId}/sites` - Get linked sites
- `POST /api/v1/gdp-sops/{sopId}/sites/{siteId}` - Link SOP to site [QAUser, ComplianceManager]
- `DELETE /api/v1/gdp-sops/{sopId}/sites/{siteId}` - Unlink SOP from site [QAUser, ComplianceManager]

### GDP Change Control (`/api/v1/gdp-changes`)
- `GET /api/v1/gdp-changes` - List all change records
- `GET /api/v1/gdp-changes/{changeId}` - Get change record by ID
- `GET /api/v1/gdp-changes/pending` - Pending change records
- `POST /api/v1/gdp-changes` - Create change record [QAUser, ComplianceManager]
- `POST /api/v1/gdp-changes/{changeId}/approve` - Approve change [ComplianceManager]
- `POST /api/v1/gdp-changes/{changeId}/reject` - Reject change [ComplianceManager]

### Approval Workflows (`/api/v1/workflows`)
- `POST /api/v1/workflows/trigger` - Trigger approval workflow [ComplianceManager]
- `POST /api/v1/workflows/callback` - Workflow callback (Logic App)
- `GET /api/v1/workflows/{workflowId}/status` - Get workflow status

### Webhook Subscriptions (`/api/v1/webhooksubscriptions`)
- `GET /api/v1/webhooksubscriptions` - List subscriptions [SystemAdmin]
- `GET /api/v1/webhooksubscriptions/{id}` - Get subscription [SystemAdmin]
- `GET /api/v1/webhooksubscriptions/event-types` - Get available event types
- `POST /api/v1/webhooksubscriptions` - Create subscription [SystemAdmin]
- `PUT /api/v1/webhooksubscriptions/{id}` - Update subscription [SystemAdmin]
- `DELETE /api/v1/webhooksubscriptions/{id}` - Delete subscription [SystemAdmin]
- `POST /api/v1/webhooksubscriptions/{id}/reactivate` - Reactivate [SystemAdmin]
- `POST /api/v1/webhooksubscriptions/{id}/deactivate` - Deactivate [SystemAdmin]

### Integration Systems (`/api/v1/integrationsystems`)
- `GET /api/v1/integrationsystems` - List integration systems [SystemAdmin]
- `GET /api/v1/integrationsystems/{id}` - Get integration system [SystemAdmin]
- `POST /api/v1/integrationsystems` - Register system [SystemAdmin]
- `PUT /api/v1/integrationsystems/{id}` - Update system [SystemAdmin]
- `DELETE /api/v1/integrationsystems/{id}` - Delete system [SystemAdmin]

---

## CLI Tool

The `RE2.ComplianceCli` console application provides a text I/O protocol for debugging, scripting, and automation. It uses in-memory repositories with seed data.

### Commands

```bash
# Validate a transaction (reads JSON from stdin)
re2-cli validate-transaction --file transaction.json

# Look up customer compliance status
re2-cli lookup-customer --account CUST001 --data-area-id nl01

# Look up licence details
re2-cli lookup-licence --number LIC-2025-001

# Generate compliance reports
re2-cli generate-report --type transaction-audit --from 2025-01-01 --to 2025-12-31
```

All commands output structured JSON to stdout and support `--verbose` for debug logging to stderr.

---

## Implementation Progress

### Phase 1: Setup - COMPLETE
- .NET 8 solution structure (RE2.sln)
- 7 source projects + 5 test projects
- Project references and dependency graph
- .gitignore, .editorconfig, appsettings.json templates

### Phase 2: Foundation - COMPLETE
- Core interfaces (IDataverseClient, ID365FoClient, IDocumentStorage)
- Data access implementations (Dataverse, D365 F&O, InMemory, BlobStorage)
- Constants (ErrorCodes, LicenceTypes, SubstanceCategories, TransactionTypes)
- Error handling middleware
- Dependency injection registration
- In-memory repositories with seed data for local development

### Phase 3: User Story 1 (Licence Management) - COMPLETE
- Domain models: Licence, LicenceType, ControlledSubstance, LicenceSubstanceMapping
- LicenceService with full business logic
- LicenceSubstanceMappingService
- LicencesController, LicenceTypesController, LicenceSubstanceMappingsController
- Web UI: licence listing, creation, editing

### Phase 4: User Story 2 (Customer Onboarding) - COMPLETE
- Domain model: Customer (composite D365 F&O + Dataverse)
- CustomerService with compliance status, suspend/reinstate, re-verification
- CustomersController with composite key routing
- Web UI: customer listing, compliance configuration

### Phase 5: User Story 3 (Document & Alert Management) - COMPLETE
- Models: LicenceDocument, LicenceVerification, LicenceScopeChange, Alert
- Document upload/download with blob storage integration
- Verification recording and scope change history
- AlertGenerationService for expiry monitoring (90/60/30 day warnings)
- Dashboard with alert summary

### Phase 6: User Story 4 (Transaction Validation) - COMPLETE
- Models: Transaction, TransactionLine, TransactionViolation, TransactionLicenceUsage
- TransactionComplianceService with multi-rule validation engine
- Product-based transaction lines (ItemNumber/DataAreaId, substance resolved server-side)
- Override approval/rejection workflow (separate approve and reject endpoints)
- Warehouse operation validation endpoint
- Threshold monitoring (ThresholdService, ThresholdsController)

### Phase 7: User Story 5 (Reporting & Reclassification) - COMPLETE
- ReportingService: transaction audit, licence usage, customer compliance history
- LicenceCorrectionImpactService for historical validation analysis (SC-038)
- SubstanceReclassificationService with impact analysis and compliance notifications
- ReportsController, SubstanceReclassificationController

### Phase 8: User Story 6 (Risk & Workflows) - COMPLETE
- ApprovalWorkflowController for high-risk event approvals (FR-030)
- WebhookSubscription model with event type filtering
- WebhookDispatchService for async notifications
- IntegrationSystem model and registration endpoints
- AuditLoggingService with full audit trail

### Phase 9: User Story 7 (GDP Sites) - COMPLETE
- GdpSite composite model (D365 warehouse + Dataverse GDP extension)
- GdpSiteWdaCoverage for WDA licence coverage tracking
- GdpComplianceService with configuration, WDA coverage, site type management
- GdpSitesController with warehouse browsing and GDP configuration
- RegulatoryInspection model and repository
- Web UI: GDP sites, inspections

### Phase 10: User Story 8 (GDP Provider Qualification) - COMPLETE
- GdpServiceProvider model with qualification status and review dates
- GdpCredential, GdpCredentialVerification, GdpDocument models
- IGdpCredentialRepository, IGdpDocumentRepository interfaces
- GdpProvidersController (API) with credential, verification, document, review endpoints
- GdpProvidersController, GdpCredentialsController (Web) with full CRUD
- Partner qualification check endpoint (FR-038)
- Web UI: provider listing, credential management, expiring credentials dashboard

### Phase 11: User Story 9 (GDP Inspections & CAPA) - COMPLETE
- GdpInspection model with inspection findings
- Capa model with corrective/preventive action tracking (FR-041, FR-042)
- IGdpInspectionRepository, ICapaRepository interfaces
- GdpInspectionsController (API) with findings, CAPAs, overdue tracking
- GdpInspectionsController (Web) with inspection/finding/CAPA management
- Web UI: inspections list, CAPA dashboard, overdue corrective actions

### Phase 12: User Story 10 (GDP Certificates & Monitoring) - COMPLETE
- Extended GdpCredential with certificate types and monitoring
- Credential verification workflows (EudraGMDP integration)
- Document upload/download for GDP credentials (FR-044)
- Expiry monitoring with configurable look-ahead

### Phase 13: User Story 11 (GDP Operational Checks) - COMPLETE
- GdpEquipmentQualification model with qualification status tracking (FR-048)
- GdpOperationalService for site/provider validation (FR-046, FR-047)
- GdpOperationsController (API) with validation and equipment CRUD
- GdpEquipmentController (Web) with equipment qualification management
- GdpOperationsController (Web) with operations dashboard
- Web UI: equipment qualifications, operations dashboard with provider stats

### Phase 14: User Story 12 (GDP Documentation, Training & Change Control) - COMPLETE
- GdpSop, GdpSiteSop models for SOP management (FR-049)
- TrainingRecord model with assessment tracking (FR-050)
- GdpChangeRecord model with approval workflow (FR-051)
- IGdpSopRepository, ITrainingRepository, IGdpChangeRepository interfaces
- GdpSopsController, GdpChangeControlController (API)
- GdpSopsController, TrainingController, ChangeControlController (Web)
- Web UI: SOP management with site linking, training records, change control with approval

---

## Infrastructure & Deployment

### Azure Resources (managed by Bicep)

All infrastructure is defined in `infra/bicep/` and deployed via the Azure DevOps pipeline.

| Resource | Naming Pattern | Purpose |
|----------|---------------|---------|
| Log Analytics + App Insights | `log-re2-{env}`, `appi-re2-{env}` | Monitoring, diagnostics, alerting |
| Storage Account | `stre2{env}` | Blob Storage (compliance documents) + Functions runtime |
| Azure Cache for Redis | `redis-re2-{env}` | Distributed caching (licence/customer lookups) |
| Key Vault | `kv-re2-{env}` | Secrets management (Redis connection string) |
| App Service Plan | `plan-re2-{env}` | Shared plan for API + Web |
| App Service (API) | `app-re2-api-{env}` | REST API hosting |
| App Service (Web) | `app-re2-web-{env}` | MVC Web UI hosting |
| Functions App | `func-re2-compliance-{env}` | Background jobs (timer-triggered) |
| Logic App | `re2-approval-workflow-{env}` | Approval workflow orchestration |

### Environment Sizing

| Resource | Dev | UAT | Prod |
|----------|-----|-----|------|
| App Service Plan | B1 | S1 | P1v3 |
| Functions Plan | Y1 (Consumption) | Y1 | EP1 (Elastic Premium) |
| Redis | Basic C0 | Standard C1 | Premium P1 |
| Storage | LRS | GRS | GRS |
| Log retention | 30 days | 60 days | 90 days |
| Deployment slots | No | Yes (staging) | Yes (staging) |

### CI/CD Pipeline (`pipelines/azure-pipelines.yml`)

```
[Build & Test] → [Deploy Dev (auto)] → [Deploy UAT (manual gate)] → [Deploy Prod (manual gate)]
```

- **Build**: Restore → Build → Test (1,432 tests) → Publish artifacts (API, Web, Functions, Bicep)
- **Dev**: Auto-deploy on main merge, direct zip deploy, smoke test `/health`
- **UAT**: Manual approval, deploy to staging slot, smoke test, slot swap (zero-downtime)
- **Prod**: 2-approver gate, same slot swap pattern as UAT

### Security Model

- All App Services and Functions use **system-assigned Managed Identity**
- Managed Identities are granted **Storage Blob Data Contributor** and **Key Vault Secrets User** via RBAC
- Redis connection string stored in Key Vault; referenced via `@Microsoft.KeyVault(...)` in App Settings
- Auth to Dataverse/D365 F&O uses `DefaultAzureCredential` (no secrets needed)

See `docs/deployment/README.md` for the full deployment guide.

---

## Quick Start

### Prerequisites

- **.NET 8 SDK** (8.0.x)
- **Visual Studio 2022** (17.8+) or **VS Code** with C# extension

No external services required for local development -- the system runs entirely with in-memory repositories and seed data.

### Build the Solution

```bash
cd C:\src\RE2
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

1,432 tests across 6 test projects:
- `RE2.ComplianceCore.Tests` - Domain model and service unit tests (890)
- `RE2.ComplianceApi.Tests` - API controller integration tests (364)
- `RE2.Contract.Tests` - API contract tests (125)
- `RE2.DataAccess.Tests` - Repository and data access tests (32)
- `RE2.ComplianceCli.Tests` - CLI command tests (14)
- `RE2.ComplianceFunctions.Tests` - Azure Functions tests (7)

### Run the API

```bash
dotnet run --project src/RE2.ComplianceApi
```

API will be available at: `https://localhost:7001/api/v1`

### Run the Web UI

```bash
dotnet run --project src/RE2.ComplianceWeb
```

Web UI will be available at: `https://localhost:5001`

### Run the CLI

```bash
dotnet run --project src/RE2.ComplianceCli -- lookup-customer --account CUST001 --data-area-id nl01
```

---

## In-Memory Mode

For local development, all repositories use in-memory implementations backed by `InMemorySeedData`. This provides:

- **23 in-memory repositories** covering all domain entities
- Pre-seeded reference data (licence types, substances, customers, products, thresholds)
- No external service dependencies (no Azure, no Dataverse, no D365 F&O)
- Registered via `services.AddInMemoryRepositories()` in DI

The same in-memory mode is used by the CLI tool and test projects.

---

## Development Workflow

### TDD Approach (Constitutional Requirement)

1. **Write tests first** (Red)
2. **Get user/stakeholder approval** on tests
3. **Verify tests fail**
4. **Implement minimum code** to pass tests (Green)
5. **Refactor** while keeping tests green

---

## Performance Targets

| Operation | Target | Requirement |
|-----------|--------|-------------|
| Transaction validation | <3 seconds | SC-005 |
| Customer compliance lookup | <1 second | SC-033 |
| Audit report generation | <2 minutes | SC-009 |
| Concurrent validation requests | 50 requests | SC-032 |
| System availability | 99.5% uptime | FR-052 |
| Mean time to recovery | <30 minutes | SC-031 |

---

## Security & Compliance

### Authentication
- **Internal users**: Azure AD with JWT tokens
- **External users**: Azure AD B2C with SSO
- **API authentication**: OAuth2 bearer tokens

### Authorization Roles
- **ComplianceManager**: Full access to licences, approvals, overrides, substance management, thresholds
- **QAUser**: GDP site management, inspections, reports
- **SalesAdmin**: Customer onboarding, qualification
- **SystemAdmin**: Integration system and webhook subscription management
- **TrainingCoordinator**: Training records management

### Compliance Features
- **Audit trail**: All data changes logged (FR-027)
- **Optimistic concurrency**: RowVersion fields prevent conflicts (FR-027a)
- **Standardized error codes**: Consistent API responses (FR-064)
- **API versioning**: 6-month backward compatibility (FR-062)
- **Webhook notifications**: Async event delivery to registered systems (FR-059)

---

## Documentation

- **API Reference**: `docs/api/README.md` -- endpoint tables, error codes, authentication
- **User Guide**: `docs/user-guide/README.md` -- compliance web UI walkthrough
- **Integration Guide**: `docs/integration/README.md` -- ERP/WMS integration patterns, webhooks, examples
- **Deployment Guide**: `docs/deployment/README.md` -- Azure deployment, CI/CD, operations
- **Specification**: `specs/001-licence-management/spec.md`
- **Technical Plan**: `specs/001-licence-management/plan.md`
- **Data Model**: `specs/001-licence-management/data-model.md`
- **API Contracts**: `specs/001-licence-management/contracts/`
- **Developer Quickstart**: `specs/001-licence-management/quickstart.md`
- **Research**: `specs/001-licence-management/research.md`
- **Tasks**: `specs/001-licence-management/tasks.md`

---

## Troubleshooting

### Common Issues

1. **NuGet restore fails**:
   - Check network connectivity
   - Clear NuGet cache: `dotnet nuget locals all --clear`
   - Retry restore: `dotnet restore --force`

2. **Azure services unavailable**:
   - Local development uses in-memory repositories by default -- no Azure services needed
   - For production-like testing, use Azurite for local blob storage

3. **Authentication issues**:
   - Configure mock JWT tokens for local development
   - Use development certificates: `dotnet dev-certs https --trust`

---

**Status**: Implemented (User Stories 1-12)
**Version**: MVP Complete
**Last Updated**: 2026-02-21
