using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trossitec.Azure.Function.Configuration;
using Trossitec.Azure.Function.Models;

namespace Trossitec.Azure.Function.Providers.Messaging;

public class ServiceBusProvider : IServiceBusProvider, IDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _config;
    private readonly ILogger<ServiceBusProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceBusProvider(IOptions<ServiceBusConfiguration> options, ILogger<ServiceBusProvider> logger)
    {
        _config = options.Value;
        _client = new ServiceBusClient(_config.ServiceBusConnection);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
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
