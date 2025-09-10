using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Function.Configuration;
using Azure.Function.Providers.Http;

namespace Azure.Function.Examples;

/// <summary>
/// Clear example showing how to use the existing HttpClientProvider with your Kiota client
/// </summary>
public class ClearKiotaIntegration
{
    private readonly IHttpClientProvider _httpClientProvider; // This is your existing provider
    private readonly ILogger<ClearKiotaIntegration> _logger;

    public ClearKiotaIntegration(
        IHttpClientProvider httpClientProvider, // Inject the existing provider
        ILogger<ClearKiotaIntegration> logger)
    {
        _httpClientProvider = httpClientProvider;
        _logger = logger;
    }

    /// <summary>
    /// Example showing how to create your Kiota client using the existing HttpClientProvider
    /// </summary>
    public void CreateYourKiotaClient()
    {
        // Cast to the concrete type to access the new methods
        var httpProvider = (HttpClientProvider)_httpClientProvider;
        
        // Get configuration and credential from the existing provider
        var config = httpProvider.GetApiConfiguration();
        var credential = httpProvider.GetTokenCredential(); // This is ManagedIdentityCredential!
        
        _logger.LogInformation("Creating Kiota client with User-Managed Identity {ClientId}",
            config.UserManagedIdentityClientId);

        // 🚫 DON'T do this (local development only):
        // var client = new YourKiotaClient(config.BaseUrl, new DefaultAzureCredential());
        
        // ✅ DO this (production ready):
        // Replace DefaultAzureCredential with the ManagedIdentityCredential from HttpClientProvider
        // var client = new YourKiotaClient(config.BaseUrl, credential);
        
        // If you need custom headers (like subscription key):
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(config.BaseUrl);
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.SubscriptionKey);
        
        // Create your Kiota client with the configured HttpClient and credential
        // var client = new YourKiotaClient(httpClient, credential);
        
        _logger.LogInformation("Kiota client created with specific User-Managed Identity");
    }
}

/// <summary>
/// Alternative approach: Create a service that wraps your Kiota client
/// </summary>
public class DocumentApiService
{
    // private readonly YourKiotaClient _kiotaClient;
    private readonly ILogger<DocumentApiService> _logger;

    public DocumentApiService(
        IHttpClientProvider httpClientProvider, // Inject existing provider
        ILogger<DocumentApiService> logger)
    {
        _logger = logger;
        
        // Cast to access the new methods
        var httpProvider = (HttpClientProvider)httpClientProvider;
        
        // Get what you need for your Kiota client
        var config = httpProvider.GetApiConfiguration();
        var credential = httpProvider.GetTokenCredential(); // ManagedIdentityCredential configured with your client ID
        
        // Create your Kiota client
        // _kiotaClient = new YourKiotaClient(config.BaseUrl, credential);
        
        _logger.LogInformation("DocumentApiService initialized with Kiota client using User-Managed Identity");
    }

    public async Task<string> SubmitDocumentAsync(string fileName, CancellationToken cancellationToken = default)
    {
        // Use your Kiota client
        // var result = await _kiotaClient.Documents.SubmitAsync(new SubmitRequest { FileName = fileName }, cancellationToken);
        // return result.RequestId;
        
        await Task.Delay(100, cancellationToken); // Simulate API call
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Register your service in DI - example for ServiceCollectionExtensions.cs
/// </summary>
public static class KiotaServiceRegistration
{
    public static void RegisterKiotaService(this IServiceCollection services)
    {
        // The HttpClientProvider is already registered, so just add your service
        services.AddScoped<DocumentApiService>();
        
        // Or if you want to replace the IHttpClientProvider implementation entirely:
        // services.AddScoped<IHttpClientProvider, YourKiotaClientImplementation>();
    }
}

/// <summary>
/// Usage in your Functions or other services
/// </summary>
public class ExampleUsage
{
    private readonly DocumentApiService _documentService;

    public ExampleUsage(DocumentApiService documentService)
    {
        _documentService = documentService;
    }

    public async Task ProcessDocumentAsync(string fileName)
    {
        // Your Kiota client (via DocumentApiService) automatically:
        // 1. Uses the ManagedIdentityCredential with your specific client ID
        // 2. Gets fresh tokens for each request
        // 3. Adds Authorization: Bearer {token} header
        // 4. Adds Ocp-Apim-Subscription-Key header (if configured)
        // 5. Makes HTTP requests with proper serialization
        
        var requestId = await _documentService.SubmitDocumentAsync(fileName);
        // Process result...
    }
}
