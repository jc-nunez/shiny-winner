using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Models;

namespace Azure.Function.Providers.Messaging;

/// <summary>
/// Service Bus messaging provider for document processing status notifications and events.
/// Uses simplified connection string authentication with JSON message serialization.
/// </summary>
/// <remarks>
/// This provider handles all Service Bus messaging for the document processing workflow,
/// including status notifications and workflow events. Messages are JSON serialized with
/// camelCase naming policy for consistency.
/// </remarks>
public class ServiceBusProvider : IServiceBusProvider, IDisposable
{
    /// <summary>
    /// Azure Service Bus client for sending messages.
    /// </summary>
    private readonly ServiceBusClient _client;
    
    /// <summary>
    /// Service Bus configuration containing connection string and topic names.
    /// </summary>
    private readonly ServiceBusConfiguration _config;
    
    /// <summary>
    /// Logger for tracking messaging operations and troubleshooting.
    /// </summary>
    private readonly ILogger<ServiceBusProvider> _logger;
    
    /// <summary>
    /// JSON serialization options for consistent message formatting.
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the ServiceBusProvider.
    /// </summary>
    /// <param name="options">Service Bus configuration containing connection string and topic names.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    /// <exception cref="ArgumentException">Thrown if ServiceBusConnection is null or empty.</exception>
    /// <remarks>
    /// Creates a ServiceBusClient using connection string authentication only.
    /// Configures JSON serialization with camelCase naming for message consistency.
    /// </remarks>
    public ServiceBusProvider(IOptions<ServiceBusConfiguration> options, ILogger<ServiceBusProvider> logger)
    {
        _config = options.Value;
        _logger = logger;
        
        if (string.IsNullOrWhiteSpace(_config.ServiceBusConnection))
            throw new ArgumentException("Service Bus connection string is required");
            
        _logger.LogDebug("Creating ServiceBusClient using connection string");
        _client = new ServiceBusClient(_config.ServiceBusConnection);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Sends a strongly-typed message to the specified Service Bus topic.
    /// </summary>
    /// <typeparam name="T">Type of message to send.</typeparam>
    /// <param name="message">Message object to serialize and send.</param>
    /// <param name="topicName">Name of the Service Bus topic.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown for Service Bus operation failures.</exception>
    /// <remarks>
    /// Messages are JSON serialized with camelCase naming. Each message gets a unique
    /// MessageId and the Subject is set to the type name for routing purposes.
    /// The sender is disposed after each operation.
    /// </remarks>
    public async Task SendMessageAsync<T>(T message, string topicName, CancellationToken cancellationToken = default) where T : class
    {
        ServiceBusSender? sender = null;
        try
        {
            sender = _client.CreateSender(topicName);
            
            var messageBody = JsonSerializer.Serialize(message, _jsonOptions);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(T).Name
            };

            _logger.LogInformation("Sending message of type {MessageType} to topic {TopicName}", typeof(T).Name, topicName);
            
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger.LogInformation("Successfully sent message {MessageId} to topic {TopicName}", serviceBusMessage.MessageId, topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message of type {MessageType} to topic {TopicName}", typeof(T).Name, topicName);
            throw;
        }
        finally
        {
            if (sender != null)
            {
                await sender.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Sends a document processing status notification to the configured status topic.
    /// </summary>
    /// <param name="notification">Status notification containing request ID and status details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown for Service Bus operation failures.</exception>
    /// <remarks>
    /// Convenience method that sends the notification to the configured StatusTopicName.
    /// Used by the monitoring function to notify about processing status changes.
    /// </remarks>
    public async Task SendNotificationAsync(StatusNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending status notification for request {RequestId} with status {Status}", 
                notification.RequestId, notification.Status);
            
            await SendMessageAsync(notification, _config.StatusTopicName, cancellationToken);
            
            _logger.LogInformation("Successfully sent status notification for request {RequestId}", notification.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status notification for request {RequestId}", notification.RequestId);
            throw;
        }
    }

    /// <summary>
    /// Disposes the ServiceBusClient and releases associated resources.
    /// </summary>
    /// <remarks>
    /// Called automatically when the provider is disposed. Ensures proper cleanup
    /// of Service Bus connections and resources.
    /// </remarks>
    public void Dispose()
    {
        _client?.DisposeAsync().GetAwaiter().GetResult();
    }
}
