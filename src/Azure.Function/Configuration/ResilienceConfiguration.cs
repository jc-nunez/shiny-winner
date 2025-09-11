namespace Azure.Function.Configuration;

/// <summary>
/// Configuration for resilience patterns including retry policies and circuit breakers.
/// Used by HTTP clients and other external service calls to handle transient failures gracefully.
/// </summary>
/// <remarks>
/// This configuration implements common resilience patterns to improve the reliability of external service calls.
/// The retry policy handles temporary failures, while the circuit breaker prevents cascade failures.
/// </remarks>
public class ResilienceConfiguration
{
    /// <summary>
    /// Configuration for retry behavior when external calls fail.
    /// </summary>
    /// <value>A <see cref="RetryPolicyConfiguration"/> instance with retry settings.</value>
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();

    /// <summary>
    /// Configuration for circuit breaker behavior to prevent cascade failures.
    /// </summary>
    /// <value>A <see cref="CircuitBreakerConfiguration"/> instance with circuit breaker settings.</value>
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Configuration settings for retry policy behavior when handling transient failures.
/// </summary>
/// <remarks>
/// Retry policies help handle temporary network issues, service unavailability, or rate limiting.
/// The backoff strategy determines how long to wait between retry attempts.
/// </remarks>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts before giving up on a failed operation.
    /// </summary>
    /// <value>
    /// A positive integer representing the maximum retry count. Default is 3.
    /// </value>
    /// <remarks>
    /// Set to 0 to disable retries entirely. Higher values increase resilience but may cause longer delays.
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Type of backoff strategy to use between retry attempts.
    /// </summary>
    /// <value>
    /// A string specifying the backoff type. Supported values: "Exponential", "Constant".
    /// Default is "Exponential".
    /// </value>
    /// <remarks>
    /// - "Exponential": Delay increases exponentially (2s, 4s, 8s, etc.)
    /// - "Constant": Fixed delay between all attempts
    /// </remarks>
    public string BackoffType { get; set; } = "Exponential";

    /// <summary>
    /// Base delay to wait before the first retry attempt.
    /// </summary>
    /// <value>
    /// A TimeSpan representing the initial delay. Default is 2 seconds.
    /// </value>
    /// <remarks>
    /// For exponential backoff, this is the starting delay that gets multiplied.
    /// For constant backoff, this is the fixed delay used for all retries.
    /// </remarks>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum delay allowed between retry attempts, even with exponential backoff.
    /// </summary>
    /// <value>
    /// A TimeSpan representing the maximum delay cap. Default is 30 seconds.
    /// </value>
    /// <remarks>
    /// This prevents exponential backoff from creating excessively long delays.
    /// Ignored when using constant backoff strategy.
    /// </remarks>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Configuration settings for circuit breaker pattern to prevent cascade failures.
/// </summary>
/// <remarks>
/// Circuit breaker pattern prevents calling a failing service repeatedly, giving it time to recover.
/// When the failure threshold is reached, the circuit "opens" and calls fail immediately.
/// After the break duration, the circuit allows test calls to check if the service has recovered.
/// </remarks>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of consecutive failures allowed before the circuit breaker opens.
    /// </summary>
    /// <value>
    /// A positive integer representing the failure threshold. Default is 3.
    /// </value>
    /// <remarks>
    /// Once this many consecutive failures occur, the circuit breaker opens and starts
    /// failing fast without making actual calls to the downstream service.
    /// </remarks>
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 3;

    /// <summary>
    /// Duration to keep the circuit breaker open before allowing test calls.
    /// </summary>
    /// <value>
    /// A TimeSpan representing how long to wait before testing recovery. Default is 30 seconds.
    /// </value>
    /// <remarks>
    /// After this duration, the circuit breaker enters "half-open" state and allows
    /// a limited number of test calls to determine if the service has recovered.
    /// </remarks>
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
}

