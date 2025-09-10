namespace Azure.Function.Configuration;

public class ApiConfiguration
{
    /// <summary>
    /// Base URL for the external API
    /// </summary>
    public required string BaseUrl { get; set; }
    
    /// <summary>
    /// Target resource/scope for managed identity token (e.g., "api://your-api-app-id/.default")
    /// </summary>
    public required string TokenScope { get; set; }
    
    /// <summary>
    /// Client ID for User-Managed Identity
    /// </summary>
    public required string UserManagedIdentityClientId { get; set; }
    
    /// <summary>
    /// Subscription key for Azure API Management / Front Door (Ocp-Apim-Subscription-Key header)
    /// </summary>
    public required string SubscriptionKey { get; set; }
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

