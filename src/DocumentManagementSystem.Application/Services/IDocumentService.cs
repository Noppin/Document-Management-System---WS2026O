using DocumentManagementSystem.Application.Dtos;

namespace DocumentManagementSystem.Application.Services;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<DocumentResponse> GetAll();
    DocumentResponse? Get(Guid id);
    DocumentDownloadResponse? GetDownload(Guid id);
    bool Delete(Guid id);
}
