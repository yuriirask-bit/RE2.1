# Quickstart: User Story 10 - GDP Certificates, Validity & Monitoring

## Prerequisites

- .NET 8 SDK installed
- Solution builds cleanly: `dotnet build RE2.sln`
- All existing tests pass: `dotnet test RE2.sln` (1049 tests from US9)

## What You're Building

User Story 10 adds:
1. **GdpDocument model** — polymorphic document attachment for GDP entities (FR-044)
2. **GdpCertificateMonitor Azure Function** — automated expiry alerts (FR-043)
3. **Web UI** — credential management, document attachment, verification logging (FR-043/044/045)

Most of the core infrastructure already exists from US3 (LicenceDocument, IDocumentStorage) and US8 (GdpCredential, GdpCredentialVerification, AlertGenerationService).

## Key Patterns to Follow

### 1. GdpDocument Model (follows LicenceDocument pattern)

```csharp
// Pattern from LicenceDocument.cs — adapt for polymorphic ownership
public class GdpDocument
{
    public Guid DocumentId { get; set; }
    public GdpDocumentEntityType OwnerEntityType { get; set; }
    public Guid OwnerEntityId { get; set; }
    public DocumentType DocumentType { get; set; }  // Reuse existing enum
    public string FileName { get; set; } = string.Empty;
    public string BlobStorageUrl { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Description { get; set; }

    public ValidationResult Validate() { /* same rules as LicenceDocument */ }
}
```

### 2. Document Upload (follows LicenceService pattern)

```csharp
// Pattern from LicenceService.UploadDocumentAsync — adapt for GDP
private const string GdpDocumentContainerName = "gdp-documents";

var blobName = $"{entityType}/{entityId}/{document.DocumentId}/{document.FileName}";
var metadata = new Dictionary<string, string>
{
    { "documentType", document.DocumentType.ToString() },
    { "entityType", document.OwnerEntityType.ToString() },
    { "entityId", document.OwnerEntityId.ToString() },
    { "uploadedBy", document.UploadedBy }
};

var blobUri = await _documentStorage.UploadDocumentAsync(
    GdpDocumentContainerName, blobName, contentStream,
    document.ContentType ?? "application/octet-stream", metadata);
```

### 3. GdpCertificateMonitor (follows LicenceExpiryMonitor pattern)

```csharp
// Pattern from LicenceExpiryMonitor.cs — daily at 3 AM UTC
[Function("GdpCertificateMonitor")]
public async Task Run(
    [TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo,
    CancellationToken cancellationToken)
{
    var credentialAlerts = await _alertService.GenerateGdpCredentialExpiryAlertsAsync(90, cancellationToken);
    var requalificationAlerts = await _alertService.GenerateProviderRequalificationAlertsAsync(cancellationToken);
    var capaAlerts = await _alertService.GenerateCapaOverdueAlertsAsync(cancellationToken);
}
```

### 4. Web MVC Views (follows GdpProviders/GdpInspections patterns)

Credential views follow established patterns:
- Index: Table listing with status badges (like GdpInspections/Index)
- Details: Card layout with tabs for documents and verifications (like GdpProviders/Details)
- Forms: Card-based forms with validation (like GdpInspections/CreateFinding)

## TDD Workflow

For each component:
1. Write test (Red) → 2. Implement (Green) → 3. Refactor

```bash
# Run specific test file
dotnet test tests/RE2.ComplianceCore.Tests --filter "FullyQualifiedName~GdpDocumentTests"

# Run all tests
dotnet test RE2.sln

# Build only
dotnet build RE2.sln
```

## Architecture Notes

- **No new projects** — all changes extend existing solution structure
- **GdpDocument reuses DocumentType enum** from LicenceDocument (no duplication)
- **GdpCertificateMonitor reuses AlertGenerationService** — no new alert logic needed
- **Polymorphic pattern** — GdpDocument uses OwnerEntityType/OwnerEntityId like GdpCredential uses EntityType/EntityId
