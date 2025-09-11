using Azure.Data.Tables;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Contract for table storage operations supporting document processing request tracking.
/// Provides generic CRUD operations for any table entity with automatic table creation.
/// </summary>
/// <remarks>
/// This interface abstracts Azure Table Storage operations for the DocumentRequests table.
/// All operations automatically ensure the table exists before performing the operation.
/// Used primarily for tracking document processing request lifecycle and status.
/// </remarks>
public interface ITableStorageProvider
{
    /// <summary>
    /// Retrieves a specific entity by partition key and row key.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ITableEntity.</typeparam>
    /// <param name="partitionKey">Partition key of the entity to retrieve.</param>
    /// <param name="rowKey">Row key of the entity to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    /// <exception cref="RequestFailedException">Thrown for table service errors other than NotFound.</exception>
    /// <remarks>
    /// Automatically ensures the table exists before attempting the operation.
    /// Most efficient way to retrieve a known entity.
    /// </remarks>
    Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new();

    /// <summary>
    /// Queries entities from the table with optional filtering and paging.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ITableEntity.</typeparam>
    /// <param name="filter">OData filter expression, or null to retrieve all entities.</param>
    /// <param name="maxPerPage">Maximum number of entities per page, or null for default.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of entities matching the filter criteria.</returns>
    /// <exception cref="RequestFailedException">Thrown for table service errors.</exception>
    /// <remarks>
    /// Automatically ensures the table exists before querying. Loads all matching entities
    /// into memory - use with caution for large result sets. Filter uses OData syntax.
    /// </remarks>
    /// <example>
    /// filter: "CurrentStatus eq 'Processing'" or "SubmittedAt gt datetime'2024-01-01T00:00:00Z'"
    /// </example>
    Task<IEnumerable<T>> QueryAsync<T>(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new();

    /// <summary>
    /// Inserts or replaces an entity in the table.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ITableEntity.</typeparam>
    /// <param name="entity">Entity to insert or update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="RequestFailedException">Thrown for table service errors.</exception>
    /// <remarks>
    /// Uses TableUpdateMode.Replace to completely replace existing entities.
    /// Automatically ensures the table exists before the operation.
    /// Primary method for saving document tracking state.
    /// </remarks>
    Task UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity;

    /// <summary>
    /// Deletes an entity from the table by its keys.
    /// </summary>
    /// <param name="partitionKey">Partition key of the entity to delete.</param>
    /// <param name="rowKey">Row key of the entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="RequestFailedException">Thrown if the entity doesn't exist or for other table service errors.</exception>
    /// <remarks>
    /// Used for cleanup operations when document processing is complete.
    /// Will throw an exception if the entity doesn't exist.
    /// </remarks>
    Task DeleteEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the DocumentRequests table exists, creating it if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="RequestFailedException">Thrown for table service errors during creation.</exception>
    /// <remarks>
    /// Called automatically by other methods to ensure the table is available.
    /// Safe to call multiple times - will not error if the table already exists.
    /// </remarks>
    Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default);
}
