using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating and managing blob storage providers for different storage accounts
/// </summary>
public class BlobStorageProviderFactory : IBlobStorageProviderFactory
{
    private readonly Dictionary<string, ISingleStorageAccountProvider> _providers;
    private readonly Dictionary<string, ISingleStorageAccountProvider> _providersByPurpose;
    private readonly StorageConfiguration _config;
    private readonly ILogger<BlobStorageProviderFactory> _logger;

    public BlobStorageProviderFactory(IOptions<StorageConfiguration> options, ILoggerFactory loggerFactory)
    {
        _config = options.Value;
        _logger = loggerFactory.CreateLogger<BlobStorageProviderFactory>();
        _providers = new Dictionary<string, ISingleStorageAccountProvider>();
        _providersByPurpose = new Dictionary<string, ISingleStorageAccountProvider>();

        InitializeProviders(loggerFactory);
    }

    private void InitializeProviders(ILoggerFactory loggerFactory)
    {
        var singleProviderLogger = loggerFactory.CreateLogger<SingleStorageAccountProvider>();

        // Initialize legacy storage accounts
        AddProvider("source", "source", _config.SourceStorageConnection, singleProviderLogger);
        AddProvider("destination", "destination", _config.DestinationStorageConnection, singleProviderLogger);

        // Initialize configured storage accounts
        foreach (var kvp in _config.StorageAccounts)
        {
            var accountId = kvp.Key;
            var accountConfig = kvp.Value;
            
            // Don't override legacy accounts if they have the same ID
            if (!_providers.ContainsKey(accountId))
            {
                AddProvider(accountId, accountConfig.Purpose, accountConfig.ConnectionString, singleProviderLogger);
            }
        }

        _logger.LogInformation("BlobStorageProviderFactory initialized with {ProviderCount} storage account providers", 
            _providers.Count);
    }

    private void AddProvider(string storageAccountId, string purpose, string connectionString, ILogger<SingleStorageAccountProvider> logger)
    {
        var provider = new SingleStorageAccountProvider(storageAccountId, purpose, connectionString, logger);
        
        _providers[storageAccountId] = provider;
        
        // Map by purpose (last one wins if multiple accounts have the same purpose)
        _providersByPurpose[purpose] = provider;
        
        _logger.LogDebug("Added storage provider for account {StorageAccountId} with purpose {Purpose}", 
            storageAccountId, purpose);
    }

    public ISingleStorageAccountProvider GetProvider(string storageAccountId)
    {
        if (string.IsNullOrWhiteSpace(storageAccountId))
            throw new ArgumentException("Storage account ID cannot be null or empty", nameof(storageAccountId));

        if (_providers.TryGetValue(storageAccountId, out var provider))
        {
            return provider;
        }

        throw new ArgumentException($"Storage account '{storageAccountId}' is not configured. Available accounts: {string.Join(", ", _providers.Keys)}");
    }

    public ISingleStorageAccountProvider GetProviderByPurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Purpose cannot be null or empty", nameof(purpose));

        if (_providersByPurpose.TryGetValue(purpose, out var provider))
        {
            return provider;
        }

        throw new ArgumentException($"No storage account configured for purpose '{purpose}'. Available purposes: {string.Join(", ", _providersByPurpose.Keys)}");
    }

    public ISingleStorageAccountProvider ResolveProviderForContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        // First, check if there's an explicit mapping for this container
        if (_config.ContainerToAccountMapping.TryGetValue(containerName, out var mappedAccountId))
        {
            return GetProvider(mappedAccountId);
        }

        // If no explicit mapping, try to infer from container name conventions
        // This is a fallback strategy - you might want to customize this logic
        if (containerName.Contains("source") || containerName.Contains("input") || containerName.Contains("upload"))
        {
            return GetProviderByPurpose("source");
        }

        if (containerName.Contains("destination") || containerName.Contains("output") || containerName.Contains("processed"))
        {
            return GetProviderByPurpose("destination");
        }

        // Default to source if no pattern matches
        _logger.LogWarning("Container {ContainerName} not explicitly mapped, defaulting to source storage account", containerName);
        return GetProviderByPurpose("source");
    }

    public IEnumerable<string> GetConfiguredStorageAccountIds()
    {
        return _providers.Keys;
    }

    public IEnumerable<ISingleStorageAccountProvider> GetAllProviders()
    {
        return _providers.Values;
    }
}
