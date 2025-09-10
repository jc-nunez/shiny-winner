using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trossitec.Azure.Function.Configuration;
using Trossitec.Azure.Function.Models;

namespace Trossitec.Azure.Function.Providers.Storage;

public class TableStorageProvider : ITableStorageProvider
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageProvider> _logger;
    private const string TableName = "DocumentRequests";

    public TableStorageProvider(IOptions<StorageConfiguration> options, ILogger<TableStorageProvider> logger)
    {
        var config = options.Value;
        var serviceClient = new TableServiceClient(config.TableStorageConnection);
        _tableClient = serviceClient.GetTableClient(TableName);
        _logger = logger;
    }

    public async Task<RequestTrackingEntity?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Getting request {RequestId} from table storage", requestId);
            
            var response = await _tableClient.GetEntityIfExistsAsync<RequestTrackingEntity>(
                "DocumentRequests", 
                requestId, 
                cancellationToken: cancellationToken);

            if (response.HasValue)
            {
                _logger.LogInformation("Found request {RequestId} in table storage", requestId);
                return response.Value;
            }

            _logger.LogInformation("Request {RequestId} not found in table storage", requestId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get request {RequestId} from table storage", requestId);
            throw;
        }
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Getting pending requests from table storage");

            var pendingStatuses = new[] { "Submitted", "Processing", "Pending" };
            var filter = string.Join(" or ", pendingStatuses.Select(status => $"CurrentStatus eq '{status}'"));

            var entities = new List<RequestTrackingEntity>();
            await foreach (var entity in _tableClient.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }

            _logger.LogInformation("Found {Count} pending requests in table storage", entities.Count);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending requests from table storage");
            throw;
        }
    }

    public async Task UpsertRequestAsync(RequestTrackingEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Upserting request {RequestId} to table storage", entity.RowKey);
            
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            
            _logger.LogInformation("Successfully upserted request {RequestId} to table storage", entity.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert request {RequestId} to table storage", entity.RowKey);
            throw;
        }
    }

    public async Task DeleteRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Deleting request {RequestId} from table storage", requestId);
            
            await _tableClient.DeleteEntityAsync("DocumentRequests", requestId, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully deleted request {RequestId} from table storage", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete request {RequestId} from table storage", requestId);
            throw;
        }
    }

    public async Task<IEnumerable<RequestTrackingEntity>> GetStaleRequestsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            _logger.LogInformation("Getting stale requests older than {MaxAge} from table storage", maxAge);

            var cutoffTime = DateTime.UtcNow - maxAge;
            var filter = $"SubmittedAt lt datetime'{cutoffTime:yyyy-MM-ddTHH:mm:ssZ}'";

            var entities = new List<RequestTrackingEntity>();
            await foreach (var entity in _tableClient.QueryAsync<RequestTrackingEntity>(filter, cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }

            _logger.LogInformation("Found {Count} stale requests in table storage", entities.Count);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stale requests from table storage");
            throw;
        }
    }
}
