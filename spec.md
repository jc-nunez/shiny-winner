# Azure Function - Document Processing PoC

## Overview
A proof-of-concept Azure Functions application that processes document uploads through EventGrid notifications, tracks processing status in Table Storage, sends notifications via Service Bus, and provides monitoring through timer-based functions.

## Architecture Flow
```
[Source Storage] → [EventGrid] → [EventGrid Function] → [DocumentHubService] 
                                                              ↓
                                                    [Destination Storage + External API]
                                                              ↓
[Service Bus] ← [Status: Submitted] ← [Table Storage] ← [Store Request ID]
     ↓
[Timer Function] → [Read Pending Requests] → [Check Status API] → [Update/Complete]
     ↓                                                                    ↓
[Service Bus] ← [Status: Completed/Failed] ← [Remove from Table] ← [Process Results]
```

## Technical Stack
- **Runtime**: .NET 8.0 (LTS)
- **Framework**: Azure Functions v4 (Isolated Worker Process)
- **Language**: C#
- **Triggers**: EventGrid, Timer
- **Storage**: Azure Blob Storage, Azure Table Storage
- **Messaging**: Azure Service Bus
- **HTTP**: Microsoft.Extensions.Http.Resilience (with Polly)

## Project Structure
```
azure-functions-app/
├── src/
│   └── Azure.Function/
│       ├── Functions/
│       │   ├── DocumentProcessingFunction.cs    # EventGrid triggered
│       │   └── DocumentStatusMonitorFunction.cs # Timer triggered
│       ├── Services/
│       │   ├── IDocumentHubService.cs
│       │   ├── DocumentHubService.cs
│       │   ├── INotificationService.cs
│       │   └── NotificationService.cs
│       ├── Providers/
│       │   ├── Storage/
│       │   │   ├── IBlobStorageProvider.cs
│       │   │   ├── BlobStorageProvider.cs
│       │   │   ├── ITableStorageProvider.cs
│       │   │   └── TableStorageProvider.cs
│       │   ├── Messaging/
│       │   │   ├── IServiceBusProvider.cs
│       │   │   └── ServiceBusProvider.cs
│       │   ├── Http/
│       │   │   ├── IHttpClientProvider.cs
│       │   │   └── HttpClientProvider.cs
│       ├── Models/
│       │   ├── DocumentRequest.cs
│       │   ├── DocumentMetadata.cs
│       │   ├── ProcessingStatus.cs
│       │   ├── RequestTrackingEntity.cs
│       │   ├── StatusNotification.cs
│       │   └── ApiResponse.cs
│       ├── Configuration/
│       │   ├── StorageConfiguration.cs
│       │   ├── ServiceBusConfiguration.cs
│       │   ├── ApiConfiguration.cs
│       │   └── ResilienceConfiguration.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       ├── host.json
│       ├── local.settings.json
│       └── Program.cs
├── tests/
│   └── Azure.Function.Tests/
├── spec.md
├── README.md
└── .gitignore
```

## Core Components

### 1. Functions

#### DocumentProcessingFunction (EventGrid Trigger)
**Purpose**: Process new/updated documents from source storage
**Workflow**:
1. Receive EventGrid blob creation/update event
2. Parse event data to extract blob information
3. Call `DocumentHubService.SubmitAsync()`
4. Send "Submitted" status notification via Service Bus
5. Log processing results and errors

#### DocumentStatusMonitorFunction (Timer Trigger)
**Purpose**: Monitor pending requests and handle completions
**Workflow**:
1. Read all pending requests from Table Storage
2. For each request, call `DocumentHubService.GetStatusAsync()`
3. Process status updates:
   - **Still Processing**: Update timestamp, continue monitoring
   - **Completed**: Send "Completed" notification, remove from table
   - **Failed**: Send "Failed" notification, remove from table
4. Handle API timeouts and errors gracefully

### 2. Services

