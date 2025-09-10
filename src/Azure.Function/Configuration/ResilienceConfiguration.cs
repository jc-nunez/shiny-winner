namespace Azure.Function.Configuration;

public class ResilienceConfiguration
{
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();
}

public class RetryPolicyConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public string BackoffType { get; set; } = "Exponential"; // or Constant
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

public class CircuitBreakerConfiguration
{
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 3;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
}

