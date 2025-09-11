using Azure.Function.Models;

namespace Azure.Function.Services;

/// <summary>
/// Generic notification service that can handle any type of notification event
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send any notification event that implements INotificationEvent
    /// </summary>
    Task SendNotificationAsync<T>(T notificationEvent, CancellationToken cancellationToken = default) where T : INotificationEvent;
    
    /// <summary>
    /// Send a notification event without generic constraints (for dynamic scenarios)
    /// </summary>
    Task SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Document-specific extension methods for backwards compatibility and convenience
/// </summary>
public static class DocumentNotificationServiceExtensions
{
    public static Task SendSubmittedNotificationAsync(this INotificationService service, 
        string requestId, DocumentRequest request, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateSubmitted(requestId, request), cancellationToken);
        
    public static Task SendCompletedNotificationAsync(this INotificationService service,
        RequestTrackingEntity trackingEntity, ProcessingStatus status, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateCompleted(trackingEntity, status), cancellationToken);
        
    public static Task SendFailedNotificationAsync(this INotificationService service,
        RequestTrackingEntity trackingEntity, string error, string? errorCode = null, CancellationToken cancellationToken = default)
        => service.SendNotificationAsync(DocumentStatusEvent.CreateFailed(trackingEntity, error, errorCode), cancellationToken);
}
