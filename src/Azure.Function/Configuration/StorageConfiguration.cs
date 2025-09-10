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
    /// <summary>
    /// Connection string for the storage account (legacy/local dev support)
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Storage account name (required for managed identity)
    /// </summary>
    public required string AccountName { get; set; }
    
    /// <summary>
    /// Authentication method: "ConnectionString", "SystemManagedIdentity", "UserManagedIdentity"
    /// </summary>
    public required string AuthenticationMethod { get; set; }
    
    /// <summary>
    /// Client ID for User-Managed Identity (required when AuthenticationMethod is "UserManagedIdentity")
    /// </summary>
    public string? UserManagedIdentityClientId { get; set; }
}

