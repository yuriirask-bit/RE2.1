# Research: User Story 10 - GDP Certificates, Validity & Monitoring

**Feature**: US10 GDP Certificates, Validity & Monitoring | **Date**: 2026-02-17

## Overview

User Story 10 adds document attachment capabilities for GDP entity records, a scheduled Azure Function for automated GDP credential expiry monitoring, and web UI for credential validity management and verification logging. This builds heavily on existing infrastructure from US3 (LicenceDocument/IDocumentStorage) and US8 (GdpCredential/GdpCredentialVerification/AlertGenerationService).

**Key insight**: FR-043 (validity periods + alerts) and FR-045 (EudraGMDP verification logging) are already implemented at core/API level in US8. The main new work centers on FR-044 (document attachment), the GdpCertificateMonitor Azure Function, and web UI.

---

## 1. GdpDocument Model Design — Polymorphic vs Per-Entity

### Decision: Single polymorphic GdpDocument model with OwnerEntityType/OwnerEntityId

**Rationale**:
- Follows the same polymorphic pattern used by GdpCredential (EntityType/EntityId), Alert (TargetEntityType/TargetEntityId), and QualificationReview (EntityType/EntityId)
- Single Dataverse table (phr_gdpdocument) is simpler than multiple tables
- Single repository and service layer, avoiding code duplication
- Reuses existing DocumentType enum (Certificate, Letter, InspectionReport, Other)
- New GdpDocumentEntityType enum: Credential, Site, Inspection, Provider, Customer

**Alternatives Considered**:
- Per-entity document models (GdpCredentialDocument, GdpSiteDocument) — rejected because it duplicates identical code across multiple models, repositories, and Dataverse tables
- Extending LicenceDocument to be polymorphic — rejected because LicenceDocument is tightly coupled to LicenceId and the licence repository

---

## 2. Azure Blob Storage Container Strategy

### Decision: Separate container `gdp-documents` with blob naming: `{entityType}/{entityId}/{documentId}/{filename}`

**Rationale**:
- Follows the established pattern from LicenceService (`licence-documents` container)
- Separate containers allow independent access policies and lifecycle management
- EntityType prefix in blob path enables efficient listing by entity type
- Consistent with Azure Blob Storage best practices for logical isolation

**Alternatives Considered**:
- Single `documents` container mixing GDP and licence documents — rejected because it complicates access control and cleanup
- Per-entity-type containers (gdp-credential-documents, gdp-site-documents) — rejected as over-engineering; single container with path prefixes is sufficient

---

## 3. GdpCertificateMonitor Scheduling and Scope

### Decision: Daily at 3 AM UTC, generating GDP credential expiry + provider requalification + CAPA overdue alerts

**Rationale**:
- 1-hour offset from LicenceExpiryMonitor (2 AM UTC) prevents resource contention
- All alert generation methods already exist in AlertGenerationService:
  - `GenerateGdpCredentialExpiryAlertsAsync(90)` — 90/60/30 day severity levels
  - `GenerateProviderRequalificationAlertsAsync()` — re-qualification due alerts
  - `GenerateCapaOverdueAlertsAsync()` — CAPA overdue alerts (from US9)
- Including CAPA overdue alerts consolidates GDP monitoring in one function
- Follows identical pattern to LicenceExpiryMonitor (timer + manual HTTP trigger)

**Alternatives Considered**:
- Same 2 AM schedule as LicenceExpiryMonitor — rejected to avoid concurrent execution pressure
- Separate functions per alert type — rejected as over-engineering; single function is simpler

---

## 4. Document Upload Size and Type Restrictions

### Decision: Same restrictions as LicenceDocument

**Implementation**:
- Allowed extensions: .pdf, .doc, .docx, .jpg, .jpeg, .png, .tiff
- Maximum file size: 50 MB (enforced at API and web UI layers)
- Content type validation matches file extension
- Validation in GdpDocument.Validate() method (same as LicenceDocument.Validate())

**Rationale**:
- Consistent with established LicenceDocument validation rules
- GDP certificates, WDA copies, and inspection reports are typically PDF or scanned images
- 50 MB accommodates high-resolution scans of multi-page documents

---

## 5. Web UI for Credential Management — Dedicated Controller

### Decision: New GdpCredentialsController for credential-centric views

**Rationale**:
- Credentials span multiple entity types (Supplier customers AND service providers) — a provider-only controller is too narrow
- Dedicated controller allows credential-centric navigation: "All Credentials" → "Expiring" → "Details with Documents & Verifications"
- Follows SRP — GdpProvidersController remains focused on provider management
- Navigation: GDP Credentials link in navbar separate from GDP Providers

**Views**:
- Index: All credentials with validity status badges (Valid/Expiring/Expired)
- Details: Credential details with documents and verification history tabs
- Expiring: Dashboard showing credentials expiring within configurable window
- RecordVerification: Form to record EudraGMDP/national DB checks
- UploadDocument: Form to upload document with metadata

**Alternatives Considered**:
- Extending GdpProvidersController — rejected because it cannot naturally show customer-held credentials
- Single GdpComplianceController — rejected as too broad, violates SRP

---

## 6. Verification Web UI Integration

### Decision: Accessible from GdpCredentials/Details view as "Record Verification" action

**Rationale**:
- Verifications are always performed in context of a specific credential
- Shows verification history alongside credential validity status for complete picture
- Form pre-populates CredentialId, offers dropdowns for Method (EudraGMDP, NationalDatabase, Other) and Outcome (Valid, Invalid, NotFound) enums
- Follows pattern from GdpInspections where findings are shown on inspection Details page

---

## 7. Company's Own WDA/GDP Certificates

### Decision: Handled by existing Licence model (HolderType=Company), not GdpCredential

**Rationale**:
- The Licence model already supports `HolderType = Company | Customer` for tracking the company's own licences including WDAs
- LicenceExpiryMonitor already generates expiry alerts for company licences
- GdpCredential is specifically for partner/external entity credentials
- US10 acceptance scenario 1 says "our company AND key partners" — company is covered by Licence, partners by GdpCredential
- No model changes needed for company WDA validity tracking — it's already complete

**Alternatives Considered**:
- Adding HolderType.Company to GdpCredentialEntityType — rejected because it would duplicate Licence functionality and create confusion about where company WDAs are tracked
