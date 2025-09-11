using Azure.Function.Models;
using Azure.Function.Services;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Examples;

/// <summary>
/// Examples of how to use the new cleaner NotificationService API
/// </summary>
public class NotificationServiceUsage
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationServiceUsage> _logger;

    public NotificationServiceUsage(INotificationService notificationService, ILogger<NotificationServiceUsage> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Method 1: Using the convenience methods (backwards compatible)
    /// </summary>
    public async Task UsingConvenienceMethodsAsync()
    {
        var documentRequest = new DocumentRequest
        {
            BlobName = "document.pdf",
            SourceContainer = "source-container",
            DestinationContainer = "dest-container",
            EventType = "Created"
        };

        var trackingEntity = new RequestTrackingEntity
        {
            RowKey = "req-123",
            BlobName = "document.pdf",
            ApiGeneratedKey = "api-456"
        };

        var processingStatus = new ProcessingStatus
        {
            RequestId = "api-456",
            Status = "Completed",
            Result = new { extractedText = "Sample text", confidence = 0.95 }
        };

        // These work exactly as before
        await _notificationService.SendSubmittedNotificationAsync("req-123", documentRequest);
        await _notificationService.SendCompletedNotificationAsync(trackingEntity, processingStatus);
        await _notificationService.SendFailedNotificationAsync(trackingEntity, "Processing failed", "ERR_001");
    }

    /// <summary>
    /// Method 2: Using the generic method with factory methods (recommended)
    /// </summary>
    public async Task UsingFactoryMethodsAsync()
    {
        var documentRequest = new DocumentRequest
        {
            BlobName = "document.pdf",
            SourceContainer = "source-container",
            DestinationContainer = "dest-container",
            EventType = "Created"
        };

        var trackingEntity = new RequestTrackingEntity
        {
            RowKey = "req-123",
            BlobName = "document.pdf",
            ApiGeneratedKey = "api-456"
        };

        var processingStatus = new ProcessingStatus
        {
            RequestId = "api-456",
            Status = "Completed",
            Result = new { extractedText = "Sample text", confidence = 0.95 }
        };

        // Clean, expressive factory methods
        await _notificationService.SendNotificationAsync(
            DocumentStatusEvent.CreateSubmitted("req-123", documentRequest));

        await _notificationService.SendNotificationAsync(
            DocumentStatusEvent.CreateCompleted(trackingEntity, processingStatus));

        await _notificationService.SendNotificationAsync(
            DocumentStatusEvent.CreateFailed(trackingEntity, "Processing failed", "ERR_001"));
    }

    /// <summary>
    /// Method 3: Creating custom events for specialized scenarios
    /// </summary>
    public async Task UsingCustomEventsAsync()
    {
        // Custom event for a partial processing update
        var partialUpdateEvent = new DocumentStatusEvent
        {
            RequestId = "req-123",
            Status = "PartiallyProcessed",
            BlobName = "large-document.pdf",
            Message = "Document processing 50% complete",
            Details = new
            {
                PagesProcessed = 50,
                TotalPages = 100,
                EstimatedCompletion = DateTime.UtcNow.AddMinutes(10),
                ApiRequestId = "api-456"
            }
        };

        await _notificationService.SendNotificationAsync(partialUpdateEvent);

        // Custom event for batch processing
        var batchCompleteEvent = new DocumentStatusEvent
        {
            RequestId = "batch-789",
            Status = "BatchCompleted",
            BlobName = "batch-results.json",
            Message = "Batch processing completed successfully",
            Details = new
            {
                TotalDocuments = 25,
                SuccessfulDocuments = 23,
                FailedDocuments = 2,
                ProcessingDuration = TimeSpan.FromMinutes(45),
                FailedDocumentIds = new[] { "doc-1", "doc-15" }
            }
        };

        await _notificationService.SendNotificationAsync(batchCompleteEvent);
    }

    /// <summary>
    /// Method 4: Extension method for validation scenarios
    /// </summary>
    public async Task UsingValidationEventsAsync()
    {
        var validationEvent = new DocumentStatusEvent
        {
            RequestId = "req-456",
            Status = "ValidationFailed",
            BlobName = "invalid-document.pdf",
            Message = "Document validation failed: unsupported format",
            Details = new
            {
                ValidationErrors = new[]
                {
                    "File format not supported",
                    "Document is password protected",
                    "File size exceeds limit"
                },
                FileSize = "15MB",
                FileFormat = "PDF",
                ValidationTimestamp = DateTime.UtcNow
            }
        };

        await _notificationService.SendNotificationAsync(validationEvent);
    }
}

/// <summary>
/// Extension methods for additional convenience (optional pattern)
/// </summary>
public static class NotificationServiceExtensions
{
    public static Task SendValidationFailedAsync(this INotificationService service, 
        string requestId, string blobName, string[] validationErrors, CancellationToken cancellationToken = default)
    {
        var validationEvent = new DocumentStatusEvent
        {
            RequestId = requestId,
            Status = "ValidationFailed",
            BlobName = blobName,
            Message = $"Document {blobName} failed validation",
            Details = new
            {
                ValidationErrors = validationErrors,
                ValidationTimestamp = DateTime.UtcNow
            }
        };

        return service.SendNotificationAsync(validationEvent, cancellationToken);
    }

    public static Task SendProgressUpdateAsync(this INotificationService service,
        string requestId, string blobName, int progressPercent, string? message = null, CancellationToken cancellationToken = default)
    {
        var progressEvent = new DocumentStatusEvent
        {
            RequestId = requestId,
            Status = "Processing",
            BlobName = blobName,
            Message = message ?? $"Processing {progressPercent}% complete",
            Details = new
            {
                ProgressPercent = progressPercent,
                UpdateTimestamp = DateTime.UtcNow
            }
        };

        return service.SendNotificationAsync(progressEvent, cancellationToken);
    }
}
