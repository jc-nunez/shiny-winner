using Azure;
using Azure.Data.Tables;

namespace Azure.Function.Models;

public class RequestTrackingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "DocumentRequests";
    public required string RowKey { get; set; } // RequestId from API
    public required string BlobName { get; set; }
    public required string SourceContainer { get; set; }
    public required string DestinationContainer { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    public int CheckCount { get; set; } = 0;
    public string CurrentStatus { get; set; } = "Submitted";
    
    // ITableEntity properties
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
