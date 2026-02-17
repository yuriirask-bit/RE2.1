# RE2 Compliance API Reference

**Base URL**: `https://{host}/api/v1`
**Authentication**: OAuth2 Bearer Token (Azure AD)
**Content-Type**: `application/json`

---

## Authentication

All API requests require a valid OAuth2 bearer token issued by Azure AD.

```
Authorization: Bearer <access_token>
```

### Obtaining a Token

| Flow | Use Case |
|------|----------|
| Client Credentials | Machine-to-machine (ERP/WMS integration) |
| Authorization Code + PKCE | Interactive users (Web UI) |
| Azure AD B2C | External partners with SSO |

---

## Authorization Roles

Endpoints enforce role-based access. The required role is noted per endpoint below.

| Role | Scope |
|------|-------|
| **ComplianceManager** | Full access: licences, approvals, overrides, substances, thresholds, GDP change approval |
| **QAUser** | GDP site management, inspections, SOPs, equipment, training, providers |
| **SalesAdmin** | Customer onboarding and qualification |
| **SystemAdmin** | Integration system and webhook subscription management |
| **TrainingCoordinator** | Training records management |

Unauthenticated or under-privileged requests receive `401` or `403` with an `ErrorResponse` body.

---

## Error Responses

All errors follow a standardized format (FR-064):

```json
{
  "errorCode": "LICENCE_EXPIRED",
  "message": "Customer licence has expired and is no longer valid for transactions.",
  "traceId": "0HN4ABCDEF:00000001",
  "timestamp": "2026-02-17T10:30:00Z",
  "details": null
}
```

The `details` field is only populated in the Development environment (stack trace).

### Error Codes

#### Compliance Violations

| Code | HTTP Status | Description |
|------|------------|-------------|
| `LICENCE_EXPIRED` | 422 | Licence has expired |
| `LICENCE_MISSING` | 422 | Required licence not found |
| `LICENCE_SUSPENDED` | 422 | Licence suspended by authority |
| `LICENCE_REVOKED` | 422 | Licence revoked by authority |
| `LICENCE_SCOPE_INSUFFICIENT` | 422 | Licence scope insufficient for activity |
| `SUBSTANCE_NOT_AUTHORIZED` | 422 | Substance not authorized under any active licence |
| `THRESHOLD_EXCEEDED` | 422 | Quantity/frequency threshold exceeded |
| `MISSING_PERMIT` | 422 | Missing import/export permit |
| `CUSTOMER_SUSPENDED` | 422 | Customer account suspended |
| `CUSTOMER_NOT_APPROVED` | 422 | Customer not approved for controlled substances |

#### GDP Violations

| Code | HTTP Status | Description |
|------|------------|-------------|
| `WDA_EXPIRED` | 422 | WDA licence expired |
| `GDP_CERTIFICATE_EXPIRED` | 422 | GDP certificate expired |
| `SITE_NOT_COVERED` | 422 | Site not covered by active WDA/GDP certificate |
| `PROVIDER_NOT_QUALIFIED` | 422 | Service provider not GDP-qualified |
| `CAPA_OVERDUE` | 422 | Corrective action overdue |
| `GDP_QUALIFICATION_INVALID` | 422 | Customer GDP qualification invalid |

#### Transaction Codes

| Code | HTTP Status | Description |
|------|------------|-------------|
| `TRANSACTION_PENDING` | 202 | Transaction pending review |
| `TRANSACTION_BLOCKED` | 422 | Transaction blocked due to violations |
| `OVERRIDE_PENDING` | 202 | Override request pending approval |
| `OVERRIDE_APPROVED` | 200 | Override approved |
| `OVERRIDE_REJECTED` | 422 | Override rejected |

#### System Errors

| Code | HTTP Status | Description |
|------|------------|-------------|
| `VALIDATION_ERROR` | 400 | Invalid request input |
| `NOT_FOUND` | 404 | Resource not found |
| `UNAUTHORIZED` | 401 | Authentication required |
| `FORBIDDEN` | 403 | Insufficient privileges |
| `RATE_LIMIT_EXCEEDED` | 429 | Rate limit exceeded |
| `CONCURRENCY_CONFLICT` | 409 | Optimistic concurrency conflict |
| `EXTERNAL_SYSTEM_UNAVAILABLE` | 503 | External system (Dataverse/D365 F&O) unavailable |
| `INTERNAL_ERROR` | 500 | Internal server error |

