using Azure.Data.Tables;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Generic table storage provider that works with any ITableEntity implementation
/// </summary>
public interface ITableStorageProvider
{
    /// <summary>
    /// Gets an entity by partition key and row key
    /// </summary>
    Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new();

    /// <summary>
    /// Gets entities by filter expression
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new();

    /// <summary>
    /// Upserts an entity (insert or replace)
    /// </summary>
    Task UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity;

    /// <summary>
    /// Deletes an entity by partition key and row key
    /// </summary>
    Task DeleteEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the table if it doesn't exist
    /// </summary>
    Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default);
}
