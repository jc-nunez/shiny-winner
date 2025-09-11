namespace Azure.Function.Models;

/// <summary>
/// Represents the current processing status of a document processing request from the external API.
/// Used for polling and tracking the progress of document processing operations.
/// </summary>
/// <remarks>
/// This model maps to the status response structure from the external document processing service.
/// It's used by the monitoring function to track progress and determine when processing is complete.
/// </remarks>
public class ProcessingStatus
{
    /// <summary>
    /// Unique identifier for the document processing request.
    /// </summary>
    /// <value>
    /// Request ID that matches the identifier returned when the document was initially submitted.
    /// </value>
    /// <remarks>
    /// This ID links the status response back to the original request and table storage entity.
    /// Must remain consistent throughout the entire processing lifecycle.
    /// </remarks>
    public required string RequestId { get; set; }

    /// <summary>
    /// Current processing status of the document request.
    /// </summary>
    /// <value>
    /// Status string indicating the current state in the processing pipeline.
    /// </value>
    /// <remarks>
    /// Typical progression: Submitted → Queued → Processing → Completed/Failed.
    /// The exact values depend on the external API's status vocabulary.
    /// </remarks>
    /// <example>
    /// "Pending", "Processing", "Completed", "Failed", "Cancelled"
    /// </example>
    public required string Status { get; set; }

    /// <summary>
    /// Optional descriptive message about the current processing state.
    /// </summary>
    /// <value>
    /// Human-readable status message from the external service, or null if not provided.
    /// </value>
    /// <remarks>
    /// May contain progress details, error descriptions, or completion summaries.
    /// Useful for debugging and user feedback.
    /// </remarks>
    /// <example>
    /// "Processing page 3 of 10", "Document extraction completed successfully", "Invalid file format detected"
    /// </example>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp when this status information was last updated.
    /// </summary>
    /// <value>
    /// UTC timestamp indicating when the status was retrieved or updated.
    /// Defaults to DateTime.UtcNow when created.
    /// </value>
    /// <remarks>
    /// Used for tracking status polling frequency and detecting stale processing requests.
    /// </remarks>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing result data when the document processing is completed.
    /// </summary>
    /// <value>
    /// Raw response data from the external API containing extracted information,
    /// or null if processing is not yet complete or failed.
    /// </value>
    /// <remarks>
    /// Contains the actual extracted data/results when Status indicates completion.
    /// Structure depends on the external API's response format.
    /// Typically includes extracted text, metadata, or structured data.
    /// </remarks>
    public object? Result { get; set; }
}
