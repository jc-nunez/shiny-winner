using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Function.Providers.Storage;
using Azure.Function.Services;
using Azure.Function.Models;
using Azure.Function.Extensions;

namespace Azure.Function.Examples;

/// <summary>
/// Comprehensive examples showing the generic patterns implemented
/// </summary>
public class GenericPatternsUsage
{
    /// <summary>
    /// Example 1: Using Generic Repositories
    /// Shows how to work with any ITableEntity without domain-specific code
    /// </summary>
    public async Task GenericRepositoryExamplesAsync()
    {
        // These examples show how a service provider would use the generic repositories
        
        IServiceProvider serviceProvider = null!; // Would be injected
        
        // Get a repository for any entity type
        var userRepo = serviceProvider.GetRequiredService<IRepository<UserEntity>>();
        var orderRepo = serviceProvider.GetRequiredService<IRepository<OrderEntity>>();
        var documentRepo = serviceProvider.GetRequiredService<IRepository<RequestTrackingEntity>>();
        
        // All repositories provide the same generic interface
        await userRepo.UpsertAsync(new UserEntity { RowKey = "user-123", Email = "user@example.com" });
        await orderRepo.UpsertAsync(new OrderEntity { RowKey = "order-456", Amount = 99.99m });
        await documentRepo.UpsertAsync(new RequestTrackingEntity { RowKey = "req-789", BlobName = "document.pdf" });
        
        // Query with filters - same pattern for all entities
        var recentUsers = await userRepo.QueryAsync("CreatedAt gt datetime'2024-01-01T00:00:00Z'");
        var highValueOrders = await orderRepo.QueryAsync("Amount gt 100.0");
        var processingRequests = await documentRepo.QueryAsync("CurrentStatus eq 'Processing'");
        
        // Generic CRUD operations work the same for all entity types
        var user = await userRepo.GetByIdAsync("user-123");
        var order = await orderRepo.GetByIdAsync("order-456");
        var request = await documentRepo.GetByIdAsync("req-789");
        
        // Check existence
        var userExists = await userRepo.ExistsAsync("user-123");
        var orderExists = await orderRepo.ExistsAsync("order-456");
        
        // Count entities
        var userCount = await userRepo.CountAsync();
        var processingCount = await documentRepo.CountAsync("CurrentStatus eq 'Processing'");
    }

    /// <summary>
    /// Example 2: Using Generic Table Storage Factory
    /// Shows how to work with different tables dynamically
    /// </summary>
    public async Task TableStorageFactoryExamplesAsync()
    {
        IServiceProvider serviceProvider = null!; // Would be injected
        var factory = serviceProvider.GetRequiredService<ITableStorageFactory>();
        
        // Get providers for different tables
        var usersTable = factory.GetProvider("Users");
        var ordersTable = factory.GetProvider("Orders"); 
        var documentsTable = factory.GetProvider("Documents");
        
        // All providers use the same generic interface
        await usersTable.UpsertEntityAsync(new UserEntity { RowKey = "user-123" });
        await ordersTable.UpsertEntityAsync(new OrderEntity { RowKey = "order-456" });
        await documentsTable.UpsertEntityAsync(new RequestTrackingEntity { RowKey = "req-789" });
        
        // Get repositories with automatic table naming (UserEntity -> "User" table)
        var userRepo = factory.GetRepository<UserEntity>("Users"); // Explicit table
        var autoUserRepo = factory.GetRepository<UserEntity>(); // Auto-derived: "User" table
        
        // Both repositories work the same way
        var user1 = await userRepo.GetByIdAsync("user-123");
        var user2 = await autoUserRepo.GetByIdAsync("user-123");
    }

