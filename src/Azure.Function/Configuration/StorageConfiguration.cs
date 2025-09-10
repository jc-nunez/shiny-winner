namespace Azure.Function.Configuration;

public class StorageConfiguration
{
    // Legacy properties for backward compatibility
    public required string SourceStorageConnection { get; set; }
    public required string DestinationStorageConnection { get; set; }
    public required string TableStorageConnection { get; set; }
    
    // Enhanced storage account configurations
    public Dictionary<string, StorageAccountConfig> StorageAccounts { get; set; } = new();
    public Dictionary<string, string> ContainerToAccountMapping { get; set; } = new();
    
    // Default account identifiers
    public string DefaultSourceAccount { get; set; } = "source";
    public string DefaultDestinationAccount { get; set; } = "destination";
}

public class StorageAccountConfig
{
    public required string ConnectionString { get; set; }
    public required string Purpose { get; set; } // "source", "destination", "processing", etc.
    public string? Description { get; set; }
}

