namespace Azure.Function.Providers.Storage;

/// <summary>
/// Generic blob storage provider that works with any configured storage account
/// </summary>
public interface IBlobStorageProvider
{
    Task<Stream> ReadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
