// RBAC Role Assignments module: Managed Identities → Storage, Key Vault

@description('Storage account name')
param storageAccountName string

@description('Key Vault name')
param keyVaultName string

@description('API App Service principal ID')
param apiPrincipalId string

@description('Web App Service principal ID')
param webPrincipalId string

@description('Function App principal ID')
param functionsPrincipalId string

@description('API staging slot principal ID (empty if no slot)')
param apiStagingPrincipalId string = ''

@description('Web staging slot principal ID (empty if no slot)')
param webStagingPrincipalId string = ''

// Built-in role definition IDs
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// Core principal IDs (always present)
var corePrincipalIds = [
  apiPrincipalId
  webPrincipalId
  functionsPrincipalId
]

// Reference existing resources for scoping
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Storage Blob Data Contributor for core principals
resource storageBlobRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (principalId, i) in corePrincipalIds: {
    name: guid(storageAccount.id, storageBlobDataContributorRoleId, principalId)
    scope: storageAccount
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

// Key Vault Secrets User for core principals
resource keyVaultRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (principalId, i) in corePrincipalIds: {
    name: guid(keyVault.id, keyVaultSecretsUserRoleId, principalId)
    scope: keyVault
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

// Storage Blob Data Contributor for API staging slot (if enabled)
resource storageBlobRoleApiStaging 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (apiStagingPrincipalId != '') {
  name: guid(storageAccount.id, storageBlobDataContributorRoleId, apiStagingPrincipalId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: apiStagingPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault Secrets User for API staging slot (if enabled)
resource keyVaultRoleApiStaging 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (apiStagingPrincipalId != '') {
  name: guid(keyVault.id, keyVaultSecretsUserRoleId, apiStagingPrincipalId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiStagingPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor for Web staging slot (if enabled)
resource storageBlobRoleWebStaging 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (webStagingPrincipalId != '') {
  name: guid(storageAccount.id, storageBlobDataContributorRoleId, webStagingPrincipalId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: webStagingPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault Secrets User for Web staging slot (if enabled)
resource keyVaultRoleWebStaging 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (webStagingPrincipalId != '') {
  name: guid(keyVault.id, keyVaultSecretsUserRoleId, webStagingPrincipalId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: webStagingPrincipalId
    principalType: 'ServicePrincipal'
  }
}