---

## API Versioning

All endpoints are versioned under `/api/v1/`. Per FR-062, backward compatibility is maintained for 6 months after a new version is introduced.

---

## Endpoints

### Licence Management

**Base**: `/api/v1/licences`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/licences` | - | List licences. Query: `holderId`, `holderType`, `status` |
| GET | `/licences/{id}` | - | Get licence by ID |
| GET | `/licences/by-number/{licenceNumber}` | - | Get licence by number |
| GET | `/licences/expiring` | - | Expiring licences. Query: `daysAhead` |
| POST | `/licences` | ComplianceManager | Create licence |
| PUT | `/licences/{id}` | ComplianceManager | Update licence |
| DELETE | `/licences/{id}` | ComplianceManager | Delete licence |
| GET | `/licences/{id}/documents` | - | List documents |
| POST | `/licences/{id}/documents` | ComplianceManager | Upload document |
| GET | `/licences/{id}/documents/{documentId}/download` | - | Download document |
| DELETE | `/licences/{id}/documents/{documentId}` | ComplianceManager | Delete document |
| GET | `/licences/{id}/verifications` | - | List verifications |
| POST | `/licences/{id}/verifications` | ComplianceManager | Record verification |
| GET | `/licences/{id}/scope-changes` | - | List scope changes |
| POST | `/licences/{id}/scope-changes` | ComplianceManager | Record scope change |

### Licence Types

**Base**: `/api/v1/licencetypes`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/licencetypes` | - | List licence types |
| GET | `/licencetypes/{id}` | - | Get by ID |
| POST | `/licencetypes` | ComplianceManager | Create |
| PUT | `/licencetypes/{id}` | ComplianceManager | Update |
| DELETE | `/licencetypes/{id}` | ComplianceManager | Delete |

### Licence-Substance Mappings

**Base**: `/api/v1/licencesubstancemappings`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/licencesubstancemappings` | - | List. Query: `licenceId`, `substanceCode`, `activeOnly` |
| GET | `/licencesubstancemappings/{id}` | - | Get by ID |
| GET | `/licencesubstancemappings/check-authorization` | - | Check substance authorization |
| POST | `/licencesubstancemappings` | ComplianceManager | Create |
| PUT | `/licencesubstancemappings/{id}` | ComplianceManager | Update |
| DELETE | `/licencesubstancemappings/{id}` | ComplianceManager | Delete |

### Customer Compliance

**Base**: `/api/v1/customers`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/customers` | - | List compliance-configured customers. Query: `status`, `category`, `country` |
| GET | `/customers/d365` | - | Browse all D365 F&O customers |
| GET | `/customers/search` | - | Search by name. Query: `q` |
| GET | `/customers/reverification-due` | - | Customers due for re-verification |
| GET | `/customers/{customerAccount}` | - | Get by composite key. Query: `dataAreaId` |
| GET | `/customers/{customerAccount}/compliance-status` | - | Compliance status (<1s). Query: `dataAreaId` |
| POST | `/customers` | SalesAdmin, ComplianceManager | Configure compliance extension |
| PUT | `/customers/{customerAccount}` | SalesAdmin, ComplianceManager | Update. Query: `dataAreaId` |
| DELETE | `/customers/{customerAccount}` | SalesAdmin, ComplianceManager | Remove. Query: `dataAreaId` |
| POST | `/customers/{customerAccount}/suspend` | ComplianceManager | Suspend. Query: `dataAreaId` |
| POST | `/customers/{customerAccount}/reinstate` | ComplianceManager | Reinstate. Query: `dataAreaId` |

### Transaction Validation

