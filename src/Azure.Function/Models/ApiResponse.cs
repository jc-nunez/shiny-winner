namespace Azure.Function.Models;

/// <summary>
/// Generic wrapper for API responses that provides consistent structure for success/failure scenarios.
/// </summary>
/// <typeparam name="T">The type of data contained in a successful response.</typeparam>
/// <remarks>
/// This wrapper standardizes how external API responses are handled throughout the application,
/// providing consistent error handling and response metadata.
/// </remarks>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API call was successful.
    /// </summary>
    /// <value>
    /// <c>true</c> if the API call succeeded and Data contains valid information;
    /// <c>false</c> if the call failed and ErrorCode/Message contain error details.
    /// </value>
    public bool Success { get; set; }

    /// <summary>
    /// The response data payload when Success is true.
    /// </summary>
    /// <value>
    /// The strongly-typed response data, or null if the call failed or returned no data.
    /// </value>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the response result.
    /// </summary>
    /// <value>
    /// For successful calls: confirmation or descriptive message.
    /// For failed calls: error description or user-friendly error message.
    /// </value>
    public string? Message { get; set; }

    /// <summary>
    /// Machine-readable error code when Success is false.
    /// </summary>
    /// <value>
    /// Application-specific error code for programmatic error handling, or null for success cases.
    /// </value>
    /// <example>
    /// "INVALID_REQUEST", "SERVICE_UNAVAILABLE", "RATE_LIMIT_EXCEEDED"
    /// </example>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp when this response was created.
    /// </summary>
    /// <value>
    /// UTC timestamp indicating when the response was processed. Defaults to DateTime.UtcNow.
    /// </value>
    /// <remarks>
    /// Useful for debugging timing issues and response caching logic.
    /// </remarks>
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Specific response model for document submission API calls to external document processing service.
/// </summary>
/// <remarks>
/// This represents the expected response structure when submitting a document for processing.
/// The RequestId is crucial for tracking the document through its processing lifecycle.
/// </remarks>
public class DocumentSubmissionResponse
{
    /// <summary>
    /// Unique identifier assigned to the document processing request.
    /// </summary>
    /// <value>
    /// A unique string identifier that can be used to query processing status later.
    /// </value>
    /// <remarks>
    /// This ID is stored in table storage and used for subsequent status polling operations.
    /// Must be preserved throughout the document processing workflow.
    /// </remarks>
    public required string RequestId { get; set; }

    /// <summary>
    /// Initial processing status returned by the external service.
    /// </summary>
    /// <value>
    /// Status string indicating the current state of the document processing request.
    /// </value>
    /// <example>
    /// "Submitted", "Queued", "Processing", "Accepted"
    /// </example>
    public required string Status { get; set; }

    /// <summary>
    /// Optional additional information about the submission.
    /// </summary>
    /// <value>
    /// Descriptive message from the external service, or null if no additional information provided.
    /// </value>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp when the document was submitted to the external service.
    /// </summary>
    /// <value>
    /// UTC timestamp of submission. Defaults to DateTime.UtcNow when the response is created.
    /// </value>
    /// <remarks>
    /// Used for tracking processing duration and debugging timing issues.
    /// </remarks>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
