using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Blob storage provider for a single storage account
/// </summary>
public class SingleStorageAccountProvider : ISingleStorageAccountProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<SingleStorageAccountProvider> _logger;

    public string StorageAccountId { get; }
    public string Purpose { get; }

    public SingleStorageAccountProvider(
        string storageAccountId,
        string purpose,
        string connectionString,
        ILogger<SingleStorageAccountProvider> logger)
    {
        StorageAccountId = storageAccountId ?? throw new ArgumentNullException(nameof(storageAccountId));
        Purpose = purpose ?? throw new ArgumentNullException(nameof(purpose));
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;

        _logger.LogDebug("Initialized storage provider for account {StorageAccountId} with purpose {Purpose}", 
            StorageAccountId, Purpose);
    }

    public async Task<Stream> ReadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Reading blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            throw;
        }
    }

    public async Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Reading metadata for blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            
            var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return response.Value.Metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read metadata for blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
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

            _logger.LogInformation("Uploading blob {BlobName} to container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);

            var uploadOptions = new BlobUploadOptions
            {
                Metadata = metadata,
                Conditions = null // Allow overwrite
            };

            var response = await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
            
            _logger.LogInformation("Successfully uploaded blob {BlobName} to container {ContainerName} in storage account {StorageAccountId}, ETag: {ETag}", 
                blobName, containerName, StorageAccountId, response.Value.ETag);
            return response.Value.ETag.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobName} to container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
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
            
            _logger.LogDebug("Blob {BlobName} in container {ContainerName} in storage account {StorageAccountId} exists: {Exists}", 
                blobName, containerName, StorageAccountId, response.Value);
            
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob {BlobName} in container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Deleting blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully deleted blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName} in storage account {StorageAccountId}", 
                blobName, containerName, StorageAccountId);
            throw;
        }
    }
}
