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
  "TokenScope": "api://your-api-app-id/.default",
  "UserManagedIdentityClientId": "12345678-1234-1234-1234-123456789abc",
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

#### **Option 1: Azure CLI Login (System Managed Identity)**
```bash
az login
# The DefaultAzureCredential will use your CLI credentials
```

#### **Option 2: Service Principal for User-Managed Identity (Recommended)**

1. **Create a Service Principal that matches your User-Managed Identity:**
   ```bash
   # Create service principal
   az ad sp create-for-rbac --name "local-dev-user-managed-identity" --skip-assignment
   
   # Note down the output:
   # {
   #   "appId": "12345678-1234-1234-1234-123456789abc",
   #   "password": "your-secret",
   #   "tenant": "your-tenant-id"
   # }
   ```

2. **Set Environment Variables:**
   ```bash
   # Windows (PowerShell)
   $env:AZURE_CLIENT_ID="12345678-1234-1234-1234-123456789abc"
   $env:AZURE_CLIENT_SECRET="your-secret"
   $env:AZURE_TENANT_ID="your-tenant-id"
   
   # macOS/Linux
   export AZURE_CLIENT_ID="12345678-1234-1234-1234-123456789abc"
   export AZURE_CLIENT_SECRET="your-secret"
   export AZURE_TENANT_ID="your-tenant-id"
   ```

3. **Grant the Service Principal the same permissions as your User-Managed Identity:**
   ```bash
   # For API access - grant to your API's App Registration
   az ad app permission admin-consent --id "12345678-1234-1234-1234-123456789abc"
   
   # For Storage access (if needed)
   az role assignment create \
     --assignee "12345678-1234-1234-1234-123456789abc" \
     --role "Storage Blob Data Contributor" \
     --scope "/subscriptions/your-sub/resourceGroups/your-rg/providers/Microsoft.Storage/storageAccounts/yourstorage"
   ```

4. **Update local.settings.json:**
   ```json
   {
     "ExternalApi": {
       "BaseUrl": "https://your-real-api.com",
       "TokenScope": "api://your-api-app-id/.default",
       "UserManagedIdentityClientId": "12345678-1234-1234-1234-123456789abc",
       "SubscriptionKey": "your-real-subscription-key"
     },
     "Storage": {
       "StorageAccounts": {
         "destination": {
           "AccountName": "yourstorageaccount",
           "AuthenticationMethod": "UserManagedIdentity",
           "UserManagedIdentityClientId": "12345678-1234-1234-1234-123456789abc"
         }
       }
     }
   }
   ```

#### **Option 3: Visual Studio/VS Code Integration**

1. **Sign in to Azure in your IDE**
2. **Use the Azure Account extension credentials**
3. **The DefaultAzureCredential will automatically use your IDE credentials**

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

### Issue: "Managed identity token acquisition failed"
- **Check environment variables are set correctly:**
  ```bash
  echo $AZURE_CLIENT_ID
  echo $AZURE_TENANT_ID
  # Don't echo the secret for security
  ```
- **Verify the service principal has correct permissions:**
  ```bash
  az ad sp show --id "your-client-id" --query "appId,displayName"
  ```
- **Test token acquisition manually:**
  ```bash
  az account get-access-token --resource "api://your-api-app-id"
  ```
- **Enable detailed logging:**
  ```json
  {
    "Values": {
      "AZURE_FUNCTIONS_ENVIRONMENT": "Development"
    },
    "logging": {
      "logLevel": {
        "Azure.Identity": "Debug",
        "Azure.Core": "Debug"
      }
    }
  }
  ```

### Issue: "Token scope not recognized"
- Verify the `TokenScope` matches your API's App ID URI
- Check the API app registration exposes the correct scopes
- Ensure the scope ends with `/.default` for application permissions

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
