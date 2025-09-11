using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Blob storage provider that wraps Azure BlobServiceClient operations for document processing.
/// Provides simplified blob operations with logging and error handling for the document workflow.
/// </summary>
/// <remarks>
/// This provider handles all blob storage operations needed for the document processing pipeline,
/// including reading source documents, uploading processing results, and managing blob metadata.
/// </remarks>
public class BlobStorageProvider : IBlobStorageProvider
{
    /// <summary>
    /// Azure Storage BlobServiceClient for performing blob operations.
    /// </summary>
    private readonly BlobServiceClient _blobServiceClient;
    
    /// <summary>
    /// Logger for tracking blob operations and troubleshooting.
    /// </summary>
    private readonly ILogger<BlobStorageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the BlobStorageProvider.
    /// </summary>
    /// <param name="blobServiceClient">Configured BlobServiceClient for the storage account.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    /// <exception cref="ArgumentNullException">Thrown if blobServiceClient or logger is null.</exception>
    public BlobStorageProvider(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageProvider> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("Initialized blob storage provider with service client for {AccountName}", 
            blobServiceClient.AccountName);
    }

    /// <summary>
    /// Reads a blob's content as a stream for processing.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name/path of the blob within the container.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Stream containing the blob content.</returns>
    /// <exception cref="RequestFailedException">Thrown if the blob doesn't exist or access is denied.</exception>
    /// <remarks>
    /// The returned stream should be disposed by the caller. Used primarily for reading
    /// source documents that need to be processed.
    /// </remarks>
    public async Task<Stream> ReadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Reading blob {BlobName} from container {ContainerName}", blobName, containerName);
            
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <summary>
    /// Reads blob metadata without downloading the blob content.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name/path of the blob within the container.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of blob metadata key-value pairs.</returns>
    /// <exception cref="RequestFailedException">Thrown if the blob doesn't exist or access is denied.</exception>
    /// <remarks>
    /// Retrieves custom metadata associated with the blob, useful for processing decisions
    /// and workflow routing without needing to download the actual content.
    /// </remarks>
    public async Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Reading metadata for blob {BlobName} from container {ContainerName}", blobName, containerName);
            
            var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return response.Value.Metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read metadata for blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <summary>
    /// Uploads blob content with metadata to the specified container.
    /// </summary>
    /// <param name="containerName">Name of the destination container.</param>
    /// <param name="blobName">Name/path for the new blob.</param>
    /// <param name="content">Stream containing the blob content to upload.</param>
    /// <param name="metadata">Metadata key-value pairs to associate with the blob.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>ETag of the uploaded blob for optimistic concurrency control.</returns>
    /// <exception cref="RequestFailedException">Thrown if upload fails due to access or storage issues.</exception>
    /// <remarks>
    /// Creates the container if it doesn't exist. Overwrites existing blobs with the same name.
    /// Used for storing processing results and extracted data in the destination storage account.
    /// </remarks>
    public async Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Uploading blob {BlobName} to container {ContainerName}", blobName, containerName);

            var uploadOptions = new BlobUploadOptions
            {
                Metadata = metadata,
                Conditions = null // Allow overwrite
            };

            var response = await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
            
            _logger.LogInformation("Successfully uploaded blob {BlobName} to container {ContainerName}, ETag: {ETag}", 
                blobName, containerName, response.Value.ETag);
            return response.Value.ETag.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobName} to container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <summary>
    /// Checks whether a blob exists in the specified container.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name/path of the blob to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the blob exists, false otherwise.</returns>
    /// <exception cref="RequestFailedException">Thrown if there's an access or service issue.</exception>
    /// <remarks>
    /// Performs a lightweight check without downloading content. Useful for validation
    /// and conditional processing logic in the document workflow.
    /// </remarks>
    public async Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync(cancellationToken);
            
            _logger.LogDebug("Blob {BlobName} in container {ContainerName} exists: {Exists}", blobName, containerName, response.Value);
            
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob {BlobName} in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    /// <summary>
    /// Deletes a blob from the specified container if it exists.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name/path of the blob to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <remarks>
    /// Uses DeleteIfExistsAsync to avoid errors if the blob doesn't exist.
    /// Used for cleanup operations and removing temporary processing files.
    /// </remarks>
    /// <exception cref="RequestFailedException">Thrown if there's an access or service issue.</exception>
    public async Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Deleting blob {BlobName} from container {ContainerName}", blobName, containerName);
            
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }
}
