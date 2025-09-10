using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Core;

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
        string accountName,
        string authenticationMethod,
        string? connectionString = null,
        string? userManagedIdentityClientId = null,
        ILogger<SingleStorageAccountProvider>? logger = null)
    {
        StorageAccountId = storageAccountId ?? throw new ArgumentNullException(nameof(storageAccountId));
        Purpose = purpose ?? throw new ArgumentNullException(nameof(purpose));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _blobServiceClient = CreateBlobServiceClient(accountName, authenticationMethod, connectionString, userManagedIdentityClientId);
        
        _logger.LogInformation("Initialized storage provider for account {StorageAccountId} ({AccountName}) with purpose {Purpose} using {AuthMethod}", 
            StorageAccountId, accountName, Purpose, authenticationMethod);
    }

    private BlobServiceClient CreateBlobServiceClient(
        string accountName,
        string authenticationMethod,
        string? connectionString,
        string? userManagedIdentityClientId)
    {
        return authenticationMethod.ToLowerInvariant() switch
        {
            "connectionstring" => CreateFromConnectionString(connectionString, accountName),
            "systemmanaged" or "systemmanagedidentity" => CreateFromSystemManagedIdentity(accountName),
            "usermanaged" or "usermanagedidentity" => CreateFromUserManagedIdentity(accountName, userManagedIdentityClientId),
            _ => throw new ArgumentException($"Unsupported authentication method: {authenticationMethod}. Supported methods: ConnectionString, SystemManagedIdentity, UserManagedIdentity")
        };
    }

    private BlobServiceClient CreateFromConnectionString(string? connectionString, string accountName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException($"Connection string is required for ConnectionString authentication method (account: {accountName})");
        
        _logger.LogDebug("Creating BlobServiceClient using connection string for account {AccountName}", accountName);
        return new BlobServiceClient(connectionString);
    }

    private BlobServiceClient CreateFromSystemManagedIdentity(string accountName)
    {
        _logger.LogDebug("Creating BlobServiceClient using System Managed Identity for account {AccountName}", accountName);
        var credential = new DefaultAzureCredential();
        var blobServiceUri = new Uri($"https://{accountName}.blob.core.windows.net");
        return new BlobServiceClient(blobServiceUri, credential);
    }

    private BlobServiceClient CreateFromUserManagedIdentity(string accountName, string? userManagedIdentityClientId)
    {
        if (string.IsNullOrWhiteSpace(userManagedIdentityClientId))
            throw new ArgumentException($"UserManagedIdentityClientId is required for UserManagedIdentity authentication method (account: {accountName})");
        
        _logger.LogDebug("Creating BlobServiceClient using User Managed Identity {ClientId} for account {AccountName}", 
            userManagedIdentityClientId, accountName);
        
        var credential = new ManagedIdentityCredential(userManagedIdentityClientId);
        var blobServiceUri = new Uri($"https://{accountName}.blob.core.windows.net");
        return new BlobServiceClient(blobServiceUri, credential);
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
