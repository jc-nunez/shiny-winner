using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Storage.Blobs;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating blob storage providers for different storage accounts using connection strings.
/// Implements caching to ensure efficient provider reuse and supports the document processing workflow's
/// multi-storage account architecture.
/// </summary>
/// <remarks>
/// This factory manages blob storage providers for three different storage accounts:
/// - Source: Where original documents are uploaded
/// - Destination: Where processed results are stored
/// - Table: Used for blob storage operations related to table storage account
/// 
/// Providers are cached to avoid repeatedly creating BlobServiceClient instances.
/// Uses simplified connection string authentication only.
/// </remarks>
public class BlobStorageProviderFactory : IBlobStorageProviderFactory
{
    /// <summary>
    /// Cache of created providers to avoid duplicate BlobServiceClient creation.
    /// Key is the configuration name ("source", "destination", "table").
    /// </summary>
    private readonly Dictionary<string, IBlobStorageProvider> _providers = new();
    
    /// <summary>
    /// Storage configuration containing connection strings for different storage accounts.
    /// </summary>
    private readonly StorageConfiguration _config;
    
    /// <summary>
    /// Factory for creating loggers for individual provider instances.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;
    
    /// <summary>
    /// Logger for this factory class operations.
    /// </summary>
    private readonly ILogger<BlobStorageProviderFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the BlobStorageProviderFactory.
    /// </summary>
    /// <param name="options">Configuration options containing storage connection strings.</param>
    /// <param name="loggerFactory">Factory for creating logger instances.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required connection strings are missing from configuration.
    /// </exception>
    public BlobStorageProviderFactory(IOptions<StorageConfiguration> options, ILoggerFactory loggerFactory)
    {
        _config = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BlobStorageProviderFactory>();
        
        _logger.LogDebug("BlobStorageProviderFactory initialized for source, destination, and table storage");
    }

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
    /// the same provider instance. The provider is created with a BlobServiceClient configured
    /// using the appropriate connection string from the storage configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// var sourceProvider = factory.GetProvider("source");
    /// var destProvider = factory.GetProvider("destination");
    /// </code>
    /// </example>
    public IBlobStorageProvider GetProvider(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("Configuration name cannot be null or empty", nameof(configName));

        // Check if we already have a cached provider for this configuration
        if (_providers.TryGetValue(configName, out var existingProvider))
        {
            return existingProvider;
        }

        _logger.LogDebug("Creating new blob storage provider for configuration '{ConfigName}'", configName);

        // Get the appropriate connection string for the requested configuration
        var connectionString = GetConnectionString(configName);
        
        // Create Azure BlobServiceClient using the connection string
        var blobServiceClient = new BlobServiceClient(connectionString);
        
        // Create the provider wrapper with its own logger
        var providerLogger = _loggerFactory.CreateLogger<BlobStorageProvider>();
        var provider = new BlobStorageProvider(blobServiceClient, providerLogger);
        
        // Cache the provider for future requests to avoid recreation
        _providers[configName] = provider;
        
        _logger.LogInformation("Created blob storage provider for '{ConfigName}' using connection string", configName);
        
        return provider;
    }

    /// <summary>
    /// Retrieves the appropriate connection string based on the configuration name.
    /// </summary>
    /// <param name="configName">The storage configuration name to look up.</param>
    /// <returns>The connection string for the specified storage account.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the configName is not recognized or supported.
    /// </exception>
    /// <remarks>
    /// This method maps configuration names to their corresponding connection strings:
    /// - "source" → SourceStorageConnection
    /// - "destination" → DestinationStorageConnection  
    /// - "table" → TableStorageConnection
    /// 
    /// The comparison is case-insensitive for convenience.
    /// </remarks>
    private string GetConnectionString(string configName)
    {
        return configName.ToLowerInvariant() switch
        {
            "source" => _config.SourceStorageConnection,
            "destination" => _config.DestinationStorageConnection,
            "table" => _config.TableStorageConnection,
            _ => throw new ArgumentException(
                $"Unknown storage configuration '{configName}'. Supported: source, destination, table")
        };
    }
}
