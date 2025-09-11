using Azure.Function.Models;

namespace Azure.Function.Services;

/// <summary>
/// Service interface for managing document extraction request state tracking
/// Provides business-specific operations for tracking document extraction request state
/// </summary>
public interface IDocumentExtractionRequestStateService
{
    /// <summary>
    /// Gets a document request by its ID
    /// </summary>
    Task<RequestTrackingEntity?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending document requests (submitted, processing, or pending status)
    /// </summary>
    Task<IEnumerable<RequestTrackingEntity>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a document request tracking entity
    /// </summary>
    Task UpsertRequestAsync(RequestTrackingEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document request by its ID
    /// </summary>
    Task DeleteRequestAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document requests that are older than the specified age (potentially stale)
    /// </summary>
    Task<IEnumerable<RequestTrackingEntity>> GetStaleRequestsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document requests with a specific status
    /// </summary>
    Task<IEnumerable<RequestTrackingEntity>> GetRequestsByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document requests submitted after a specific time
    /// </summary>
    Task<IEnumerable<RequestTrackingEntity>> GetRequestsSubmittedAfterAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
}
