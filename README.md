# Azure Functions - Document Processing PoC

A proof-of-concept Azure Functions application for processing document uploads with EventGrid notifications, Table Storage tracking, Service Bus messaging, and external API integration.

## 🏗️ Architecture Overview

```
Source Storage → EventGrid → Functions → Document Processing → Destination Storage
                                ↓                                      ↓
                         Service Bus ← Table Storage ← External API Integration
                              ↓
                       Status Monitoring (Timer Function)
```

## 🚀 Features

- **EventGrid Integration**: Automatic processing of blob creation/update events
- **Document Transfer**: Secure transfer of documents between storage accounts with metadata preservation
- **External API Integration**: Resilient HTTP communication with retry policies and circuit breaker
- **Status Tracking**: Table Storage for monitoring request status and completion
- **Service Bus Notifications**: Real-time status updates via messaging
- **Timer-based Monitoring**: Automated status checking and completion handling

## 🛠️ Technology Stack

- **.NET 8.0** (LTS) - Runtime framework
- **Azure Functions v4** - Isolated Worker Process
- **Azure EventGrid** - Event-driven blob notifications
- **Azure Blob Storage** - Source and destination document storage
- **Azure Table Storage** - Request tracking and status management
- **Azure Service Bus** - Status notifications and messaging
- **Microsoft.Extensions.Http.Resilience** - HTTP resilience with Polly integration

## 📁 Project Structure

```
azure-functions-app/
├── src/Azure.Function/                  # Main function app project
│   ├── Functions/                       # Azure Functions
│   │   ├── DocumentProcessingFunction.cs
│   │   └── DocumentStatusMonitorFunction.cs
│   ├── Services/                        # Business logic services
│   │   ├── IDocumentHubService.cs
│   │   ├── DocumentHubService.cs
│   │   ├── INotificationService.cs
│   │   └── NotificationService.cs
│   ├── Providers/                       # Infrastructure providers
│   │   ├── Storage/                     # Blob and Table storage
│   │   ├── Messaging/                   # Service Bus
│   │   └── Http/                        # HTTP client with resilience
│   ├── Models/                          # Data models and DTOs
│   ├── Configuration/                   # Configuration classes
│   └── Extensions/                      # DI and extension methods
├── tests/                               # Unit and integration tests
├── spec.md                             # Detailed project specification
└── README.md                           # This file
```

## ⚙️ Prerequisites

- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Azure Functions Core Tools v4** - [Installation guide](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- **Azure CLI** (optional) - For deployment and resource management
- **Visual Studio Code** or **Visual Studio 2022** - Recommended IDE

### Verify Installation
```bash
dotnet --version                # Should show 8.0.x
func --version                  # Should show 4.x.x
az --version                    # Optional: Azure CLI
```

## 🔧 Local Development Setup

### 1. Clone and Navigate
```bash
git clone <repository-url>
cd azure-functions-app
```

### 2. Install Dependencies
```bash
cd src/Azure.Function
dotnet restore
```

### 3. Configure Local Settings
Create `local.settings.json` in the function project directory:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "SourceStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "DestinationStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "TableStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...",
    
    "ExternalApi__BaseUrl": "https://api.example.com",
    "ExternalApi__ApiKey": "your-api-key",
    "ExternalApi__TimeoutSeconds": "30",
    
    "ServiceBus__StatusTopicName": "document-status-updates",
    "ServiceBus__NotificationTopicName": "document-notifications",
    
    "Monitoring__TimerInterval": "0 */5 * * * *",
    "Monitoring__MaxCheckCount": "100",
    "Monitoring__MaxAge": "24:00:00"
  }
}
```

### 4. Start Local Development
```bash
func start
```

## 🔨 Build and Test

### Build the Project
```bash
dotnet build
```

### Run Unit Tests
```bash
dotnet test
```

### Run with Watch (Development)
```bash
func start --watch
```

## 📋 Core Components

### Functions

#### DocumentProcessingFunction
- **Trigger**: EventGrid (blob creation/update)
- **Purpose**: Process new documents and initiate workflow
- **Endpoint**: Configured via EventGrid subscription

#### DocumentStatusMonitorFunction  
- **Trigger**: Timer (every 5 minutes by default)
- **Purpose**: Monitor pending requests and handle completions

### Services

#### DocumentHubService
Primary service for document processing operations:
- `SubmitAsync()` - Process document submission
- `GetStatusAsync()` - Check processing status

#### NotificationService
Handles Service Bus messaging for status updates:
- Submitted notifications
- Completed notifications  
- Failed notifications

### Providers

#### Storage Providers
- **BlobStorageProvider**: Blob operations (read, write, metadata)
- **TableStorageProvider**: Request tracking and status management

#### Messaging Provider
- **ServiceBusProvider**: Status notifications via topics

#### HTTP Provider
- **HttpClientProvider**: Resilient external API communication

## 🔄 Workflow

### Document Submission Flow
1. Blob created/updated in source storage
2. EventGrid triggers DocumentProcessingFunction
3. Function processes document and metadata
4. Document transferred to destination storage
5. External API called with document details
6. Request ID stored in Table Storage
7. "Submitted" status sent via Service Bus

### Status Monitoring Flow
1. Timer triggers DocumentStatusMonitorFunction
2. Function queries pending requests from Table Storage
3. For each request, external API status checked
4. Based on status:
   - **Processing**: Update timestamp, continue monitoring
   - **Completed**: Send notification, remove from table
   - **Failed**: Send error notification, remove from table

## 🚀 Deployment

### Azure Resources Required
- Azure Storage Account (source documents)
- Azure Storage Account (destination documents)  
- Azure Storage Account (table storage for tracking)
- Azure Service Bus Namespace with topics
- Azure Function App
- Application Insights (recommended)

### Deploy to Azure
```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-azure-functions --location eastus

