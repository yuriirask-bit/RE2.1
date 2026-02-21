// App Service Plan module: Shared plan for API + Web

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('App Service Plan SKU name')
param skuName string = 'B1'

@description('Standard tags to apply to all resources')
param tags object

var planName = 'plan-${namePrefix}-${environment}'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    reserved: false // Windows
  }
}

@description('App Service Plan resource ID')
output id string = appServicePlan.id

@description('App Service Plan name')
output name string = appServicePlan.name
