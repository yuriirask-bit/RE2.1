using './main.bicep'

param environment = 'prod'

// ── Environment Sizing (Prod: production-grade with staging slots) ───────────
param appServicePlanSku = 'P1v3'
param functionsPlanSkuName = 'EP1'
param functionsPlanSkuTier = 'ElasticPremium'
param redisSkuName = 'Premium'
param redisSkuFamily = 'P'
param redisCapacity = 1
param storageSkuName = 'Standard_GRS'
param logRetentionDays = 90
param enableStagingSlots = true

// ── External Configuration (placeholders — override via pipeline variables) ──
param dataverseUrl = 'https://re2-prod.crm4.dynamics.com'
param d365foODataEndpoint = 'https://re2-prod.operations.dynamics.com/data'
param d365foResource = 'https://re2-prod.operations.dynamics.com'

// ── Azure AD (replace with actual values or override in pipeline) ────────────
param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CInstance = 'https://re2prod.b2clogin.com/'
param azureAdB2CDomain = 're2prod.onmicrosoft.com'
param azureAdB2CTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CClientId = '00000000-0000-0000-0000-000000000000'
param azureAdB2CSignUpSignInPolicyId = 'B2C_1_signupsignin'
param approverEmailGroup = 'compliance-approvers@company.com'
