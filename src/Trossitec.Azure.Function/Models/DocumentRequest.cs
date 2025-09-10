namespace Trossitec.Azure.Function.Models;

public class DocumentRequest
{
    public required string SourceContainer { get; set; }
    public required string BlobName { get; set; }
    public required string DestinationContainer { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required string EventType { get; set; } // Created, Modified
}
