using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Models;

namespace Azure.Function.Providers.Http;

public class HttpClientProvider : IHttpClientProvider
{
    private readonly HttpClient _httpClient;
    private readonly ApiConfiguration _config;
    private readonly ILogger<HttpClientProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpClientProvider(HttpClient httpClient, IOptions<ApiConfiguration> options, ILogger<HttpClientProvider> logger)
    {
        _httpClient = httpClient;
        _config = options.Value;
        _logger = logger;
        
        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocumentProcessor/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResponse<DocumentSubmissionResponse>> SubmitDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting document {BlobName} to external API", request.BlobName);

            var payload = new
            {
                fileName = request.BlobName,
                sourceContainer = request.SourceContainer,
                destinationContainer = request.DestinationContainer,
                metadata = request.Metadata,
                eventType = request.EventType,
                submittedAt = request.CreatedAt
            };

            var response = await PostAsync<DocumentSubmissionResponse>("/api/documents/submit", payload, cancellationToken);
            
            if (response.Success)
            {
                _logger.LogInformation("Successfully submitted document {BlobName}, received request ID {RequestId}", 
                    request.BlobName, response.Data?.RequestId);
            }
            else
            {
                _logger.LogWarning("Failed to submit document {BlobName}: {ErrorMessage}", 
                    request.BlobName, response.Message);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting document {BlobName} to external API", request.BlobName);
            return new ApiResponse<DocumentSubmissionResponse>
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "SUBMISSION_ERROR"
            };
        }
    }

    public async Task<ApiResponse<ProcessingStatus>> GetStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting status for request {RequestId} from external API", requestId);

            var response = await GetAsync<ProcessingStatus>($"/api/documents/status/{requestId}", cancellationToken);
            
            if (response.Success)
            {
                _logger.LogInformation("Successfully retrieved status for request {RequestId}: {Status}", 
                    requestId, response.Data?.Status);
            }
            else
            {
                _logger.LogWarning("Failed to get status for request {RequestId}: {ErrorMessage}", 
                    requestId, response.Message);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for request {RequestId} from external API", requestId);
            return new ApiResponse<ProcessingStatus>
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "STATUS_ERROR"
            };
        }
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making GET request to {Endpoint}", endpoint);
            
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await ProcessHttpResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "GET_ERROR"
            };
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making POST request to {Endpoint}", endpoint);

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            return await ProcessHttpResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making POST request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "POST_ERROR"
            };
        }
    }

    private async Task<ApiResponse<T>> ProcessHttpResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return new ApiResponse<T>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing successful response: {Content}", content);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Invalid response format",
                    ErrorCode = "DESERIALIZATION_ERROR"
                };
            }
        }
        else
        {
            _logger.LogWarning("HTTP request failed with status {StatusCode}: {Content}", 
                response.StatusCode, content);
                
            return new ApiResponse<T>
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}: {content}",
                ErrorCode = response.StatusCode.ToString()
            };
        }
    }
}