**Base**: `/api/v1/transactions`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| POST | `/transactions/validate` | - | Validate transaction (<3s SLA) |
| GET | `/transactions` | - | List. Query: `status`, `customerAccount`, `from`, `to` |
| GET | `/transactions/{id}` | - | Get by ID |
| GET | `/transactions/by-external/{externalId}` | - | Get by external ERP order number |
| GET | `/transactions/pending` | ComplianceManager | Pending overrides |
| GET | `/transactions/pending/count` | - | Pending override count |
| POST | `/transactions/{id}/approve` | ComplianceManager | Approve override |
| POST | `/transactions/{id}/reject` | ComplianceManager | Reject override |
| POST | `/warehouse/operations/validate` | - | Validate warehouse operation |

### Controlled Substances

**Base**: `/api/v1/controlledsubstances`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/controlledsubstances` | - | List. Query: `activeOnly`, `opiumActList`, `precursorCategory`, `search` |
| GET | `/controlledsubstances/{substanceCode}` | - | Get by substance code |
| POST | `/controlledsubstances/configure-compliance` | ComplianceManager | Configure compliance |
| PUT | `/controlledsubstances/{substanceCode}/compliance` | ComplianceManager | Update compliance |
| POST | `/controlledsubstances/{substanceCode}/deactivate` | ComplianceManager | Deactivate |
| POST | `/controlledsubstances/{substanceCode}/reactivate` | ComplianceManager | Reactivate |

### Products

**Base**: `/api/v1/products`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/products` | - | List. Query: `controlled` |
| GET | `/products/{itemNumber}` | - | Get by item number. Query: `dataAreaId` |
| GET | `/products/by-substance/{substanceCode}` | - | Products by substance |

### Thresholds

**Base**: `/api/v1/thresholds`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/thresholds` | - | List. Query: `activeOnly`, `type`, `substanceCode`, `search` |
| GET | `/thresholds/{id}` | - | Get by ID |
| GET | `/thresholds/by-substance/{substanceCode}` | - | By substance |
| GET | `/thresholds/by-category/{category}` | - | By customer category |
| POST | `/thresholds` | ComplianceManager | Create |
| PUT | `/thresholds/{id}` | ComplianceManager | Update |
| DELETE | `/thresholds/{id}` | ComplianceManager | Delete |

### Substance Reclassifications

| Method | Path | Role | Description |
|--------|------|------|-------------|
| POST | `/substances/{substanceCode}/reclassify` | ComplianceManager | Create reclassification |
| GET | `/substances/{substanceCode}/reclassifications` | - | History |
| GET | `/substances/{substanceCode}/classification` | - | Effective classification at date |
| GET | `/reclassifications/{id}` | - | Get by ID |
| GET | `/reclassifications/pending` | - | Pending reclassifications |
| POST | `/reclassifications/{id}/process` | ComplianceManager | Process |
| GET | `/reclassifications/{id}/impact-analysis` | - | Impact analysis |
| GET | `/reclassifications/{id}/notification` | - | Compliance notification |
| POST | `/reclassifications/{id}/customers/{customerId}/requalify` | ComplianceManager | Mark re-qualified |
| GET | `/customers/{customerId}/reclassification-status` | - | Check reclassification blocking |

### Reports

**Base**: `/api/v1/reports`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/reports/transaction-audit` | - | Transaction audit (FR-026). Query: `from`, `to` |
| POST | `/reports/transaction-audit` | - | Transaction audit (complex criteria) |
| GET | `/reports/licence-usage` | - | Licence usage (FR-026). Query: `from`, `to` |
| POST | `/reports/licence-usage` | - | Licence usage (complex criteria) |
| GET | `/reports/customer-compliance/{customerAccount}/{dataAreaId}` | - | Customer compliance history (FR-029) |
| POST | `/reports/customer-compliance` | - | Customer compliance (complex criteria) |
| GET | `/reports/licence-correction-impact` | - | Correction impact analysis (SC-038) |
| POST | `/reports/licence-correction-impact` | - | Correction impact (complex criteria) |

### GDP Sites

