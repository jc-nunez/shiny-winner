namespace Azure.Function.Configuration;

/// <summary>
/// Simplified storage configuration for the document processing function app
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// Connection string for source blob storage (where documents are uploaded)
    /// </summary>
    public required string SourceStorageConnection { get; set; }
    
    /// <summary>
    /// Connection string for destination blob storage (where processed documents go)
    /// </summary>
    public required string DestinationStorageConnection { get; set; }
    
    /// <summary>
    /// Connection string for table storage (for tracking document requests)
    /// </summary>
    public required string TableStorageConnection { get; set; }
}

/// <summary>
/// Optional: Future-ready configuration with managed identity support
/// Only add this if/when you migrate away from connection strings
/// </summary>
public class ManagedIdentityStorageConfiguration
{
    /// <summary>
    /// Source storage account name for managed identity authentication
    /// </summary>
    public required string SourceAccountName { get; set; }
    
    /// <summary>
    /// Destination storage account name for managed identity authentication
    /// </summary>
    public required string DestinationAccountName { get; set; }
    
    /// <summary>
    /// Table storage account name for managed identity authentication
    /// </summary>
    public required string TableAccountName { get; set; }
    
    /// <summary>
    /// Whether to use managed identity (true) or connection strings (false)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;
    
    /// <summary>
    /// Optional: Client ID for user-managed identity
    /// Leave null for system-managed identity
    /// </summary>
    public string? UserManagedIdentityClientId { get; set; }
}
