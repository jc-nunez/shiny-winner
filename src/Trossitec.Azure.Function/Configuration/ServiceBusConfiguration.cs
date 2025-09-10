namespace Trossitec.Azure.Function.Configuration;

public class ServiceBusConfiguration
{
    public required string ServiceBusConnection { get; set; }
    public required string StatusTopicName { get; set; }
    public required string NotificationTopicName { get; set; }
}

