metadata description = 'Create a PostgreSQL Flexible Server'

@minLength(3)
@maxLength(63)
param serverName string

@description('PostgreSQL Server admin login')
param adminLogin string = 'remitmon_admin'

@secure()
param adminPassword string

@description('Database name')
param dbName string = 'remitmon'

@description('PostgreSQL version')
param postgresqlVersion string = '16'

@description('SKU name')
param skuName string = 'Standard_B2s'

@description('Storage size in GB')
param storageSizeGB int = 32

@description('Backup retention days')
param backupRetentionDays int = 7

@description('Location for all resources')
param location string = resourceGroup().location

@description('Tags for resources')
param tags object = {}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    version: postgresqlVersion
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: 'Disabled'
    }
    network: {
      delegatedSubnetResourceId: null
      privateDnsZoneArmResourceId: null
    }
    highAvailability: {
      mode: 'Disabled'
    }
    maintenanceWindow: {
      customWindow: 'Disabled'
      dayOfWeek: 0
      startHour: 0
      startMinute: 0
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresServer
  name: dbName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

@description('Database server FQDN')
output fqdn string = postgresServer.properties.fullyQualifiedDomainName

@description('Database server ID')
output id string = postgresServer.id

@description('Database connection string')
output connectionString string = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=${dbName};Port=5432;'
