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

// Collect all principal IDs (filter out empty strings from disabled staging slots)
var allPrincipalIds = concat(
  [apiPrincipalId, webPrincipalId, functionsPrincipalId]
  , apiStagingPrincipalId != '' ? [apiStagingPrincipalId] : []
  , webStagingPrincipalId != '' ? [webStagingPrincipalId] : []
)

// Reference existing resources for scoping
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Storage Blob Data Contributor for each principal
resource storageBlobRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (principalId, i) in allPrincipalIds: {
    name: guid(storageAccount.id, storageBlobDataContributorRoleId, principalId)
    scope: storageAccount
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

// Key Vault Secrets User for each principal
resource keyVaultRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (principalId, i) in allPrincipalIds: {
    name: guid(keyVault.id, keyVaultSecretsUserRoleId, principalId)
    scope: keyVault
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]
