# Implementation Plan: User Story 10 - GDP Certificates, Validity & Monitoring

**Branch**: `001-licence-management` | **Date**: 2026-02-17 | **Spec**: [spec.md](../001-licence-management/spec.md)
**Input**: Feature specification from `/specs/001-licence-management/spec.md` (User Story 10, lines 172-184)

## Summary

User Story 10 adds document attachment capabilities for GDP entity records, a scheduled Azure Function for automated GDP credential expiry monitoring, and web UI for credential validity management and verification logging. This builds on the existing GDP framework from US7-US9, extending the established LicenceDocument pattern to GDP entities and operationalizing the AlertGenerationService's existing GDP credential expiry alert methods via a timer-triggered function.

**Key insight from codebase analysis**: Much of FR-043 (validity periods + alerts) and FR-045 (EudraGMDP verification logging) are already implemented at the core/API level in US8. The main new work centers on FR-044 (document attachment), the GdpCertificateMonitor Azure Function, and web UI to surface existing capabilities.

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL)
**Primary Dependencies**: ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Azure SDK libraries (Azure.Identity, Azure.Storage.Blobs), Azure.Functions.Worker, Microsoft.PowerPlatform.Dataverse.Client 1.0.x
**Storage**: Dataverse virtual tables (phr_gdpdocument), Azure Blob Storage for document files (via existing IDocumentStorage/DocumentStorageClient)
**Testing**: xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
**Target Platform**: Azure App Service (Web), Azure Functions (background jobs)
**Project Type**: web (extends existing solution: ComplianceCore, DataAccess, ComplianceApi, ComplianceWeb, ComplianceFunctions)
**Performance Goals**: <3 seconds for document upload/download, <1 second for credential validity lookup
**Constraints**: Stateless services, all business data in Dataverse/D365 F&O, documents in Azure Blob Storage
**Scale/Scope**: 1,000 GDP credentials, 10,000 documents, 50 GDP sites

## Gap Analysis (What Exists vs What's Needed)

### FR-043: Validity Periods & Automated Expiry Alerts

| Component | Status | Details |
|-----------|--------|---------|
| GdpCredential model with ValidityStartDate/ValidityEndDate | DONE (US8) | `ComplianceCore/Models/GdpCredential.cs` |
| GdpCredential.IsValid() method | DONE (US8) | Checks current date against validity dates |
| AlertGenerationService.GenerateGdpCredentialExpiryAlertsAsync() | DONE (US8) | 90/60/30 day severity levels |
| AlertGenerationService.GenerateProviderRequalificationAlertsAsync() | DONE (US8) | Provider re-qualification alerts |
| GdpComplianceService.GetCredentialsExpiringAsync() | DONE (US8) | Query for expiring credentials |
| API: GET /api/v1/gdp-providers/credentials/expiring | DONE (US8) | `GdpProvidersController` |
| **GdpCertificateMonitor Azure Function** | **TODO** | Timer trigger calling existing alert generation methods |
| **Web UI: Credential validity management views** | **TODO** | View/manage validity, see expiring credentials |

### FR-044: Document Attachment to Entity Records

| Component | Status | Details |
|-----------|--------|---------|
| IDocumentStorage interface | DONE (US3) | `ComplianceCore/Interfaces/IDocumentStorage.cs` |
| DocumentStorageClient (Azure Blob) | DONE (US3) | `DataAccess/BlobStorage/DocumentStorageClient.cs` |
| LicenceDocument model (PATTERN) | DONE (US3) | Template for GdpDocument model |
| LicenceService document methods (PATTERN) | DONE (US3) | Upload/download/delete pattern |
| **GdpDocument model** | **TODO** | New entity for GDP document attachments |
| **IGdpDocumentRepository** | **TODO** | Repository interface for GdpDocument CRUD |
| **InMemoryGdpDocumentRepository** | **TODO** | In-memory implementation |
| **DataverseGdpDocumentRepository** | **TODO** | Dataverse implementation (phr_gdpdocument) |
| **GdpComplianceService document methods** | **TODO** | Upload/download/delete/list for GDP documents |
| **API endpoints for GDP documents** | **TODO** | REST API for document CRUD |
| **Web UI: Document upload/view/delete** | **TODO** | File upload forms, document listings per entity |

### FR-045: EudraGMDP Verification Logging

| Component | Status | Details |
|-----------|--------|---------|
| GdpCredentialVerification model | DONE (US8) | VerificationMethod, Outcome, VerifiedBy |
| IGdpCredentialRepository verification methods | DONE (US8) | GetVerificationsByCredentialAsync, CreateVerificationAsync |
| GdpComplianceService.RecordVerificationAsync() | DONE (US8) | Records verification, updates LastVerificationDate |
| API: GET/POST credentials/{id}/verifications | DONE (US8) | `GdpProvidersController` |
| **Web UI: Verification logging form** | **TODO** | Form to record EudraGMDP/national DB checks |
| **Web UI: Verification history view** | **TODO** | View past verifications per credential |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Specification-First**: Feature spec is complete and technology-agnostic (US10 in spec.md lines 172-184 with 3 acceptance scenarios)
- [x] **Test-First**: TDD workflow planned — tests written before implementation for new GdpDocument model, repository, service methods, and API endpoints
- [⚠️] **Library-First**: Core logic in RE2.ComplianceCore library with clear boundaries (same justified violation as parent plan — multi-service architecture required)
- [⚠️] **CLI Interface**: Web APIs and Azure Functions primary interfaces (same justified violation as parent plan — RE2.ComplianceCli exists for mitigation)
- [x] **Versioning**: API versioning via /api/v1 URL paths; new endpoints backward-compatible (additions only)
- [x] **Observability**: Structured logging via ILogger in all new components; AlertGenerationService logs alert counts
- [x] **Simplicity**: GdpDocument follows established LicenceDocument pattern; GdpCertificateMonitor follows LicenceExpiryMonitor pattern; no new abstractions
- [x] **Independent Stories**: US10 independently testable — document attachment, certificate monitoring, and verification UI can each be tested standalone

