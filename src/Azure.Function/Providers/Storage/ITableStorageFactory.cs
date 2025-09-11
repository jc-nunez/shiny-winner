using Azure.Data.Tables;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating table storage providers for different tables
/// </summary>
public interface ITableStorageFactory
{
    /// <summary>
    /// Gets a table storage provider for the specified table name
    /// </summary>
    ITableStorageProvider GetProvider(string tableName);
    
    /// <summary>
    /// Gets a repository for the specified entity type and table
    /// </summary>
    IRepository<TEntity> GetRepository<TEntity>(string tableName, string partitionKey = "DefaultPartition") 
        where TEntity : class, ITableEntity, new();
        
    /// <summary>
    /// Gets a repository for the specified entity type using naming convention
    /// Table name will be derived from entity type name (e.g., RequestTrackingEntity -> RequestTracking)
    /// </summary>
    IRepository<TEntity> GetRepository<TEntity>(string partitionKey = "DefaultPartition") 
        where TEntity : class, ITableEntity, new();
}

/// <summary>
/// Specific table storage provider for a single table
/// </summary>
public interface ITableStorageProvider<TEntity> : ITableStorageProvider where TEntity : class, ITableEntity
{
    /// <summary>
    /// Gets an entity by partition key and row key with strong typing
    /// </summary>
    Task<TEntity?> GetEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets entities by filter expression with strong typing
    /// </summary>
    Task<IEnumerable<TEntity>> QueryAsync(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upserts an entity with strong typing
    /// </summary>
    Task UpsertEntityAsync(TEntity entity, CancellationToken cancellationToken = default);
}
