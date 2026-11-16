using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Search;

namespace DocumentManagementSystem.Application.Tests;

public sealed class DocumentSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_MapsElasticsearchResultsToApiResponses()
    {
        var documentId = Guid.NewGuid();
        var index = new FakeSearchIndex(
        [
            new SearchDocument(
                documentId,
                "HelloWorld.pdf",
                "Hello World",
                DateTimeOffset.UtcNow)
        ]);

        var service = new DocumentSearchService(index);

        var results = await service.SearchAsync("Hello");

        var result = Assert.Single(results);
        Assert.Equal(documentId, result.Id);
        Assert.Equal("HelloWorld.pdf", result.FileName);
        Assert.Equal("Hello", index.LastQuery);
    }

    private sealed class FakeSearchIndex(IReadOnlyList<SearchDocument> results) : IDocumentSearchIndex
    {
        public string? LastQuery { get; private set; }

        public Task IndexAsync(SearchDocument document, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<SearchDocument>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(results);
        }
    }
}
