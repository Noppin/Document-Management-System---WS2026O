using DocumentManagementSystem.Application.Dtos;

namespace DocumentManagementSystem.Application.Services;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<DocumentResponse> GetAll();
    DocumentResponse? Get(Guid id);
    Task<DocumentDownloadResponse?> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
