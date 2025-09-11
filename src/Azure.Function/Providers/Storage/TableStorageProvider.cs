using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;

namespace Azure.Function.Providers.Storage;

/// <summary>
/// Table storage provider for document processing entities with fixed "DocumentRequests" table.
/// Provides CRUD operations with automatic table creation and comprehensive logging.
/// </summary>
/// <remarks>
/// This provider is configured for the DocumentRequests table specifically and handles
/// all document tracking entity operations. Uses connection string authentication only.
/// </remarks>
public class TableStorageProvider : ITableStorageProvider
{
    /// <summary>
    /// Azure Table Storage client for the DocumentRequests table.
    /// </summary>
    private readonly TableClient _tableClient;
    
    /// <summary>
    /// Logger for tracking table operations and troubleshooting.
    /// </summary>
    private readonly ILogger<TableStorageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the TableStorageProvider for the DocumentRequests table.
    /// </summary>
    /// <param name="options">Storage configuration containing the table storage connection string.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    /// <remarks>
    /// Creates a TableServiceClient using the TableStorageConnection and configures it
    /// for the fixed "DocumentRequests" table used by the document processing workflow.
    /// </remarks>
    public TableStorageProvider(IOptions<StorageConfiguration> options, ILogger<TableStorageProvider> logger)
    {
        var config = options.Value;
        var serviceClient = new TableServiceClient(config.TableStorageConnection);
        _tableClient = serviceClient.GetTableClient("DocumentRequests"); // Default table name
        _logger = logger;
    }

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

    /// <summary>
    /// Ensures the DocumentRequests table exists, creating it if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="RequestFailedException">Thrown for table service errors during creation.</exception>
    /// <remarks>
    /// Called automatically by other methods to ensure the table is available.
    /// Safe to call multiple times - will not error if the table already exists.
    /// </remarks>
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
