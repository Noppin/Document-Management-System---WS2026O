using DocumentManagementSystem.Application.Dtos;

namespace DocumentManagementSystem.Application.Services;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<DocumentSearchResponse>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
