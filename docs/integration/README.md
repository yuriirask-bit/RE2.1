# RE2 Integration Guide

This guide describes how external systems (ERP, WMS, e-commerce platforms) integrate with the RE2 Compliance API.

---

## Architecture Overview

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  D365 F&O    │────>│  RE2 Compliance  │<────│  Dataverse      │
│  (Master     │     │  API             │     │  (Compliance    │
│   Data)      │     │                  │     │   Extensions)   │
└──────────────┘     └────────┬─────────┘     └─────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
     ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐
     │  ERP/WMS    │  │  Web UI     │  │  Azure      │
     │  Systems    │  │  (MVC)      │  │  Functions  │
     └─────────────┘  └─────────────┘  └─────────────┘
```

RE2 follows a **stateless** architecture. Business data lives in D365 Finance & Operations (master data) and Dataverse (compliance extensions). The API layer reads from and writes to these external stores.

---

## Getting Started

### 1. Register Your Integration System

Before making API calls, register your system with a SystemAdmin:

```http
POST /api/v1/integrationsystems
Content-Type: application/json
Authorization: Bearer <admin_token>

{
  "systemName": "WMS-SAP",
  "systemType": "WMS",
  "contactEmail": "wms-team@example.com",
  "description": "SAP Extended Warehouse Management"
}
```

The response includes your `integrationSystemId`, which is required for webhook subscriptions.

### 2. Obtain OAuth2 Credentials

Use the Azure AD **Client Credentials** flow for machine-to-machine integration:

1. Register an application in Azure AD
2. Request the appropriate API scopes
3. Exchange credentials for an access token

```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={your_client_id}
&client_secret={your_client_secret}
&scope=api://{re2_api_app_id}/.default
```

Include the token in all API requests:

```
Authorization: Bearer <access_token>
```

### 3. Test Connectivity

Verify your setup with a simple read request:

```http
GET /api/v1/licencetypes
Authorization: Bearer <access_token>
```

---

## Core Integration: Transaction Validation

The primary integration point is real-time transaction validation. When your ERP or WMS creates a sales order containing controlled substances, call the validation endpoint before releasing the order.

### Request

```http
POST /api/v1/transactions/validate
Content-Type: application/json
Authorization: Bearer <token>

{
  "externalTransactionId": "SO-2026-001234",
  "customerAccount": "CUST001",
  "dataAreaId": "nl01",
  "transactionType": "SalesOrder",
  "lines": [
    {
      "lineNumber": 1,
      "itemNumber": "PROD-001",
      "dataAreaId": "nl01",
      "quantity": 100.0,
      "unitOfMeasure": "KG"
    },
    {
      "lineNumber": 2,
      "itemNumber": "PROD-002",
      "dataAreaId": "nl01",
      "quantity": 50.0,
      "unitOfMeasure": "KG"
    }
  ]
}
```

Key points:
- `externalTransactionId` is your ERP order number -- used for idempotency and lookup
- `itemNumber` + `dataAreaId` identify the D365 F&O released product; the API resolves the associated controlled substance server-side
- `customerAccount` + `dataAreaId` form the composite customer key

### Response: Approved

```json
{
  "transactionId": "a1b2c3d4-...",
  "externalTransactionId": "SO-2026-001234",
  "status": "Approved",
  "violations": [],
  "licenceUsage": [
    {
      "licenceNumber": "LIC-2025-001",
      "substanceCode": "MORPH",
      "quantityUsed": 100.0
    }
  ]
}
```

### Response: Blocked

```json
{
  "transactionId": "a1b2c3d4-...",
  "externalTransactionId": "SO-2026-001234",
  "status": "Blocked",
  "violations": [
    {
      "errorCode": "LICENCE_EXPIRED",
      "message": "Customer licence LIC-2025-001 expired on 2026-01-15",
      "substanceCode": "MORPH",
      "lineNumber": 1
    },
    {
      "errorCode": "THRESHOLD_EXCEEDED",
      "message": "Quantity 100.0 KG exceeds threshold of 50.0 KG per transaction",
      "substanceCode": "MORPH",
      "lineNumber": 1
    }
  ]
}
```

### Response: PendingOverride

When violations are present but an override is requested, the status becomes `PendingOverride`. A ComplianceManager must approve or reject via the web UI or API.

### SLA

Transaction validation must complete in **< 3 seconds** (SC-005).

---

## Customer Compliance Lookup

Before processing orders, you may want to check if a customer is compliant:

```http
GET /api/v1/customers/CUST001/compliance-status?dataAreaId=nl01
Authorization: Bearer <token>
```

Response:

```json
{
  "customerAccount": "CUST001",
  "dataAreaId": "nl01",
  "isCompliant": true,
  "approvalStatus": "Approved",
  "isSuspended": false,
  "activeLicenceCount": 3,
  "nextReverificationDate": "2026-06-15"
}
```

**SLA**: < 1 second (SC-033).

---

## Webhook Notifications

Instead of polling, subscribe to real-time event notifications.

### Subscribe

```http
POST /api/v1/webhooksubscriptions
Content-Type: application/json
Authorization: Bearer <admin_token>

