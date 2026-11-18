metadata description = 'RemitMon main infrastructure deployment'

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Unique suffix for resource names')
param uniqueSuffix string = substring(uniqueString(resourceGroup().id), 0, 8)

@description('Location for resources')
param location string = resourceGroup().location

@description('Tags for all resources')
param tags object = {
  environment: environment
  project: 'RemitMon'
  createdAt: utcNow('u')
}

@description('PostgreSQL admin password')
@secure()
param postgresqlAdminPassword string

@description('Container image URI')
param apiImageUri string = 'mcr.microsoft.com/dotnet/samples:latest'

@description('GitHub repository URL')
param repositoryUrl string

@description('GitHub token')
@secure()
param githubToken string

// Resource names
var resourcePrefix = 'remitmon-${environment}'
var postgresqlServerName = '${resourcePrefix}-postgres-${uniqueSuffix}'
var containerAppsEnvName = '${resourcePrefix}-cae-${uniqueSuffix}'
var containerAppName = '${resourcePrefix}-api-${uniqueSuffix}'
var staticWebAppName = '${resourcePrefix}-web-${uniqueSuffix}'

// Deploy PostgreSQL
module postgres 'modules/postgres.bicep' = {
  name: 'postgres-deployment'
  params: {
    serverName: postgresqlServerName
    adminPassword: postgresqlAdminPassword
    location: location
    tags: tags
  }
}

// Deploy Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: containerAppsEnvName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
    }
  }
}

// Deploy Container App (API)
module containerApp 'modules/container-app.bicep' = {
  name: 'container-app-deployment'
  params: {
    containerAppName: containerAppName
    imageUri: apiImageUri
    containerPort: 5000
    cpuCore: '0.5'
    memory: '1'
    minReplicas: 1
    maxReplicas: 3
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironment.id
    environmentVariables: [
      {
        name: 'ConnectionStrings__DefaultConnection'
        secretRef: 'db-connection-string'
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environment
      }
    ]
  }
}

// Deploy Static Web App (Frontend)
module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'static-web-app-deployment'
  params: {
    staticWebAppName: staticWebAppName
    repositoryUrl: repositoryUrl
    repositoryBranch: 'main'
    apiLocation: 'web'
    appLocation: 'web'
    outputLocation: 'dist'
    location: 'eastus' // SWA only available in certain regions
    tags: tags
    githubToken: githubToken
  }
}

// Outputs
output postgresqlFqdn string = postgres.outputs.fqdn
output postgresqlConnectionString string = postgres.outputs.connectionString
output apiUrl string = containerApp.outputs.url
output webUrl string = staticWebApp.outputs.url
output containerAppName string = containerAppName
