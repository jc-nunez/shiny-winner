using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Generic repository implementation using table storage
/// Works with any ITableEntity and provides common CRUD operations
/// </summary>
public class TableRepository<TEntity> : IRepository<TEntity> where TEntity : class, ITableEntity, new()
{
    private readonly ITableStorageProvider _tableStorageProvider;
    private readonly ILogger<TableRepository<TEntity>> _logger;
    private readonly string _partitionKey;

    public TableRepository(
        ITableStorageProvider tableStorageProvider, 
        ILogger<TableRepository<TEntity>> logger,
        string partitionKey = "DefaultPartition")
    {
        _tableStorageProvider = tableStorageProvider;
        _logger = logger;
        _partitionKey = partitionKey;
    }

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        return await _tableStorageProvider.GetEntityAsync<TEntity>(_partitionKey, id, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying {EntityType} with filter: {Filter}", typeof(TEntity).Name, filter ?? "(no filter)");
        
        return await _tableStorageProvider.QueryAsync<TEntity>(filter, maxPerPage, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all {EntityType} entities", typeof(TEntity).Name);
        
        return await _tableStorageProvider.QueryAsync<TEntity>(cancellationToken: cancellationToken);
    }

    public async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting {EntityType} with key {RowKey}", typeof(TEntity).Name, entity.RowKey);
        
        // Ensure partition key is set
        if (string.IsNullOrEmpty(entity.PartitionKey))
            entity.PartitionKey = _partitionKey;
        
        await _tableStorageProvider.UpsertEntityAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        await _tableStorageProvider.DeleteEntityAsync(_partitionKey, id, cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(entity.RowKey, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity != null;
    }

    public async Task<int> CountAsync(string? filter = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting {EntityType} entities with filter: {Filter}", typeof(TEntity).Name, filter ?? "(no filter)");
        
        var entities = await QueryAsync(filter, cancellationToken: cancellationToken);
        return entities.Count();
    }
}

/// <summary>
/// Generic repository with typed key support
/// </summary>
public class TableRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class, ITableEntity, new()
{
    private readonly IRepository<TEntity> _baseRepository;
    private readonly Func<TKey, string> _keyConverter;

    public TableRepository(IRepository<TEntity> baseRepository, Func<TKey, string> keyConverter)
    {
        _baseRepository = baseRepository;
        _keyConverter = keyConverter;
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _baseRepository.GetByIdAsync(_keyConverter(id), cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default)
    {
        return await _baseRepository.QueryAsync(filter, maxPerPage, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _baseRepository.GetAllAsync(cancellationToken);
    }

    public async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _baseRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await _baseRepository.DeleteAsync(_keyConverter(id), cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _baseRepository.DeleteAsync(entity, cancellationToken);
    }

    public async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _baseRepository.ExistsAsync(_keyConverter(id), cancellationToken);
    }

    public async Task<int> CountAsync(string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _baseRepository.CountAsync(filter, cancellationToken);
    }
}
