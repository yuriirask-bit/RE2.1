// Reusable App Service module (used for both API and Web)

@description('Name prefix for all resources')
param namePrefix string

@description('Component name (api, web)')
param component string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('App Service Plan resource ID')
param appServicePlanId string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Redis connection string Key Vault reference')
param redisKeyVaultReference string

@description('Dataverse environment URL')
param dataverseUrl string

@description('D365 F&O OData endpoint')
param d365foODataEndpoint string

@description('D365 F&O resource identifier')
param d365foResource string

@description('Storage account blob endpoint')
param storageBlobEndpoint string

@description('Azure AD tenant ID')
param azureAdTenantId string

@description('Azure AD client ID for API')
param azureAdClientId string

@description('Azure AD B2C instance URL (API only, empty for Web)')
param azureAdB2CInstance string = ''

@description('Azure AD B2C domain (API only)')
param azureAdB2CDomain string = ''

@description('Azure AD B2C tenant ID (API only)')
param azureAdB2CTenantId string = ''

@description('Azure AD B2C client ID (API only)')
param azureAdB2CClientId string = ''

@description('Azure AD B2C sign-up/sign-in policy (API only)')
param azureAdB2CSignUpSignInPolicyId string = ''

@description('Whether to create a staging deployment slot')
param enableStagingSlot bool = false

@description('Standard tags to apply to all resources')
param tags object

var appName = 'app-${namePrefix}-${component}-${environment}'
var azureAdInstance = '${az.environment().authentication.loginEndpoint}${azureAdTenantId}'

var aspnetEnvironment = environment == 'prod' ? 'Production' : 'Staging'

var commonAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: aspnetEnvironment
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    name: 'Dataverse__Url'
    value: dataverseUrl
  }
  {
    name: 'D365FO__ODataEndpoint'
    value: d365foODataEndpoint
  }
  {
    name: 'D365FO__Resource'
    value: d365foResource
  }
  {
    name: 'BlobStorage__AccountUrl'
    value: storageBlobEndpoint
  }
  {
    name: 'Caching__Enabled'
    value: 'true'
  }
  {
    name: 'Caching__RedisConnectionString'
    value: redisKeyVaultReference
  }
  {
    name: 'Caching__KeyPrefix'
    value: 're2:'
  }
  {
    name: 'AzureAd__Instance'
    value: azureAdInstance
  }
  {
    name: 'AzureAd__TenantId'
    value: azureAdTenantId
  }
  {
    name: 'AzureAd__ClientId'
    value: azureAdClientId
  }
]

var apiAppSettings = [
  {
    name: 'AzureAd__Audience'
    value: 'api://${azureAdClientId}'
  }
  {
    name: 'AzureAdB2C__Instance'
    value: azureAdB2CInstance
  }
  {
    name: 'AzureAdB2C__Domain'
    value: azureAdB2CDomain
  }
  {
    name: 'AzureAdB2C__TenantId'
    value: azureAdB2CTenantId
  }
  {
    name: 'AzureAdB2C__ClientId'
    value: azureAdB2CClientId
  }
  {
    name: 'AzureAdB2C__SignUpSignInPolicyId'
    value: azureAdB2CSignUpSignInPolicyId
  }
]

var appSettings = component == 'api' ? concat(commonAppSettings, apiAppSettings) : commonAppSettings

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  tags: union(tags, { component: component })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: appSettings
    }
  }
}

// Staging deployment slot (UAT and Prod only)
resource stagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = if (enableStagingSlot) {
  parent: appService
  name: 'staging'
  location: location
  tags: union(tags, { component: component, slot: 'staging' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: appSettings
    }
  }
}

@description('App Service name')
output name string = appService.name

@description('App Service default hostname')
output defaultHostName string = appService.properties.defaultHostName

@description('App Service resource ID')
output id string = appService.id

@description('App Service system-assigned Managed Identity principal ID')
output principalId string = appService.identity.principalId

@description('Staging slot principal ID (empty string if slot not enabled)')
output stagingPrincipalId string = enableStagingSlot ? stagingSlot!.identity.principalId : ''
