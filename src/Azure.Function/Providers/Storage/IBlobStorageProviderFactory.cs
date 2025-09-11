namespace Azure.Function.Providers.Storage;

/// <summary>
/// Simple factory for creating blob storage providers from configuration
/// </summary>
public interface IBlobStorageProviderFactory
{
    /// <summary>
    /// Gets a storage provider for the specified configuration name
    /// </summary>
    /// <param name="configName">The configuration name from appsettings (e.g., "source", "destination")</param>
    /// <returns>A configured blob storage provider</returns>
    /// <exception cref="ArgumentException">Thrown when the configuration name is not found</exception>
    IBlobStorageProvider GetProvider(string configName);
}
