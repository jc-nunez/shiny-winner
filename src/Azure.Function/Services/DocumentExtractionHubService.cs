using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Storage;
using Azure.Function.Providers.Http;

namespace Azure.Function.Services;

/// <summary>
/// Orchestration service for document extraction workflow operations.
/// Coordinates between blob storage, external API, and state tracking for document processing.
/// </summary>
/// <remarks>
/// This service implements the core business logic for document processing:
/// 1. Reads documents from source blob storage
/// 2. Uploads documents to destination storage
/// 3. Submits documents to external processing API
/// 4. Tracks processing state in table storage
/// 5. Polls for status updates and manages request lifecycle
/// 
/// Acts as the primary orchestrator between all infrastructure providers.
/// </remarks>
public class DocumentExtractionHubService : IDocumentExtractionHubService
{
    /// <summary>
    /// Factory for creating blob storage providers for different storage accounts (source, destination, table).
    /// </summary>
    private readonly IBlobStorageProviderFactory _blobStorageProviderFactory;
    
    /// <summary>
    /// State service for managing document processing request tracking entities.
    /// </summary>
    private readonly IDocumentExtractionRequestStateService _documentRequestRepository;
    
    /// <summary>
    /// HTTP client provider for external document processing API integration.
    /// </summary>
    private readonly IHttpClientProvider _httpClientProvider;
    
