namespace Azure.Function.Models;

/// <summary>
/// Metadata information about a document being processed through the system.
/// Contains file information and custom properties for document tracking and processing.
/// </summary>
/// <remarks>
/// This class captures essential information about uploaded documents, including file characteristics
/// and extensible custom properties for domain-specific metadata requirements.
/// </remarks>
public class DocumentMetadata
{
    /// <summary>
    /// Original name of the uploaded document file.
    /// </summary>
    /// <value>
    /// The filename including extension as provided during upload.
    /// </value>
    /// <remarks>
    /// Preserved for audit trails and user reference. May differ from blob storage naming.
    /// </remarks>
    /// <example>
    /// "invoice_2024_001.pdf", "contract_draft.docx"
    /// </example>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type of the document content.
    /// </summary>
    /// <value>
    /// Standard MIME type string identifying the document format.
    /// </value>
    /// <remarks>
    /// Used for content validation and processing pipeline routing.
    /// Should match the actual file content, not just the extension.
    /// </remarks>
    /// <example>
    /// "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    /// </example>
    public required string ContentType { get; set; }

    /// <summary>
    /// Size of the document content in bytes.
    /// </summary>
    /// <value>
    /// File size as a positive long integer representing bytes.
    /// </value>
    /// <remarks>
    /// Used for storage cost calculation, processing time estimation, and validation.
    /// </remarks>
    public long ContentLength { get; set; }

    /// <summary>
    /// Timestamp when the document was uploaded to the system.
    /// </summary>
    /// <value>
    /// UTC timestamp of the upload event. Defaults to DateTime.UtcNow when created.
    /// </value>
    /// <remarks>
    /// Used for tracking document age, audit logs, and processing SLA monitoring.
    /// </remarks>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional human-readable description of the document.
    /// </summary>
    /// <value>
    /// Descriptive text provided by the user or system, or null if not specified.
    /// </value>
    /// <remarks>
    /// Useful for document organization and search functionality.
    /// </remarks>
    /// <example>
    /// "Q4 2024 Financial Report", "Updated Terms of Service"
    /// </example>
    public string? Description { get; set; }

    /// <summary>
    /// Extensible key-value pairs for custom document properties.
    /// </summary>
    /// <value>
    /// Dictionary containing custom metadata specific to business requirements.
    /// Initialized as empty dictionary.
    /// </value>
    /// <remarks>
    /// Allows for domain-specific metadata without modifying the core model.
    /// Common uses include department codes, priority levels, or workflow tags.
    /// </remarks>
    /// <example>
    /// { "Department": "Finance", "Priority": "High", "ReviewRequired": "true" }
    /// </example>
    public IDictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
}
