namespace Azure.Function.Configuration;

public class ApiConfiguration
{
    /// <summary>
    /// Base URL for the external API
    /// </summary>
    public required string BaseUrl { get; set; }
    
    /// <summary>
    /// Authentication method: "ApiKey", "BearerToken", "ManagedIdentityToken"
    /// </summary>
    public required string AuthenticationMethod { get; set; }
    
    /// <summary>
    /// API Key for ApiKey authentication
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Static bearer token for BearerToken authentication
    /// </summary>
    public string? BearerToken { get; set; }
    
    /// <summary>
    /// Target resource/scope for managed identity token (e.g., "https://graph.microsoft.com/.default")
    /// </summary>
    public string? TokenScope { get; set; }
    
    /// <summary>
    /// Client ID for User-Managed Identity when using ManagedIdentityToken authentication
    /// </summary>
    public string? UserManagedIdentityClientId { get; set; }
    
    /// <summary>
    /// Subscription key for Azure API Management / Front Door (Ocp-Apim-Subscription-Key header)
    /// </summary>
    public string? SubscriptionKey { get; set; }
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

