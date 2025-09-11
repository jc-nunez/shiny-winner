using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Azure.Function.Models;
using Azure.Function.Services;

namespace Azure.Function.Functions;

public class DocumentProcessingFunction
{
    private readonly IDocumentExtractionHubService _documentHubService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DocumentProcessingFunction> _logger;

    public DocumentProcessingFunction(
        IDocumentExtractionHubService documentHubService,
        INotificationService notificationService,
        ILogger<DocumentProcessingFunction> logger)
    {
        _documentHubService = documentHubService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [Function(nameof(DocumentProcessingFunction))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        try
        {
            _logger.LogInformation("EventGrid trigger function processed event: {Subject}", eventGridEvent.Subject);
            
            // Validate this is a blob storage event
            if (!IsValidBlobStorageEvent(eventGridEvent))
            {
                _logger.LogWarning("Ignoring non-blob storage event: {EventType}", eventGridEvent.EventType);
                return;
            }

            // Extract blob information from EventGrid event
            var documentRequest = ExtractDocumentRequestFromEvent(eventGridEvent);
            if (documentRequest == null)
            {
                _logger.LogError("Failed to extract document request from EventGrid event");
                return;
            }

            _logger.LogInformation("Processing document: {BlobName} from container {SourceContainer}", 
                documentRequest.BlobName, documentRequest.SourceContainer);

            // Step 1: Submit document for processing (mark when event was received)
            var eventReceivedAt = DateTime.UtcNow;
            var requestId = await _documentHubService.SubmitAsync(documentRequest);
            
            // Update the tracking entity with the actual event received timestamp
            // Note: This could be enhanced to pass the timestamp to SubmitAsync if needed
            
            _logger.LogInformation("Document {BlobName} submitted successfully with source RequestId {RequestId} (event received at {EventReceivedAt})", 
                documentRequest.BlobName, requestId, eventReceivedAt);

            // Step 2: Send submitted notification
            await _notificationService.SendSubmittedNotificationAsync(requestId, documentRequest);

            _logger.LogInformation("Document processing completed for {BlobName} with request ID {RequestId}", 
                documentRequest.BlobName, requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EventGrid event: {Subject}", eventGridEvent.Subject);
            
            // Log error details - complex error notifications would require a tracking entity

            throw; // Re-throw to ensure the function fails and can be retried
        }
    }

    private static bool IsValidBlobStorageEvent(EventGridEvent eventGridEvent)
    {
        // Check for blob storage events
        return eventGridEvent.EventType?.StartsWith("Microsoft.Storage.Blob") == true &&
               (eventGridEvent.EventType.Contains("Created") || eventGridEvent.EventType.Contains("Deleted"));
    }

    private DocumentRequest? ExtractDocumentRequestFromEvent(EventGridEvent eventGridEvent)
    {
        try
        {
            // EventGrid blob events have the subject in format: /blobServices/default/containers/{container-name}/blobs/{blob-name}
            var subject = eventGridEvent.Subject;
            if (string.IsNullOrEmpty(subject))
            {
                _logger.LogError("EventGrid event missing subject");
                return null;
            }

            // Parse the subject to extract container and blob name
            var subjectParts = subject.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (subjectParts.Length < 6)
            {
                _logger.LogError("Invalid EventGrid subject format: {Subject}", subject);
                return null;
            }

            var containerIndex = Array.IndexOf(subjectParts, "containers");
            if (containerIndex == -1 || containerIndex + 1 >= subjectParts.Length)
            {
                _logger.LogError("Could not find container name in subject: {Subject}", subject);
                return null;
            }

            var blobIndex = Array.IndexOf(subjectParts, "blobs");
            if (blobIndex == -1 || blobIndex + 1 >= subjectParts.Length)
            {
                _logger.LogError("Could not find blob name in subject: {Subject}", subject);
                return null;
            }

            var sourceContainer = subjectParts[containerIndex + 1];
            var blobName = string.Join("/", subjectParts.Skip(blobIndex + 1)); // Handle blobs with slashes in name

            // Extract additional data from the event
            var eventData = eventGridEvent.Data?.ToObjectFromJson<JsonElement>();
            var metadata = ExtractMetadataFromEventData(eventData);

            // Determine destination container (could be configurable in the future)
            var destinationContainer = $"{sourceContainer}-processed";

            var documentRequest = new DocumentRequest
            {
                SourceContainer = sourceContainer,
                BlobName = blobName,
                DestinationContainer = destinationContainer,
                Metadata = metadata,
                CreatedAt = eventGridEvent.EventTime.DateTime,
                EventType = ExtractEventType(eventGridEvent.EventType)
            };

            _logger.LogDebug("Extracted document request: Container={SourceContainer}, Blob={BlobName}, EventType={EventType}", 
                documentRequest.SourceContainer, documentRequest.BlobName, documentRequest.EventType);

            return documentRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting document request from EventGrid event");
            return null;
        }
    }

    private static Dictionary<string, string> ExtractMetadataFromEventData(JsonElement? eventData)
    {
        var metadata = new Dictionary<string, string>();

        if (eventData.HasValue)
        {
            // Add common EventGrid properties as metadata
            if (eventData.Value.TryGetProperty("api", out var apiElement))
                metadata["eventGridApi"] = apiElement.GetString() ?? "";
                
            if (eventData.Value.TryGetProperty("clientRequestId", out var clientIdElement))
                metadata["clientRequestId"] = clientIdElement.GetString() ?? "";
                
            if (eventData.Value.TryGetProperty("requestId", out var requestIdElement))
                metadata["RequestId"] = requestIdElement.GetString() ?? "";

            if (eventData.Value.TryGetProperty("eTag", out var eTagElement))
                metadata["eTag"] = eTagElement.GetString() ?? "";

            if (eventData.Value.TryGetProperty("contentType", out var contentTypeElement))
                metadata["contentType"] = contentTypeElement.GetString() ?? "";

            if (eventData.Value.TryGetProperty("contentLength", out var contentLengthElement))
                metadata["contentLength"] = contentLengthElement.ToString();

            if (eventData.Value.TryGetProperty("url", out var urlElement))
                metadata["blobUrl"] = urlElement.GetString() ?? "";
        }

        return metadata;
    }

    private static string ExtractEventType(string? eventGridEventType)
    {
        if (string.IsNullOrEmpty(eventGridEventType))
            return "Unknown";

        if (eventGridEventType.Contains("Created"))
            return "Created";
        if (eventGridEventType.Contains("Deleted"))
            return "Deleted";
        if (eventGridEventType.Contains("Modified"))
            return "Modified";

        return "Other";
    }

    private static string? ExtractBlobNameFromEvent(EventGridEvent eventGridEvent)
    {
        try
        {
            var subject = eventGridEvent.Subject;
            if (string.IsNullOrEmpty(subject)) return null;

            var subjectParts = subject.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var blobIndex = Array.IndexOf(subjectParts, "blobs");
            
            return blobIndex != -1 && blobIndex + 1 < subjectParts.Length 
                ? string.Join("/", subjectParts.Skip(blobIndex + 1))
                : null;
        }
        catch
        {
            return null;
        }
    }
}
