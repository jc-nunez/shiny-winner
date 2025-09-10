# Local Development Setup Guide

This guide explains how to set up and configure the Azure Functions app for local development and testing.

## Prerequisites

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **Azure Functions Core Tools** - [Install](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
   ```bash
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```
3. **Azurite** - Local Azure Storage emulator
   ```bash
   npm install -g azurite
   ```

## Configuration Files

### local.settings.json

The `local.settings.json` file contains all configuration settings for local development. This file is **git-ignored** for security reasons and contains:

- **Runtime Configuration**: Azure Functions settings
- **Connection Strings**: Legacy connection strings for backward compatibility
- **Storage Configuration**: Multi-account storage settings
- **Service Bus Configuration**: Message bus settings  
- **External API Configuration**: API authentication settings
- **Monitoring & Resilience**: Timer intervals and retry policies

### Key Configuration Sections

#### 1. Storage Configuration
```json
"Storage": {
  "StorageAccounts": {
    "source": {
      "AccountName": "devstoreaccount1",
      "AuthenticationMethod": "ConnectionString",
      "ConnectionString": "UseDevelopmentStorage=true",
      "Purpose": "source"
    }
  }
}
```

#### 2. Service Bus Configuration  
```json
"ServiceBus": {
  "Namespace": "your-namespace",
  "AuthenticationMethod": "ConnectionString",
  "ServiceBusConnection": "Endpoint=sb://...",
  "StatusTopicName": "document-status-updates"
}
```

#### 3. External API Configuration
```json
"ExternalApi": {
  "BaseUrl": "https://httpbin.org",
  "AuthenticationMethod": "ApiKey", 
  "ApiKey": "test-api-key",
  "SubscriptionKey": "test-subscription-key"
}
```

## Local Development Setup

### 1. Start Azurite (Local Storage Emulator)

```bash
# Start Azurite with default settings
azurite --silent --location c:\azurite --debug c:\azurite\debug.log

# Or start with custom port
azurite --blobPort 10000 --queuePort 10001 --tablePort 10002
```

### 2. Configure Your local.settings.json

1. Copy the template from the repository
2. Update connection strings with your values:
   - **Azure Storage**: Replace with real storage accounts or keep Azurite settings
   - **Service Bus**: Replace with your Service Bus namespace connection string
   - **External API**: Replace with your API endpoints and keys

### 3. Start the Functions Runtime

```bash
cd src/Azure.Function
func start
```

### 4. Test API Endpoints

The function will be available at:
- **HTTP Triggers**: `http://localhost:7071/api/[function-name]`
- **Admin Endpoints**: `http://localhost:7071/admin/[function-name]`

## Testing Scenarios

### Local Storage Testing with Azurite

1. **Create test containers**:
   ```bash
   # Using Azure CLI
   az storage container create --name uploads --connection-string "UseDevelopmentStorage=true"
   az storage container create --name processed --connection-string "UseDevelopmentStorage=true"
   ```

2. **Upload test files**:
   ```bash
   az storage blob upload \
     --container-name uploads \
     --file ./test-document.pdf \
     --name test-document.pdf \
     --connection-string "UseDevelopmentStorage=true"
   ```

### Service Bus Testing

For local Service Bus testing, you have two options:

1. **Use Azure Service Bus** (recommended):
   - Create a development Service Bus namespace in Azure
   - Update the connection string in local.settings.json
   - Create topics: `document-status-updates`, `document-notifications`

2. **Use Service Bus Emulator** (if available):
   ```bash
   # Note: Microsoft doesn't provide an official Service Bus emulator
   # Consider using docker-based alternatives for testing
   ```

### API Testing with HTTPBin

The default configuration uses [HTTPBin](https://httpbin.org) for testing API calls:

- **Submit Document**: POST to `https://httpbin.org/post`
- **Get Status**: GET to `https://httpbin.org/get`

HTTPBin echoes back the request, making it perfect for testing headers, authentication, and payload formatting.

## Environment-Specific Configuration

### Development
```json
{
  "Storage": {
    "StorageAccounts": {
      "source": {
        "AuthenticationMethod": "ConnectionString",
        "ConnectionString": "UseDevelopmentStorage=true"
      }
    }
  }
}
```

### Integration Testing with Real Azure Resources
```json
{
  "Storage": {
    "StorageAccounts": {
      "source": {
        "AuthenticationMethod": "ConnectionString",
        "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=devstorage;AccountKey=..."
      }
    }
  }
}
```

### Testing Managed Identity Locally

For testing managed identity scenarios locally, you can:

1. **Use Azure CLI Login**:
   ```bash
   az login
   # The DefaultAzureCredential will use your CLI credentials
   ```

2. **Use Service Principal**:
   ```bash
   # Set environment variables
   export AZURE_CLIENT_ID="your-service-principal-id"
   export AZURE_CLIENT_SECRET="your-service-principal-secret"
   export AZURE_TENANT_ID="your-tenant-id"
   ```

3. **Update Configuration**:
   ```json
   {
     "Storage": {
       "StorageAccounts": {
         "source": {
           "AccountName": "yourstorageaccount",
           "AuthenticationMethod": "SystemManagedIdentity"
         }
       }
     }
   }
   ```

## Debugging Tips

### 1. Enable Verbose Logging
```json
{
  "Values": {
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development"
  },
  "logging": {
    "logLevel": {
      "default": "Debug"
    }
  }
}
```

### 2. Test Individual Components
```csharp
// Test storage factory
var provider = _storageFactory.GetProvider("source");
var exists = await provider.BlobExistsAsync("uploads", "test.pdf");

// Test API client  
var response = await _httpClient.GetAsync("/test");
```

### 3. Monitor Azurite
- View Azurite logs in the console
- Use Azure Storage Explorer to inspect containers and blobs
- Check debug logs at the specified location

## Common Issues

### Issue: "Storage account not found"
- Ensure Azurite is running
- Check connection string format
- Verify container names match configuration

### Issue: "Service Bus connection failed"  
- Verify Service Bus namespace is accessible
- Check connection string format
- Ensure topics exist

### Issue: "API authentication failed"
- Verify API key/token values
- Check base URL is accessible
- Test with curl/Postman first

## Security Notes

- **Never commit local.settings.json** - It's git-ignored for security
- Use separate configuration for each developer
- Use Azure Key Vault references for sensitive values in shared environments
- Rotate keys and tokens regularly

## Next Steps

After local setup:
1. Test basic functionality with Azurite
2. Test with real Azure resources  
3. Deploy to Azure staging environment
4. Configure production managed identities
