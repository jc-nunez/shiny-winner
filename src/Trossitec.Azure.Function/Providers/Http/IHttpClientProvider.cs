using Trossitec.Azure.Function.Models;

namespace Trossitec.Azure.Function.Providers.Http;

public interface IHttpClientProvider
{
    Task<ApiResponse<DocumentSubmissionResponse>> SubmitDocumentAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProcessingStatus>> GetStatusAsync(string requestId, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
}
