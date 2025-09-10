namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating blob storage providers for different storage accounts
/// </summary>
public interface IBlobStorageProviderFactory
{
    /// <summary>
    /// Gets a provider for a specific storage account by ID
    /// </summary>
    /// <param name="storageAccountId">The storage account identifier (e.g., "source", "destination", "customer-a")</param>
    /// <returns>A provider for the specified storage account</returns>
    /// <exception cref="ArgumentException">Thrown when the storage account ID is not configured</exception>
    ISingleStorageAccountProvider GetProvider(string storageAccountId);
    
    /// <summary>
    /// Gets a provider for a storage account by purpose
    /// </summary>
    /// <param name="purpose">The purpose of the storage account (e.g., "source", "destination")</param>
    /// <returns>A provider for a storage account with the specified purpose</returns>
    /// <exception cref="ArgumentException">Thrown when no storage account with the specified purpose is found</exception>
    ISingleStorageAccountProvider GetProviderByPurpose(string purpose);
    
    /// <summary>
    /// Resolves which storage account a container belongs to based on configuration
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <returns>A provider for the storage account that contains the specified container</returns>
    /// <exception cref="ArgumentException">Thrown when the container is not mapped to any storage account</exception>
    ISingleStorageAccountProvider ResolveProviderForContainer(string containerName);
    
    /// <summary>
    /// Gets all configured storage account IDs
    /// </summary>
    /// <returns>Collection of all configured storage account identifiers</returns>
    IEnumerable<string> GetConfiguredStorageAccountIds();
    
    /// <summary>
    /// Gets all configured providers
    /// </summary>
    /// <returns>Collection of all configured storage account providers</returns>
    IEnumerable<ISingleStorageAccountProvider> GetAllProviders();
}
