# RE2 Deployment & Operations Guide

Step-by-step guide for deploying the RE2 Compliance Platform to Azure.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Azure DevOps Setup](#3-azure-devops-setup)
4. [First Deployment (Dev)](#4-first-deployment-dev)
5. [Post-Deployment Configuration](#5-post-deployment-configuration)
6. [Promoting to UAT](#6-promoting-to-uat)
7. [Promoting to Production](#7-promoting-to-production)
8. [Monitoring & Health Checks](#8-monitoring--health-checks)
9. [Rollback Procedures](#9-rollback-procedures)
10. [Troubleshooting](#10-troubleshooting)
11. [Infrastructure Reference](#11-infrastructure-reference)

---

## 1. Architecture Overview

```
                    ┌─────────────────────────────────────────────┐
                    │              Azure Resource Group            │
                    │              rg-re2-{env}                   │
                    │                                             │
  Users ──────────► │  ┌──────────────┐    ┌──────────────┐      │
                    │  │ App Service  │    │ App Service  │      │
                    │  │ (API)        │    │ (Web UI)     │      │
                    │  │ /health      │    │ /health      │      │
                    │  │ /ready       │    │ /ready       │      │
                    │  └──────┬───────┘    └──────┬───────┘      │
                    │         │                    │              │
                    │         ▼                    ▼              │
                    │  ┌──────────────┐    ┌──────────────┐      │
                    │  │ Key Vault    │    │ Redis Cache  │      │
                    │  │ (secrets)    │    │ (caching)    │      │
                    │  └──────────────┘    └──────────────┘      │
                    │                                             │
                    │  ┌──────────────┐    ┌──────────────┐      │
                    │  │ Functions    │    │ Logic App    │      │
                    │  │ (timers)     │    │ (approvals)  │      │
                    │  └──────────────┘    └──────────────┘      │
                    │                                             │
                    │  ┌──────────────┐    ┌──────────────┐      │
                    │  │ Storage      │    │ App Insights │      │
                    │  │ (blobs)      │    │ + Log Analyt │      │
                    │  └──────────────┘    └──────────────┘      │
                    └─────────────────────────────────────────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    ▼                   ▼                   ▼
             ┌──────────┐      ┌──────────────┐    ┌──────────┐
             │ Dataverse │      │ D365 F&O     │    │ Azure AD │
             │ (virtual  │      │ (OData)      │    │ / B2C    │
             │  tables)  │      │              │    │          │
             └──────────┘      └──────────────┘    └──────────┘
             Pre-provisioned (NOT managed by this pipeline)
```

### Deployable Artifacts

| Project | Azure Target | Purpose |
|---------|-------------|---------|
| `src/RE2.ComplianceApi/` | App Service | REST API for ERP/WMS integration |
| `src/RE2.ComplianceWeb/` | App Service | MVC Web UI for compliance staff |
| `src/RE2.ComplianceFunctions/` | Azure Functions | Timer-triggered background jobs |

### Pipeline Flow

```
[Build & Test] ──► [Deploy Dev] ──► [Deploy UAT] ──► [Deploy Prod]
   (auto)          (auto on main)   (manual gate)   (manual gate)
                                     1 approver      2 approvers
                   Direct deploy    Slot swap        Slot swap
```

---

## 2. Prerequisites

### Azure Subscriptions

You need three Azure subscriptions (or one subscription with three resource groups):

| Environment | Resource Group | Purpose |
|-------------|---------------|---------|
| Dev | `rg-re2-dev` | Development and integration testing |
| UAT | `rg-re2-uat` | User acceptance testing |
| Prod | `rg-re2-prod` | Production |

### Pre-Provisioned External Services

These must exist before deployment and are **NOT managed** by the pipeline:

1. **Dataverse environments** (one per stage) with virtual tables configured
2. **D365 Finance & Operations instances** (dev sandbox, UAT sandbox, production)
3. **Azure AD app registrations** for API authentication
4. **Azure AD B2C tenant** (if external user access is needed)

### Required Tools

- Azure CLI (`az`) 2.50+
- Bicep CLI (`az bicep`) 0.20+
- .NET 8 SDK (for local validation)

### Required Permissions

The person performing initial setup needs:
- **Owner** role on each Azure subscription/resource group
- **Azure DevOps Project Administrator** role
- Access to Power Platform Admin Center (for Dataverse identity registration)

---

## 3. Azure DevOps Setup

### 3.1 Create Service Connections

In Azure DevOps: **Project Settings** > **Service connections** > **New service connection** > **Azure Resource Manager**

Create three service connections:

| Name | Scope | Environment |
|------|-------|-------------|
| `RE2-Dev-ServiceConnection` | Resource group `rg-re2-dev` | Dev |
| `RE2-UAT-ServiceConnection` | Resource group `rg-re2-uat` | UAT |
| `RE2-Prod-ServiceConnection` | Resource group `rg-re2-prod` | Prod |

Use **Workload Identity federation (automatic)** for each. This creates an Azure AD app registration and federated credential automatically.

### 3.2 Create Environments

In Azure DevOps: **Pipelines** > **Environments** > **New environment**

| Environment | Approvals |
|-------------|-----------|
| `re2-dev` | None (auto-deploy) |
| `re2-uat` | 1 approver (e.g., QA lead) |
| `re2-prod` | 2 approvers (e.g., QA lead + compliance manager) |

To configure approvals: Environment > **...** > **Approvals and checks** > **Approvals**

### 3.3 Create Variable Groups

In Azure DevOps: **Pipelines** > **Library** > **+ Variable group**

Create three variable groups with these variables:

| Variable Group | Variables |
|---------------|-----------|
| `re2-dev-secrets` | `AzureAdTenantId`, `AzureAdClientId` |
| `re2-uat-secrets` | `AzureAdTenantId`, `AzureAdClientId` |
| `re2-prod-secrets` | `AzureAdTenantId`, `AzureAdClientId` |

Mark sensitive values as secrets (lock icon). These override the placeholder values in the `.bicepparam` files.

### 3.4 Create the Pipeline

In Azure DevOps: **Pipelines** > **New pipeline** > **Azure Repos Git** (or GitHub) > Select repo > **Existing Azure Pipelines YAML file**

- Branch: `main`
- Path: `pipelines/azure-pipelines.yml`

---

## 4. First Deployment (Dev)

### 4.1 Trigger the Pipeline

Push to `main` (or manually run the pipeline). The Build stage will:

1. Install .NET 8 SDK
2. Restore, build, and test the solution (1,432 tests)
3. Publish three zip artifacts: API, Web, Functions
4. Copy Bicep templates as an artifact

### 4.2 Deploy Dev Stage

The pipeline automatically proceeds to deploy Dev:

1. **Creates resource group** `rg-re2-dev` in West Europe
2. **Deploys Bicep templates** — provisions all Azure resources
3. **Deploys applications** via zip deploy:
   - API → `app-re2-api-dev`
   - Web → `app-re2-web-dev`
   - Functions → `func-re2-compliance-dev`
4. **Runs smoke tests** — verifies `/health` returns HTTP 200

### 4.3 Verify Deployment

After the pipeline succeeds, verify in the Azure Portal:

```bash
# Check API health
curl https://app-re2-api-dev.azurewebsites.net/health

# Check Web health
curl https://app-re2-web-dev.azurewebsites.net/health

# Check readiness (external dependencies)
curl https://app-re2-api-dev.azurewebsites.net/ready
```

---

## 5. Post-Deployment Configuration

After the first successful deployment, manual steps are required to connect external services.

### 5.1 Register Managed Identity in Dataverse

1. Open **Power Platform Admin Center** > Environments > Dev
2. Go to **Settings** > **Users** > **Application users**
3. Click **+ New app user**
4. Search for the App Service Managed Identity (find the Object ID in Azure Portal under the App Service > Identity > System assigned)
5. Assign the **System Administrator** security role (or a custom role with Dataverse virtual table access)
6. Repeat for the Functions App Managed Identity

### 5.2 Register Managed Identity in D365 F&O

1. Open **Lifecycle Services (LCS)** > Dev sandbox
2. Go to **System administration** > **Azure Active Directory applications**
3. Add a new record with the Managed Identity client ID
4. Assign appropriate D365 F&O security roles

### 5.3 Verify End-to-End Connectivity

```bash
# Readiness probe checks Dataverse, D365 F&O, and Blob Storage
curl https://app-re2-api-dev.azurewebsites.net/ready
```

Expected response when all services are connected:
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "dataverse", "status": "Healthy" },
    { "name": "d365fo", "status": "Healthy" },
    { "name": "blobstorage", "status": "Healthy" }
  ]
}
```

### 5.4 Verify Functions Timer Triggers

1. Open Azure Portal > `func-re2-compliance-dev` > **Functions**
2. Verify the three timer-triggered functions are listed
3. Open **Application Insights** > **Live Metrics** to watch for executions
4. Or check **Monitor** tab on each function for recent invocations

### 5.5 Verify Logic App

1. Open Azure Portal > `re2-approval-workflow-dev` > **Overview**
2. Check the Logic App status is **Enabled**
3. The Logic App calls back to `https://app-re2-api-dev.azurewebsites.net/api/v1/workflows/callback`

---

## 6. Promoting to UAT

### 6.1 Pipeline Approval

When the Dev stage succeeds, the UAT stage appears in the pipeline with a **pending approval**. The designated approver reviews and approves.

### 6.2 What Happens

1. Bicep deploys/updates UAT infrastructure (S1 plan, Standard Redis, GRS storage, staging slots)
2. API and Web are deployed to **staging slots** first
3. Smoke tests run against the staging slot URLs
4. On success, **slot swap** promotes staging to production (zero-downtime)
5. Functions are deployed directly (no slot swap on Consumption plan)

### 6.3 UAT Staging Slot URLs

During deployment (before swap):
- API staging: `https://app-re2-api-uat-staging.azurewebsites.net`
- Web staging: `https://app-re2-web-uat-staging.azurewebsites.net`

After swap, the production slots serve traffic:
- API: `https://app-re2-api-uat.azurewebsites.net`
- Web: `https://app-re2-web-uat.azurewebsites.net`

### 6.4 Post-UAT Steps

Repeat [Section 5](#5-post-deployment-configuration) for the UAT Dataverse and D365 F&O environments.

---

## 7. Promoting to Production

### 7.1 Pipeline Approval

The Prod stage requires **2 approvers**. Both must approve before deployment begins.

### 7.2 Production Deployment

Same pattern as UAT:
1. Bicep deploys infrastructure (P1v3 plan, Premium Redis, staging slots)
2. Deploy to staging slots → smoke test → slot swap
3. Post-swap health verification runs automatically

### 7.3 Production Checklist

Before approving the Prod deployment:

- [ ] UAT testing signed off by business stakeholders
- [ ] Dataverse production environment Managed Identity registered
- [ ] D365 F&O production Managed Identity registered
- [ ] Azure AD app registrations configured for production
- [ ] Approver email group (`compliance-approvers@company.com`) confirmed
- [ ] DNS/custom domain configured (if applicable)

---

## 8. Monitoring & Health Checks

### 8.1 Health Check Endpoints

All App Services expose two health endpoints:

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `/health` | **Liveness probe** | None (always 200 if app is running) |
| `/ready` | **Readiness probe** | Dataverse, D365 F&O, Blob Storage connectivity |

Azure App Service monitors `/health` automatically (configured in Bicep via `healthCheckPath`).

### 8.2 Application Insights

All three applications send telemetry to a shared Application Insights instance (`appi-re2-{env}`).

Key views:
- **Live Metrics**: Real-time request/failure rates
- **Failures**: Exception drill-down
- **Performance**: Request duration percentiles
- **Logs**: Kusto query interface

### 8.3 Useful Kusto Queries

```kusto
// API 5xx errors in the last hour
requests
| where timestamp > ago(1h)
| where resultCode startswith "5"
| summarize count() by bin(timestamp, 5m), name
| render timechart

// Slowest API endpoints
requests
| where timestamp > ago(24h)
| summarize avg(duration), percentile(duration, 95) by name
| order by percentile_duration_95 desc

// Functions execution failures
traces
| where timestamp > ago(24h)
| where severityLevel >= 3
| where cloud_RoleName == "func-re2-compliance-dev"
| project timestamp, message, severityLevel

// Health check results over time
requests
| where name == "GET /ready"
| where timestamp > ago(7d)
| summarize healthy=countif(resultCode == "200"),
            unhealthy=countif(resultCode != "200")
            by bin(timestamp, 1h)
| render timechart
```

### 8.4 Log Analytics

Logs are retained in the Log Analytics workspace (`log-re2-{env}`):

| Environment | Retention |
|-------------|-----------|
| Dev | 30 days |
| UAT | 60 days |
| Prod | 90 days |

---

## 9. Rollback Procedures

### 9.1 Application Rollback (UAT/Prod — Slot Swap)

For UAT and Prod, the previous version remains in the staging slot after a swap. To roll back:

```bash
# Swap production back to staging (instant rollback)
az webapp deployment slot swap \
  --resource-group rg-re2-prod \
  --name app-re2-api-prod \
  --slot staging

az webapp deployment slot swap \
  --resource-group rg-re2-prod \
  --name app-re2-web-prod \
  --slot staging
```

This is instant and zero-downtime because the old code is still warm in the staging slot.

### 9.2 Application Rollback (Dev — No Slots)

Dev has no staging slots. To roll back, redeploy the previous successful build:

1. Go to Azure DevOps > **Pipelines** > **Runs**
2. Find the last successful run
3. Click **Rerun** on the DeployDev stage

### 9.3 Infrastructure Rollback

Bicep deployments are idempotent. To roll back infrastructure changes:

1. Find the previous commit's Bicep templates in git
2. Run the deployment again with those templates

```bash
git checkout <previous-commit> -- infra/bicep/
az deployment group create \
  --resource-group rg-re2-prod \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/prod.bicepparam
```

---

## 10. Troubleshooting

### App Service won't start

1. Check **Diagnose and solve problems** in Azure Portal
2. Check Application Insights for startup exceptions
3. Verify Key Vault access: the Managed Identity must have **Key Vault Secrets User** role
4. Check app settings are correctly populated (Dataverse URL, D365 endpoint, etc.)

### /ready returns Unhealthy

The readiness probe checks Dataverse, D365 F&O, and Blob Storage. If any check fails:

1. **Dataverse**: Verify Managed Identity is registered as an application user in Power Platform
2. **D365 F&O**: Verify Managed Identity is registered in D365 System Administration
3. **Blob Storage**: Verify Managed Identity has **Storage Blob Data Contributor** role (should be assigned by Bicep)

### Functions not triggering

1. Check `host.json` is present in the deployed Functions app
2. Verify `AzureWebJobsStorage` connection string is valid
3. Check Application Insights for any startup errors
4. Verify the Functions plan is running (not scaled to zero permanently)

### Key Vault references not resolving

App Settings using `@Microsoft.KeyVault(...)` syntax require:
1. The App Service identity must have **Key Vault Secrets User** role on the vault
2. The Key Vault must have RBAC authorization enabled (not access policies)
3. Check the secret name matches exactly (case-sensitive)

Status indicator in Azure Portal: App Service > **Configuration** > look for green checkmarks next to Key Vault references.

### Slot swap fails

Common causes:
1. Staging slot app settings differ from production (check slot-sticky settings)
2. Staging slot health check fails — the swap waits for the staging slot to be healthy
3. Timeout — increase the swap timeout in deployment task

---

## 11. Infrastructure Reference

### Naming Convention

All resources follow: `{type}-re2-{component?}-{env}`

| Type Prefix | Resource |
|-------------|----------|
| `log-` | Log Analytics workspace |
| `appi-` | Application Insights |
| `st` | Storage account (no hyphens allowed) |
| `redis-` | Azure Cache for Redis |
| `kv-` | Key Vault |
| `plan-` | App Service Plan |
| `app-` | App Service |
| `func-` | Function App |

### Bicep Module Map

```
main.bicep
  ├── 1. monitoring.bicep        ← Log Analytics + App Insights
  ├── 2. storage-account.bicep   ← Storage (blobs + Functions runtime)
  ├── 3. redis-cache.bicep       ← Redis (distributed caching)
  ├── 4. key-vault.bicep         ← Secrets (depends on: redis)
  ├── 5. app-service-plan.bicep  ← Shared plan for API + Web
  ├── 6. app-service.bicep (API) ← API App Service (depends on: plan, monitoring, keyvault, storage)
  ├── 7. app-service.bicep (Web) ← Web App Service (depends on: plan, monitoring, keyvault, storage)
  ├── 8. function-app.bicep      ← Functions (depends on: storage, monitoring)
  ├── 9. logic-app.bicep         ← Logic App (depends on: API hostname)
  └── 10. role-assignments.bicep ← RBAC (depends on: all identity outputs)
```

### Security: Managed Identity RBAC

| Principal | Role | Scope |
|-----------|------|-------|
| API App Service | Storage Blob Data Contributor | Storage Account |
| API App Service | Key Vault Secrets User | Key Vault |
| Web App Service | Storage Blob Data Contributor | Storage Account |
| Web App Service | Key Vault Secrets User | Key Vault |
| Functions App | Storage Blob Data Contributor | Storage Account |
| Functions App | Key Vault Secrets User | Key Vault |
| Staging slots (UAT/Prod) | Same roles as production slots | Same scopes |

### App Settings Reference

These are set by Bicep and override `appsettings.json` at runtime:

| Setting | Source | Example |
|---------|--------|---------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Bicep output | `InstrumentationKey=...` |
| `Dataverse__Url` | Parameter file | `https://re2-dev.crm4.dynamics.com` |
| `D365FO__ODataEndpoint` | Parameter file | `https://re2-dev.sandbox.operations.dynamics.com/data` |
| `D365FO__Resource` | Parameter file | `https://re2-dev.sandbox.operations.dynamics.com` |
| `BlobStorage__AccountUrl` | Bicep output | `https://stre2dev.blob.core.windows.net/` |
| `Caching__Enabled` | Bicep | `true` |
| `Caching__RedisConnectionString` | Key Vault reference | `@Microsoft.KeyVault(VaultName=kv-re2-dev;SecretName=RedisConnectionString)` |
| `AzureAd__TenantId` | Pipeline variable | `<guid>` |
| `AzureAd__ClientId` | Pipeline variable | `<guid>` |
