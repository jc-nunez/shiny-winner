using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Messaging;

namespace Azure.Function.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IServiceBusProvider serviceBusProvider,
        ILogger<NotificationService> logger)
    {
        _serviceBusProvider = serviceBusProvider;
        _logger = logger;
    }

    public async Task SendSubmittedNotificationAsync(string requestId, DocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending submitted notification for request {RequestId}, document {BlobName}", 
                requestId, request.BlobName);

            var notification = new StatusNotification
            {
                RequestId = requestId,
                Status = "Submitted",
                BlobName = request.BlobName,
                Timestamp = DateTime.UtcNow,
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

            await _serviceBusProvider.SendNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully sent submitted notification for request {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send submitted notification for request {RequestId}, document {BlobName}", 
                requestId, request.BlobName);
            throw;
        }
    }

    public async Task SendCompletedNotificationAsync(string requestId, ProcessingStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending completed notification for request {RequestId} with status {Status}", 
                requestId, status.Status);

            var notification = new StatusNotification
            {
                RequestId = requestId,
                Status = status.Status, // Should be "Completed"
                BlobName = ExtractBlobNameFromStatus(status) ?? requestId, // Fallback to requestId if no blob name
                Timestamp = DateTime.UtcNow,
                Message = status.Message ?? $"Request {requestId} has been completed successfully",
                Details = new
                {
                    ProcessingStatus = status.Status,
                    LastUpdated = status.LastUpdated,
                    ApiResult = status.Result,
                    CompletedAt = DateTime.UtcNow
                }
            };

            await _serviceBusProvider.SendNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully sent completed notification for request {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send completed notification for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task SendFailedNotificationAsync(string requestId, string error, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending failed notification for request {RequestId}", requestId);

            var notification = new StatusNotification
            {
                RequestId = requestId,
                Status = "Failed",
                BlobName = requestId, // Use requestId as fallback since we don't have blob name context
                Timestamp = DateTime.UtcNow,
                Message = $"Request {requestId} has failed: {error}",
                Details = new
                {
                    ErrorMessage = error,
                    FailedAt = DateTime.UtcNow,
                    ErrorType = "ProcessingError"
                }
            };

            await _serviceBusProvider.SendNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully sent failed notification for request {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send failed notification for request {RequestId}", requestId);
            throw;
        }
    }

    private static string? ExtractBlobNameFromStatus(ProcessingStatus status)
    {
        // Try to extract blob name from status result if it's structured
        // This is a best-effort approach since the API response structure may vary
        if (status.Result is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.TryGetProperty("blobName", out var blobNameElement))
            {
                return blobNameElement.GetString();
            }
            if (jsonElement.TryGetProperty("fileName", out var fileNameElement))
            {
                return fileNameElement.GetString();
            }
            if (jsonElement.TryGetProperty("documentName", out var docNameElement))
            {
                return docNameElement.GetString();
            }
        }

        return null;
    }
}
