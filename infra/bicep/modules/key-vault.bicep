// Key Vault module: Secrets management

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Azure AD tenant ID')
param tenantId string

@description('Redis connection string to store as secret')
@secure()
param redisConnectionString string

@description('Standard tags to apply to all resources')
param tags object

var keyVaultName = 'kv-${namePrefix}-${environment}'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

resource redisSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'RedisConnectionString'
  properties: {
    value: redisConnectionString
  }
}

@description('Key Vault name')
output name string = keyVault.name

@description('Key Vault resource ID')
output id string = keyVault.id

@description('Key Vault URI')
output uri string = keyVault.properties.vaultUri

@description('Redis connection string Key Vault reference for App Service config')
output redisKeyVaultReference string = '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=RedisConnectionString)'
