using Azure.Function.Models;

namespace Azure.Function.Providers.Messaging;

public interface IServiceBusProvider
{
    Task SendMessageAsync<T>(T message, string topicName, CancellationToken cancellationToken = default) where T : class;
    Task SendNotificationAsync(StatusNotification notification, CancellationToken cancellationToken = default);
}
