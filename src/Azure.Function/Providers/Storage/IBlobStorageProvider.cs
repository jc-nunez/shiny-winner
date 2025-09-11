namespace Azure.Function.Providers.Storage;

/// <summary>
/// Contract for blob storage operations supporting document processing workflow.
/// Provides methods for reading, writing, and managing blobs with metadata support.
/// </summary>
/// <remarks>
/// This interface abstracts Azure Blob Storage operations and can work with any storage account
/// configuration. Used by the factory pattern to support multiple storage accounts (source, destination, table).
/// </remarks>
public interface IBlobStorageProvider
{
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
    Task<Stream> ReadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    
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
    Task<IDictionary<string, string>> ReadBlobMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    
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
    Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
    
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
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a blob from the specified container if it exists.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name/path of the blob to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="RequestFailedException">Thrown if there's an access or service issue.</exception>
    /// <remarks>
    /// Uses DeleteIfExistsAsync to avoid errors if the blob doesn't exist.
    /// Used for cleanup operations and removing temporary processing files.
    /// </remarks>
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
