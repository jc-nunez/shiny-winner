using Azure;
using Azure.Data.Tables;

namespace Azure.Function.Models;

public class RequestTrackingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "DocumentRequests";
    public string RowKey { get; set; } = string.Empty; // Source system RequestId (from metadata)
    
    // Document information
    public string BlobName { get; set; } = string.Empty;
    public string SourceContainer { get; set; } = string.Empty;
    public string DestinationContainer { get; set; } = string.Empty;
    
    // Tracking timestamps
    public DateTime BlobCreatedAt { get; set; } // When the blob was created (from EventGrid)
    public DateTime EventReceivedAt { get; set; } = DateTime.UtcNow; // When EventGrid event was received by function
    public DateTime ApiSubmittedAt { get; set; } // When submitted to extraction API
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow; // Last status check
    
    // API tracking
    public string ApiGeneratedKey { get; set; } = string.Empty; // API's own tracking key for monitoring
    public string CurrentStatus { get; set; } = "Processing"; // Simple state: Processing until completed/failed
    public int CheckCount { get; set; } = 0;
    
    // ITableEntity properties
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