{
  "integrationSystemId": "<your_system_id>",
  "eventTypes": ["ComplianceStatusChanged", "OrderApproved", "OrderRejected", "LicenceExpiring"],
  "callbackUrl": "https://your-system.example.com/webhooks/re2",
  "secretKey": "<min_32_char_secret_for_hmac_signing>",
  "description": "WMS compliance event listener"
}
```

### Available Event Types

| Event | Trigger |
|-------|---------|
| `ComplianceStatusChanged` | Customer compliance status changed (e.g., licence expired) |
| `OrderApproved` | Transaction passed validation or override approved |
| `OrderRejected` | Transaction failed validation |
| `LicenceExpiring` | Licence approaching expiry (90/60/30 day warnings) |
| `OverrideApproved` | Compliance override approved by manager |

### Webhook Payload

Webhook deliveries include an HMAC-SHA256 signature header for verification:

```
X-RE2-Signature: sha256=<hmac_hex_digest>
Content-Type: application/json
```

Payload example:

```json
{
  "eventType": "OrderRejected",
  "timestamp": "2026-02-17T10:30:00Z",
  "data": {
    "transactionId": "a1b2c3d4-...",
    "externalTransactionId": "SO-2026-001234",
    "status": "Blocked",
    "violations": [...]
  }
}
```

### Verifying Signatures

Compute HMAC-SHA256 over the raw request body using your secret key and compare with the `X-RE2-Signature` header:

```csharp
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
bool isValid = expected == signatureHeader;
```

### Circuit Breaker

Subscriptions are auto-disabled after **10 consecutive failed deliveries**. Use the reactivate endpoint to re-enable:

```http
POST /api/v1/webhooksubscriptions/{id}/reactivate
```

---

## Warehouse Operation Validation

For warehouse management systems, validate that a warehouse operation is permitted:

```http
POST /api/v1/warehouse/operations/validate
Content-Type: application/json
Authorization: Bearer <token>

{
  "warehouseId": "WH-AMS-01",
  "dataAreaId": "nl01",
  "operationType": "Storage",
  "substanceCode": "MORPH"
}
```

This checks:
- GDP site configuration and permitted activities
- WDA coverage for the site
- Equipment qualification status
- Active CAPA status

---

## GDP Provider Qualification Check

Before assigning a logistics provider to handle controlled substances:

```http
GET /api/v1/gdp-providers/check-qualification?providerId={id}&requiresTempControl=true
Authorization: Bearer <token>
```

Verifies:
- Provider has active GDP credentials
- Credentials are not expired
- Provider qualification review is current (FR-039)
- Temperature-controlled capability if required

---

## Licence Lookup

Look up licence details by number (e.g., to verify a customer's licence during onboarding):

```http
GET /api/v1/licences/by-number/LIC-2025-001
Authorization: Bearer <token>
```

Check substance authorization:

```http
GET /api/v1/licencesubstancemappings/check-authorization?licenceId={id}&substanceCode=MORPH
Authorization: Bearer <token>
```

---

## Expiry Monitoring

Proactively monitor upcoming expirations:

```http
# Licences expiring within 90 days
GET /api/v1/licences/expiring?daysAhead=90

# GDP credentials expiring within 60 days
GET /api/v1/gdp-providers/credentials/expiring?daysAhead=60

# Equipment due for requalification within 30 days
GET /api/v1/gdp-operations/equipment/due-for-requalification?daysAhead=30

# Customers due for re-verification
GET /api/v1/customers/reverification-due
```

---

## Error Handling

All errors follow a standardized format. See [API Reference](../api/README.md) for the full error code table.

Key integration error codes to handle:

| Code | Action |
|------|--------|
| `VALIDATION_ERROR` (400) | Fix request payload |
| `UNAUTHORIZED` (401) | Refresh OAuth2 token |
| `FORBIDDEN` (403) | Check role assignments |
| `NOT_FOUND` (404) | Resource does not exist |
| `CONCURRENCY_CONFLICT` (409) | Re-read and retry with current RowVersion |
| `RATE_LIMIT_EXCEEDED` (429) | Back off and retry after `Retry-After` header |
| `EXTERNAL_SYSTEM_UNAVAILABLE` (503) | Retry with exponential backoff |

### Retry Strategy

For transient errors (429, 503, 504), implement exponential backoff:

```
Attempt 1: wait 1s
Attempt 2: wait 2s
Attempt 3: wait 4s
Attempt 4: wait 8s
Max retries: 5
```

---

## Concurrency

The API uses **optimistic concurrency** via `RowVersion` fields. When updating a resource:

1. Read the current resource (includes `rowVersion`)
2. Send the update with the same `rowVersion`
3. If another user modified the resource, you receive `CONCURRENCY_CONFLICT` (409)
4. Re-read and retry

---

## Data Model: Composite Keys

Several entities use **composite keys** spanning D365 F&O and Dataverse:

| Entity | Key Fields | Example |
|--------|-----------|---------|
| Customer | `customerAccount` + `dataAreaId` | `CUST001` / `nl01` |
| Product | `itemNumber` + `dataAreaId` | `PROD-001` / `nl01` |
| GDP Site | `warehouseId` + `dataAreaId` | `WH-AMS-01` / `nl01` |
| Substance | `substanceCode` | `MORPH` |

Always provide both key fields when referencing composite entities.

---

## Rate Limits

API rate limits are enforced per client. When exceeded, the API returns `429 Too Many Requests` with a `Retry-After` header indicating when to retry.

---

## Support

- **API Reference**: [docs/api/README.md](../api/README.md)
- **User Guide**: [docs/user-guide/README.md](../user-guide/README.md)
- **Specification**: [specs/001-licence-management/spec.md](../../specs/001-licence-management/spec.md)
- **Technical Plan**: [specs/001-licence-management/plan.md](../../specs/001-licence-management/plan.md)
