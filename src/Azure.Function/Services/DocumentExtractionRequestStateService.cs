using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Storage;

namespace Azure.Function.Services;

/// <summary>
/// Service for managing document extraction request lifecycle and state tracking in table storage.
/// Provides business-specific operations for document processing requests while abstracting
/// the underlying table storage implementation details.
/// </summary>
/// <remarks>
/// This service acts as the domain-specific layer over the generic table storage provider,
/// implementing business logic for document processing request management including:
/// - Request lifecycle tracking from submission to completion
/// - Status querying and filtering for monitoring operations
/// - Cleanup operations for completed/failed requests
/// 
/// All entities are stored in a single partition ("DocumentRequests") for efficient querying,
/// with row keys typically being unique request identifiers.
/// </remarks>
public class DocumentExtractionRequestStateService : IDocumentExtractionRequestStateService
{
    /// <summary>
    /// Generic table storage provider for performing CRUD operations on request tracking entities.
    /// </summary>
    private readonly ITableStorageProvider _tableStorageProvider;
    
    /// <summary>
    /// Logger for tracking service operations and troubleshooting.
    /// </summary>
    private readonly ILogger<DocumentExtractionRequestStateService> _logger;

    /// <summary>
    /// Fixed partition key used for all document processing request entities.
    /// Groups all requests together for efficient table storage operations.
    /// </summary>
    /// <remarks>
    /// Using a single partition key ensures all document requests can be queried efficiently
    /// but may become a hot partition under very high load. Consider date-based partitioning
    /// if processing volume requires it.
    /// </remarks>
    private const string PartitionKey = "DocumentRequests";

    /// <summary>
    /// Initializes a new instance of the DocumentExtractionRequestStateService.
    /// </summary>
    /// <param name="tableStorageProvider">
    /// Table storage provider for performing entity operations.
    /// </param>
    /// <param name="logger">
    /// Logger instance for recording service operations.
    /// </param>
    public DocumentExtractionRequestStateService(
        ITableStorageProvider tableStorageProvider,
        ILogger<DocumentExtractionRequestStateService> logger)
    {
        _tableStorageProvider = tableStorageProvider;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a specific document processing request by its unique identifier.
    /// </summary>
    /// <param name="requestId">Unique identifier for the document processing request.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// The <see cref="RequestTrackingEntity"/> if found, or null if no request exists with the given ID.
    /// </returns>
    /// <remarks>
    /// This method performs a direct lookup using the request ID as the row key.
    /// It's the most efficient way to retrieve a specific request's tracking information.
    /// </remarks>
    public async Task<RequestTrackingEntity?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting document request {RequestId}", requestId);
        
        return await _tableStorageProvider.GetEntityAsync<RequestTrackingEntity>(
            PartitionKey, 
            requestId, 
            cancellationToken);
    }

    /// <summary>
    /// Retrieves all document processing requests that are currently pending completion.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// Collection of <see cref="RequestTrackingEntity"/> objects representing pending requests.
    /// </returns>
    /// <remarks>
    /// This method implements business logic to determine which request statuses are considered "pending".
    /// Currently includes: "Submitted", "Processing", and "Pending" statuses.
    /// Used primarily by the monitoring function to identify requests requiring status updates.
    /// </remarks>
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
