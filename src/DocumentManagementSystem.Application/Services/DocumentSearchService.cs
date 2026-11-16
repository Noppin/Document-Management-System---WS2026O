using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Search;

namespace DocumentManagementSystem.Application.Services;

public sealed class DocumentSearchService(
    IDocumentSearchIndex searchIndex) : IDocumentSearchService
{
    public async Task<IReadOnlyList<DocumentSearchResponse>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var results = await searchIndex.SearchAsync(query, cancellationToken);
        return results
            .Select(result => new DocumentSearchResponse(result.DocumentId, result.FileName))
            .ToList();
    }
}
