using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Simple factory for creating blob storage providers from configuration
/// </summary>
public class BlobStorageProviderFactory : IBlobStorageProviderFactory
{
    private readonly Dictionary<string, IBlobStorageProvider> _providers = new();
    private readonly StorageConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BlobStorageProviderFactory> _logger;

    public BlobStorageProviderFactory(IOptions<StorageConfiguration> options, ILoggerFactory loggerFactory)
    {
        _config = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BlobStorageProviderFactory>();
        
        _logger.LogDebug("BlobStorageProviderFactory initialized with {ConfigCount} storage configurations", 
            _config.StorageAccounts.Count);
    }

    public IBlobStorageProvider GetProvider(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("Configuration name cannot be null or empty", nameof(configName));

        // Check if we already have a cached provider
        if (_providers.TryGetValue(configName, out var existingProvider))
        {
            return existingProvider;
        }

        // Check if the configuration exists
        if (!_config.StorageAccounts.TryGetValue(configName, out var config))
        {
            var availableConfigs = string.Join(", ", _config.StorageAccounts.Keys);
            throw new ArgumentException(
                $"Storage configuration '{configName}' not found. Available configurations: [{availableConfigs}]");
        }

        _logger.LogDebug("Creating new blob storage provider for configuration '{ConfigName}'", configName);

        // Create BlobServiceClient based on authentication method
        var blobServiceClient = CreateBlobServiceClient(config);
        
        // Create the provider
        var providerLogger = _loggerFactory.CreateLogger<BlobStorageProvider>();
        var provider = new BlobStorageProvider(blobServiceClient, providerLogger);
        
        // Cache it for future use
        _providers[configName] = provider;
        
        _logger.LogInformation("Created blob storage provider for '{ConfigName}' using {AuthMethod}", 
            configName, config.AuthenticationMethod);
        
        return provider;
    }

    private BlobServiceClient CreateBlobServiceClient(StorageAccountConfig config)
    {
        return config.AuthenticationMethod.ToLowerInvariant() switch
        {
            "connectionstring" => new BlobServiceClient(config.ConnectionString),
            
            "systemmanaged" or "systemmanagedidentity" => 
                new BlobServiceClient(new Uri($"https://{config.AccountName}.blob.core.windows.net"), new DefaultAzureCredential()),
                
            "usermanaged" or "usermanagedidentity" => 
                new BlobServiceClient(new Uri($"https://{config.AccountName}.blob.core.windows.net"), 
                    new ManagedIdentityCredential(config.UserManagedIdentityClientId)),
                    
            _ => throw new ArgumentException(
                $"Unsupported authentication method: {config.AuthenticationMethod}. " +
                "Supported methods: ConnectionString, SystemManagedIdentity, UserManagedIdentity")
        };
    }
}
