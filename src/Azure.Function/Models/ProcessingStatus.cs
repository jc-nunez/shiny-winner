namespace Azure.Function.Models;

public class ProcessingStatus
{
    public required string RequestId { get; set; }
    public required string Status { get; set; } // Pending, Processing, Completed, Failed
    public string? Message { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public object? Result { get; set; } // API response data when completed
}
