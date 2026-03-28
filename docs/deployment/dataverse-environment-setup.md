# Dataverse Environment Setup — Full Recreation Guide

How to create a Power Platform / Dataverse environment from scratch for the RE2 Compliance Platform. This covers the manual steps that are **not automated** by Bicep or the CI/CD pipeline.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Create the Dataverse Environment](#2-create-the-dataverse-environment)
3. [Link Pay-As-You-Go Billing to Azure](#3-link-pay-as-you-go-billing-to-azure)
4. [Provision the RE2 Schema](#4-provision-the-re2-schema)
5. [Register Managed Identities](#5-register-managed-identities)
6. [Update App Settings](#6-update-app-settings)
7. [Verify Connectivity](#7-verify-connectivity)
8. [Dev Environment Reference Values](#8-dev-environment-reference-values)

---

## 1. Overview

The Dataverse environment is a **pre-provisioned dependency** — it must exist before the RE2 application can function. It is managed through the Power Platform Admin Center, not through Azure Resource Manager / Bicep.

### What is automated

| Step | Tool | Location |
|------|------|----------|
| Schema creation (27 custom `phr_*` entities) | PowerShell script | `scripts/provision-dataverse-schema.ps1` |
| Schema provisioning pipeline | Azure DevOps | `pipelines/provision-dataverse.yml` |
| Entity schema definitions | JSON | `scripts/dataverse-schema.json` |

### What is manual (this guide)

| Step | Tool |
|------|------|
| Create the Dataverse environment | Power Platform Admin Center |
| Link pay-as-you-go billing to Azure subscription | Power Platform Admin Center |
| Register application users (managed identities) | Power Platform Admin Center |
| Assign security roles to application users | Power Platform Admin Center |

---

## 2. Create the Dataverse Environment

### Prerequisites

- **Global Admin** or **Power Platform Admin** role in the Microsoft 365 / Azure AD tenant
- Access to [Power Platform Admin Center](https://admin.powerplatform.microsoft.com)

### Steps

1. Open **Power Platform Admin Center** > **Environments** > **+ New**

2. Configure the environment:

   | Field | Dev Value | Notes |
   |-------|-----------|-------|
   | Name | `RE2-Dev` | Use `RE2-UAT` / `RE2-Prod` for other environments |
   | Region | `United Kingdom` | Must match your Azure region for latency (our Azure resources are in West Europe, but Dataverse UK South is the closest Power Platform region) |
   | Type | `Developer` (dev) / `Sandbox` (UAT) / `Production` (prod) | Developer type is free but limited to a single user; use Sandbox for shared dev if multiple developers need access |
   | Purpose | `RE2 Compliance Platform — Development` | Description for administrators |
   | Create a database for this environment | **Yes** | Required — Dataverse is the database |

3. Database settings:

   | Field | Value | Notes |
   |-------|-------|-------|
   | Language | `English` | |
   | Currency | `GBP (£)` | Match your business currency |
   | Enable Dynamics 365 apps | **No** | RE2 uses custom virtual tables, not D365 CE apps |
   | Deploy sample apps and data | **No** | |
   | Security group | Leave blank for dev | For UAT/Prod, restrict to a specific Azure AD security group |

4. Click **Save**. Provisioning takes 5-15 minutes.

5. Once created, note the **Environment URL** — this is your `DataverseUrl` (e.g., `https://<org-id>.crm11.dynamics.com`). The URL is auto-generated and will differ each time you create a new environment.

---

## 3. Link Pay-As-You-Go Billing to Azure

Without a billing link, the Dataverse environment runs on limited capacity (especially API call throughput). The pay-as-you-go billing policy links usage charges to your Azure subscription.

### Steps

1. In **Power Platform Admin Center** > **Billing** > **Billing policies** > **+ New billing policy**

2. Configure:

   | Field | Value |
   |-------|-------|
   | Policy name | `PowerPlatformBillingPolicy2` (or any descriptive name) |
   | Azure subscription | Your Azure subscription (the one hosting `rg-re2-{env}`) |
   | Resource group | Select existing or let it create `PowerPlatformEnvironments_UKSouth` |
   | Region | `United Kingdom` |

3. Click **Save**, then **Add environments** and select the `RE2-Dev` environment.

### What this creates in Azure

- Resource group: `PowerPlatformEnvironments_UKSouth` (auto-created if it doesn't exist)
- Resource: `Microsoft.PowerPlatform/accounts/PowerPlatformBillingPolicy2`
- A storage account in `DynamicsDeployments-eastus2` may also be auto-created by the platform

### Cost implications

This is what generates the ~£83/month Dataverse cost. Charges are based on API calls and storage. To stop charges, either:
- Remove the environment from the billing policy (keeps environment, limits throughput)
- Delete the environment entirely (stops all charges)

---

## 4. Provision the RE2 Schema

Once the Dataverse environment exists, create the 27 custom `phr_*` entities.

### Option A: Via Azure DevOps pipeline (recommended)

1. Ensure the `re2-dev-secrets` variable group in Azure DevOps has the `DataverseUrl` variable set to the new environment URL

2. Run the `provision-dataverse.yml` pipeline:
   - Go to **Pipelines** > select the Dataverse provisioning pipeline
   - Click **Run pipeline**
   - Select environment: `dev`

3. The pipeline will create all entities and verify 29 `phr_*` entities exist

### Option B: Manual / local execution

```powershell
# Using Azure CLI authentication (you must be logged in with az login)
./scripts/provision-dataverse-schema.ps1 `
  -DataverseUrl "https://<your-new-org-url>.crm11.dynamics.com"

# Or with explicit SPN credentials:
./scripts/provision-dataverse-schema.ps1 `
  -DataverseUrl "https://<your-new-org-url>.crm11.dynamics.com" `
  -TenantId "<your-tenant-id>" `
  -ClientId "<app-registration-client-id>" `
  -ClientSecret "<app-registration-client-secret>"
```

The script is idempotent — safe to re-run if it partially fails.

### What the script creates

- **27 custom entities** prefixed `phr_` (e.g., `phr_licence`, `phr_gdpcredential`, `phr_capa`)
- All columns, data types, and constraints per `scripts/dataverse-schema.json`
- An **unmanaged solution** `RE2ComplianceSchema` containing all entities (for traceability and potential export)

---

## 5. Register Managed Identities

After deploying the RE2 Azure resources (via Bicep), the App Service and Function App managed identities must be registered as Dataverse application users.

### Find the Managed Identity details

```bash
# Get the principal IDs for each app
az webapp identity show --name app-re2-api-dev --resource-group rg-re2-dev \
  --query "{name:'API', principalId:principalId}" --output table

az webapp identity show --name app-re2-web-dev --resource-group rg-re2-dev \
  --query "{name:'Web', principalId:principalId}" --output table

az functionapp identity show --name func-re2-compliance-dev --resource-group rg-re2-dev \
  --query "{name:'Functions', principalId:principalId}" --output table
```

### Register in Power Platform Admin Center

For **each** managed identity (API, Web, Functions):

1. Open **Power Platform Admin Center** > **Environments** > select `RE2-Dev` > **Settings**
2. Go to **Users + permissions** > **Application users** > **+ New app user**
3. Click **+ Add an app** and search by the **Application (client) ID** (not the principal/object ID)
   - Note: For system-assigned managed identities, the Application ID can be found in Azure Portal under the App Service > **Identity** > **System assigned** > **Object (principal) ID**, then look up the corresponding **Application ID** in Azure AD > **Enterprise applications** > search by Object ID
4. Select the **Business unit** (root business unit is fine for dev)
5. Under **Security roles**, add **System Administrator** (or a custom role — see below)
6. Click **Create**

### Minimum required security role

If you prefer not to grant System Administrator, create a custom role with:

| Table | Read | Write | Notes |
|-------|------|-------|-------|
| `phr_customercomplianceextension` | Yes | Yes | |
| `phr_gdpwarehouseextension` | Yes | Yes | |
| `phr_gdpcredential` | Yes | Yes | |
| `phr_gdpserviceprovider` | Yes | Yes | |
| `phr_qualificationreview` | Yes | Yes | |
| `phr_gdpcredentialverification` | Yes | Yes | |
| `phr_gdpinspection` | Yes | Yes | |
| `phr_gdpinspectionfinding` | Yes | Yes | |
| `phr_capa` | Yes | Yes | |
| `phr_gdpdocument` | Yes | Yes | |
| Licence/substance config tables | Yes | No | Read-only reference data |

---

## 6. Update App Settings

After creating the new Dataverse environment, update the configuration to point to it.

### Azure DevOps variable group

Update `re2-dev-secrets` in Azure DevOps Pipelines > Library:

| Variable | New value |
|----------|-----------|
| `DataverseUrl` | `https://<new-org-url>.crm11.dynamics.com` |

### Local backup parameter file

Update `infra/bicep/backup_dev.bicepparam` (local only, not in git):

```
param dataverseUrl = 'https://<new-org-url>.crm11.dynamics.com'
```

### If redeploying via Bicep

The Dataverse URL is passed as a parameter. The next pipeline run or manual Bicep deployment will pick up the new value and configure it in the App Service settings automatically.

---

## 7. Verify Connectivity

After all steps are complete:

```bash
# Readiness probe checks Dataverse, D365 F&O, and Blob Storage
curl https://app-re2-api-dev.azurewebsites.net/ready
```

Expected response:
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

If Dataverse shows `Unhealthy`, check:
1. Is the Dataverse URL correct in app settings?
2. Are the managed identities registered as application users?
3. Do the application users have the correct security role?
4. See `docs/deployment/challenges-resolved-log.md` for known authentication issues

---

## 8. Environment Reference Values

Template of values to capture when setting up each environment. Actual values for dev are stored in `infra/bicep/backup_dev.bicepparam` (local only, excluded from git).

| Setting | Example / Placeholder |
|---------|----------------------|
| Dataverse URL | `https://<org-id>.crm11.dynamics.com/` |
| Dataverse region | UK South |
| Billing policy name | e.g., `PowerPlatformBillingPolicy2` |
| Billing policy resource group | `PowerPlatformEnvironments_<region>` (auto-created) |
| Azure subscription | Your target subscription ID |
| Tenant ID | Your Azure AD tenant ID |
| API managed identity (registered in Dataverse) | App ID from `az webapp identity show` |
| Functions managed identity (registered in Dataverse) | App ID from `az functionapp identity show` |
| Security role assigned | System Administrator (or custom role per section 5) |
| Schema solution | `RE2ComplianceSchema` (27 entities, unmanaged) |
| Approximate monthly cost | ~£83 (Dataverse API + storage via pay-as-you-go) |

### Related Azure resources (auto-created, not managed by RE2)

| Resource | Resource Group | Notes |
|----------|---------------|-------|
| Billing policy account | `PowerPlatformEnvironments_<region>` | Created by billing policy setup (section 3) |
| Dynamics storage account | `DynamicsDeployments-<region>` | Auto-created by the Dynamics platform — may be shared across environments, do not delete without checking |
