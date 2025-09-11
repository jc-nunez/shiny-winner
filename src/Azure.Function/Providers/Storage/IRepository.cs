using Azure.Data.Tables;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Generic repository interface for any table entity operations
/// </summary>
public interface IRepository<TEntity, TKey> where TEntity : class, ITableEntity
{
    /// <summary>
    /// Gets an entity by its key
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets entities by filter expression
    /// </summary>
    Task<IEnumerable<TEntity>> QueryAsync(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities (use with caution on large tables)
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upserts an entity (insert or replace)
    /// </summary>
    Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity by its key
    /// </summary>
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an entity exists by its key
    /// </summary>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets count of entities matching filter
    /// </summary>
    Task<int> CountAsync(string? filter = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository interface for string-keyed entities (most common case)
/// </summary>
public interface IRepository<TEntity> : IRepository<TEntity, string> where TEntity : class, ITableEntity
{
}
