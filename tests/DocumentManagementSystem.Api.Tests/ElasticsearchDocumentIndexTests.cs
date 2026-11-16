// Mit AI geschrieben.
using System.Net;
using System.Text.Json;
using DocumentManagementSystem.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.Api.Tests;

public sealed class ElasticsearchDocumentIndexTests
{
    [Fact]
    public async Task Search_TrimsQueryAndEnablesFuzziness()
    {
        using var handler = new RecordingHandler(_ => Json("{\"hits\":{\"hits\":[]}}"));
        var index = CreateIndex(handler);

        Assert.Empty(await index.SearchAsync("  Hlelo  "));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/documents/_search", request.Path);
        using var body = JsonDocument.Parse(request.Body);
        var query = body.RootElement.GetProperty("query").GetProperty("multi_match");
        Assert.Equal("AUTO", query.GetProperty("fuzziness").GetString());
        Assert.Equal("Hlelo", query.GetProperty("query").GetString());
    }

    [Fact]
    public async Task Index_CreatesExplicitMappingAndReusesDocumentId()
    {
        using var handler = new RecordingHandler(_ => Json("{}"));
        var index = CreateIndex(handler);
        var id = Guid.NewGuid();

        await index.IndexAsync(new SearchDocument(id, "test.pdf", "before", DateTimeOffset.UtcNow));
        await index.IndexAsync(new SearchDocument(id, "test.pdf", "after", DateTimeOffset.UtcNow));

        Assert.Equal(3, handler.Requests.Count);
        using var mapping = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal("keyword", mapping.RootElement.GetProperty("mappings")
            .GetProperty("properties").GetProperty("documentId").GetProperty("type").GetString());
        Assert.All(handler.Requests.Skip(1), request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal($"/documents/_doc/{id}", request.Path);
        });
    }

    [Fact]
    public async Task Index_ExistingIndexReceivesMappingUpdate()
    {
        using var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath == "/documents"
            ? Json("{\"error\":{\"type\":\"resource_already_exists_exception\"}}", HttpStatusCode.BadRequest)
            : Json("{}"));

        await CreateIndex(handler).IndexAsync(new SearchDocument(Guid.NewGuid(), "a.pdf", "text", DateTimeOffset.UtcNow));

        Assert.Equal("/documents/_mapping", handler.Requests[1].Path);
    }

    [Fact]
    public async Task Index_FailedInitializationCanBeRetried()
    {
        var fail = true;
        using var handler = new RecordingHandler(_ => fail
            ? Json("{}", HttpStatusCode.ServiceUnavailable) : Json("{}"));
        var index = CreateIndex(handler);
        var document = new SearchDocument(Guid.NewGuid(), "a.pdf", "text", DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<SearchIndexException>(() => index.IndexAsync(document));
        fail = false;
        await index.IndexAsync(document);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Search_UnavailableServerThrowsDomainException()
    {
        using var handler = new RecordingHandler(_ => Json("{}", HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<SearchIndexException>(() => CreateIndex(handler).SearchAsync("Hello"));
    }

    private static ElasticsearchDocumentIndex CreateIndex(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
        Options.Create(new ElasticsearchOptions()), NullLogger<ElasticsearchDocumentIndex>.Instance);

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath,
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            return respond(request);
        }
    }
}