    /// <summary>
    /// Logger for tracking orchestration operations and workflow progress.
    /// </summary>
    private readonly ILogger<DocumentExtractionHubService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentExtractionHubService.
    /// </summary>
    /// <param name="blobStorageProviderFactory">Factory for creating storage providers.</param>
    /// <param name="documentRequestRepository">Service for managing request state tracking.</param>
    /// <param name="httpClientProvider">Provider for external API communication.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public DocumentExtractionHubService(
        IBlobStorageProviderFactory blobStorageProviderFactory,
        IDocumentExtractionRequestStateService documentRequestRepository,
        IHttpClientProvider httpClientProvider,
        ILogger<DocumentExtractionHubService> logger)
    {
        _blobStorageProviderFactory = blobStorageProviderFactory;
        _documentRequestRepository = documentRequestRepository;
        _httpClientProvider = httpClientProvider;
        _logger = logger;
    }

    /// <summary>
    /// Submits a document for processing through the complete extraction workflow.
    /// </summary>
    /// <param name="request">Document request containing source location and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The source system RequestId used for tracking this processing request.</returns>
    /// <exception cref="InvalidOperationException">Thrown if RequestId metadata is missing or API submission fails.</exception>
    /// <remarks>
    /// Complete workflow:
    /// 1. Reads document from source blob storage
    /// 2. Uploads document to destination storage (without metadata)
    /// 3. Submits to external API using RequestId from metadata
    /// 4. Creates tracking entity with timestamps and API-generated key
    /// 5. Returns source RequestId for subsequent status polling
    /// 
    /// The RequestId must be present in request.Metadata for tracking purposes.
    /// </remarks>
    public async Task<string> SubmitAsync(DocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting document submission for blob {BlobName} from container {SourceContainer}", 
                request.BlobName, request.SourceContainer);

            // Step 1: Read source blob content and metadata from source storage account
            _logger.LogDebug("Reading blob content and metadata for {BlobName} from {SourceContainer}", request.BlobName, request.SourceContainer);
            
            var sourceProvider = _blobStorageProviderFactory.GetProvider("source");
            var blobContent = await sourceProvider.ReadBlobAsync(
                request.SourceContainer, 
                request.BlobName, 
                cancellationToken);

            // Step 2: Upload blob to destination storage account (without metadata)
            _logger.LogDebug("Uploading blob {BlobName} to destination container {DestinationContainer}", 
                request.BlobName, request.DestinationContainer);
            
            var destinationProvider = _blobStorageProviderFactory.GetProvider("destination");
            var destinationETag = await destinationProvider.UploadBlobAsync(
                request.DestinationContainer,
                request.BlobName,
                blobContent,
                new Dictionary<string, string>(), // Empty metadata for destination
                cancellationToken);

            _logger.LogInformation("Successfully uploaded blob {BlobName} to destination storage, ETag: {ETag}", 
                request.BlobName, destinationETag);

            // Step 3: Extract RequestId from metadata and call external API
            if (!request.Metadata.TryGetValue("RequestId", out var requestId) || string.IsNullOrWhiteSpace(requestId))
            {
                throw new InvalidOperationException("RequestId metadata attribute is required but not found or empty");
            }

            _logger.LogDebug("Submitting document {BlobName} to external API with RequestId {RequestId}", request.BlobName, requestId);
            
            var apiResponse = await _httpClientProvider.SubmitDocumentAsync(request, cancellationToken);
            
            if (!apiResponse.Success || apiResponse.Data == null)
            {
                var errorMessage = $"External API submission failed: {apiResponse.Message}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var apiGeneratedKey = apiResponse.Data.RequestId; // API's own tracking key
            _logger.LogInformation("External API accepted document {BlobName} for source RequestId {RequestId}, API generated key {ApiKey}", 
                request.BlobName, requestId, apiGeneratedKey);

            // Step 4: Store request tracking information in Table Storage
            _logger.LogDebug("Storing comprehensive tracking information for source RequestId {RequestId}", requestId);
            
            var now = DateTime.UtcNow;
            var trackingEntity = new RequestTrackingEntity
            {
                RowKey = requestId, // Source system RequestId (from metadata)
                BlobName = request.BlobName,
                SourceContainer = request.SourceContainer,
                DestinationContainer = request.DestinationContainer,
                
                // Timestamps
                BlobCreatedAt = request.CreatedAt, // From EventGrid event
                EventReceivedAt = request.CreatedAt, // Will be updated by the calling function
                ApiSubmittedAt = now, // When we submitted to API
                LastCheckedAt = now,
                
                // API tracking
                ApiGeneratedKey = apiGeneratedKey, // Store API's tracking key for monitoring
                CurrentStatus = "Processing", // Simple state: always Processing when submitted
                CheckCount = 0
            };

            await _documentRequestRepository.UpsertRequestAsync(trackingEntity, cancellationToken);

            _logger.LogInformation("Successfully completed document submission for {BlobName}, request ID {RequestId}", 
                request.BlobName, requestId);

            return requestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit document {BlobName} from container {SourceContainer}", 
                request.BlobName, request.SourceContainer);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the current processing status of a document extraction request.
    /// </summary>
    /// <param name="requestId">Source system RequestId to check status for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Current processing status including state, message, and any results.</returns>
    /// <remarks>
    /// Status checking workflow:
    /// 1. Looks up tracking entity by source RequestId
    /// 2. Uses stored API-generated key to poll external API
    /// 3. Updates tracking entity with latest check information
    /// 4. Returns current status from external API
    /// 
    /// Returns a "NotFound" status if no tracking entity exists.
    /// Returns an "ApiError" status if the external API call fails.
    /// Updates CheckCount and LastCheckedAt timestamps for monitoring.
    /// </remarks>
    public async Task<ProcessingStatus> GetStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting status for source RequestId {RequestId}", requestId);

            // Step 1: Get tracking entity to find API generated key
            var trackingEntity = await _documentRequestRepository.GetRequestAsync(requestId, cancellationToken);
            if (trackingEntity == null)
            {
                _logger.LogWarning("Tracking entity not found for source RequestId {RequestId}", requestId);
                return new ProcessingStatus
                {
                    RequestId = requestId,
                    Status = "NotFound",
                    Message = "Request tracking entity not found",
                    LastUpdated = DateTime.UtcNow
                };
            }

            // Step 2: Call external API using the API generated key for monitoring
            var apiGeneratedKey = trackingEntity.ApiGeneratedKey;
            _logger.LogDebug("Checking status using API generated key {ApiGeneratedKey} for source RequestId {RequestId}", 
                apiGeneratedKey, requestId);
                
            var apiResponse = await _httpClientProvider.GetStatusAsync(apiGeneratedKey, cancellationToken);
            
            if (!apiResponse.Success || apiResponse.Data == null)
            {
                var errorMessage = $"Failed to get status from external API for source RequestId {requestId}: {apiResponse.Message}";
                _logger.LogWarning(errorMessage);
                
                // Return a status indicating API error but don't throw
                return new ProcessingStatus
                {
                    RequestId = requestId,
                    Status = "ApiError",
                    Message = apiResponse.Message ?? "Unknown API error",
                    LastUpdated = DateTime.UtcNow
                };
            }

            var status = apiResponse.Data;
            _logger.LogInformation("Retrieved status for source RequestId {RequestId}: {Status}", requestId, status.Status);

            // Step 3: Update tracking entity with latest check information
            trackingEntity.LastCheckedAt = DateTime.UtcNow;
            trackingEntity.CheckCount++;
            trackingEntity.CurrentStatus = status.Status;

            await _documentRequestRepository.UpsertRequestAsync(trackingEntity, cancellationToken);
            
            _logger.LogDebug("Updated tracking entity for source RequestId {RequestId}, check count: {CheckCount}", 
                requestId, trackingEntity.CheckCount);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for request {RequestId}", requestId);
            throw;
        }
    }
}
