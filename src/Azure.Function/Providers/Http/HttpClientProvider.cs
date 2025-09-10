using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Models;
using Azure.Identity;
using Azure.Core;

namespace Azure.Function.Providers.Http;

public class HttpClientProvider : IHttpClientProvider
{
    private readonly ApiConfiguration _config;
    private readonly ILogger<HttpClientProvider> _logger;

    public HttpClientProvider(IOptions<ApiConfiguration> options, ILogger<HttpClientProvider> logger)
    {
        _config = options.Value;
        _logger = logger;
    }


    public async Task<ApiResponse<DocumentSubmissionResponse>> SubmitDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting document {BlobName} to external API", request.BlobName);
        
        // TODO: Implement using your NuGet package client
        // You have access to:
        // - _config.BaseUrl
        // - _config.SubscriptionKey (for Ocp-Apim-Subscription-Key header)
        // - await GetManagedIdentityTokenAsync(cancellationToken) for Authorization header
        
        throw new NotImplementedException("Implement using your NuGet package client");
    }

    public async Task<ApiResponse<ProcessingStatus>> GetStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for request {RequestId} from external API", requestId);
        
        // TODO: Implement using your NuGet package client
        // You have access to:
        // - _config.BaseUrl
        // - _config.SubscriptionKey (for Ocp-Apim-Subscription-Key header)
        // - await GetManagedIdentityTokenAsync(cancellationToken) for Authorization header
        
        throw new NotImplementedException("Implement using your NuGet package client");
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Making GET request to {Endpoint}", endpoint);
        
        // TODO: Implement using your NuGet package client
        throw new NotImplementedException("Implement using your NuGet package client");
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Making POST request to {Endpoint}", endpoint);
        
        // TODO: Implement using your NuGet package client
        throw new NotImplementedException("Implement using your NuGet package client");
    }

    /// <summary>
    /// Gets a managed identity token for the configured scope
    /// </summary>
    public async Task<string> GetManagedIdentityTokenAsync(CancellationToken cancellationToken = default)
    {
        var credential = GetTokenCredential();
        var tokenContext = new TokenRequestContext(new[] { _config.TokenScope });
        var tokenResult = await credential.GetTokenAsync(tokenContext, cancellationToken);
        
        _logger.LogDebug("Successfully obtained managed identity token for scope {TokenScope} using client ID {ClientId}", 
            _config.TokenScope, _config.UserManagedIdentityClientId);
        
        return tokenResult.Token;
    }
    
    /// <summary>
    /// Gets a TokenCredential for use with NuGet packages that accept DefaultAzureCredential
    /// This returns a ManagedIdentityCredential configured with the user-managed identity
    /// </summary>
    public TokenCredential GetTokenCredential()
    {
        return new ManagedIdentityCredential(_config.UserManagedIdentityClientId);
    }
    
    /// <summary>
    /// Gets the API configuration for use in your NuGet package client
    /// </summary>
    public ApiConfiguration GetApiConfiguration() => _config;
}
