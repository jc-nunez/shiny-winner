namespace Azure.Function.Models;

/// <summary>
/// Represents a document processing request triggered by blob storage events.
/// Contains information needed to locate, process, and store the document.
/// </summary>
/// <remarks>
/// This model is typically populated from EventGrid blob events and contains all necessary
/// information for the document processing workflow including source location, destination,
/// and metadata for tracking purposes.
/// </remarks>
public class DocumentRequest
{
    /// <summary>
    /// Name of the source blob storage container where the original document is stored.
    /// </summary>
    /// <value>
    /// Container name as provided in the EventGrid blob event.
    /// </value>
    /// <remarks>
    /// Used to construct the full blob URI for reading the original document.
    /// Must exist in the source storage account configured in the system.
    /// </remarks>
    /// <example>
    /// "incoming-documents", "uploads", "user-files"
    /// </example>
    public required string SourceContainer { get; set; }

    /// <summary>
    /// Name/path of the blob within the source container.
    /// </summary>
    /// <value>
    /// Full blob name including any virtual directory structure.
    /// </value>
    /// <remarks>
    /// Combined with SourceContainer to form the complete blob identifier.
    /// May include forward slashes for virtual folder structures.
    /// </remarks>
    /// <example>
    /// "document.pdf", "2024/03/invoice_001.pdf", "user123/contract.docx"
    /// </example>
    public required string BlobName { get; set; }

    /// <summary>
    /// Name of the destination blob storage container where processed results will be stored.
    /// </summary>
    /// <value>
    /// Target container name for storing processing results and metadata.
    /// </value>
    /// <remarks>
    /// Must exist in the destination storage account. Processing results and
    /// extracted data will be stored here using a structured naming convention.
    /// </remarks>
    /// <example>
    /// "processed-documents", "extraction-results", "output"
    /// </example>
    public required string DestinationContainer { get; set; }

    /// <summary>
    /// Key-value metadata associated with the document for processing context.
    /// </summary>
    /// <value>
    /// Dictionary containing metadata from the blob or EventGrid event.
    /// Initialized as empty dictionary.
    /// </value>
    /// <remarks>
    /// May include blob metadata, custom properties, or EventGrid event data.
    /// Used for processing decisions, routing, and audit trails.
    /// </remarks>
    /// <example>
    /// { "userId": "12345", "department": "finance", "priority": "high" }
    /// </example>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Timestamp when this document request was created in the system.
    /// </summary>
    /// <value>
    /// UTC timestamp when the DocumentRequest object was instantiated.
    /// Defaults to DateTime.UtcNow.
    /// </value>
    /// <remarks>
    /// Used for tracking processing latency and request lifecycle timing.
    /// May differ from the actual blob creation time in storage.
    /// </remarks>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of blob event that triggered this document processing request.
    /// </summary>
    /// <value>
    /// EventGrid blob event type string indicating what happened to the blob.
    /// </value>
    /// <remarks>
    /// Used for processing logic decisions - newly created files may need different
    /// handling than modified files. Maps to EventGrid blob event types.
    /// </remarks>
    /// <example>
    /// "Microsoft.Storage.BlobCreated", "Microsoft.Storage.BlobUpdated"
    /// </example>
    public required string EventType { get; set; }
}
