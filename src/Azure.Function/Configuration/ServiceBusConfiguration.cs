namespace Azure.Function.Configuration;

public class ServiceBusConfiguration
{
    /// <summary>
    /// Connection string for Service Bus (legacy/local dev support)
    /// </summary>
    public string? ServiceBusConnection { get; set; }
    
    /// <summary>
    /// Service Bus namespace (required for managed identity)
    /// </summary>
    public required string Namespace { get; set; }
    
    /// <summary>
    /// Authentication method: "ConnectionString", "SystemManagedIdentity", "UserManagedIdentity"
    /// </summary>
    public required string AuthenticationMethod { get; set; }
    
    /// <summary>
    /// Client ID for User-Managed Identity (required when AuthenticationMethod is "UserManagedIdentity")
    /// </summary>
    public string? UserManagedIdentityClientId { get; set; }
    
    /// <summary>
    /// Status topic name
    /// </summary>
    public required string StatusTopicName { get; set; }
    
    /// <summary>
    /// Notification topic name
    /// </summary>
    public required string NotificationTopicName { get; set; }
}

