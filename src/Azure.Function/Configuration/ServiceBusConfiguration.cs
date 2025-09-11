namespace Azure.Function.Configuration;

/// <summary>
/// Simplified Service Bus configuration using connection string only
/// </summary>
public class ServiceBusConfiguration
{
    /// <summary>
    /// Connection string for Service Bus
    /// </summary>
    public required string ServiceBusConnection { get; set; }
    
    /// <summary>
    /// Status topic name
    /// </summary>
    public required string StatusTopicName { get; set; }
    
    /// <summary>
    /// Notification topic name
    /// </summary>
    public required string NotificationTopicName { get; set; }
}

