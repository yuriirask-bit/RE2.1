// RE2 Compliance Platform - Main Bicep Orchestrator
// Deploys all Azure resources in dependency order.

targetScope = 'resourceGroup'

// ─── Common Parameters ───────────────────────────────────────────────────────

@description('Name prefix for all resources')
param namePrefix string = 're2'

@description('Environment name')
@allowed(['dev', 'uat', 'prod'])
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

// ─── Environment Sizing ──────────────────────────────────────────────────────

@description('App Service Plan SKU (B1=Dev, S1=UAT, P1v3=Prod)')
param appServicePlanSku string

@description('Functions Plan SKU name (Y1=Consumption, EP1=Elastic Premium)')
param functionsPlanSkuName string = 'Y1'

@description('Functions Plan SKU tier')
param functionsPlanSkuTier string = 'Dynamic'

@description('Redis SKU name')
@allowed(['Basic', 'Standard', 'Premium'])
param redisSkuName string = 'Basic'

@description('Redis SKU family')
@allowed(['C', 'P'])
param redisSkuFamily string = 'C'

@description('Redis cache capacity')
param redisCapacity int = 0

@description('Storage replication SKU')
@allowed(['Standard_LRS', 'Standard_GRS'])
param storageSkuName string = 'Standard_LRS'

@description('Log retention in days')
param logRetentionDays int = 30

@description('Enable staging deployment slots (UAT/Prod)')
param enableStagingSlots bool = false

// ─── External Configuration ──────────────────────────────────────────────────

@description('Dataverse environment URL')
param dataverseUrl string

@description('D365 F&O OData endpoint')
param d365foODataEndpoint string

@description('D365 F&O resource identifier')
param d365foResource string

@description('Azure AD tenant ID')
param azureAdTenantId string

@description('Azure AD client ID for the API/Web applications')
param azureAdClientId string

@description('Azure AD B2C instance URL')
param azureAdB2CInstance string = ''

@description('Azure AD B2C domain')
param azureAdB2CDomain string = ''

@description('Azure AD B2C tenant ID')
param azureAdB2CTenantId string = ''

@description('Azure AD B2C client ID')
param azureAdB2CClientId string = ''

@description('Azure AD B2C sign-up/sign-in policy ID')
param azureAdB2CSignUpSignInPolicyId string = ''

@description('Approval email distribution group')
param approverEmailGroup string = 'compliance-approvers@company.com'

// ─── Standard Tags ───────────────────────────────────────────────────────────

var tags = {
  environment: environment
  application: 're2-compliance'
  managedBy: 'bicep'
}

// ─── 1. Monitoring ───────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    retentionInDays: logRetentionDays
    tags: tags
  }
}

// ─── 2. Storage Account ─────────────────────────────────────────────────────

module storage 'modules/storage-account.bicep' = {
  name: 'storage-${environment}'
  params: {
    environment: environment
    location: location
    skuName: storageSkuName
    tags: tags
  }
}

// ─── 3. Redis Cache ──────────────────────────────────────────────────────────

module redis 'modules/redis-cache.bicep' = {
  name: 'redis-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    skuName: redisSkuName
    skuFamily: redisSkuFamily
    capacity: redisCapacity
    tags: tags
  }
}

// ─── 4. Key Vault ────────────────────────────────────────────────────────────

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyvault-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    tenantId: azureAdTenantId
    redisConnectionString: redis.outputs.connectionString
    tags: tags
  }
}

// ─── 5. App Service Plan (shared for API + Web) ─────────────────────────────

module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'appplan-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    skuName: appServicePlanSku
    tags: tags
  }
}

// ─── 6. API App Service ─────────────────────────────────────────────────────

module apiApp 'modules/app-service.bicep' = {
  name: 'app-api-${environment}'
  params: {
    namePrefix: namePrefix
    component: 'api'
    environment: environment
    location: location
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    redisKeyVaultReference: keyVault.outputs.redisKeyVaultReference
    dataverseUrl: dataverseUrl
    d365foODataEndpoint: d365foODataEndpoint
    d365foResource: d365foResource
    storageBlobEndpoint: storage.outputs.blobEndpoint
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
    azureAdB2CInstance: azureAdB2CInstance
    azureAdB2CDomain: azureAdB2CDomain
    azureAdB2CTenantId: azureAdB2CTenantId
    azureAdB2CClientId: azureAdB2CClientId
    azureAdB2CSignUpSignInPolicyId: azureAdB2CSignUpSignInPolicyId
    enableStagingSlot: enableStagingSlots
    tags: tags
  }
}

// ─── 7. Web App Service ─────────────────────────────────────────────────────

module webApp 'modules/app-service.bicep' = {
  name: 'app-web-${environment}'
  params: {
    namePrefix: namePrefix
    component: 'web'
    environment: environment
    location: location
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    redisKeyVaultReference: keyVault.outputs.redisKeyVaultReference
    dataverseUrl: dataverseUrl
    d365foODataEndpoint: d365foODataEndpoint
    d365foResource: d365foResource
    storageBlobEndpoint: storage.outputs.blobEndpoint
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
    enableStagingSlot: enableStagingSlots
    tags: tags
  }
}

// ─── 8. Function App ─────────────────────────────────────────────────────────

module functionApp 'modules/function-app.bicep' = {
  name: 'funcapp-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    skuName: functionsPlanSkuName
    skuTier: functionsPlanSkuTier
    storageConnectionString: storage.outputs.connectionString
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    dataverseUrl: dataverseUrl
    d365foODataEndpoint: d365foODataEndpoint
    d365foResource: d365foResource
    storageBlobEndpoint: storage.outputs.blobEndpoint
    tags: tags
  }
}

// ─── 9. Logic App ────────────────────────────────────────────────────────────

module logicApp 'modules/logic-app.bicep' = {
  name: 'logicapp-${environment}'
  params: {
    namePrefix: namePrefix
    environment: environment
    location: location
    complianceApiBaseUrl: 'https://${apiApp.outputs.defaultHostName}'
    approverEmailGroup: approverEmailGroup
    tags: tags
  }
}

// ─── 10. Role Assignments ────────────────────────────────────────────────────

module roleAssignments 'modules/role-assignments.bicep' = {
  name: 'rbac-${environment}'
  params: {
    storageAccountName: storage.outputs.storageAccountName
    keyVaultName: keyVault.outputs.name
    apiPrincipalId: apiApp.outputs.principalId
    webPrincipalId: webApp.outputs.principalId
    functionsPrincipalId: functionApp.outputs.principalId
    apiStagingPrincipalId: apiApp.outputs.stagingPrincipalId
    webStagingPrincipalId: webApp.outputs.stagingPrincipalId
  }
}

// ─── Outputs ─────────────────────────────────────────────────────────────────

@description('API App Service default hostname')
output apiHostName string = apiApp.outputs.defaultHostName

@description('Web App Service default hostname')
output webHostName string = webApp.outputs.defaultHostName

@description('Function App default hostname')
output functionAppHostName string = functionApp.outputs.defaultHostName

@description('Application Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('Key Vault URI')
output keyVaultUri string = keyVault.outputs.uri
