using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Storage;
using Azure.Function.Providers.Http;

namespace Azure.Function.Services;

public class DocumentHubService : IDocumentHubService
{
    private readonly IBlobStorageProviderFactory _blobStorageProviderFactory;
    private readonly ITableStorageProvider _tableStorageProvider;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ILogger<DocumentHubService> _logger;

    public DocumentHubService(
        IBlobStorageProviderFactory blobStorageProviderFactory,
        ITableStorageProvider tableStorageProvider,
        IHttpClientProvider httpClientProvider,
        ILogger<DocumentHubService> logger)
    {
        _blobStorageProviderFactory = blobStorageProviderFactory;
        _tableStorageProvider = tableStorageProvider;
        _httpClientProvider = httpClientProvider;
        _logger = logger;
    }

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

            var blobMetadata = await sourceProvider.ReadBlobMetadataAsync(
                request.SourceContainer, 
                request.BlobName, 
                cancellationToken);

            // Merge request metadata with blob metadata
            var combinedMetadata = new Dictionary<string, string>(blobMetadata);
            foreach (var kvp in request.Metadata)
            {
                combinedMetadata[kvp.Key] = kvp.Value;
            }

            // Step 2: Upload blob to destination storage account
            _logger.LogDebug("Uploading blob {BlobName} to destination container {DestinationContainer}", 
                request.BlobName, request.DestinationContainer);
            
            var destinationProvider = _blobStorageProviderFactory.GetProvider("destination");
            var destinationETag = await destinationProvider.UploadBlobAsync(
                request.DestinationContainer,
                request.BlobName,
                blobContent,
                combinedMetadata,
                cancellationToken);

            _logger.LogInformation("Successfully uploaded blob {BlobName} to destination storage, ETag: {ETag}", 
                request.BlobName, destinationETag);

            // Step 3: Call external API with document details
            _logger.LogDebug("Submitting document {BlobName} to external API", request.BlobName);
            
            var apiResponse = await _httpClientProvider.SubmitDocumentAsync(request, cancellationToken);
            
            if (!apiResponse.Success || apiResponse.Data == null)
            {
                var errorMessage = $"External API submission failed: {apiResponse.Message}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var requestId = apiResponse.Data.RequestId;
            _logger.LogInformation("External API accepted document {BlobName}, assigned request ID {RequestId}", 
                request.BlobName, requestId);

            // Step 4: Store request tracking information in Table Storage
            _logger.LogDebug("Storing request tracking information for {RequestId}", requestId);
            
            var trackingEntity = new RequestTrackingEntity
            {
                RowKey = requestId,
                BlobName = request.BlobName,
                SourceContainer = request.SourceContainer,
                DestinationContainer = request.DestinationContainer,
                SubmittedAt = DateTime.UtcNow,
                LastCheckedAt = DateTime.UtcNow,
                CheckCount = 0,
                CurrentStatus = apiResponse.Data.Status
            };

            await _tableStorageProvider.UpsertRequestAsync(trackingEntity, cancellationToken);

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

    public async Task<ProcessingStatus> GetStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting status for request {RequestId}", requestId);

            // Step 1: Call external API to retrieve current processing status
            var apiResponse = await _httpClientProvider.GetStatusAsync(requestId, cancellationToken);
            
            if (!apiResponse.Success || apiResponse.Data == null)
            {
                var errorMessage = $"Failed to get status from external API for request {requestId}: {apiResponse.Message}";
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
            _logger.LogInformation("Retrieved status for request {RequestId}: {Status}", requestId, status.Status);

            // Step 2: Update tracking entity with latest check information
            var trackingEntity = await _tableStorageProvider.GetRequestAsync(requestId, cancellationToken);
            if (trackingEntity != null)
            {
                trackingEntity.LastCheckedAt = DateTime.UtcNow;
                trackingEntity.CheckCount++;
                trackingEntity.CurrentStatus = status.Status;

                await _tableStorageProvider.UpsertRequestAsync(trackingEntity, cancellationToken);
                
                _logger.LogDebug("Updated tracking entity for request {RequestId}, check count: {CheckCount}", 
                    requestId, trackingEntity.CheckCount);
            }
            else
            {
                _logger.LogWarning("Tracking entity not found for request {RequestId}", requestId);
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for request {RequestId}", requestId);
            throw;
        }
    }
}
