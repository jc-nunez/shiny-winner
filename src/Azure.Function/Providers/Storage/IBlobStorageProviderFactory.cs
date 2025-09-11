namespace Azure.Function.Providers.Storage;

/// <summary>
/// Contract for factory that creates blob storage providers for different storage accounts.
/// Supports multi-storage account architecture with provider caching for efficiency.
/// </summary>
/// <remarks>
/// This factory enables the document processing workflow to work with multiple storage accounts:
/// - "source": Where original documents are uploaded
/// - "destination": Where processed results are stored  
/// - "table": Used for blob operations related to table storage account
/// 
/// Implements caching to avoid repeatedly creating providers for the same configuration.
/// </remarks>
public interface IBlobStorageProviderFactory
{
    /// <summary>
    /// Gets or creates a blob storage provider for the specified storage account configuration.
    /// </summary>
    /// <param name="configName">
    /// Name of the storage configuration. Supported values: "source", "destination", "table".
    /// Case-insensitive.
    /// </param>
    /// <returns>
    /// A cached or newly created <see cref="IBlobStorageProvider"/> instance for the specified storage account.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if configName is null, empty, or not a recognized configuration name.
    /// </exception>
    /// <remarks>
    /// This method implements caching - subsequent calls with the same configName will return
    /// the same provider instance. The provider is created with appropriate connection string
    /// authentication based on the simplified storage configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// var sourceProvider = factory.GetProvider("source");
    /// var destProvider = factory.GetProvider("destination");
    /// </code>
    /// </example>
    IBlobStorageProvider GetProvider(string configName);
}