# Deploy Function App (example)
func azure functionapp publish azure-document-processor
```

### Environment Configuration
Set the following application settings in your Azure Function App:
- All connection strings and API keys from `local.settings.json`
- Configure EventGrid subscription to point to your Function App endpoint

## 📊 Monitoring and Logging

- **Application Insights**: Automatic telemetry and logging
- **Service Bus**: Message tracking and dead letter handling
- **Table Storage**: Request status and completion tracking
- **Structured Logging**: Comprehensive logging throughout the application

## 🔧 Configuration Options

### Resilience Policies
- **Retry Policy**: Exponential backoff with jitter
- **Circuit Breaker**: Prevents cascade failures
- **Timeout**: Configurable HTTP timeouts

### Monitoring Settings
- **Timer Interval**: How often to check status (default: 5 minutes)
- **Max Check Count**: Maximum status checks before timeout
- **Max Age**: Maximum time to track a request

## 🤝 Development Guidelines

### Code Standards
- Follow C# coding conventions
- Use async/await patterns
- Implement proper error handling
- Add comprehensive logging
- Write unit tests for new features

### Testing
- Unit tests for all services and providers
- Integration tests for end-to-end workflows
- Mock external dependencies appropriately

### Pull Request Process
1. Create feature branch from `main`
2. Implement changes with tests
3. Ensure all tests pass
4. Update documentation if needed
5. Submit pull request for review

## 📚 Additional Resources

- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)
- [EventGrid Documentation](https://docs.microsoft.com/en-us/azure/event-grid/)
- [Service Bus Documentation](https://docs.microsoft.com/en-us/azure/service-bus/)
- [Polly Resilience Framework](https://github.com/App-vNext/Polly)

## 🐛 Troubleshooting

### Common Issues

**Functions not triggering**
- Verify EventGrid subscription configuration
- Check Function App logs in Application Insights
- Ensure proper connection strings in configuration

**Storage connection issues**
- Validate connection strings format
- Check storage account access permissions
- Verify firewall rules if using restricted access

**External API timeouts**
- Review retry policy configuration
- Check API endpoint availability
- Monitor circuit breaker status

### Debug Locally
```bash
# Enable detailed logging
export AZURE_FUNCTIONS_ENVIRONMENT=Development
func start --verbose
```

## 📄 License

This project is part of a proof-of-concept implementation. Please refer to your organization's licensing guidelines.

## 👥 Contributors

- **Development Team**: Azure Functions Team
- **Architecture**: Based on Azure Well-Architected Framework principles

---

For detailed implementation specifications, see [spec.md](spec.md).

For questions or support, please contact the development team.
