using './main.bicep'

param environment = 'dev'

// ── Environment Sizing (Dev: minimal) ────────────────────────────────────────
param appServicePlanSku = 'B1'
param functionsPlanSkuName = 'Y1'
param functionsPlanSkuTier = 'Dynamic'
param redisSkuName = 'Basic'
param redisSkuFamily = 'C'
param redisCapacity = 0
param storageSkuName = 'Standard_LRS'
param logRetentionDays = 30
param enableStagingSlots = false

// ── External Configuration (placeholders — override via pipeline variables) ──
param dataverseUrl = 'https://re2-dev.crm4.dynamics.com'
param d365foODataEndpoint = 'https://re2-dev.sandbox.operations.dynamics.com/data'
param d365foResource = 'https://re2-dev.sandbox.operations.dynamics.com'

// ── Azure AD (TenantId overridden via pipeline variable `azureAdTenantId`) ───
param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = 'e588c712-6b6d-4180-9ad4-fb7a4eece97a'
param azureAdB2CInstance = 'https://re2dev.b2clogin.com/'
param azureAdB2CDomain = 're2dev.onmicrosoft.com'
param azureAdB2CTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CClientId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CSignUpSignInPolicyId = 'B2C_1_signupsignin'
param approverEmailGroup = 'compliance-approvers-dev@company.com'
