using Azure.Function.Models;

namespace Azure.Function.Services;

/// <summary>
/// Contract for document extraction orchestration service.
/// Defines operations for submitting documents and tracking processing status.
/// </summary>
/// <remarks>
/// This interface defines the core business operations for document extraction workflow:
/// - Document submission including blob transfer and external API integration
/// - Status polling and progress tracking throughout processing lifecycle
/// </remarks>
public interface IDocumentExtractionHubService
{
    /// <summary>
    /// Submits a document for processing through the complete extraction workflow.
    /// </summary>
    /// <param name="request">Document request containing source location and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The source system RequestId used for tracking this processing request.</returns>
    /// <exception cref="InvalidOperationException">Thrown if RequestId metadata is missing or API submission fails.</exception>
    Task<string> SubmitAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves the current processing status of a document extraction request.
    /// </summary>
    /// <param name="requestId">Source system RequestId to check status for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Current processing status including state, message, and any results.</returns>
    Task<ProcessingStatus> GetStatusAsync(string requestId, CancellationToken cancellationToken = default);
}