**Base**: `/api/v1/gdpsites`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/gdpsites/warehouses` | - | Browse D365 F&O warehouses |
| GET | `/gdpsites/warehouses/{warehouseId}` | - | Get warehouse. Query: `dataAreaId` |
| GET | `/gdpsites` | - | List GDP-configured sites |
| GET | `/gdpsites/{warehouseId}` | - | Get GDP site. Query: `dataAreaId` |
| POST | `/gdpsites` | QAUser, ComplianceManager | Configure GDP |
| PUT | `/gdpsites/{warehouseId}` | QAUser, ComplianceManager | Update GDP config |
| DELETE | `/gdpsites/{warehouseId}` | ComplianceManager | Remove GDP config. Query: `dataAreaId` |
| GET | `/gdpsites/{warehouseId}/wda-coverage` | - | WDA coverage. Query: `dataAreaId` |
| POST | `/gdpsites/{warehouseId}/wda-coverage` | ComplianceManager | Add WDA coverage |
| DELETE | `/gdpsites/{warehouseId}/wda-coverage/{coverageId}` | ComplianceManager | Remove WDA coverage |

### GDP Providers

**Base**: `/api/v1/gdp-providers`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/gdp-providers` | - | List providers |
| GET | `/gdp-providers/{providerId}` | - | Get by ID |
| POST | `/gdp-providers` | QAUser, ComplianceManager | Create |
| PUT | `/gdp-providers/{providerId}` | QAUser, ComplianceManager | Update |
| DELETE | `/gdp-providers/{providerId}` | ComplianceManager | Delete |
| GET | `/gdp-providers/requiring-review` | - | Providers due for re-qualification (FR-039) |
| GET | `/gdp-providers/{providerId}/credentials` | - | Provider credentials |
| GET | `/gdp-providers/credentials/{credentialId}` | - | Get credential |
| POST | `/gdp-providers/credentials` | QAUser, ComplianceManager | Create credential |
| GET | `/gdp-providers/credentials/expiring` | - | Expiring credentials. Query: `daysAhead` |
| GET | `/gdp-providers/{providerId}/reviews` | - | Qualification reviews |
| POST | `/gdp-providers/{providerId}/reviews` | QAUser, ComplianceManager | Record review |
| GET | `/gdp-providers/credentials/{credentialId}/verifications` | - | Verifications |
| POST | `/gdp-providers/credentials/{credentialId}/verifications` | QAUser, ComplianceManager | Record verification |
| GET | `/gdp-providers/credentials/{credentialId}/documents` | - | Documents (FR-044) |
| POST | `/gdp-providers/credentials/{credentialId}/documents` | QAUser, ComplianceManager | Upload document |
| GET | `/gdp-providers/documents/{documentId}/download` | - | Download document |
| DELETE | `/gdp-providers/documents/{documentId}` | QAUser, ComplianceManager | Delete document |
| GET | `/gdp-providers/check-qualification` | - | Check partner GDP qualification (FR-038) |

### GDP Inspections & CAPA

