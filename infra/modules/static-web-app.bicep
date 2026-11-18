metadata description = 'Create an Azure Static Web Apps instance'

@minLength(1)
@maxLength(60)
param staticWebAppName string

@description('Repository URL')
param repositoryUrl string

@description('Repository branch')
param repositoryBranch string = 'main'

@description('API location')
param apiLocation string = 'web'

@description('App location')
param appLocation string = 'web'

@description('Output location')
param outputLocation string = 'dist'

@description('GitHub token for repository access')
@secure()
param githubToken string

@description('Location for all resources')
param location string = 'eastus'

@description('Tags for resources')
param tags object = {}

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    provider: 'GitHub'
    repositoryUrl: repositoryUrl
    branch: repositoryBranch
    buildProperties: {
      apiLocation: apiLocation
      appLocation: appLocation
      outputLocation: outputLocation
    }
  }
}

@description('Static Web App URL')
output url string = staticWebApp.properties.defaultHostname

@description('Static Web App ID')
output id string = staticWebApp.id

@description('Deployment token')
output deploymentToken string = staticWebApp.listSecrets().properties[0].value
