using Trossitec.Azure.Function.Models;

namespace Trossitec.Azure.Function.Services;

public interface INotificationService
{
    Task SendSubmittedNotificationAsync(string requestId, DocumentRequest request, CancellationToken cancellationToken = default);
    Task SendCompletedNotificationAsync(string requestId, ProcessingStatus status, CancellationToken cancellationToken = default);
    Task SendFailedNotificationAsync(string requestId, string error, CancellationToken cancellationToken = default);
}
