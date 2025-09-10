namespace Trossitec.Azure.Function.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

// Specific API response for document submission
public class DocumentSubmissionResponse
{
    public required string RequestId { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
