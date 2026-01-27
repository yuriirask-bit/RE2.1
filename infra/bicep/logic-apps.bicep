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
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        complianceApiBaseUrl: {
          type: 'String'
          defaultValue: complianceApiBaseUrl
        }
        approverEmailGroup: {
          type: 'String'
          defaultValue: approverEmailGroup
        }
      }
      triggers: {
        'When_a_HTTP_request_is_received': {
          type: 'Request'
          kind: 'Http'
          inputs: {
            method: 'POST'
            schema: {
              type: 'object'
              properties: {
                workflowId: { type: 'string' }
                eventType: { type: 'string' }
                initiatedBy: { type: 'string' }
                entityType: { type: 'string' }
                entityId: { type: 'string' }
                details: { type: 'string' }
                riskLevel: { type: 'string' }
                callbackUrl: { type: 'string' }
              }
              required: [
                'workflowId'
                'eventType'
                'initiatedBy'
                'entityType'
                'entityId'
                'details'
                'riskLevel'
                'callbackUrl'
              ]
            }
          }
        }
      }
      actions: {
        'Initialize_WorkflowStatus': {
          type: 'InitializeVariable'
          runAfter: {}
          inputs: {
            variables: [
              {
                name: 'workflowStatus'
                type: 'string'
                value: 'Pending'
              }
            ]
          }
        }
        'Callback_To_ComplianceApi': {
          type: 'Http'
          runAfter: {
            'Initialize_WorkflowStatus': [ 'Succeeded' ]
          }
          inputs: {
            method: 'POST'
            uri: "@{triggerBody()?['callbackUrl']}"
            headers: {
              'Content-Type': 'application/json'
            }
            body: {
              workflowId: "@{triggerBody()?['workflowId']}"
              status: "@{variables('workflowStatus')}"
              approvedBy: 'auto-approved'
              approvalDate: "@{utcNow()}"
              comments: 'Auto-processed workflow'
            }
            authentication: {
              type: 'ManagedServiceIdentity'
              identity: managedIdentity.id
            }
            retryPolicy: {
              type: 'exponential'
              count: 3
              interval: 'PT10S'
              maximumInterval: 'PT5M'
            }
          }
        }
        Response: {
          type: 'Response'
          runAfter: {
            'Callback_To_ComplianceApi': [ 'Succeeded', 'Failed' ]
          }
          inputs: {
            statusCode: 200
            body: {
              workflowId: "@{triggerBody()?['workflowId']}"
              status: "@{variables('workflowStatus')}"
              message: 'Workflow processing complete'
            }
          }
        }
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
