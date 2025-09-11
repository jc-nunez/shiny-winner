namespace Azure.Function.Configuration;

/// <summary>
/// Configuration settings for document processing monitoring and cleanup operations.
/// Used by the DocumentStatusMonitorFunction to control polling behavior and data retention.
/// </summary>
/// <remarks>
/// This configuration controls how frequently the system checks for document processing status updates
/// and manages the lifecycle of tracking entities in table storage.
/// </remarks>
public class MonitoringConfiguration
{
    /// <summary>
    /// Cron expression defining how often the status monitoring function should run.
    /// Default is "0 */5 * * * *" which means every 5 minutes at the top of the minute.
    /// </summary>
    /// <value>
    /// A valid cron expression. Examples:
    /// - "0 */5 * * * *" = Every 5 minutes
    /// - "0 */1 * * * *" = Every minute  
    /// - "0 0 */1 * * *" = Every hour
    /// </value>
    /// <example>
    /// "0 */2 * * * *" for every 2 minutes
    /// </example>
    public string TimerInterval { get; set; } = "0 */5 * * * *";

    /// <summary>
    /// Maximum number of pending document requests to check in a single monitoring run.
    /// This prevents the function from processing too many items at once and timing out.
    /// </summary>
    /// <value>
    /// A positive integer. Default is 100.
    /// </value>
    /// <remarks>
    /// If there are more than this many pending requests, they will be processed in subsequent runs.
    /// Consider your function timeout settings when configuring this value.
    /// </remarks>
    public int MaxCheckCount { get; set; } = 100;

    /// <summary>
    /// Maximum age for keeping completed or failed document requests in the tracking table.
    /// Requests older than this will be cleaned up to prevent unbounded table growth.
    /// </summary>
    /// <value>
    /// A TimeSpan representing the retention period. Default is 24 hours.
    /// </value>
    /// <remarks>
    /// This affects both successful and failed requests. Consider your audit and 
    /// troubleshooting needs when setting this value.
    /// </remarks>
    /// <example>
    /// TimeSpan.FromDays(7) for 7-day retention
    /// </example>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(24);
}
