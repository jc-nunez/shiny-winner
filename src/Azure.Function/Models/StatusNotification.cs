namespace Azure.Function.Models;

public class StatusNotification
{
    public required string RequestId { get; set; }
    public required string Status { get; set; } // Submitted, Processing, Completed, Failed
    public required string BlobName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Message { get; set; }
    public object? Details { get; set; } // Additional status-specific data
}
