using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Messaging;

namespace Azure.Function.Services;

/// <summary>
/// Service for sending document processing notifications via Service Bus messaging.
/// Converts notification events to Service Bus messages and handles delivery to configured topics.
/// </summary>
/// <remarks>
/// This service provides a unified interface for sending notifications about document processing
/// events to external systems via Service Bus. It converts various notification event types
/// into standardized StatusNotification messages for consistent messaging patterns.
/// </remarks>
public class NotificationService : INotificationService
{
    /// <summary>
    /// Service Bus provider for sending notification messages.
    /// </summary>
    private readonly IServiceBusProvider _serviceBusProvider;
    
    /// <summary>
    /// Logger for tracking notification operations and delivery status.
    /// </summary>
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the NotificationService.
    /// </summary>
    /// <param name="serviceBusProvider">Provider for Service Bus messaging operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public NotificationService(
        IServiceBusProvider serviceBusProvider,
        ILogger<NotificationService> logger)
    {
        _serviceBusProvider = serviceBusProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sends a strongly-typed notification event to the configured Service Bus topic.
    /// </summary>
    /// <typeparam name="T">Type of notification event implementing INotificationEvent.</typeparam>
    /// <param name="notificationEvent">Notification event to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown if Service Bus delivery fails.</exception>
    /// <remarks>
    /// Converts the notification event to a standardized StatusNotification format
    /// before sending via Service Bus for consistent message structure.
    /// </remarks>
    public async Task SendNotificationAsync<T>(T notificationEvent, CancellationToken cancellationToken = default) where T : INotificationEvent
    {
        await SendNotificationInternalAsync(notificationEvent, cancellationToken);
    }

    /// <summary>
    /// Sends a notification event to the configured Service Bus topic.
    /// </summary>
    /// <param name="notificationEvent">Notification event implementing INotificationEvent.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown if Service Bus delivery fails.</exception>
    /// <remarks>
    /// Non-generic overload for interface-based notification sending.
    /// Converts the event to StatusNotification format for consistent messaging.
    /// </remarks>
    public async Task SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        await SendNotificationInternalAsync(notificationEvent, cancellationToken);
    }

    /// <summary>
    /// Internal implementation for sending notification events via Service Bus.
    /// </summary>
    /// <param name="notificationEvent">Notification event to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown if Service Bus delivery fails.</exception>
    /// <remarks>
    /// Converts the notification event to StatusNotification format and sends it
    /// through the Service Bus provider with comprehensive error handling and logging.
    /// </remarks>
    private async Task SendNotificationInternalAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending {EventType} notification for event {EventId}", 
                notificationEvent.EventType, notificationEvent.EventId);

            // Convert INotificationEvent to StatusNotification for the messaging provider
            var notification = CreateStatusNotification(notificationEvent);

            await _serviceBusProvider.SendNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully sent {EventType} notification for event {EventId}", 
                notificationEvent.EventType, notificationEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {EventType} notification for event {EventId}", 
                notificationEvent.EventType, notificationEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Converts a notification event to the standardized StatusNotification format.
    /// </summary>
    /// <param name="notificationEvent">Notification event to convert.</param>
    /// <returns>StatusNotification with mapped properties from the source event.</returns>
    /// <remarks>
    /// Handles special case mapping for DocumentStatusEvent to extract BlobName.
    /// Provides consistent StatusNotification structure for all notification types.
    /// </remarks>
    private static StatusNotification CreateStatusNotification(INotificationEvent notificationEvent)
    {
        // Handle document-specific events by extracting BlobName if available
        var blobName = notificationEvent is DocumentStatusEvent docEvent ? docEvent.BlobName : notificationEvent.EventId;
        
        return new StatusNotification
        {
            RequestId = notificationEvent.EventId,
            Status = notificationEvent.EventType,
            BlobName = blobName,
            Timestamp = notificationEvent.Timestamp,
            Message = notificationEvent.Message,
            Details = notificationEvent.Details
        };
    }
}