#### IDocumentHubService
```csharp
public interface IDocumentHubService
{
    Task<string> SubmitAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    Task<ProcessingStatus> GetStatusAsync(string requestId, CancellationToken cancellationToken = default);
}
```

#### DocumentHubService
**Dependencies**: `IBlobStorageProvider`, `IHttpClientProvider`, `ITableStorageProvider`

**SubmitAsync Implementation**:
1. Read source blob content and metadata
2. Upload blob to destination storage account
3. Call external API with document details
4. Store request tracking information in Table Storage
5. Return API-generated request ID

**GetStatusAsync Implementation**:
1. Call external API to retrieve current processing status
2. Return structured status information
3. Handle API errors and timeouts

#### INotificationService / NotificationService
**Dependencies**: `IServiceBusProvider`
**Purpose**: Send status notifications via Service Bus
**Methods**:
- `SendSubmittedNotificationAsync(string requestId, DocumentRequest request)`
- `SendCompletedNotificationAsync(string requestId, ProcessingStatus status)`
- `SendFailedNotificationAsync(string requestId, string error)`

### 3. Providers

#### Storage Providers

**IBlobStorageProvider / BlobStorageProvider**
```csharp
Task<Stream> ReadBlobAsync(string containerName, string blobName);
Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName);
Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, IDictionary<string, string> metadata);
```

**ITableStorageProvider / TableStorageProvider**
```csharp
Task<RequestTrackingEntity> GetRequestAsync(string requestId);
Task<IEnumerable<RequestTrackingEntity>> GetPendingRequestsAsync();
Task UpsertRequestAsync(RequestTrackingEntity entity);
Task DeleteRequestAsync(string requestId);
```

#### Messaging Provider

**IServiceBusProvider / ServiceBusProvider**
```csharp
Task SendMessageAsync<T>(T message, string topicName) where T : class;
Task SendNotificationAsync(StatusNotification notification);
```

#### HTTP Provider

**IHttpClientProvider / HttpClientProvider**
- Uses `Microsoft.Extensions.Http.Resilience`
- Configured with retry policies, circuit breaker, timeout
- Structured logging and telemetry integration

### 4. Models

#### DocumentRequest
```csharp
public class DocumentRequest
{
    public string SourceContainer { get; set; }
    public string BlobName { get; set; }
    public string DestinationContainer { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string EventType { get; set; } // Created, Modified
}
```

