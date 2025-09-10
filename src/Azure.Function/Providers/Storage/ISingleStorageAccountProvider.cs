namespace Azure.Function.Providers.Storage;

/// <summary>
/// Represents a blob storage provider for a single storage account
/// </summary>
public interface ISingleStorageAccountProvider
{
    /// <summary>
    /// The identifier of the storage account this provider manages
    /// </summary>
    string StorageAccountId { get; }
    
    /// <summary>
    /// The purpose of this storage account (e.g., "source", "destination", "processing")
    /// </summary>
    string Purpose { get; }

    Task<Stream> ReadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
