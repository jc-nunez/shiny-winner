using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Simple blob storage provider that works with a configured BlobServiceClient
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageProvider> _logger;

    public BlobStorageProvider(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageProvider> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("Initialized blob storage provider with service client for {AccountName}", 
            blobServiceClient.AccountName);
    }

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
