// Azure Functions module: Functions app + dedicated consumption/premium plan

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Functions plan SKU name (Y1 = Consumption, EP1 = Elastic Premium)')
param skuName string = 'Y1'

@description('Functions plan tier')
param skuTier string = 'Dynamic'

@description('Storage account connection string for Functions runtime')
@secure()
param storageConnectionString string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Dataverse environment URL')
param dataverseUrl string

@description('D365 F&O OData endpoint')
param d365foODataEndpoint string

@description('D365 F&O resource identifier')
param d365foResource string

@description('Storage account blob endpoint')
param storageBlobEndpoint string

@description('D365 F&O auth mode: ManagedIdentity (Tier-2+) or ClientCredentials (CHE)')
@allowed(['ManagedIdentity', 'ClientCredentials'])
param d365foAuthMode string = 'ManagedIdentity'

@description('D365 F&O Azure AD tenant ID (ClientCredentials only)')
param d365foTenantId string = ''

@description('D365 F&O app registration client ID (ClientCredentials only)')
param d365foClientId string = ''

@description('D365 F&O client secret Key Vault reference (ClientCredentials only)')
param d365foClientSecretKeyVaultReference string = ''

@description('Standard tags to apply to all resources')
param tags object

var functionsPlanName = 'plan-${namePrefix}-func-${environment}'
var functionAppName = 'func-${namePrefix}-compliance-${environment}'

var functionBaseSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: functionAppName
  }
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: '~4'
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: 'dotnet-isolated'
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
    name: 'D365FO__AuthMode'
    value: d365foAuthMode
  }
  {
    name: 'BlobStorage__AccountUrl'
    value: storageBlobEndpoint
  }
]

var functionD365foClientCredentialsSettings = d365foAuthMode == 'ClientCredentials' ? [
  {
    name: 'D365FO__TenantId'
    value: d365foTenantId
  }
  {
    name: 'D365FO__ClientId'
    value: d365foClientId
  }
  {
    name: 'D365FO__ClientSecret'
    value: d365foClientSecretKeyVaultReference
  }
] : []

var functionAppSettings = concat(functionBaseSettings, functionD365foClientCredentialsSettings)

resource functionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: functionsPlanName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    reserved: false // Windows
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  tags: union(tags, { component: 'functions' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsPlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: functionAppSettings
    }
  }
}

@description('Function App name')
output name string = functionApp.name

@description('Function App default hostname')
output defaultHostName string = functionApp.properties.defaultHostName

@description('Function App resource ID')
output id string = functionApp.id

@description('Function App system-assigned Managed Identity principal ID')
output principalId string = functionApp.identity.principalId