#### RequestTrackingEntity (Table Storage Entity)
```csharp
public class RequestTrackingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "DocumentRequests";
    public string RowKey { get; set; } // RequestId from API
    public string BlobName { get; set; }
    public string SourceContainer { get; set; }
    public string DestinationContainer { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public int CheckCount { get; set; }
    public string CurrentStatus { get; set; }
    // ITableEntity properties
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

#### StatusNotification
```csharp
public class StatusNotification
{
    public string RequestId { get; set; }
    public string Status { get; set; } // Submitted, Processing, Completed, Failed
    public string BlobName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
    public object Details { get; set; } // Additional status-specific data
}
```

#### ProcessingStatus
```csharp
public class ProcessingStatus
{
    public string RequestId { get; set; }
    public string Status { get; set; } // Pending, Processing, Completed, Failed
    public string Message { get; set; }
    public DateTime LastUpdated { get; set; }
    public object Result { get; set; } // API response data when completed
}
```

## Configuration

### Application Settings
```json
{
  "SourceStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...",
  "DestinationStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...",
  "TableStorageConnection": "DefaultEndpointsProtocol=https;AccountName=...",
  "ServiceBusConnection": "Endpoint=sb://...",
  
  "ExternalApi": {
    "BaseUrl": "https://api.example.com",
    "ApiKey": "...",
    "TimeoutSeconds": 30
  },
  
  "ServiceBus": {
    "StatusTopicName": "document-status-updates",
    "NotificationTopicName": "document-notifications"
  },
  
  "Monitoring": {
    "TimerInterval": "0 */5 * * * *",
    "MaxCheckCount": 100,
    "MaxAge": "24:00:00"
  },
  
  "Resilience": {
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffType": "Exponential",
      "BaseDelay": "00:00:02",
      "MaxDelay": "00:00:30"
    },
    "CircuitBreaker": {
      "HandledEventsAllowedBeforeBreaking": 3,
      "DurationOfBreak": "00:00:30"
    }
  }
}
```

## Detailed Workflow

### Document Submission Flow
1. **EventGrid Trigger**: Blob created/updated in source storage
2. **Document Processing**: 
   - Read blob content and custom metadata
   - Upload to destination storage account
   - Call external API with document details
3. **Tracking**: Store request ID in Table Storage with submission timestamp
4. **Notification**: Send "Submitted" status via Service Bus
5. **Logging**: Record successful submission or errors

### Status Monitoring Flow
1. **Timer Trigger**: Every 5 minutes (configurable)
2. **Query Pending**: Read all pending requests from Table Storage
3. **Status Check**: For each request, query external API for current status
4. **Process Results**:
   - **Still Processing**: Update `LastCheckedAt`, increment `CheckCount`
   - **Completed**: Send "Completed" notification, delete from table
   - **Failed**: Send "Failed" notification, delete from table
   - **Timeout/Max Checks**: Send "Timeout" notification, delete from table

### Error Handling & Resilience
- **HTTP Resilience**: Automatic retries with exponential backoff
- **Circuit Breaker**: Prevent cascade failures
- **Dead Letter**: Failed Service Bus messages handled appropriately  
- **Table Storage**: Cleanup of stale entries (configurable max age)
- **Logging**: Comprehensive structured logging for troubleshooting

## Dependencies (NuGet Packages)
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.EventGrid" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Timer" />
<PackageReference Include="Azure.Storage.Blobs" />
<PackageReference Include="Azure.Data.Tables" />
<PackageReference Include="Azure.Messaging.ServiceBus" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" />
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" />
<PackageReference Include="Microsoft.Extensions.Configuration.AzureAppConfiguration" />
```

## Development Phases

### Phase 1: Foundation (Week 1)
- [ ] Set up project structure and models
- [ ] Implement provider interfaces and basic implementations
- [ ] Configure dependency injection and settings
- [ ] Set up logging and Application Insights

### Phase 2: Storage & Messaging (Week 1-2)
- [ ] Implement Blob Storage provider with read/write operations
- [ ] Implement Table Storage provider for request tracking
- [ ] Implement Service Bus provider for notifications
- [ ] Unit tests for providers

### Phase 3: HTTP Integration (Week 2)
- [ ] Implement HTTP client provider with resilience policies
- [ ] Configure retry, circuit breaker, and timeout policies
- [ ] Test external API integration
- [ ] Error handling and logging

### Phase 4: Core Services (Week 2-3)
- [ ] Implement DocumentHubService with submit/status methods
- [ ] Implement NotificationService for Service Bus messaging
- [ ] Integration testing of service layer
- [ ] End-to-end workflow testing

### Phase 5: Functions (Week 3)
- [ ] Implement EventGrid function for document processing
- [ ] Implement Timer function for status monitoring
- [ ] Configure EventGrid subscription
- [ ] Test complete workflow

### Phase 6: Testing & Deployment (Week 3-4)
- [ ] Comprehensive testing (unit, integration, end-to-end)
- [ ] Performance testing and optimization
- [ ] Infrastructure setup (ARM/Bicep templates)
- [ ] Deployment pipeline and documentation

## Success Criteria
- [x] EventGrid function processes blob events reliably
- [x] Documents transferred between storage accounts with metadata
- [x] External API integration with resilient HTTP communication
- [x] Request tracking in Table Storage works correctly
- [x] Service Bus notifications sent for all status changes
- [x] Timer function monitors and completes requests appropriately
- [x] Comprehensive error handling and logging
- [x] No memory leaks or performance issues in PoC testing

---
*This specification provides the complete blueprint for the document processing PoC with full integration across Azure services.*
