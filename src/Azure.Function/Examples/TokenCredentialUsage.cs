using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Function.Configuration;
using Azure.Function.Providers.Http;

namespace Azure.Function.Examples;

/// <summary>
/// Examples showing how to use TokenCredential from HttpClientProvider with different NuGet packages
/// </summary>
public class TokenCredentialUsageExamples
{
    private readonly HttpClientProvider _authProvider;
    private readonly ILogger<TokenCredentialUsageExamples> _logger;

    public TokenCredentialUsageExamples(
        HttpClientProvider authProvider, 
        ILogger<TokenCredentialUsageExamples> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: NuGet package that accepts TokenCredential in constructor
    /// </summary>
    public void Example1_ConstructorCredential()
    {
        var config = _authProvider.GetApiConfiguration();
        var credential = _authProvider.GetTokenCredential();
        
        // Many Azure SDK packages follow this pattern
        // var client = new YourApiClient(config.BaseUrl, credential);
        // var client = new ServiceBusClient("namespace.servicebus.windows.net", credential);
        // var client = new BlobServiceClient("https://account.blob.core.windows.net", credential);
        
        _logger.LogInformation("Created client with TokenCredential in constructor");
    }

    /// <summary>
    /// Example 2: NuGet package with options pattern
    /// </summary>
    public void Example2_OptionsPattern()
    {
        var config = _authProvider.GetApiConfiguration();
        var credential = _authProvider.GetTokenCredential();
        
        // Common pattern for modern .NET packages
        // var options = new YourClientOptions
        // {
        //     Endpoint = new Uri(config.BaseUrl),
        //     Credential = credential,
        //     // Add custom headers if needed
        //     AdditionalHeaders = new Dictionary<string, string>
        //     {
        //         ["Ocp-Apim-Subscription-Key"] = config.SubscriptionKey
        //     }
        // };
        // var client = new YourApiClient(options);
        
        _logger.LogInformation("Created client with options pattern");
    }

    /// <summary>
    /// Example 3: NuGet package with builder pattern
    /// </summary>
    public void Example3_BuilderPattern()
    {
        var config = _authProvider.GetApiConfiguration();
        var credential = _authProvider.GetTokenCredential();
        
        // Builder pattern for flexible configuration
        // var client = new YourApiClientBuilder()
        //     .WithEndpoint(config.BaseUrl)
        //     .WithCredential(credential)
        //     .WithSubscriptionKey(config.SubscriptionKey)
        //     .WithTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds))
        //     .Build();
        
        _logger.LogInformation("Created client with builder pattern");
    }

    /// <summary>
    /// Example 4: For packages that need DefaultAzureCredential specifically
    /// </summary>
    public void Example4_DefaultAzureCredentialWrapper()
    {
        var config = _authProvider.GetApiConfiguration();
        
        // Some packages specifically require DefaultAzureCredential type
        // You can create a DefaultAzureCredential that will use your user-managed identity
        var defaultCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = config.UserManagedIdentityClientId,
            // This tells DefaultAzureCredential to use the specific user-managed identity
            ExcludeEnvironmentCredential = false,
            ExcludeInteractiveBrowserCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeManagedIdentityCredential = false
        });
        
        // var client = new YourApiClient(config.BaseUrl, defaultCredential);
        
        _logger.LogInformation("Created client with DefaultAzureCredential configured for user-managed identity");
    }

    /// <summary>
    /// Example 5: Manual token management for packages that need bearer tokens directly
    /// </summary>
    public async Task Example5_ManualTokenManagement()
    {
        var config = _authProvider.GetApiConfiguration();
        
        // For packages that need you to manage tokens manually
        var token = await _authProvider.GetManagedIdentityTokenAsync();
        
        // var client = new YourApiClient(config.BaseUrl);
        // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.SubscriptionKey);
        
        _logger.LogInformation("Manually configured client with bearer token");
    }

    /// <summary>
    /// Example 6: For packages that accept custom token providers
    /// </summary>
    public void Example6_CustomTokenProvider()
    {
        var config = _authProvider.GetApiConfiguration();
        
        // Create a token provider function that the package can call
        Func<CancellationToken, Task<string>> tokenProvider = async (cancellationToken) =>
        {
            return await _authProvider.GetManagedIdentityTokenAsync(cancellationToken);
        };
        
        // var client = new YourApiClient(config.BaseUrl, tokenProvider);
        // client.SetSubscriptionKey(config.SubscriptionKey);
        
        _logger.LogInformation("Created client with custom token provider");
    }

    /// <summary>
    /// Example 7: Complete real-world example with error handling
    /// </summary>
    public async Task Example7_RealWorldUsage()
    {
        try
        {
            var config = _authProvider.GetApiConfiguration();
            var credential = _authProvider.GetTokenCredential();
            
            _logger.LogInformation("Initializing API client for {BaseUrl} with user-managed identity {ClientId}",
                config.BaseUrl, config.UserManagedIdentityClientId);
            
            // Real example - replace with your actual NuGet package
            // var client = new YourRealApiClient(new YourClientOptions
            // {
            //     BaseUrl = config.BaseUrl,
            //     Credential = credential,
            //     SubscriptionKey = config.SubscriptionKey,
            //     Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            // });
            
            // Test the connection
            // var healthCheck = await client.HealthCheckAsync();
            // _logger.LogInformation("API client initialized successfully. Health check: {Status}", healthCheck.Status);
            
            _logger.LogInformation("API client setup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize API client");
            throw;
        }
    }
}

/// <summary>
/// Example showing how to register your implementation in DI
/// </summary>
public static class ServiceRegistrationExample
{
    public static void RegisterYourNuGetClient(IServiceCollection services)
    {
        // Option 1: Replace the default implementation
        services.AddScoped<IHttpClientProvider, YourNuGetClientImplementation>();
        
        // Option 2: Register alongside the default
        services.AddScoped<HttpClientProvider>();
        services.AddScoped<IYourNuGetClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApiConfiguration>>();
            var logger = provider.GetRequiredService<ILogger<IYourNuGetClient>>();
            var authProvider = provider.GetRequiredService<HttpClientProvider>();
            
            var config = authProvider.GetApiConfiguration();
            var credential = authProvider.GetTokenCredential();
            
            // Return your actual NuGet client
            // return new YourRealApiClient(config.BaseUrl, credential, config.SubscriptionKey);
            
            throw new NotImplementedException("Replace with your actual NuGet client instantiation");
        });
    }
}

/// <summary>
/// Usage in your service classes
/// </summary>
public class YourServiceExample
{
    private readonly IYourNuGetClient _apiClient;
    private readonly ILogger<YourServiceExample> _logger;

    public YourServiceExample(IYourNuGetClient apiClient, ILogger<YourServiceExample> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(string blobName)
    {
        _logger.LogInformation("Processing document {BlobName}", blobName);
        
        // Your NuGet client automatically handles:
        // - Token acquisition using the user-managed identity
        // - Adding Authorization header with fresh bearer token
        // - Adding Ocp-Apim-Subscription-Key header
        // - HTTP request/response handling
        // - Serialization/deserialization
        
        // var result = await _apiClient.ProcessDocumentAsync(blobName);
        // _logger.LogInformation("Document processed successfully: {RequestId}", result.RequestId);
    }
}
