# Configuration Simplification - Azure Functions App

## Summary

Successfully simplified the Azure Functions app configuration to use connection strings only, removing complex managed identity configuration patterns. This aligns with your preference for Key Vault references and simplifies the overall architecture.

## Changes Made

### 1. Storage Configuration
- **Simplified `StorageConfiguration.cs`** to contain only 3 connection string properties:
  - `SourceStorageConnection`
  - `DestinationStorageConnection` 
  - `TableStorageConnection`
- **Updated `BlobStorageProviderFactory.cs`** to use only connection strings
- **Kept `TableStorageProvider.cs`** as it was already using the simplified approach
- **Removed complex configuration files** that used dictionaries and authentication methods

### 2. Service Bus Configuration
- **Simplified `ServiceBusConfiguration.cs`** to use connection string only:
  - `ServiceBusConnection` (required)
  - `StatusTopicName` and `NotificationTopicName` (with defaults)
- **Updated `ServiceBusProvider.cs`** to use only connection string authentication
- **Removed managed identity complexity**

### 3. API Configuration  
- **Simplified `ApiConfiguration.cs`** to prioritize subscription key authentication:
  - `BaseUrl` and `SubscriptionKey` (required)
  - `TimeoutSeconds` (with default)
  - Made managed identity properties optional for advanced scenarios
- **Updated `HttpClientProvider.cs`** to handle optional managed identity gracefully

### 4. Dependency Injection Updates
- **Updated `ServiceCollectionExtensions.cs`** to use simplified configuration binding
- **Removed complex configuration logic** and fallback chains
- **Streamlined provider registration**

### 5. File Cleanup
- **Removed duplicate configuration files**:
  - `SimpleBlobStorageFactory.cs` (kept the simplified `BlobStorageProviderFactory.cs`)
  - `SimpleServiceCollectionExtensions.cs` (consolidated into main extensions)
  - Complex `StorageConfiguration.cs` (kept the simplified version)
- **Removed unused table storage complexity**:
  - `TableStorageFactory.cs` and `ITableStorageFactory.cs` (unused factory pattern)
  - `TableRepository.cs` and `IRepository.cs` (unused repository pattern)
  - Generic repository extension methods (unused abstractions)
  - All example files in `Examples/` directory (not actual implementation)

## Current Simplified Architecture

### Storage Providers
- **Blob Storage**: Multi-account factory pattern
  - `IBlobStorageProviderFactory` creates providers for different storage accounts ("source", "destination", "table")
  - Uses simplified `StorageConfiguration` with 3 connection strings
  - Registered as singleton factory, providers cached per account name

- **Table Storage**: Direct provider registration
  - `ITableStorageProvider` directly registered as scoped service
  - Uses `TableStorageConnection` from `StorageConfiguration`
  - Fixed table name "DocumentRequests" (hardcoded in constructor)
  - Used by `DocumentExtractionRequestStateService` for business logic

### Service Bus
- Simple connection string approach only
- No authentication method switching
- Topic names configurable with defaults

### API Configuration
- Prioritizes subscription key authentication
- Managed identity properties optional for advanced scenarios

## Configuration Structure

### Development (local.settings.json)
```json
{
  "Values": {
    "SourceStorageConnection": "DefaultEndpointsProtocol=https;AccountName=dev;AccountKey=...",
    "DestinationStorageConnection": "DefaultEndpointsProtocol=https;AccountName=dev;AccountKey=...",
    "TableStorageConnection": "DefaultEndpointsProtocol=https;AccountName=dev;AccountKey=...",
    "ServiceBusConnection": "Endpoint=sb://dev.servicebus.windows.net/;SharedAccessKeyName=...",
    "ExternalApi:BaseUrl": "https://api.example.com",
    "ExternalApi:SubscriptionKey": "dev-api-key"
  }
}
```

### Production (Function App Application Settings)
```json
{
  "SourceStorageConnection": "@Microsoft.KeyVault(VaultName=mykv;SecretName=SourceStorageConnection)",
  "DestinationStorageConnection": "@Microsoft.KeyVault(VaultName=mykv;SecretName=DestinationStorageConnection)",
  "TableStorageConnection": "@Microsoft.KeyVault(VaultName=mykv;SecretName=TableStorageConnection)",
  "ServiceBusConnection": "@Microsoft.KeyVault(VaultName=mykv;SecretName=ServiceBusConnection)",
  "ExternalApi__BaseUrl": "https://api.example.com",
  "ExternalApi__SubscriptionKey": "@Microsoft.KeyVault(VaultName=mykv;SecretName=ApiSubscriptionKey)"
}
```

## Key Benefits

1. **Simplified Configuration**: Only connection strings needed, no complex authentication method switching
2. **Key Vault Ready**: All sensitive values can be easily moved to Key Vault using `@Microsoft.KeyVault()` references
3. **Consistent Pattern**: Same pattern for all infrastructure connections (Storage, Service Bus, API)
4. **Reduced Complexity**: Removed managed identity complexity that added unnecessary configuration burden
5. **Future Ready**: Can still add managed identity support later if needed (properties are optional in API config)

## Migration Path

If you later want to use managed identity instead of connection strings:

1. **Storage**: Update `BlobStorageProviderFactory` to check for managed identity configuration
2. **Service Bus**: Update `ServiceBusProvider` to create clients with `DefaultAzureCredential`
3. **Configuration**: The optional managed identity classes (`ManagedIdentityStorageConfiguration`) are still available

## Files Created

- `local.settings.example.json` - Example development configuration
- `docs/production-config-example.json` - Example production configuration with Key Vault references
- `docs/CONFIGURATION_SIMPLIFICATION.md` - This summary document

## Build Status

✅ **Project builds successfully** with no errors (only minor warnings in placeholder implementations)

The application is now ready for Key Vault integration and follows a much simpler, more maintainable configuration pattern.
