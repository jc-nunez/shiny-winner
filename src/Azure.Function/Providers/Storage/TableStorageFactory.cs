using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using System.Collections.Concurrent;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Factory for creating table storage providers and repositories
/// Manages multiple table clients with caching for performance
/// </summary>
public class TableStorageFactory : ITableStorageFactory
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, ITableStorageProvider> _providers = new();
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    public TableStorageFactory(IOptions<StorageConfiguration> options, ILoggerFactory loggerFactory)
    {
        var config = options.Value;
        _tableServiceClient = new TableServiceClient(config.TableStorageConnection);
        _loggerFactory = loggerFactory;
    }

    public ITableStorageProvider GetProvider(string tableName)
    {
        return _providers.GetOrAdd(tableName, name => 
            new NamedTableStorageProvider(
                _tableServiceClient.GetTableClient(name), 
                _loggerFactory.CreateLogger<NamedTableStorageProvider>()));
    }

    public IRepository<TEntity> GetRepository<TEntity>(string tableName, string partitionKey = "DefaultPartition") 
        where TEntity : class, ITableEntity, new()
    {
        var key = $"{tableName}_{typeof(TEntity).Name}_{partitionKey}";
        
        return (IRepository<TEntity>)_repositories.GetOrAdd(key, _ =>
            new TableRepository<TEntity>(
                GetProvider(tableName),
                _loggerFactory.CreateLogger<TableRepository<TEntity>>(),
                partitionKey));
    }

    public IRepository<TEntity> GetRepository<TEntity>(string partitionKey = "DefaultPartition") 
        where TEntity : class, ITableEntity, new()
    {
        var tableName = GetTableNameFromEntityType<TEntity>();
        return GetRepository<TEntity>(tableName, partitionKey);
    }

    private static string GetTableNameFromEntityType<TEntity>() where TEntity : class, ITableEntity
    {
        var entityName = typeof(TEntity).Name;
        
        // Remove common suffixes to get clean table names
        if (entityName.EndsWith("Entity"))
            entityName = entityName[..^6]; // Remove "Entity"
        
        if (entityName.EndsWith("Model"))
            entityName = entityName[..^5]; // Remove "Model"
            
        return entityName;
    }
}

/// <summary>
/// Table storage provider for a specific table
/// </summary>
internal class NamedTableStorageProvider : ITableStorageProvider
{
    private readonly TableClient _tableClient;
    private readonly ILogger<NamedTableStorageProvider> _logger;

    public NamedTableStorageProvider(TableClient tableClient, ILogger<NamedTableStorageProvider> logger)
    {
        _tableClient = tableClient;
        _logger = logger;
    }

    public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new()
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogDebug("Getting {EntityType} with partition key {PartitionKey} and row key {RowKey} from table {TableName}", 
                typeof(T).Name, partitionKey, rowKey, _tableClient.Name);
            
            var response = await _tableClient.GetEntityIfExistsAsync<T>(
                partitionKey, 
                rowKey, 
                cancellationToken: cancellationToken);

            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get {EntityType} with partition key {PartitionKey} and row key {RowKey} from table {TableName}", 
                typeof(T).Name, partitionKey, rowKey, _tableClient.Name);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new()
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogDebug("Querying {EntityType} from table {TableName} with filter: {Filter}", 
                typeof(T).Name, _tableClient.Name, filter ?? "(no filter)");

            var entities = new List<T>();
            
            await foreach (var entity in _tableClient.QueryAsync<T>(filter, maxPerPage, cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }

            _logger.LogDebug("Found {Count} {EntityType} entities in table {TableName}", 
                entities.Count, typeof(T).Name, _tableClient.Name);
            
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query {EntityType} from table {TableName} with filter: {Filter}", 
                typeof(T).Name, _tableClient.Name, filter ?? "(no filter)");
            throw;
        }
    }

    public async Task UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogDebug("Upserting {EntityType} with partition key {PartitionKey} and row key {RowKey} in table {TableName}", 
                typeof(T).Name, entity.PartitionKey, entity.RowKey, _tableClient.Name);
            
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            
            _logger.LogDebug("Successfully upserted {EntityType} with partition key {PartitionKey} and row key {RowKey} in table {TableName}", 
                typeof(T).Name, entity.PartitionKey, entity.RowKey, _tableClient.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert {EntityType} with partition key {PartitionKey} and row key {RowKey} in table {TableName}", 
                typeof(T).Name, entity.PartitionKey, entity.RowKey, _tableClient.Name);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogDebug("Deleting entity with partition key {PartitionKey} and row key {RowKey} from table {TableName}", 
                partitionKey, rowKey, _tableClient.Name);
            
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            
            _logger.LogDebug("Successfully deleted entity with partition key {PartitionKey} and row key {RowKey} from table {TableName}", 
                partitionKey, rowKey, _tableClient.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity with partition key {PartitionKey} and row key {RowKey} from table {TableName}", 
                partitionKey, rowKey, _tableClient.Name);
            throw;
        }
    }

    public async Task CreateTableIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create table {TableName} if not exists", _tableClient.Name);
            throw;
        }
    }
}
