using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Models;
using Azure.Identity;

namespace Azure.Function.Providers.Messaging;

public class ServiceBusProvider : IServiceBusProvider, IDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _config;
    private readonly ILogger<ServiceBusProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceBusProvider(IOptions<ServiceBusConfiguration> options, ILogger<ServiceBusProvider> logger)
    {
        _config = options.Value;
        _client = CreateServiceBusClient(_config);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private ServiceBusClient CreateServiceBusClient(ServiceBusConfiguration config)
    {
        return config.AuthenticationMethod.ToLowerInvariant() switch
        {
            "connectionstring" => CreateFromConnectionString(config),
            "systemmanaged" or "systemmanagedidentity" => CreateFromSystemManagedIdentity(config),
            "usermanaged" or "usermanagedidentity" => CreateFromUserManagedIdentity(config),
            _ => throw new ArgumentException($"Unsupported authentication method: {config.AuthenticationMethod}. Supported methods: ConnectionString, SystemManagedIdentity, UserManagedIdentity")
        };
    }

    private ServiceBusClient CreateFromConnectionString(ServiceBusConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.ServiceBusConnection))
            throw new ArgumentException($"Service Bus connection string is required for ConnectionString authentication method");
        
        _logger.LogDebug("Creating ServiceBusClient using connection string");
        return new ServiceBusClient(config.ServiceBusConnection);
    }

    private ServiceBusClient CreateFromSystemManagedIdentity(ServiceBusConfiguration config)
    {
        _logger.LogDebug("Creating ServiceBusClient using System Managed Identity for namespace {Namespace}", config.Namespace);
        var credential = new DefaultAzureCredential();
        var fullyQualifiedNamespace = $"{config.Namespace}.servicebus.windows.net";
        return new ServiceBusClient(fullyQualifiedNamespace, credential);
    }

    private ServiceBusClient CreateFromUserManagedIdentity(ServiceBusConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.UserManagedIdentityClientId))
            throw new ArgumentException($"UserManagedIdentityClientId is required for UserManagedIdentity authentication method");
        
        _logger.LogDebug("Creating ServiceBusClient using User Managed Identity {ClientId} for namespace {Namespace}", 
            config.UserManagedIdentityClientId, config.Namespace);
        
        var credential = new ManagedIdentityCredential(config.UserManagedIdentityClientId);
        var fullyQualifiedNamespace = $"{config.Namespace}.servicebus.windows.net";
        return new ServiceBusClient(fullyQualifiedNamespace, credential);
    }

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

    public void Dispose()
    {
        _client?.DisposeAsync().GetAwaiter().GetResult();
    }
}
