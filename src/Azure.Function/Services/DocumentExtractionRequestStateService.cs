using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Storage;

namespace Azure.Function.Services;

/// <summary>
/// Service for managing document extraction request state tracking
/// Uses the generic table storage provider with business-specific logic for state management
/// </summary>
public class DocumentExtractionRequestStateService : IDocumentExtractionRequestStateService
{
    private readonly ITableStorageProvider _tableStorageProvider;
    private readonly ILogger<DocumentExtractionRequestStateService> _logger;

    // Business constants for this domain
    private const string PartitionKey = "DocumentRequests";

    public DocumentExtractionRequestStateService(
        ITableStorageProvider tableStorageProvider,
        ILogger<DocumentExtractionRequestStateService> logger)
    {
        _tableStorageProvider = tableStorageProvider;
        _logger = logger;
    }

    public async Task<RequestTrackingEntity?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document request {RequestId}", requestId);
        
        return await _tableStorageProvider.GetEntityAsync<RequestTrackingEntity>(
            PartitionKey, 
            requestId, 
            cancellationToken);
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pending document requests");
        
        // Business logic: define what statuses are considered "pending"
        var pendingStatuses = new[] { "Submitted", "Processing", "Pending" };
        var filter = string.Join(" or ", pendingStatuses.Select(status => $"CurrentStatus eq '{status}'"));
        
        return await _tableStorageProvider.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken);
    }

    public async Task UpsertRequestAsync(RequestTrackingEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upserting document request {RequestId}", entity.RowKey);
        
        // Business logic: ensure partition key is set correctly
        entity.PartitionKey = PartitionKey;
        
        await _tableStorageProvider.UpsertEntityAsync(entity, cancellationToken);
    }

    public async Task DeleteRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting document request {RequestId}", requestId);
        
        await _tableStorageProvider.DeleteEntityAsync(PartitionKey, requestId, cancellationToken);
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetStaleRequestsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stale document requests older than {MaxAge}", maxAge);
        
        // Business logic: define what makes a request "stale"
        var cutoffTime = DateTime.UtcNow - maxAge;
        var filter = $"SubmittedAt lt datetime'{cutoffTime:yyyy-MM-ddTHH:mm:ssZ}'";
        
        return await _tableStorageProvider.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetRequestsByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document requests with status {Status}", status);
        
        var filter = $"CurrentStatus eq '{status}'";
        
        return await _tableStorageProvider.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetRequestsSubmittedAfterAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document requests submitted after {CutoffTime}", cutoffTime);
        
        var filter = $"SubmittedAt gt datetime'{cutoffTime:yyyy-MM-ddTHH:mm:ssZ}'";
        
        return await _tableStorageProvider.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken);
    }
}