    /// <summary>
    /// Example 3: Using Generic Notification Service
    /// Shows how to send any type of notification event
    /// </summary>
    public async Task GenericNotificationExamplesAsync()
    {
        IServiceProvider serviceProvider = null!; // Would be injected
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        
        // Document processing events (existing use case)
        var docEvent = DocumentStatusEvent.CreateCompleted(
            new RequestTrackingEntity { RowKey = "req-123", BlobName = "contract.pdf" },
            new ProcessingStatus { RequestId = "req-123", Status = "Completed", Result = new { text = "Extracted content" } }
        );
        await notificationService.SendNotificationAsync(docEvent);
        
        // User authentication events (different domain)
        var loginEvent = new UserAuthenticationEvent
        {
            UserId = "user-456",
            Action = "Login",
            IpAddress = "192.168.1.100",
            Success = true
        };
        await notificationService.SendNotificationAsync(loginEvent);
        
        // System health events (another domain)
        var healthEvent = new SystemHealthEvent
        {
            ServiceName = "DocumentProcessingService",
            Status = "Healthy",
            ResponseTime = TimeSpan.FromMilliseconds(150)
        };
        await notificationService.SendNotificationAsync(healthEvent);
        
        // Business workflow events
        var orderEvent = new OrderProcessingEvent
        {
            OrderId = "order-789",
            CustomerId = "customer-123",
            Stage = "PaymentProcessed",
            Amount = 299.99m
        };
        await notificationService.SendNotificationAsync(orderEvent);
        
        // All use the same service, but generate different notification types
    }

    /// <summary>
    /// Example 4: Service Registration Patterns
    /// Shows different ways to register generic services
    /// </summary>
    public void ServiceRegistrationExamples()
    {
        var services = new ServiceCollection();
        
        // Method 1: Register individual repositories
        services.AddRepository<UserEntity>(tableName: "Users", partitionKey: "AllUsers");
        services.AddRepository<OrderEntity>(tableName: "Orders", partitionKey: "AllOrders");
        services.AddRepository<RequestTrackingEntity>(tableName: "Documents", partitionKey: "DocumentRequests");
        
        // Method 2: Register multiple repositories at once
        services.AddRepositories<UserEntity, OrderEntity>(partitionKey: "DefaultPartition");
        
        // Method 3: Fluent configuration
        services.AddTypedRepositories(builder =>
            builder
                .AddRepository<UserEntity>(tableName: "Users", partitionKey: "AllUsers")
                .AddRepository<OrderEntity>(tableName: "Orders", partitionKey: "AllOrders")
                .AddRepositories<SessionEntity, LogEntity>(partitionKey: "ApplicationData"));
        
        // Method 4: Using existing domain-specific extension
        services.AddDocumentProcessingRepositories(); // Uses generic infrastructure internally
        
        // All methods result in fully functional generic repositories
        var serviceProvider = services.BuildServiceProvider();
        var userRepo = serviceProvider.GetRequiredService<IRepository<UserEntity>>();
        var orderRepo = serviceProvider.GetRequiredService<IRepository<OrderEntity>>();
        // These will work identically regardless of registration method used
    }

    /// <summary>
    /// Example 5: Creating New Domain Services with Generic Infrastructure
    /// Shows how to build domain-specific services on top of generic components
    /// </summary>
    public class UserManagementService
    {
        private readonly IRepository<UserEntity> _userRepository;
        private readonly IRepository<UserSessionEntity> _sessionRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            IRepository<UserEntity> userRepository,
            IRepository<UserSessionEntity> sessionRepository,
            INotificationService notificationService,
            ILogger<UserManagementService> logger)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<string> CreateUserAsync(string email, string name)
        {
            var userId = Guid.NewGuid().ToString();
            
            // Use generic repository
            var user = new UserEntity
            {
                RowKey = userId,
                Email = email,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            
            await _userRepository.UpsertAsync(user);
            
            // Use generic notification service
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            
            await _notificationService.SendNotificationAsync(userCreatedEvent);
            
            _logger.LogInformation("Created user {UserId} with email {Email}", userId, email);
            return userId;
        }

        public async Task<IEnumerable<UserEntity>> GetActiveUsersAsync()
        {
            // Use generic repository with domain-specific filter
            return await _userRepository.QueryAsync("IsActive eq true");
        }
    }
}

#region Example Entity Types

public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Users";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
    
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserSessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "UserSessions";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Orders";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
    
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

public class SessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Sessions";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
}

public class LogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Logs";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
}

#endregion

#region Example Event Types

public class UserCreatedEvent : INotificationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string EventId => UserId;
    public string EventType => "User.Created";
    public string Message => $"New user created: {Email}";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Details => new { UserId, Email, CreatedAt };
}

// Note: UserAuthenticationEvent, SystemHealthEvent, and OrderProcessingEvent
// are defined in GenericNotificationExamples.cs to avoid duplication

#endregion
