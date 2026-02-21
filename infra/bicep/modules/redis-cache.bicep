// Azure Cache for Redis module

@description('Name prefix for all resources')
param namePrefix string

@description('Environment name (dev, uat, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Redis SKU name')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Basic'

@description('Redis SKU family (C for Basic/Standard, P for Premium)')
@allowed(['C', 'P'])
param skuFamily string = 'C'

@description('Redis cache capacity (0-6 for C family, 1-5 for P family)')
param capacity int = 0

@description('Standard tags to apply to all resources')
param tags object

var redisName = 'redis-${namePrefix}-${environment}'

resource redisCache 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  tags: tags
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: capacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

@description('Redis cache hostname')
output hostName string = redisCache.properties.hostName

@description('Redis cache SSL port')
output sslPort int = redisCache.properties.sslPort

@description('Redis cache connection string (primary key)')
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'

@description('Redis cache resource ID')
output id string = redisCache.id
