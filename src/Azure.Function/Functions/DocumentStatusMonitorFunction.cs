using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Models;
using Azure.Function.Services;
using Azure.Function.Providers.Storage;

namespace Azure.Function.Functions;

public class DocumentStatusMonitorFunction
{
    private readonly IDocumentHubService _documentHubService;
    private readonly INotificationService _notificationService;
    private readonly ITableStorageProvider _tableStorageProvider;
    private readonly MonitoringConfiguration _monitoringConfig;
    private readonly ILogger<DocumentStatusMonitorFunction> _logger;

    public DocumentStatusMonitorFunction(
        IDocumentHubService documentHubService,
        INotificationService notificationService,
        ITableStorageProvider tableStorageProvider,
        IOptions<MonitoringConfiguration> monitoringOptions,
        ILogger<DocumentStatusMonitorFunction> logger)
    {
        _documentHubService = documentHubService;
        _notificationService = notificationService;
        _tableStorageProvider = tableStorageProvider;
        _monitoringConfig = monitoringOptions.Value;
        _logger = logger;
    }

    [Function(nameof(DocumentStatusMonitorFunction))]
    public async Task Run([TimerTrigger("%Monitoring:TimerInterval%")] TimerInfo myTimer)
    {
        _logger.LogInformation("Document status monitor function executed at: {Timestamp}", DateTime.UtcNow);

        try
        {
            // Step 1: Get all pending requests from Table Storage
            var pendingRequests = await _tableStorageProvider.GetPendingRequestsAsync();
            var pendingRequestsList = pendingRequests.ToList();

            _logger.LogInformation("Found {Count} pending requests to monitor", pendingRequestsList.Count);

            if (!pendingRequestsList.Any())
            {
                _logger.LogInformation("No pending requests found, monitoring cycle complete");
                return;
            }

            // Step 2: Process each pending request
            var processedCount = 0;
            var completedCount = 0;
            var failedCount = 0;
            var timeoutCount = 0;

            foreach (var trackingEntity in pendingRequestsList)
            {
                try
                {
                    await ProcessPendingRequestAsync(trackingEntity);
                    processedCount++;

                    // Check if request should be completed or timed out
                    var updatedEntity = await _tableStorageProvider.GetRequestAsync(trackingEntity.RowKey);
                    if (updatedEntity == null)
                    {
                        completedCount++; // Removed from table = completed or failed
                    }
                    else if (IsRequestTimedOut(updatedEntity))
                    {
                        await HandleTimeoutRequest(updatedEntity);
                        timeoutCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing pending request {RequestId}", trackingEntity.RowKey);
                    failedCount++;
                }
            }

            _logger.LogInformation("Monitoring cycle completed: Processed={ProcessedCount}, Completed={CompletedCount}, Failed={FailedCount}, TimedOut={TimeoutCount}", 
                processedCount, completedCount, failedCount, timeoutCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in document status monitor function");
            throw; // Re-throw to ensure function fails and can be retried
        }
    }

    private async Task ProcessPendingRequestAsync(RequestTrackingEntity trackingEntity)
    {
        try
        {
            _logger.LogDebug("Processing status for request {RequestId} (Check #{CheckCount})", 
                trackingEntity.RowKey, trackingEntity.CheckCount + 1);

            // Step 1: Get current status from external API
            var processingStatus = await _documentHubService.GetStatusAsync(trackingEntity.RowKey);

            _logger.LogDebug("Retrieved status for request {RequestId}: {Status}", 
                trackingEntity.RowKey, processingStatus.Status);

            // Step 2: Process based on status
            await ProcessStatusUpdateAsync(trackingEntity, processingStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing status for request {RequestId}", trackingEntity.RowKey);
            
            // Update error count and potentially fail the request if too many errors
            await HandleProcessingError(trackingEntity, ex.Message);
        }
    }

    private async Task ProcessStatusUpdateAsync(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        switch (status.Status.ToLowerInvariant())
        {
            case "completed":
            case "success":
            case "finished":
                await HandleCompletedRequest(trackingEntity, status);
                break;

            case "failed":
            case "error":
            case "cancelled":
                await HandleFailedRequest(trackingEntity, status);
                break;

            case "processing":
            case "pending":
            case "inprogress":
            case "running":
                // Still processing - tracking entity already updated by DocumentHubService.GetStatusAsync
                _logger.LogDebug("Request {RequestId} still processing, continuing to monitor", trackingEntity.RowKey);
                break;

            case "apierror":
                // API error from DocumentHubService - handle as temporary failure
                _logger.LogWarning("API error for request {RequestId}: {Message}", trackingEntity.RowKey, status.Message);
                break;

            default:
                _logger.LogWarning("Unknown status '{Status}' for request {RequestId}, treating as still processing", 
                    status.Status, trackingEntity.RowKey);
                break;
        }
    }

    private async Task HandleCompletedRequest(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        try
        {
            _logger.LogInformation("Request {RequestId} completed successfully", trackingEntity.RowKey);

            // Step 1: Send completion notification
            await _notificationService.SendCompletedNotificationAsync(trackingEntity.RowKey, status);

            // Step 2: Remove from table storage (request is complete)
            await _tableStorageProvider.DeleteRequestAsync(trackingEntity.RowKey);

            _logger.LogInformation("Successfully handled completion for request {RequestId}", trackingEntity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling completed request {RequestId}", trackingEntity.RowKey);
            throw;
        }
    }

    private async Task HandleFailedRequest(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        try
        {
            _logger.LogInformation("Request {RequestId} failed with status {Status}", trackingEntity.RowKey, status.Status);

            // Step 1: Send failure notification
            var errorMessage = status.Message ?? $"Request failed with status: {status.Status}";
            await _notificationService.SendFailedNotificationAsync(trackingEntity.RowKey, errorMessage);

            // Step 2: Remove from table storage (request is complete, even if failed)
            await _tableStorageProvider.DeleteRequestAsync(trackingEntity.RowKey);

            _logger.LogInformation("Successfully handled failure for request {RequestId}", trackingEntity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling failed request {RequestId}", trackingEntity.RowKey);
            throw;
        }
    }

    private async Task HandleProcessingError(RequestTrackingEntity trackingEntity, string errorMessage)
    {
        try
        {
            // Increment error count (using CheckCount as error count for simplicity)
            trackingEntity.CheckCount++;
            trackingEntity.LastCheckedAt = DateTime.UtcNow;
            trackingEntity.CurrentStatus = "ProcessingError";

            await _tableStorageProvider.UpsertRequestAsync(trackingEntity);

            // If too many errors, fail the request
            if (trackingEntity.CheckCount >= _monitoringConfig.MaxCheckCount)
            {
                _logger.LogWarning("Request {RequestId} exceeded maximum error count ({MaxCheckCount}), marking as failed", 
                    trackingEntity.RowKey, _monitoringConfig.MaxCheckCount);

                var failureMessage = $"Request exceeded maximum retry count ({_monitoringConfig.MaxCheckCount}). Last error: {errorMessage}";
                await _notificationService.SendFailedNotificationAsync(trackingEntity.RowKey, failureMessage);
                await _tableStorageProvider.DeleteRequestAsync(trackingEntity.RowKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling processing error for request {RequestId}", trackingEntity.RowKey);
        }
    }

    private async Task HandleTimeoutRequest(RequestTrackingEntity trackingEntity)
    {
        try
        {
            _logger.LogWarning("Request {RequestId} has timed out (age: {Age})", 
                trackingEntity.RowKey, DateTime.UtcNow - trackingEntity.SubmittedAt);

            var timeoutMessage = $"Request timed out after {DateTime.UtcNow - trackingEntity.SubmittedAt:dd\\.hh\\:mm\\:ss}";
            await _notificationService.SendFailedNotificationAsync(trackingEntity.RowKey, timeoutMessage);
            await _tableStorageProvider.DeleteRequestAsync(trackingEntity.RowKey);

            _logger.LogInformation("Successfully handled timeout for request {RequestId}", trackingEntity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling timeout for request {RequestId}", trackingEntity.RowKey);
            throw;
        }
    }

    private bool IsRequestTimedOut(RequestTrackingEntity trackingEntity)
    {
        var age = DateTime.UtcNow - trackingEntity.SubmittedAt;
        return age > _monitoringConfig.MaxAge;
    }

    // Additional method to clean up stale requests (could be called less frequently)
    [Function("CleanupStaleRequests")]
    public async Task CleanupStaleRequests([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer) // Daily at 2 AM
    {
        _logger.LogInformation("Cleanup stale requests function executed at: {Timestamp}", DateTime.UtcNow);

        try
        {
            var staleRequests = await _tableStorageProvider.GetStaleRequestsAsync(_monitoringConfig.MaxAge);
            var staleRequestsList = staleRequests.ToList();

            _logger.LogInformation("Found {Count} stale requests to cleanup", staleRequestsList.Count);

            foreach (var staleRequest in staleRequestsList)
            {
                try
                {
                    await _notificationService.SendFailedNotificationAsync(staleRequest.RowKey, 
                        $"Request was automatically cleaned up due to age ({DateTime.UtcNow - staleRequest.SubmittedAt:dd\\.hh\\:mm\\:ss})");
                    
                    await _tableStorageProvider.DeleteRequestAsync(staleRequest.RowKey);
                    
                    _logger.LogInformation("Cleaned up stale request {RequestId}", staleRequest.RowKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up stale request {RequestId}", staleRequest.RowKey);
                }
            }

            _logger.LogInformation("Stale request cleanup completed, cleaned up {Count} requests", staleRequestsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cleanup stale requests function");
            throw;
        }
    }
}