**Base**: `/api/v1/gdp-inspections`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/gdp-inspections` | - | List inspections |
| GET | `/gdp-inspections/{inspectionId}` | - | Get by ID |
| GET | `/gdp-inspections/by-site/{siteId}` | - | By site |
| POST | `/gdp-inspections` | QAUser, ComplianceManager | Create |
| PUT | `/gdp-inspections/{inspectionId}` | QAUser, ComplianceManager | Update |
| GET | `/gdp-inspections/{inspectionId}/findings` | - | Findings |
| GET | `/gdp-inspections/findings/{findingId}` | - | Get finding |
| POST | `/gdp-inspections/findings` | QAUser, ComplianceManager | Create finding |
| DELETE | `/gdp-inspections/findings/{findingId}` | QAUser, ComplianceManager | Delete finding |
| GET | `/gdp-inspections/capas` | - | List CAPAs |
| GET | `/gdp-inspections/capas/{capaId}` | - | Get CAPA |
| GET | `/gdp-inspections/capas/by-finding/{findingId}` | - | CAPAs by finding |
| GET | `/gdp-inspections/capas/overdue` | - | Overdue CAPAs (FR-042) |
| POST | `/gdp-inspections/capas` | QAUser, ComplianceManager | Create CAPA |
| PUT | `/gdp-inspections/capas/{capaId}` | QAUser, ComplianceManager | Update CAPA |
| POST | `/gdp-inspections/capas/{capaId}/complete` | QAUser, ComplianceManager | Complete CAPA |

### GDP Operations

**Base**: `/api/v1/gdp-operations`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| POST | `/gdp-operations/validate/site-assignment` | - | Validate site eligibility (FR-046) |
| POST | `/gdp-operations/validate/provider-assignment` | - | Validate provider eligibility (FR-047) |
| GET | `/gdp-operations/approved-providers` | - | Approved providers. Query: `tempControlled` |
| GET | `/gdp-operations/equipment` | - | List equipment qualifications |
| GET | `/gdp-operations/equipment/{equipmentId}` | - | Get equipment |
| GET | `/gdp-operations/equipment/due-for-requalification` | - | Due for requalification. Query: `daysAhead` |
| POST | `/gdp-operations/equipment` | QAUser, ComplianceManager | Create |
| PUT | `/gdp-operations/equipment/{equipmentId}` | QAUser, ComplianceManager | Update |
| DELETE | `/gdp-operations/equipment/{equipmentId}` | QAUser, ComplianceManager | Delete |

### GDP SOPs

**Base**: `/api/v1/gdp-sops`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/gdp-sops` | - | List SOPs |
| GET | `/gdp-sops/{sopId}` | - | Get SOP |
| POST | `/gdp-sops` | QAUser, ComplianceManager | Create |
| PUT | `/gdp-sops/{sopId}` | QAUser, ComplianceManager | Update |
| DELETE | `/gdp-sops/{sopId}` | QAUser, ComplianceManager | Delete |
| GET | `/gdp-sops/{sopId}/sites` | - | Linked sites |
| POST | `/gdp-sops/{sopId}/sites/{siteId}` | QAUser, ComplianceManager | Link to site |
| DELETE | `/gdp-sops/{sopId}/sites/{siteId}` | QAUser, ComplianceManager | Unlink from site |

### GDP Change Control

**Base**: `/api/v1/gdp-changes`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/gdp-changes` | - | List change records |
| GET | `/gdp-changes/{changeId}` | - | Get change record |
| GET | `/gdp-changes/pending` | - | Pending changes |
| POST | `/gdp-changes` | QAUser, ComplianceManager | Create |
| POST | `/gdp-changes/{changeId}/approve` | ComplianceManager | Approve |
| POST | `/gdp-changes/{changeId}/reject` | ComplianceManager | Reject |

### Approval Workflows

**Base**: `/api/v1/workflows`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| POST | `/workflows/trigger` | ComplianceManager | Trigger approval workflow |
| POST | `/workflows/callback` | - | Workflow callback (Logic App) |
| GET | `/workflows/{workflowId}/status` | - | Workflow status |

### Webhook Subscriptions

**Base**: `/api/v1/webhooksubscriptions`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/webhooksubscriptions` | SystemAdmin | List |
| GET | `/webhooksubscriptions/{id}` | SystemAdmin | Get by ID |
| GET | `/webhooksubscriptions/event-types` | - | Available event types |
| POST | `/webhooksubscriptions` | SystemAdmin | Create |
| PUT | `/webhooksubscriptions/{id}` | SystemAdmin | Update |
| DELETE | `/webhooksubscriptions/{id}` | SystemAdmin | Delete |
| POST | `/webhooksubscriptions/{id}/reactivate` | SystemAdmin | Reactivate |
| POST | `/webhooksubscriptions/{id}/deactivate` | SystemAdmin | Deactivate |

### Integration Systems

**Base**: `/api/v1/integrationsystems`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| GET | `/integrationsystems` | SystemAdmin | List |
| GET | `/integrationsystems/{id}` | SystemAdmin | Get by ID |
| POST | `/integrationsystems` | SystemAdmin | Register |
| PUT | `/integrationsystems/{id}` | SystemAdmin | Update |
| DELETE | `/integrationsystems/{id}` | SystemAdmin | Delete |

---

## Swagger / OpenAPI

When running locally, interactive API documentation is available at:

```
https://localhost:7001/swagger
```

XML documentation comments are included in the Swagger output for all public API models and endpoints.

---

## Performance SLAs

| Operation | Target |
|-----------|--------|
| Transaction validation | < 3 seconds |
| Customer compliance lookup | < 1 second |
| Audit report generation | < 2 minutes |
| Concurrent validation requests | 50 requests |
