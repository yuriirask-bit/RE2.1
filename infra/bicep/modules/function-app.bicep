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

@description('Standard tags to apply to all resources')
param tags object

var functionsPlanName = 'plan-${namePrefix}-func-${environment}'
var functionAppName = 'func-${namePrefix}-compliance-${environment}'

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
      appSettings: [
        { name: 'AzureWebJobsStorage'; value: storageConnectionString }
        { name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'; value: storageConnectionString }
        { name: 'WEBSITE_CONTENTSHARE'; value: functionAppName }
        { name: 'FUNCTIONS_EXTENSION_VERSION'; value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME'; value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'; value: appInsightsConnectionString }
        { name: 'Dataverse__Url'; value: dataverseUrl }
        { name: 'D365FO__ODataEndpoint'; value: d365foODataEndpoint }
        { name: 'D365FO__Resource'; value: d365foResource }
        { name: 'BlobStorage__AccountUrl'; value: storageBlobEndpoint }
      ]
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
