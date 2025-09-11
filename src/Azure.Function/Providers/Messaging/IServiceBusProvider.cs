using Azure.Function.Models;

namespace Azure.Function.Providers.Messaging;

/// <summary>
/// Contract for Service Bus messaging provider that handles document processing notifications and events.
/// Provides methods for sending messages to Service Bus topics with JSON serialization.
/// </summary>
/// <remarks>
/// This interface abstracts Service Bus messaging operations, supporting both generic message
/// sending and specific notification delivery for the document processing workflow.
/// </remarks>
public interface IServiceBusProvider
{
    /// <summary>
    /// Sends a strongly-typed message to the specified Service Bus topic.
    /// </summary>
    /// <typeparam name="T">Type of message to send (must be a reference type).</typeparam>
    /// <param name="message">Message object to serialize and send.</param>
    /// <param name="topicName">Name of the Service Bus topic to send to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown for Service Bus operation failures.</exception>
    /// <exception cref="ArgumentNullException">Thrown if message or topicName is null.</exception>
    /// <remarks>
    /// Messages are JSON serialized with camelCase naming policy for consistency.
    /// Each message receives a unique MessageId and Subject based on the type name.
    /// </remarks>
    Task SendMessageAsync<T>(T message, string topicName, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Sends a document processing status notification to the configured status topic.
    /// </summary>
    /// <param name="notification">Status notification containing request details and current status.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown for Service Bus operation failures.</exception>
    /// <exception cref="ArgumentNullException">Thrown if notification is null.</exception>
    /// <remarks>
    /// Convenience method that sends the notification to the pre-configured StatusTopicName.
    /// Used primarily by the monitoring function for status change notifications.
    /// </remarks>
    Task SendNotificationAsync(StatusNotification notification, CancellationToken cancellationToken = default);
}
