// T176b: Azure Logic App deployment for approval workflows per FR-030.
// Deploys Logic App with Managed Identity authentication to ComplianceApi callback endpoints.

@description('Name prefix for all resources')
param namePrefix string = 're2'

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Base URL of the RE2 Compliance API')
param complianceApiBaseUrl string

@description('Email distribution group for approval requests')
param approverEmailGroup string = 'compliance-approvers@company.com'

var logicAppName = '${namePrefix}-approval-workflow-${environment}'
var managedIdentityName = '${namePrefix}-logicapp-identity-${environment}'

// User-assigned Managed Identity for Logic App authentication to ComplianceApi
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
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
    definition: loadJsonContent('logic-app-definition.json')
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
  tags: {
    environment: environment
    application: 're2-compliance'
    component: 'approval-workflow'
    story: 'US6'
  }
}

// Outputs
output logicAppName string = logicApp.name
output logicAppId string = logicApp.id
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output logicAppCallbackUrl string = logicApp.listCallbackUrl().value
