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
    private readonly IDocumentExtractionHubService _documentHubService;
    private readonly INotificationService _notificationService;
    private readonly IDocumentExtractionRequestStateService _documentRequestRepository;
    private readonly MonitoringConfiguration _monitoringConfig;
    private readonly ILogger<DocumentStatusMonitorFunction> _logger;

    public DocumentStatusMonitorFunction(
        IDocumentExtractionHubService documentHubService,
        INotificationService notificationService,
        IDocumentExtractionRequestStateService documentRequestRepository,
        IOptions<MonitoringConfiguration> monitoringOptions,
        ILogger<DocumentStatusMonitorFunction> logger)
    {
        _documentHubService = documentHubService;
        _notificationService = notificationService;
        _documentRequestRepository = documentRequestRepository;
        _monitoringConfig = monitoringOptions.Value;
        _logger = logger;
    }

    [Function(nameof(DocumentStatusMonitorFunction))]
    public async Task Run([TimerTrigger("%Monitoring:TimerInterval%")] TimerInfo myTimer)
    {
        _logger.LogInformation("Document status monitor function executed at: {Timestamp}", DateTime.UtcNow);

        try
        {
            // Step 1: Get all requests with 'Processing' status only
            var processingRequests = await _documentRequestRepository.GetPendingRequestsAsync();
            var processingRequestsList = processingRequests.Where(r => r.CurrentStatus == "Processing").ToList();

            _logger.LogInformation("Found {Count} processing requests to monitor", processingRequestsList.Count);

            if (!processingRequestsList.Any())
            {
                _logger.LogInformation("No processing requests found, monitoring cycle complete");
                return;
            }

            // Step 2: Process each processing request
            var completedCount = 0;
            var failedCount = 0;
            var stillProcessingCount = 0;

            foreach (var trackingEntity in processingRequestsList)
            {
                try
                {
                    // Check if request has timed out first
                    if (IsRequestTimedOut(trackingEntity))
                    {
                        await HandleTimeoutAsync(trackingEntity);
                        failedCount++;
                        continue;
                    }

                    // Get status from external API
                    var status = await _documentHubService.GetStatusAsync(trackingEntity.RowKey);
                    
                    // Handle based on simple status logic
                    switch (status.Status.ToLowerInvariant())
                    {
                        case "completed":
                        case "success":
                        case "finished":
                            await HandleCompletedAsync(trackingEntity, status);
                            completedCount++;
                            break;

                        case "failed":
                        case "error":
                        case "cancelled":
                            await HandleFailedAsync(trackingEntity, status);
                            failedCount++;
                            break;

                        default:
                            // Still processing - do nothing, just update last checked time
                            trackingEntity.LastCheckedAt = DateTime.UtcNow;
                            trackingEntity.CheckCount++;
                            await _documentRequestRepository.UpsertRequestAsync(trackingEntity);
                            stillProcessingCount++;
                            _logger.LogDebug("Request {RequestId} still processing (status: {Status})", trackingEntity.RowKey, status.Status);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request {RequestId}", trackingEntity.RowKey);
                    failedCount++;
                }
            }

            _logger.LogInformation("Monitoring cycle completed: Completed={CompletedCount}, Failed={FailedCount}, StillProcessing={StillProcessingCount}", 
                completedCount, failedCount, stillProcessingCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in document status monitor function");
            throw;
        }
    }

    private async Task HandleCompletedAsync(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        _logger.LogInformation("Request {RequestId} completed successfully", trackingEntity.RowKey);
        
        // Send completion notification with extracted data
        await _notificationService.SendCompletedNotificationAsync(trackingEntity, status);
        
        // Remove from table storage (request is complete)
        await _documentRequestRepository.DeleteRequestAsync(trackingEntity.RowKey);
    }

    private async Task HandleFailedAsync(RequestTrackingEntity trackingEntity, ProcessingStatus status)
    {
        _logger.LogInformation("Request {RequestId} failed with status {Status}", trackingEntity.RowKey, status.Status);
        
        // Send failure notification with error details and timestamps
        var errorMessage = status.Message ?? $"Request failed with status: {status.Status}";
        await _notificationService.SendFailedNotificationAsync(trackingEntity, errorMessage, status.Status);
        
        // Remove from table storage (request is complete, even if failed)
        await _documentRequestRepository.DeleteRequestAsync(trackingEntity.RowKey);
    }

    private async Task HandleTimeoutAsync(RequestTrackingEntity trackingEntity)
    {
        _logger.LogWarning("Request {RequestId} has timed out (age: {Age})", 
            trackingEntity.RowKey, DateTime.UtcNow - trackingEntity.ApiSubmittedAt);
        
        var timeoutMessage = $"Request timed out after {DateTime.UtcNow - trackingEntity.ApiSubmittedAt:dd\\.hh\\:mm\\:ss}";
        await _notificationService.SendFailedNotificationAsync(trackingEntity, timeoutMessage, "TIMEOUT");
        
        await _documentRequestRepository.DeleteRequestAsync(trackingEntity.RowKey);
    }

    private bool IsRequestTimedOut(RequestTrackingEntity trackingEntity)
    {
        var age = DateTime.UtcNow - trackingEntity.ApiSubmittedAt;
        return age > _monitoringConfig.MaxAge;
    }
}
