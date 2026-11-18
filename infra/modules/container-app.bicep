metadata description = 'Create an Azure Container App'

@minLength(1)
@maxLength(64)
param containerAppName string

@description('Container image URI')
param imageUri string

@description('Container port')
param containerPort int = 5000

@description('CPU cores')
param cpuCore string = '0.5'

@description('Memory (Gi)')
param memory string = '1'

@description('Minimum replicas')
param minReplicas int = 1

@description('Maximum replicas')
param maxReplicas int = 3

@description('Environment variables')
param environmentVariables array = []

@description('Location for all resources')
param location string = resourceGroup().location

@description('Tags for resources')
param tags object = {}

@description('Container Apps Environment resource ID')
param containerAppsEnvironmentId string

resource containerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: containerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
      }
      secrets: []
    }
    template: {
      containers: [
        {
          name: 'api'
          image: imageUri
          resources: {
            cpu: json(cpuCore)
            memory: '${memory}Gi'
          }
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

@description('Container App FQDN')
output fqdn string = containerApp.properties.configuration.ingress.fqdn

@description('Container App ID')
output id string = containerApp.id

@description('Container App URL')
output url string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
