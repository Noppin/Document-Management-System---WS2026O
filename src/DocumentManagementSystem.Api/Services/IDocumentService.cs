using DocumentManagementSystem.Api.Contracts;

namespace DocumentManagementSystem.Api.Services;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<DocumentResponse> GetAll();
    DocumentResponse? Get(Guid id);
    bool Delete(Guid id);
}
