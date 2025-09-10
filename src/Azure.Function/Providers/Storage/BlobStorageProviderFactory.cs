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

        // Initialize legacy storage accounts (for backward compatibility)
        AddLegacyProvider("source", "source", _config.SourceStorageConnection, singleProviderLogger);
        AddLegacyProvider("destination", "destination", _config.DestinationStorageConnection, singleProviderLogger);

        // Initialize configured storage accounts
        foreach (var kvp in _config.StorageAccounts)
        {
            var accountId = kvp.Key;
            var accountConfig = kvp.Value;
            
            // Don't override legacy accounts if they have the same ID
            if (!_providers.ContainsKey(accountId))
            {
                AddProvider(accountId, accountConfig, singleProviderLogger);
            }
        }

        _logger.LogInformation("BlobStorageProviderFactory initialized with {ProviderCount} storage account providers", 
            _providers.Count);
    }

    private void AddLegacyProvider(string storageAccountId, string purpose, string connectionString, ILogger<SingleStorageAccountProvider> logger)
    {
        // Extract account name from connection string for backward compatibility
        var accountName = ExtractAccountNameFromConnectionString(connectionString);
        
        var provider = new SingleStorageAccountProvider(
            storageAccountId, 
            purpose, 
            accountName,
            "ConnectionString",
            connectionString,
            null,
            logger);
        
        _providers[storageAccountId] = provider;
        _providersByPurpose[purpose] = provider;
        
        _logger.LogDebug("Added legacy storage provider for account {StorageAccountId} with purpose {Purpose}", 
            storageAccountId, purpose);
    }

    private void AddProvider(string storageAccountId, StorageAccountConfig accountConfig, ILogger<SingleStorageAccountProvider> logger)
    {
        var provider = new SingleStorageAccountProvider(
            storageAccountId,
            accountConfig.Purpose,
            accountConfig.AccountName,
            accountConfig.AuthenticationMethod,
            accountConfig.ConnectionString,
            accountConfig.UserManagedIdentityClientId,
            logger);
        
        _providers[storageAccountId] = provider;
        
        // Map by purpose (last one wins if multiple accounts have the same purpose)
        _providersByPurpose[accountConfig.Purpose] = provider;
        
        _logger.LogDebug("Added storage provider for account {StorageAccountId} with purpose {Purpose} using {AuthMethod}", 
            storageAccountId, accountConfig.Purpose, accountConfig.AuthenticationMethod);
    }

    private static string ExtractAccountNameFromConnectionString(string connectionString)
    {
        // Parse connection string to extract AccountName
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals("AccountName", StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }
        
        throw new ArgumentException($"Could not extract AccountName from connection string: {connectionString}");
    }

    public ISingleStorageAccountProvider GetProvider(string configurationName)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
            throw new ArgumentException("Configuration name cannot be null or empty", nameof(configurationName));

        _logger.LogDebug("Resolving storage provider for configuration name: {ConfigurationName}", configurationName);

        // Strategy 1: Direct storage account ID lookup
        if (_providers.TryGetValue(configurationName, out var directProvider))
        {
            _logger.LogDebug("Found direct provider for account ID: {AccountId}", configurationName);
            return directProvider;
        }

        // Strategy 2: Purpose-based lookup
        if (_providersByPurpose.TryGetValue(configurationName, out var purposeProvider))
        {
            _logger.LogDebug("Found provider by purpose: {Purpose} -> {AccountId}", 
                configurationName, purposeProvider.StorageAccountId);
            return purposeProvider;
        }

        // Strategy 3: Container-to-account mapping
        if (_config.ContainerToAccountMapping.TryGetValue(configurationName, out var mappedAccountId))
        {
            _logger.LogDebug("Found container mapping: {ContainerName} -> {AccountId}", 
                configurationName, mappedAccountId);
            
            if (_providers.TryGetValue(mappedAccountId, out var mappedProvider))
            {
                return mappedProvider;
            }
            
            _logger.LogWarning("Container {ContainerName} mapped to account {AccountId}, but account not found", 
                configurationName, mappedAccountId);
        }

        // Strategy 4: Container name pattern inference (fallback)
        if (IsContainerName(configurationName))
        {
            _logger.LogDebug("Attempting pattern-based container resolution for: {ContainerName}", configurationName);
            
            if (configurationName.Contains("source") || configurationName.Contains("input") || configurationName.Contains("upload"))
            {
                if (_providersByPurpose.TryGetValue("source", out var sourceProvider))
                {
                    _logger.LogDebug("Inferred source provider for container: {ContainerName}", configurationName);
                    return sourceProvider;
                }
            }

            if (configurationName.Contains("destination") || configurationName.Contains("output") || configurationName.Contains("processed"))
            {
                if (_providersByPurpose.TryGetValue("destination", out var destProvider))
                {
                    _logger.LogDebug("Inferred destination provider for container: {ContainerName}", configurationName);
                    return destProvider;
                }
            }

            // Default fallback for container names
            if (_providersByPurpose.TryGetValue("source", out var fallbackProvider))
            {
                _logger.LogWarning("Container {ContainerName} not explicitly mapped, defaulting to source storage account", 
                    configurationName);
                return fallbackProvider;
            }
        }

        // If all strategies fail, throw an exception with helpful information
        var availableAccounts = string.Join(", ", _providers.Keys);
        var availablePurposes = string.Join(", ", _providersByPurpose.Keys);
        var availableContainers = string.Join(", ", _config.ContainerToAccountMapping.Keys);
        
        throw new ArgumentException(
            $"Cannot resolve storage provider for '{configurationName}'. " +
            $"Available accounts: [{availableAccounts}]. " +
            $"Available purposes: [{availablePurposes}]. " +
            $"Mapped containers: [{availableContainers}]");
    }

    private static bool IsContainerName(string name)
    {
        // Simple heuristic: container names are typically lowercase and may contain hyphens
        // This is a rough approximation - adjust based on your naming conventions
        return name.Length > 2 && 
               name.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-' || c == '_') &&
               !name.All(char.IsLetter); // Not just letters (likely not a simple purpose name)
    }
}
