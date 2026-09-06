// claimsflow-portal — application-owned infrastructure.
//
// BASELINE STATE, deliberately dated: 32-bit worker, Always On off, TLS 1.0,
// FTP basic auth allowed, no staging slot on the dev profile, and the storage
// account key sitting in an app setting. All of it is migration surface.
//
// Deploy demo/infra/shared.bicep first.

@description('Azure region.')
param location string = resourceGroup().location

@description('dev = B1, no slots. demo = S1 with a staging slot.')
@allowed([
  'dev'
  'demo'
])
param skuProfile string = 'dev'

@description('Storage account created by shared.bicep.')
param storageAccountName string

@description('Application Insights created by shared.bicep.')
param appInsightsName string

@description('Key Vault created by shared.bicep.')
param keyVaultName string

@description('Base URL of the claims API served by claimsflow-functions.')
param claimsApiBaseUrl string = ''

var suffix = uniqueString(resourceGroup().id)
var webAppName = 'app-claims-portal-${suffix}'
var planName = 'plan-claims-web-${skuProfile}'
var isDemo = skuProfile == 'demo'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: isDemo ? 'S1' : 'B1'
    tier: isDemo ? 'Standard' : 'Basic'
    capacity: 1
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    // BASELINE: HTTPS is not enforced.
    httpsOnly: false
    siteConfig: {
      // The app is published self-contained and carries its own .NET 6 runtime,
      // because App Service no longer offers a .NET 6 stack. This value only
      // describes the host, not what the app actually runs.
      netFrameworkVersion: 'v8.0'
      // BASELINE: 32-bit worker, no Always On, TLS 1.0, FTP with basic auth.
      use32BitWorkerProcess: true
      alwaysOn: false
      minTlsVersion: '1.0'
      ftpsState: 'AllAllowed'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ClaimsApi__BaseUrl'
          value: claimsApiBaseUrl
        }
      ]
      connectionStrings: [
        {
          // BASELINE: the account key travels in configuration. Managed identity
          // with DefaultAzureCredential is the target.
          name: 'ClaimsStorage'
          connectionString: storageConnectionString
          type: 'Custom'
        }
      ]
    }
  }
}

// Staging slot only on the demo profile — B1 does not support slots.
resource stagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = if (isDemo) {
  parent: webApp
  name: 'staging'
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: false
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: true
      alwaysOn: false
      minTlsVersion: '1.0'
      ftpsState: 'AllAllowed'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ClaimsApi__BaseUrl'
          value: claimsApiBaseUrl
        }
      ]
      connectionStrings: [
        {
          name: 'ClaimsStorage'
          connectionString: storageConnectionString
          type: 'Custom'
        }
      ]
    }
  }
}

resource vaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output planSku string = isDemo ? 'S1' : 'B1'
