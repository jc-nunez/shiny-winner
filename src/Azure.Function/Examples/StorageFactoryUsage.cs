using Azure.Function.Providers.Storage;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Examples;

/// <summary>
/// Examples showing how to use the BlobStorageProviderFactory for multi-account scenarios
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
    /// Example 1: Basic usage with purpose-based providers
    /// </summary>
    public async Task BasicUsageExample()
    {
        // Get provider for source storage account
        var sourceProvider = _storageFactory.GetProviderByPurpose("source");
        
        // Read from source storage account
        var document = await sourceProvider.ReadBlobAsync("uploads", "document.pdf");
        var metadata = await sourceProvider.ReadBlobMetadataAsync("uploads", "document.pdf");

        // Get provider for destination storage account
        var destinationProvider = _storageFactory.GetProviderByPurpose("destination");
        
        // Upload to destination storage account
        await destinationProvider.UploadBlobAsync("processed", "document.pdf", document, metadata);
        
        _logger.LogInformation("Document transferred from {SourceAccount} to {DestinationAccount}",
            sourceProvider.StorageAccountId, destinationProvider.StorageAccountId);
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
        var sharedDestination = _storageFactory.GetProviderByPurpose("destination");
        await sharedDestination.UploadBlobAsync("shared-processed", "customer-a-invoice.pdf", docA, new Dictionary<string, string>());
        await sharedDestination.UploadBlobAsync("shared-processed", "customer-b-receipt.pdf", docB, new Dictionary<string, string>());
        
        _logger.LogInformation("Processed documents from multiple customers to shared destination");
    }

    /// <summary>
    /// Example 3: Container-based routing
    /// </summary>
    public async Task ContainerBasedRoutingExample()
    {
        // Let the factory resolve which storage account based on container name
        var containerA = "customer-a-uploads";
        var providerA = _storageFactory.ResolveProviderForContainer(containerA);
        
        var containerB = "customer-b-uploads"; 
        var providerB = _storageFactory.ResolveProviderForContainer(containerB);
        
        _logger.LogInformation("Container {ContainerA} routed to storage account {AccountA}",
            containerA, providerA.StorageAccountId);
        _logger.LogInformation("Container {ContainerB} routed to storage account {AccountB}", 
            containerB, providerB.StorageAccountId);
    }

    /// <summary>
    /// Example 4: List all configured storage accounts
    /// </summary>
    public void ListStorageAccountsExample()
    {
        var accountIds = _storageFactory.GetConfiguredStorageAccountIds();
        var providers = _storageFactory.GetAllProviders();

        _logger.LogInformation("Configured storage accounts:");
        foreach (var provider in providers)
        {
            _logger.LogInformation("- {AccountId}: {Purpose} ({Description})",
                provider.StorageAccountId, 
                provider.Purpose,
                "Storage account for " + provider.Purpose);
        }
    }
}
