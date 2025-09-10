using Azure.Function.Models;

namespace Azure.Function.Providers.Storage;

public interface ITableStorageProvider
{
    Task<RequestTrackingEntity?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RequestTrackingEntity>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task UpsertRequestAsync(RequestTrackingEntity entity, CancellationToken cancellationToken = default);
    Task DeleteRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RequestTrackingEntity>> GetStaleRequestsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}
