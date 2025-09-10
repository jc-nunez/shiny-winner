namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating blob storage providers for different storage accounts
/// </summary>
public interface IBlobStorageProviderFactory
{
    /// <summary>
    /// Gets a storage provider for the specified configuration name.
    /// This can be:
    /// - A storage account ID (e.g., "source", "destination", "customer-a")
    /// - A purpose name (automatically resolved to matching storage account)
    /// - A container name (automatically resolved via container-to-account mapping)
    /// </summary>
    /// <param name="configurationName">The configuration identifier</param>
    /// <returns>A provider for the resolved storage account</returns>
    /// <exception cref="ArgumentException">Thrown when the configuration name cannot be resolved</exception>
    ISingleStorageAccountProvider GetProvider(string configurationName);
}
