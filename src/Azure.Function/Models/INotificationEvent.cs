namespace Azure.Function.Models;

/// <summary>
/// Base interface for all notification events
/// </summary>
public interface INotificationEvent
{
    /// <summary>
    /// Unique identifier for the event (e.g., RequestId, UserId, BatchId, etc.)
    /// </summary>
    string EventId { get; }
    
    /// <summary>
    /// Type/Status of the event (e.g., "Submitted", "Completed", "Failed", "UserLoggedIn", etc.)
    /// </summary>
    string EventType { get; }
    
    /// <summary>
    /// Human-readable message describing the event
    /// </summary>
    string Message { get; }
    
    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Additional event-specific data
    /// </summary>
    object? Details { get; }
}
