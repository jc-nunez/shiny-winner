using Microsoft.Extensions.Logging;
using Azure.Function.Models;
using Azure.Function.Providers.Messaging;

namespace Azure.Function.Services;

/// <summary>
/// Generic notification service that can handle any type of notification event
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IServiceBusProvider serviceBusProvider,
        ILogger<NotificationService> logger)
    {
        _serviceBusProvider = serviceBusProvider;
        _logger = logger;
    }

    public async Task SendNotificationAsync<T>(T notificationEvent, CancellationToken cancellationToken = default) where T : INotificationEvent
    {
        await SendNotificationInternalAsync(notificationEvent, cancellationToken);
    }

    public async Task SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        await SendNotificationInternalAsync(notificationEvent, cancellationToken);
    }

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
