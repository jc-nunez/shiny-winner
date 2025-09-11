using Azure.Function.Providers.Storage;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Examples;

/// <summary>
/// Examples showing how to use the simplified BlobStorageProviderFactory
/// </summary>
public class StorageFactoryUsage
{
    private readonly IBlobStorageProviderFactory _storageFactory;
    private readonly ILogger<StorageFactoryUsage> _logger;

    public StorageFactoryUsage(IBlobStorageProviderFactory storageFactory, ILogger<StorageFactoryUsage> logger)
    {
        _storageFactory = storageFactory;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Basic usage with configuration-based providers
    /// </summary>
    public async Task BasicUsageExample()
    {
        // Get provider for source storage account
        var sourceProvider = _storageFactory.GetProvider("source");
        
        // Read from source storage account
        var document = await sourceProvider.ReadBlobAsync("uploads", "document.pdf");
        var metadata = await sourceProvider.ReadBlobMetadataAsync("uploads", "document.pdf");

        // Get provider for destination storage account
        var destinationProvider = _storageFactory.GetProvider("destination");
        
        // Upload to destination storage account
        await destinationProvider.UploadBlobAsync("processed", "document.pdf", document, metadata);
        
        _logger.LogInformation("Document transferred from source to destination storage accounts");
    }

    /// <summary>
    /// Example 2: Multi-customer scenario with specific storage accounts
    /// </summary>
    public async Task MultiCustomerExample()
    {
        // Customer A uploads to their own storage account
        var customerAProvider = _storageFactory.GetProvider("customer-a");
        var docA = await customerAProvider.ReadBlobAsync("customer-a-uploads", "invoice.pdf");

        // Customer B uploads to their own storage account  
        var customerBProvider = _storageFactory.GetProvider("customer-b");
        var docB = await customerBProvider.ReadBlobAsync("customer-b-uploads", "receipt.pdf");

        // Both get processed to the shared destination account
        var sharedDestination = _storageFactory.GetProvider("destination");
        await sharedDestination.UploadBlobAsync("shared-processed", "customer-a-invoice.pdf", docA, new Dictionary<string, string>());
        await sharedDestination.UploadBlobAsync("shared-processed", "customer-b-receipt.pdf", docB, new Dictionary<string, string>());
        
        _logger.LogInformation("Processed documents from multiple customers to shared destination");
    }

    /// <summary>
    /// Example 3: Using different storage configurations
    /// </summary>
    public async Task MultipleConfigurationsExample()
    {
        // Get providers for different configured storage accounts
        var configA = "customer-a";
        var providerA = _storageFactory.GetProvider(configA);
        
        var configB = "customer-b"; 
        var providerB = _storageFactory.GetProvider(configB);
        
        _logger.LogInformation("Configuration {ConfigA} resolved to provider", configA);
        _logger.LogInformation("Configuration {ConfigB} resolved to provider", configB);
    }

    /// <summary>
    /// Example 4: Using different storage configurations
    /// </summary>
    public async Task DifferentConfigurationsExample()
    {
        // Different configured storage accounts
        var sourceProvider = _storageFactory.GetProvider("source");
        var destProvider = _storageFactory.GetProvider("destination");
        var customerProvider = _storageFactory.GetProvider("customer-a");
        
        _logger.LogInformation("Source provider created successfully");
        _logger.LogInformation("Destination provider created successfully");
        _logger.LogInformation("Customer provider created successfully");
    }
}
