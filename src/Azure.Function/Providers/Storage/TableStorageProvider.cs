using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Generic table storage provider that works with any ITableEntity implementation
/// </summary>
public class TableStorageProvider : ITableStorageProvider
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageProvider> _logger;

    public TableStorageProvider(IOptions<StorageConfiguration> options, ILogger<TableStorageProvider> logger)
    {
        var config = options.Value;
        var serviceClient = new TableServiceClient(config.TableStorageConnection);
        _tableClient = serviceClient.GetTableClient("DocumentRequests"); // Default table name
        _logger = logger;
    }

    public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new()
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Getting entity with partition key {PartitionKey} and row key {RowKey} from table storage", 
                partitionKey, rowKey);
            
            var response = await _tableClient.GetEntityIfExistsAsync<T>(
                partitionKey, 
                rowKey, 
                cancellationToken: cancellationToken);

            if (response.HasValue)
            {
                _logger.LogInformation("Found entity with partition key {PartitionKey} and row key {RowKey}", 
                    partitionKey, rowKey);
                return response.Value;
            }

            _logger.LogInformation("Entity with partition key {PartitionKey} and row key {RowKey} not found", 
                partitionKey, rowKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity with partition key {PartitionKey} and row key {RowKey}", 
                partitionKey, rowKey);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string? filter = null, int? maxPerPage = null, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity, new()
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Querying entities from table storage with filter: {Filter}", filter ?? "(no filter)");

            var entities = new List<T>();
            
            await foreach (var entity in _tableClient.QueryAsync<T>(filter, maxPerPage, cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }

            _logger.LogInformation("Found {Count} entities in table storage", entities.Count);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query entities from table storage with filter: {Filter}", filter ?? "(no filter)");
            throw;
        }
    }

    public async Task UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) 
        where T : class, ITableEntity
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Upserting entity with partition key {PartitionKey} and row key {RowKey}", 
                entity.PartitionKey, entity.RowKey);
            
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            
            _logger.LogInformation("Successfully upserted entity with partition key {PartitionKey} and row key {RowKey}", 
                entity.PartitionKey, entity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert entity with partition key {PartitionKey} and row key {RowKey}", 
                entity.PartitionKey, entity.RowKey);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateTableIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Deleting entity with partition key {PartitionKey} and row key {RowKey}", 
                partitionKey, rowKey);
            
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully deleted entity with partition key {PartitionKey} and row key {RowKey}", 
                partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity with partition key {PartitionKey} and row key {RowKey}", 
                partitionKey, rowKey);
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
            _logger.LogError(ex, "Failed to create table if not exists");
            throw;
        }
    }
}
