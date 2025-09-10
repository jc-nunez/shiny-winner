# Managed Identity Configuration Guide

This guide explains how to configure the Azure Functions app to use System Managed Identity and User-Managed Identity for blob storage authentication.

## Overview

The application supports multiple authentication methods across different Azure services:

**Storage Accounts:**
- **System Managed Identity** - Function App's system identity (for source storage)
- **User-Managed Identity** - Dedicated identity with client ID (for destination storage)
- **Connection String** - Traditional authentication (for local dev/legacy)

**Service Bus:**
- **System Managed Identity** - Function App's system identity
- **Connection String** - Traditional authentication (for local dev/legacy)

**External API:**
- **Managed Identity Token** - Dynamic bearer tokens from Azure AD
- **Subscription Key** - Azure API Management/Front Door support

## Configuration

### appsettings.json Structure

```json
{
  "Storage": {
    "StorageAccounts": {
      "source": {
        "AccountName": "mysourceaccount",
        "AuthenticationMethod": "SystemManagedIdentity"
      },
      "destination": {
        "AccountName": "mydestinationaccount", 
        "AuthenticationMethod": "UserManagedIdentity",
        "UserManagedIdentityClientId": "12345678-1234-1234-1234-123456789abc"
      }
    },
    "ContainerToAccountMapping": {
      "uploads": "source",
      "processed": "destination"
    }
  },
  "ServiceBus": {
    "Namespace": "your-servicebus-namespace",
    "AuthenticationMethod": "SystemManagedIdentity",
    "StatusTopicName": "document-status-updates",
    "NotificationTopicName": "document-notifications"
  },
  "ExternalApi": {
    "BaseUrl": "https://api.external-service.com",
    "TokenScope": "api://your-external-api/.default",
    "UserManagedIdentityClientId": "87654321-4321-4321-4321-876543210def",
    "SubscriptionKey": "your-ocp-apim-subscription-key"
  }
}
```

## Azure Setup

### 1. System Managed Identity Setup

For the **source** storage account using System Managed Identity:

1. **Enable System Managed Identity on Function App:**
   ```bash
   az functionapp identity assign \
     --name your-function-app-name \
     --resource-group your-resource-group
   ```

2. **Grant Storage Blob Data Contributor role to the Function App:**
   ```bash
   az role assignment create \
     --assignee-object-id $(az functionapp identity show --name your-function-app-name --resource-group your-resource-group --query principalId -o tsv) \
     --role "Storage Blob Data Contributor" \
     --scope "/subscriptions/your-subscription-id/resourceGroups/your-resource-group/providers/Microsoft.Storage/storageAccounts/mysourceaccount"
   ```

### 2. User-Managed Identity Setup

For the **destination** storage account using User-Managed Identity:

1. **Create User-Managed Identity:**
   ```bash
   az identity create \
     --resource-group your-resource-group \
     --name your-user-managed-identity-name
   ```

2. **Get the Client ID:**
   ```bash
   az identity show \
     --resource-group your-resource-group \
     --name your-user-managed-identity-name \
     --query clientId -o tsv
   ```

3. **Assign the User-Managed Identity to Function App:**
   ```bash
   az functionapp identity assign \
     --name your-function-app-name \
     --resource-group your-resource-group \
     --identities "/subscriptions/your-subscription-id/resourceGroups/your-resource-group/providers/Microsoft.ManagedIdentity/userAssignedIdentities/your-user-managed-identity-name"
   ```

4. **Grant Storage Blob Data Contributor role to the User-Managed Identity:**
   ```bash
   az role assignment create \
     --assignee $(az identity show --resource-group your-resource-group --name your-user-managed-identity-name --query principalId -o tsv) \
     --role "Storage Blob Data Contributor" \
     --scope "/subscriptions/your-subscription-id/resourceGroups/your-resource-group/providers/Microsoft.Storage/storageAccounts/mydestinationaccount"
   ```

### 3. Service Bus Managed Identity Setup

For Service Bus using System Managed Identity:

1. **Grant Azure Service Bus Data Sender role to the Function App's System Managed Identity:**
   ```bash
   az role assignment create \
     --assignee-object-id $(az functionapp identity show --name your-function-app-name --resource-group your-resource-group --query principalId -o tsv) \
     --role "Azure Service Bus Data Sender" \
     --scope "/subscriptions/your-subscription-id/resourceGroups/your-resource-group/providers/Microsoft.ServiceBus/namespaces/your-servicebus-namespace"
   ```

### 4. External API Authentication Setup

For APIs behind Azure Front Door/API Management:

1. **Configure the API to accept managed identity tokens:**
   - Register your Function App's managed identity in Azure AD
   - Configure your API to validate tokens from your tenant
   - Set the `TokenScope` to match your API's App ID URI (e.g., `api://your-api-app-id/.default`)

2. **Get the Subscription Key from API Management:**
   ```bash
   # List subscription keys
   az apim subscription list \
     --service-name your-apim-service \
     --resource-group your-resource-group
   ```

### 5. Application Settings

Set the User-Managed Identity Client ID in your Function App settings:

```bash
az functionapp config appsettings set \
  --name your-function-app-name \
  --resource-group your-resource-group \
  --settings "Storage__StorageAccounts__destination__UserManagedIdentityClientId=12345678-1234-1234-1234-123456789abc"
```

## Usage Examples

### Basic Usage

```csharp
public class DocumentProcessor
{
    private readonly IBlobStorageProviderFactory _storageFactory;

    public DocumentProcessor(IBlobStorageProviderFactory storageFactory)
    {
        _storageFactory = storageFactory;
    }

    public async Task ProcessDocument()
    {
        // Get source provider (uses System Managed Identity)
        var sourceProvider = _storageFactory.GetProvider("source");
        var document = await sourceProvider.ReadBlobAsync("uploads", "document.pdf");

        // Get destination provider (uses User-Managed Identity)
        var destProvider = _storageFactory.GetProvider("destination");
        await destProvider.UploadBlobAsync("processed", "document.pdf", document, metadata);
    }
}
```

## Local Development

For local development, you can still use connection strings:

### local.settings.json
```json
{
  "ConnectionStrings": {
    "SourceStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...",
    "DestinationStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...",
    "TableStorageConnection": "DefaultEndpointsProtocol=https;AccountName=..."
  }
}
```

Or configure using connection strings in the new format:

```json
{
  "Storage": {
    "StorageAccounts": {
      "source": {
        "AccountName": "devsourceaccount",
        "AuthenticationMethod": "ConnectionString",
        "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=devsourceaccount;AccountKey=...",
        "Purpose": "source"
      }
    }
  }
}
```

## Benefits

1. **Enhanced Security**: No storage keys in configuration
2. **Simplified Key Management**: Azure manages authentication automatically  
3. **Granular Access Control**: Different identities can have different permissions
4. **Audit Trail**: All access is logged with the specific identity used
5. **Flexibility**: Mix and match authentication methods per storage account
6. **Zero Configuration Secrets**: No secrets to rotate or manage

## Required Permissions

For blob operations, the managed identities need:
- **Storage Blob Data Reader** - For read operations
- **Storage Blob Data Contributor** - For read/write operations  
- **Storage Blob Data Owner** - For full access including permission management

Choose the least privileged role that meets your needs.
