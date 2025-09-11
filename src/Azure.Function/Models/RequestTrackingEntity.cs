using Azure;
using Azure.Data.Tables;

namespace Azure.Function.Models;

/// <summary>
/// Table storage entity for tracking document processing requests throughout their lifecycle.
/// Implements ITableEntity for Azure Table Storage persistence.
/// </summary>
/// <remarks>
/// This entity serves as the central tracking record for each document processing request,
/// storing timestamps, status information, and references needed for the complete workflow.
/// The entity is created when a blob event is received and updated throughout processing.
/// </remarks>
public class RequestTrackingEntity : ITableEntity
{
    /// <summary>
    /// Partition key for table storage organization. Fixed value for all document requests.
    /// </summary>
    /// <value>
    /// Always "DocumentRequests" to group all document processing entities in the same partition.
    /// </value>
    /// <remarks>
    /// Using a fixed partition key keeps all document requests together for efficient querying,
    /// but may create hot partitions under very high load. Consider partitioning by date/time
    /// if processing volume exceeds table storage partition limits.
    /// </remarks>
    public string PartitionKey { get; set; } = "DocumentRequests";

    /// <summary>
    /// Row key uniquely identifying this document processing request within the partition.
    /// </summary>
    /// <value>
    /// Unique identifier for the request, typically from blob metadata or generated.
    /// </value>
    /// <remarks>
    /// This serves as the primary key for the specific document processing request.
    /// Must be unique within the partition and URL-safe for table storage.
    /// Often corresponds to external system request IDs or generated GUIDs.
    /// </remarks>
    public string RowKey { get; set; } = string.Empty;

    // Document Information Section

    /// <summary>
    /// Name/path of the source blob that triggered this processing request.
    /// </summary>
    /// <value>
    /// Full blob name including any virtual directory structure from the source container.
    /// </value>
    /// <remarks>
    /// Combined with SourceContainer to form the complete source blob reference.
    /// Used for audit trails and potential reprocessing scenarios.
    /// </remarks>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the source storage container where the original document is located.
    /// </summary>
    /// <value>
    /// Container name from the source storage account.
    /// </value>
    /// <remarks>
    /// Part of the complete source blob reference. Used for accessing the original document
    /// if reprocessing or additional operations are needed.
    /// </remarks>
    public string SourceContainer { get; set; } = string.Empty;

    /// <summary>
    /// Name of the destination container where processing results will be stored.
    /// </summary>
    /// <value>
    /// Container name in the destination storage account.
    /// </value>
    /// <remarks>
    /// Used for storing extracted data, processed documents, or result artifacts.
    /// May be used to construct output file paths and validate storage operations.
    /// </remarks>
    public string DestinationContainer { get; set; } = string.Empty;

    // Timing and Lifecycle Section

    /// <summary>
    /// Timestamp when the source blob was originally created, as reported by EventGrid.
    /// </summary>
    /// <value>
    /// UTC timestamp from the blob creation event.
    /// </value>
    /// <remarks>
    /// This represents the actual file upload/creation time, which may differ from
    /// when the system received the event. Used for calculating total processing latency.
    /// </remarks>
    public DateTime BlobCreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the EventGrid blob event was received and processed by the function.
    /// </summary>
    /// <value>
    /// UTC timestamp when the Azure Function began processing the blob event.
    /// Defaults to DateTime.UtcNow when the entity is created.
    /// </value>
    /// <remarks>
    /// Measures the delay between blob creation and event processing.
    /// Used for monitoring EventGrid latency and function responsiveness.
    /// </remarks>
    public DateTime EventReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the document was successfully submitted to the external processing API.
    /// </summary>
    /// <value>
    /// UTC timestamp of successful API submission.
    /// </value>
    /// <remarks>
    /// Marks the transition from internal processing to external API dependency.
    /// Used for measuring API submission latency and tracking workflow progress.
    /// </remarks>
    public DateTime ApiSubmittedAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent status check with the external API.
    /// </summary>
    /// <value>
    /// UTC timestamp of the last status polling operation.
    /// Defaults to DateTime.UtcNow when the entity is created.
    /// </value>
    /// <remarks>
    /// Updated by the monitoring function each time it polls for status updates.
    /// Used to detect stale requests and control polling frequency.
    /// </remarks>
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;

    // API Integration Section

    /// <summary>
    /// Unique tracking identifier assigned by the external processing API.
    /// </summary>
    /// <value>
    /// External system's request ID used for status polling and result retrieval.
    /// </value>
    /// <remarks>
    /// This is typically different from the RowKey and represents the external system's
    /// internal tracking ID. Essential for polling status and retrieving results.
    /// Must be preserved for the entire processing lifecycle.
    /// </remarks>
    public string ApiGeneratedKey { get; set; } = string.Empty;

    /// <summary>
    /// Current processing status of the document request.
    /// </summary>
    /// <value>
    /// Simple status string tracking the current state. Defaults to "Processing".
    /// </value>
    /// <remarks>
    /// Simplified status representation for internal tracking. Typical values:
    /// - "Processing": Request submitted, awaiting completion
    /// - "Completed": Processing finished successfully
    /// - "Failed": Processing failed or encountered an error
    /// - "Cancelled": Processing was cancelled or timed out
    /// </remarks>
    public string CurrentStatus { get; set; } = "Processing";

    /// <summary>
    /// Number of times the status has been checked with the external API.
    /// </summary>
    /// <value>
    /// Integer counter starting at 0, incremented with each status poll.
    /// </value>
    /// <remarks>
    /// Used for implementing maximum retry limits and detecting stuck requests.
    /// Helps prevent infinite polling of failed or stale requests.
    /// Consider cleanup logic when this count exceeds reasonable thresholds.
    /// </remarks>
    public int CheckCount { get; set; } = 0;

    // Azure Table Storage Interface Properties

    /// <summary>
    /// Azure Table Storage timestamp for the entity.
    /// </summary>
    /// <value>
    /// Automatically managed by Table Storage to track entity modification time.
    /// </value>
    /// <remarks>
    /// This is managed by Azure Table Storage and reflects the last update time
    /// of the entity in the table. Do not modify manually.
    /// </remarks>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Entity tag for optimistic concurrency control in Table Storage.
    /// </summary>
    /// <value>
    /// Automatically managed by Table Storage for concurrency control.
    /// </value>
    /// <remarks>
    /// Used by Table Storage to prevent conflicting updates to the same entity.
    /// The ETag changes with each update and is used for conditional operations.
    /// Do not modify manually.
    /// </remarks>
    public ETag ETag { get; set; }
}
