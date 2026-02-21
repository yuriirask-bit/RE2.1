using './main.bicep'

param environment = 'uat'

// ── Environment Sizing (UAT: mid-tier with staging slots) ────────────────────
param appServicePlanSku = 'S1'
param functionsPlanSkuName = 'Y1'
param functionsPlanSkuTier = 'Dynamic'
param redisSkuName = 'Standard'
param redisSkuFamily = 'C'
param redisCapacity = 1
param storageSkuName = 'Standard_GRS'
param logRetentionDays = 60
param enableStagingSlots = true

// ── External Configuration (placeholders — override via pipeline variables) ──
param dataverseUrl = 'https://re2-uat.crm4.dynamics.com'
param d365foODataEndpoint = 'https://re2-uat.sandbox.operations.dynamics.com/data'
param d365foResource = 'https://re2-uat.sandbox.operations.dynamics.com'

// ── Azure AD (replace with actual values or override in pipeline) ────────────
param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CInstance = 'https://re2uat.b2clogin.com/'
param azureAdB2CDomain = 're2uat.onmicrosoft.com'
param azureAdB2CTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CClientId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CSignUpSignInPolicyId = 'B2C_1_signupsignin'
param approverEmailGroup = 'compliance-approvers-uat@company.com'
