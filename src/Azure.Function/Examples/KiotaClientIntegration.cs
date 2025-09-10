using Azure.Core;
using Microsoft.Extensions.Logging;
using Azure.Function.Configuration;
using Azure.Function.Providers.Http;

namespace Azure.Function.Examples;

/// <summary>
/// Example showing how to replace DefaultAzureCredential with ManagedIdentityCredential in Kiota clients
/// </summary>
public class KiotaClientIntegration
{
    private readonly HttpClientProvider _authProvider;
    private readonly ILogger<KiotaClientIntegration> _logger;

    public KiotaClientIntegration(
        HttpClientProvider authProvider,
        ILogger<KiotaClientIntegration> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// Shows how to create Kiota client with ManagedIdentityCredential instead of DefaultAzureCredential
    /// </summary>
    public void CreateKiotaClientExample()
    {
        var config = _authProvider.GetApiConfiguration();
        
        // 🚫 DON'T do this in production (local development only):
        // var defaultCredential = new DefaultAzureCredential();
        // var client = new YourKiotaClient(baseUrl, defaultCredential);
        
        // ✅ DO this for production deployment:
        // Get the ManagedIdentityCredential configured with your specific user-managed identity
        var managedIdentityCredential = _authProvider.GetTokenCredential();
        
        _logger.LogInformation("Using ManagedIdentityCredential with client ID {ClientId} instead of DefaultAzureCredential",
            config.UserManagedIdentityClientId);

        // Replace DefaultAzureCredential with ManagedIdentityCredential in your Kiota client:
        // var client = new YourKiotaClient(config.BaseUrl, managedIdentityCredential);
        
        // Or if your Kiota client uses HttpClient with custom headers:
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(config.BaseUrl);
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.SubscriptionKey);
        
        // Create Kiota client with custom HttpClient (pattern varies by generated client)
        // var client = new YourKiotaClient(httpClient, managedIdentityCredential);
        
        _logger.LogInformation("Kiota client configured with User-Managed Identity for production deployment");
    }

    /// <summary>
    /// Example of local vs production credential selection
    /// </summary>
    public void LocalVsProductionCredentials()
    {
        var config = _authProvider.GetApiConfiguration();
        TokenCredential credential;
        
        // For local development, you might want to use DefaultAzureCredential
        // which will try Azure CLI, Visual Studio, etc.
        if (IsLocalDevelopment())
        {
            // credential = new DefaultAzureCredential();
            _logger.LogInformation("Using DefaultAzureCredential for local development");
        }
        else
        {
            // For production, always use the specific ManagedIdentityCredential
            credential = _authProvider.GetTokenCredential();
            _logger.LogInformation("Using ManagedIdentityCredential for production deployment");
        }
        
        // Use the credential with your Kiota client
        // var client = new YourKiotaClient(config.BaseUrl, credential);
    }
    
    private bool IsLocalDevelopment()
    {
        // Check if running in local development environment
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        return environment == "Development";
    }

}
