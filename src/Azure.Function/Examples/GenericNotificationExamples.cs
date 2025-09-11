using Azure.Function.Models;
using Azure.Function.Services;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Examples;

/// <summary>
/// Examples demonstrating the truly generic notification service
/// that can handle ANY type of notification event
/// </summary>
public class GenericNotificationExamples
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<GenericNotificationExamples> _logger;

    public GenericNotificationExamples(INotificationService notificationService, ILogger<GenericNotificationExamples> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Document processing events (our existing use case)
    /// </summary>
    public async Task DocumentProcessingEventsAsync()
    {
        // Still works exactly as before with document events
        var docEvent = DocumentStatusEvent.CreateCompleted(
            new RequestTrackingEntity { RowKey = "doc-123", BlobName = "contract.pdf", ApiGeneratedKey = "api-456" },
            new ProcessingStatus { RequestId = "api-456", Status = "Completed", Result = new { extractedText = "Contract details..." } }
        );

        await _notificationService.SendNotificationAsync(docEvent);
    }

    /// <summary>
    /// Example 2: User authentication events (completely different domain)
    /// </summary>
    public async Task UserAuthenticationEventsAsync()
    {
        var loginEvent = new UserAuthenticationEvent
        {
            UserId = "user-789",
            Action = "Login",
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0...",
            Success = true,
            Timestamp = DateTime.UtcNow
        };

        // Same service, different event type - truly generic!
        await _notificationService.SendNotificationAsync(loginEvent);

        var logoutEvent = new UserAuthenticationEvent
        {
            UserId = "user-789",
            Action = "Logout",
            IpAddress = "192.168.1.100",
            SessionDuration = TimeSpan.FromHours(2),
            Success = true,
            Timestamp = DateTime.UtcNow
        };

        await _notificationService.SendNotificationAsync(logoutEvent);
    }

    /// <summary>
    /// Example 3: System monitoring events
    /// </summary>
    public async Task SystemMonitoringEventsAsync()
    {
        var healthCheckEvent = new SystemHealthEvent
        {
            ServiceName = "DocumentExtractionApi",
            Status = "Healthy",
            ResponseTime = TimeSpan.FromMilliseconds(150),
            Timestamp = DateTime.UtcNow
        };

        await _notificationService.SendNotificationAsync(healthCheckEvent);

        var errorEvent = new SystemHealthEvent
        {
            ServiceName = "BlobStorage",
            Status = "Degraded",
            ResponseTime = TimeSpan.FromSeconds(5),
            ErrorMessage = "High latency detected",
            Timestamp = DateTime.UtcNow
        };

        await _notificationService.SendNotificationAsync(errorEvent);
    }

    /// <summary>
    /// Example 4: Business workflow events
    /// </summary>
    public async Task BusinessWorkflowEventsAsync()
    {
        var orderEvent = new OrderProcessingEvent
        {
            OrderId = "order-12345",
            CustomerId = "customer-67890",
            Stage = "PaymentProcessed",
            Amount = 299.99m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        await _notificationService.SendNotificationAsync(orderEvent);
    }
}

#region Example Event Types - Showing the generic nature

/// <summary>
/// User authentication event - completely different from document processing
/// </summary>
public class UserAuthenticationEvent : INotificationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Login, Logout, PasswordChange, etc.
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan? SessionDuration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // INotificationEvent implementation
    public string EventId => UserId;
    public string EventType => Action;
    public string Message => Success 
        ? $"User {UserId} {Action.ToLower()} successful"
        : $"User {UserId} {Action.ToLower()} failed";
    
    public object? Details => new
    {
        IpAddress,
        UserAgent,
        Success,
        SessionDuration,
        AuthenticationMethod = "OAuth2"
    };
}

/// <summary>
/// System health monitoring event
/// </summary>
public class SystemHealthEvent : INotificationEvent
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Healthy, Degraded, Unhealthy
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // INotificationEvent implementation
    public string EventId => $"{ServiceName}-{Timestamp:yyyyMMddHHmmss}";
    public string EventType => $"HealthCheck.{Status}";
    public string Message => string.IsNullOrEmpty(ErrorMessage)
        ? $"Service {ServiceName} is {Status.ToLower()} (response time: {ResponseTime.TotalMilliseconds}ms)"
        : $"Service {ServiceName} is {Status.ToLower()}: {ErrorMessage}";

    public object? Details => new
    {
        ServiceName,
        Status,
        ResponseTimeMs = ResponseTime.TotalMilliseconds,
        ErrorMessage,
        CheckType = "Automated"
    };
}

/// <summary>
/// Business order processing event
/// </summary>
public class OrderProcessingEvent : INotificationEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty; // OrderPlaced, PaymentProcessed, Shipped, etc.
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // INotificationEvent implementation
    public string EventId => OrderId;
    public string EventType => $"Order.{Stage}";
    public string Message => $"Order {OrderId} has reached stage: {Stage}";

    public object? Details => new
    {
        OrderId,
        CustomerId,
        Stage,
        Amount,
        Currency,
        ProcessedBy = "OrderProcessingService"
    };
}

#endregion

/// <summary>
/// Extension methods for specific event types (optional convenience)
/// </summary>
public static class GenericNotificationServiceExtensions
{
    public static Task SendUserLoginAsync(this INotificationService service, 
        string userId, string ipAddress, bool success, CancellationToken cancellationToken = default)
    {
        var loginEvent = new UserAuthenticationEvent
        {
            UserId = userId,
            Action = "Login",
            IpAddress = ipAddress,
            Success = success,
            Timestamp = DateTime.UtcNow
        };

        return service.SendNotificationAsync(loginEvent, cancellationToken);
    }

    public static Task SendSystemHealthCheckAsync(this INotificationService service,
        string serviceName, string status, TimeSpan responseTime, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var healthEvent = new SystemHealthEvent
        {
            ServiceName = serviceName,
            Status = status,
            ResponseTime = responseTime,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        return service.SendNotificationAsync(healthEvent, cancellationToken);
    }

    public static Task SendOrderStageUpdateAsync(this INotificationService service,
        string orderId, string customerId, string stage, decimal amount, CancellationToken cancellationToken = default)
    {
        var orderEvent = new OrderProcessingEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            Stage = stage,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        return service.SendNotificationAsync(orderEvent, cancellationToken);
    }
}
