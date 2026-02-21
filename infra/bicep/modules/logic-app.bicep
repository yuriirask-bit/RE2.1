// Logic App module: Approval workflows (refactored from logic-apps.bicep)

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Base URL of the RE2 Compliance API')
param complianceApiBaseUrl string

@description('Email distribution group for approval requests')
param approverEmailGroup string = 'compliance-approvers@company.com'

@description('Standard tags to apply to all resources')
param tags object

var logicAppName = '${namePrefix}-approval-workflow-${environment}'
var managedIdentityName = '${namePrefix}-logicapp-identity-${environment}'

// User-assigned Managed Identity for Logic App authentication to ComplianceApi
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

// Logic App for approval workflows
resource logicApp 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('../logic-app-definition.json')
    parameters: {
      complianceApiBaseUrl: {
        value: complianceApiBaseUrl
      }
      approverEmailGroup: {
        value: approverEmailGroup
      }
      managedIdentityId: {
        value: managedIdentity.id
      }
    }
  }
  tags: union(tags, { component: 'approval-workflow' })
}

@description('Logic App name')
output logicAppName string = logicApp.name

@description('Logic App resource ID')
output logicAppId string = logicApp.id

@description('Logic App Managed Identity principal ID')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
