using Azure.Function.Models;

namespace Azure.Function.Providers.Http;

/// <summary>
/// Contract for HTTP client provider that handles external document processing API integration.
/// Provides methods for document submission, status polling, and general API operations.
/// </summary>
/// <remarks>
/// This interface abstracts HTTP communication with external document processing services,
/// supporting both subscription key authentication and optional managed identity scenarios.
/// </remarks>
public interface IHttpClientProvider
{
    /// <summary>
    /// Submits a document processing request to the external API.
    /// </summary>
    /// <param name="request">Document request containing blob information and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>API response containing the submission result and assigned request ID.</returns>
    /// <exception cref="HttpRequestException">Thrown for HTTP communication failures.</exception>
    /// <exception cref="NotImplementedException">Thrown if not yet implemented.</exception>
    Task<ApiResponse<DocumentSubmissionResponse>> SubmitDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves the current processing status of a submitted document request.
    /// </summary>
    /// <param name="requestId">Unique identifier of the document processing request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>API response containing the current processing status and any results.</returns>
    /// <exception cref="HttpRequestException">Thrown for HTTP communication failures.</exception>
    /// <exception cref="NotImplementedException">Thrown if not yet implemented.</exception>
    Task<ApiResponse<ProcessingStatus>> GetStatusAsync(string requestId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs a generic GET request to the specified API endpoint.
    /// </summary>
    /// <typeparam name="T">Expected response data type.</typeparam>
    /// <param name="endpoint">API endpoint path relative to base URL.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>API response with strongly-typed data.</returns>
    /// <exception cref="HttpRequestException">Thrown for HTTP communication failures.</exception>
    /// <exception cref="NotImplementedException">Thrown if not yet implemented.</exception>
    Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs a generic POST request to the specified API endpoint with data.
    /// </summary>
    /// <typeparam name="T">Expected response data type.</typeparam>
    /// <param name="endpoint">API endpoint path relative to base URL.</param>
    /// <param name="data">Data object to serialize and send in request body.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>API response with strongly-typed data.</returns>
    /// <exception cref="HttpRequestException">Thrown for HTTP communication failures.</exception>
    /// <exception cref="NotImplementedException">Thrown if not yet implemented.</exception>
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
}
