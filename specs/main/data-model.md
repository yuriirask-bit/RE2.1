# Data Model: User Story 10 - GDP Certificates, Validity & Monitoring

**Feature**: US10 GDP Certificates, Validity & Monitoring | **Date**: 2026-02-17
**Parent Data Model**: [../001-licence-management/data-model.md](../001-licence-management/data-model.md) (entities 12, 18, 30)

## Overview

One new domain entity (GdpDocument) for attaching documents to GDP entity records. Builds on existing entities: GdpCredential (entity 18) for validity periods, GdpCredentialVerification (entity 30) for EudraGMDP logging, LicenceDocument (entity 12) as the pattern template, and AlertGenerationService for automated expiry monitoring.

## New Entities

### 1. GdpDocument

Stores metadata and Azure Blob Storage reference for documents attached to GDP entity records (credentials, sites, inspections, providers, customers). Follows the established LicenceDocument pattern.

**Attributes**:
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `DocumentId` | Guid | PK | Unique identifier |
| `OwnerEntityType` | enum | Yes | Which entity type this document belongs to |
| `OwnerEntityId` | Guid | Yes | FK → owning entity (polymorphic) |
| `DocumentType` | enum | Yes | Reuses existing DocumentType enum |
| `FileName` | string | Yes | Original filename |
| `BlobStorageUrl` | string | Yes | Azure Blob Storage URL (no SAS token) |
| `UploadedDate` | DateTime | Yes | When document was uploaded |
| `UploadedBy` | string | Yes | Who uploaded (user display name) |
| `ContentType` | string | No | MIME type (e.g., "application/pdf") |
| `FileSizeBytes` | long | No | File size in bytes |
| `Description` | string | No | Optional description of the document |

**Relationships**:
- Belongs to one entity (polymorphic via OwnerEntityType/OwnerEntityId):
  - `GdpCredential` (OwnerEntityType.Credential → credential.CredentialId) — GDP certificates, WDA copies
  - `GdpSite` (OwnerEntityType.Site → site.GdpExtensionId) — warehouse documentation
  - `GdpInspection` (OwnerEntityType.Inspection → inspection.InspectionId) — inspection reports
  - `GdpServiceProvider` (OwnerEntityType.Provider → provider.ProviderId) — provider qualification docs
  - `Customer` (OwnerEntityType.Customer → customer.ComplianceExtensionId) — supplier GDP certificates

**Storage**: Dataverse virtual table `phr_gdpdocument`

**Blob Storage**: Container `gdp-documents`, blob path: `{entityType}/{entityId}/{documentId}/{filename}`

**Validation Rules**:
- `OwnerEntityId` must not be Guid.Empty
- `FileName` must not be empty
- `BlobStorageUrl` must not be empty
- `UploadedBy` must not be empty
- File extension must be in allowed list: .pdf, .doc, .docx, .jpg, .jpeg, .png, .tiff
- Maximum file size: 50 MB

**Enums**:
```csharp
public enum GdpDocumentEntityType
{
    Credential,     // GdpCredential — GDP certificates, WDA copies
    Site,           // GdpSite — warehouse documentation
    Inspection,     // GdpInspection — inspection reports
    Provider,       // GdpServiceProvider — provider qualification docs
    Customer        // Customer — supplier GDP certificates
}
```

**Reuses existing enum** from LicenceDocument:
```csharp
public enum DocumentType
{
    Certificate = 1,       // GDP certificates, WDA copies
    Letter = 2,            // Correspondence
    InspectionReport = 3,  // Inspection reports
    Other = 4              // Other supporting documentation
}
```

---

## Existing Entities Referenced (No Changes)

### GdpCredential (Entity 18 — FR-043 Support)

Already provides validity period tracking:
- `ValidityStartDate` (DateOnly, nullable) — when credentials became valid
- `ValidityEndDate` (DateOnly, nullable) — when credentials expire
- `IsValid()` — checks if credential is currently valid based on dates
- `LastVerificationDate` (DateOnly, nullable) — updated when verification recorded
- `NextReviewDate` (DateOnly, nullable) — next periodic review due

**Alert integration** (already implemented in AlertGenerationService):
- `GenerateGdpCredentialExpiryAlertsAsync(90)` — alerts at 90/60/30 day intervals
- `GenerateProviderRequalificationAlertsAsync()` — re-qualification due alerts
- Alert types: `GdpCertificateExpiring` (6), `GdpCertificateExpired` (7), `VerificationOverdue` (8)

### GdpCredentialVerification (Entity 30 — FR-045 Support)

Already provides EudraGMDP verification logging:
- `VerificationMethod` (enum: EudraGMDP, NationalDatabase, Other)
- `Outcome` (enum: Valid, Invalid, NotFound)
- `VerifiedBy` (string) — who performed verification
- `VerificationDate` (DateOnly) — when verified
- `RecordVerificationAsync()` in GdpComplianceService updates credential's `LastVerificationDate`

### LicenceDocument (Entity 12 — Pattern Template)

Pattern template for GdpDocument:
- Same validation rules (file extensions, required fields)
- Same blob storage approach (URL stored, SAS generated on demand)
- Same service pattern (upload → validate → store blob → save metadata)

---

## Entity Relationships Diagram

```text
# New entity (US10)
GdpDocument ----< (M) GdpCredential      (via OwnerEntityType.Credential)
GdpDocument ----< (M) GdpSite            (via OwnerEntityType.Site)
GdpDocument ----< (M) GdpInspection      (via OwnerEntityType.Inspection)
GdpDocument ----< (M) GdpServiceProvider  (via OwnerEntityType.Provider)
GdpDocument ----< (M) Customer           (via OwnerEntityType.Customer)

# Existing relationships (unchanged)
GdpCredential (1) ----< (M) GdpCredentialVerification
GdpCredential → Alert (via AlertGenerationService)
Licence (HolderType=Company) → Alert (via LicenceExpiryMonitor)
```

## Integration Points

| Component | Integration | Details |
|-----------|-------------|---------|
| **IDocumentStorage** | Reuse existing | Upload/download/delete via DocumentStorageClient |
| **DocumentType enum** | Reuse existing | Certificate, Letter, InspectionReport, Other |
| **AlertGenerationService** | Reuse existing methods | GDP credential expiry + provider requalification + CAPA overdue |
| **GdpCertificateMonitor** | NEW Azure Function | Timer trigger (3 AM UTC) calling existing alert methods |
| **GdpComplianceService** | Extend | Add document CRUD methods |
| **IGdpDocumentRepository** | NEW interface | CRUD for phr_gdpdocument |
