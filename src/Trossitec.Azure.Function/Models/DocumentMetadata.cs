namespace Trossitec.Azure.Function.Models;

public class DocumentMetadata
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long ContentLength { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public IDictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
}
