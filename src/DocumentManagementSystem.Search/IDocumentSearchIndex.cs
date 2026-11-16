namespace DocumentManagementSystem.Search;

public interface IDocumentSearchIndex
{
    Task IndexAsync(
        SearchDocument document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchDocument>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
