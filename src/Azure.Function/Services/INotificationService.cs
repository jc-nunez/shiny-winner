using Azure.Function.Models;

namespace Azure.Function.Services;

/// <summary>
/// Contract for notification service that handles document processing event notifications.
/// Provides methods for sending various types of notification events via Service Bus messaging.
/// </summary>
/// <remarks>
/// This interface abstracts notification delivery mechanisms and provides a consistent
/// way to send document processing status updates to external systems.
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Sends a strongly-typed notification event to the configured messaging system.
    /// </summary>
    /// <typeparam name="T">Type of notification event implementing INotificationEvent.</typeparam>
    /// <param name="notificationEvent">Notification event to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown if message delivery fails.</exception>
    Task SendNotificationAsync<T>(T notificationEvent, CancellationToken cancellationToken = default) where T : INotificationEvent;
    
    /// <summary>
    /// Sends a notification event without generic type constraints for dynamic scenarios.
    /// </summary>
    /// <param name="notificationEvent">Notification event implementing INotificationEvent.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceBusException">Thrown if message delivery fails.</exception>
    Task SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods providing convenient shortcuts for common document processing notifications.
/// </summary>
/// <remarks>
/// These methods provide strongly-typed convenience methods for common notification scenarios
/// in the document processing workflow, reducing boilerplate code in calling services.
/// </remarks>
public static class DocumentNotificationServiceExtensions
{
    /// <summary>
    /// Sends a document submitted notification for a newly processed document request.
    /// </summary>
    /// <param name="service">The notification service instance.</param>
    /// <param name="requestId">Unique identifier of the document request.</param>
    /// <param name="request">Original document request details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the notification sending operation.</returns>
    public static Task SendSubmittedNotificationAsync(this INotificationService service, 
        string requestId, DocumentRequest request, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateSubmitted(requestId, request), cancellationToken);
        
    /// <summary>
    /// Sends a document completed notification for a successfully processed document.
    /// </summary>
    /// <param name="service">The notification service instance.</param>
    /// <param name="trackingEntity">Tracking entity containing request details.</param>
    /// <param name="status">Processing status with completion details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the notification sending operation.</returns>
    public static Task SendCompletedNotificationAsync(this INotificationService service,
        RequestTrackingEntity trackingEntity, ProcessingStatus status, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateCompleted(trackingEntity, status), cancellationToken);
        
    /// <summary>
    /// Sends a document failed notification for a document that failed processing.
    /// </summary>
    /// <param name="service">The notification service instance.</param>
    /// <param name="trackingEntity">Tracking entity containing request details.</param>
    /// <param name="error">Error message describing the failure.</param>
    /// <param name="errorCode">Optional error code for categorization.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the notification sending operation.</returns>
    public static Task SendFailedNotificationAsync(this INotificationService service,
        RequestTrackingEntity trackingEntity, string error, string? errorCode = null, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateFailed(trackingEntity, error, errorCode), cancellationToken);
}