**Violations**: Same as parent plan (see Complexity Tracking) — Library-First and CLI Interface violations carry forward with same justification.

## Project Structure

### Documentation (this feature)

```text
specs/main/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (files new or modified for US10)

```text
src/
├── RE2.ComplianceCore/
│   ├── Models/
│   │   └── GdpDocument.cs                          # NEW: GDP document attachment model
│   ├── Interfaces/
│   │   └── IGdpDocumentRepository.cs               # NEW: Repository interface for GdpDocument
│   └── Services/
│       └── GdpCompliance/
│           └── GdpComplianceService.cs             # MODIFIED: Add document CRUD methods
│
├── RE2.DataAccess/
│   ├── InMemory/
│   │   ├── InMemoryGdpDocumentRepository.cs        # NEW: In-memory GdpDocument repository
│   │   └── InMemorySeedData.cs                     # MODIFIED: Add sample GDP documents
│   ├── Dataverse/
│   │   └── Repositories/
│   │       └── DataverseGdpDocumentRepository.cs   # NEW: Dataverse GdpDocument repository
│   └── DependencyInjection/
│       └── InfrastructureExtensions.cs             # MODIFIED: Register new repositories
│
├── RE2.ComplianceApi/
│   └── Controllers/V1/
│       └── GdpProvidersController.cs               # MODIFIED: Add document endpoints
│
├── RE2.ComplianceWeb/
│   ├── Controllers/
│   │   ├── GdpProvidersController.cs               # MODIFIED: Add document & verification views
│   │   └── GdpCredentialsController.cs             # NEW: Dedicated controller for credential views
│   └── Views/
│       ├── GdpCredentials/                         # NEW: Credential management views
│       │   ├── Index.cshtml                        # Credential listing with validity status
│       │   ├── Details.cshtml                      # Credential details with documents & verifications
│       │   ├── Expiring.cshtml                     # Expiring credentials dashboard
│       │   ├── RecordVerification.cshtml            # Verification logging form
│       │   └── UploadDocument.cshtml               # Document upload form
│       └── GdpProviders/                           # MODIFIED: Add document section to provider views
│           └── Details.cshtml                      # MODIFIED: Add documents tab
│
├── RE2.ComplianceFunctions/
│   └── GdpCertificateMonitor.cs                    # NEW: Timer trigger for GDP credential expiry
│
└── RE2.ComplianceCli/                              # MODIFIED: Add GDP document commands

tests/
├── RE2.ComplianceCore.Tests/
│   └── Models/
│       └── GdpDocumentTests.cs                     # NEW: GdpDocument validation tests
├── RE2.ComplianceApi.Tests/
│   └── Controllers/V1/
│       └── GdpProvidersControllerDocumentTests.cs  # NEW: Document API endpoint tests
└── RE2.ComplianceFunctions.Tests/
    └── GdpCertificateMonitorTests.cs               # NEW: Monitor function tests
```

**Structure Decision**: Extends existing web application architecture. No new projects — all changes fit within existing solution structure. GdpDocument follows established LicenceDocument pattern. GdpCertificateMonitor follows LicenceExpiryMonitor pattern.

## Complexity Tracking

> Violations carry forward from parent plan (001-licence-management/plan.md)

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| **Multi-service architecture** | Same as parent plan | Same as parent plan |
| **Web APIs instead of CLI** | Same as parent plan | Same as parent plan |

---

## Post-Design Constitution Re-Evaluation

**Date**: 2026-02-17 | **Phase**: After Phase 1 Design (data-model.md, contracts/, quickstart.md completed)

### Re-evaluation Results

- [x] **Specification-First**: PASS — Feature spec remains technology-agnostic; design artifacts maintain separation
- [x] **Test-First**: PASS — TDD workflow documented in quickstart.md with GdpDocument tests before implementation
- [⚠️] **Library-First**: JUSTIFIED VIOLATION — Same as parent plan; core logic in RE2.ComplianceCore
- [⚠️] **CLI Interface**: JUSTIFIED VIOLATION — Same as parent plan; RE2.ComplianceCli exists
- [x] **Versioning**: PASS — New API endpoints are backward-compatible additions to existing /api/v1
- [x] **Observability**: PASS — Structured logging in GdpCertificateMonitor; ILogger in all services
- [x] **Simplicity**: PASS — Single new entity (GdpDocument) following established pattern; no new abstractions
- [x] **Independent Stories**: PASS — Document attachment, certificate monitoring, and verification UI independently testable

### Design Quality Assessment

**data-model.md**: One new entity (GdpDocument) with clear polymorphic ownership pattern. Correctly identifies that FR-043 and FR-045 core infrastructure already exists from US8.

**contracts/gdp-documents-api.yaml**: OpenAPI 3.0.3 contract for document upload/download/delete with multipart form data. Includes existing verification and credential expiry endpoints for reference.

**quickstart.md**: Developer onboarding with code patterns from LicenceDocument and LicenceExpiryMonitor. TDD workflow documented.

**research.md**: Seven research items resolved. Key decisions: polymorphic GdpDocument model, separate gdp-documents blob container, GdpCertificateMonitor at 3 AM UTC, dedicated GdpCredentialsController.

### Gate Status

APPROVED — Proceed to Phase 2 (task generation via `/speckit.tasks`)
