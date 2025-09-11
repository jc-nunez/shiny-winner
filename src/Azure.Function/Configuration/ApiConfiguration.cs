namespace Azure.Function.Configuration;

/// <summary>
/// Simplified API configuration primarily using subscription key authentication
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// Base URL for the external API
    /// </summary>
    public required string BaseUrl { get; set; }
    
    /// <summary>
    /// Subscription key for Azure API Management / Front Door (Ocp-Apim-Subscription-Key header)
    /// </summary>
    public required string SubscriptionKey { get; set; }
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    // Optional managed identity properties (for advanced scenarios)
    
    /// <summary>
    /// Target resource/scope for managed identity token (e.g., "api://your-api-app-id/.default")
    /// Only needed if using managed identity authentication
    /// </summary>
    public string? TokenScope { get; set; }
    
    /// <summary>
    /// Client ID for User-Managed Identity (optional)
    /// Only needed if using user-managed identity instead of system-managed
    /// </summary>
    public string? UserManagedIdentityClientId { get; set; }
}

