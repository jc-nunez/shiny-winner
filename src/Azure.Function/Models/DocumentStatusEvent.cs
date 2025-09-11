namespace Azure.Function.Models;

/// <summary>
/// Document processing specific notification event
/// </summary>
public class DocumentStatusEvent : INotificationEvent
{
    // Document-specific properties
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    
    // INotificationEvent implementation
    public string EventId => RequestId;
    public string EventType => Status;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Details { get; set; }

    // Factory methods for common scenarios
    public static DocumentStatusEvent CreateSubmitted(string requestId, DocumentRequest request)
    {
        return new DocumentStatusEvent
        {
            RequestId = requestId,
            Status = "Submitted",
            BlobName = request.BlobName,
            Message = $"Document {request.BlobName} has been submitted for processing",
            Details = new
            {
                SourceContainer = request.SourceContainer,
                DestinationContainer = request.DestinationContainer,
                EventType = request.EventType,
                SubmittedAt = request.CreatedAt,
                MetadataCount = request.Metadata?.Count ?? 0
            }
        };
    }

    public static DocumentStatusEvent CreateCompleted(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        return new DocumentStatusEvent
        {
            RequestId = trackingEntity.RowKey,
            Status = "Completed",
            BlobName = trackingEntity.BlobName,
            Message = status.Message ?? $"Request {trackingEntity.RowKey} has been completed successfully",
            Details = new
            {
                ApiRequestId = trackingEntity.ApiGeneratedKey,
                ExtractedData = status.Result,
                BlobCreatedAt = trackingEntity.BlobCreatedAt,
                EventReceivedAt = trackingEntity.EventReceivedAt,
                ApiSubmittedAt = trackingEntity.ApiSubmittedAt,
                CompletedAt = DateTime.UtcNow,
                ProcessingDuration = DateTime.UtcNow - trackingEntity.ApiSubmittedAt
            }
        };
    }

    public static DocumentStatusEvent CreateFailed(RequestTrackingEntity trackingEntity, string error, string? errorCode = null)
    {
        return new DocumentStatusEvent
        {
            RequestId = trackingEntity.RowKey,
            Status = "Failed",
            BlobName = trackingEntity.BlobName,
            Message = $"Request {trackingEntity.RowKey} has failed: {error}",
            Details = new
            {
                ApiRequestId = trackingEntity.ApiGeneratedKey,
                ErrorMessage = error,
                ErrorCode = errorCode,
                BlobCreatedAt = trackingEntity.BlobCreatedAt,
                EventReceivedAt = trackingEntity.EventReceivedAt,
                ApiSubmittedAt = trackingEntity.ApiSubmittedAt,
                FailedAt = DateTime.UtcNow,
                ProcessingDuration = DateTime.UtcNow - trackingEntity.ApiSubmittedAt,
                ErrorType = "ProcessingError"
            }
        };
    }
}
