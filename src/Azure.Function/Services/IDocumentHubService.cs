using Azure.Function.Models;

namespace Azure.Function.Services;

public interface IDocumentHubService
{
    Task<string> SubmitAsync(DocumentRequest request, CancellationToken cancellationToken = default);
    Task<ProcessingStatus> GetStatusAsync(string requestId, CancellationToken cancellationToken = default);
}
