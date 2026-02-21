// Storage Account module: Functions runtime storage + Blob document storage

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Storage SKU name')
@allowed(['Standard_LRS', 'Standard_GRS'])
param skuName string = 'Standard_LRS'

@description('Standard tags to apply to all resources')
param tags object

// Storage account names must be 3-24 chars, lowercase alphanumeric only
var storageAccountName = 'st${namePrefix}${environment}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: skuName
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Blob container for compliance documents
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'compliance-documents'
  properties: {
    publicAccess: 'None'
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account ID')
output storageAccountId string = storageAccount.id

@description('Storage account primary blob endpoint')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Storage account primary connection string for Functions runtime')
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
