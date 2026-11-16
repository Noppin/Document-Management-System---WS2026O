// Mit AI geschrieben.
using DocumentManagementSystem.Search;
using DocumentManagementSystem.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;

namespace DocumentManagementSystem.Api.Tests;

public sealed class ExternalServicesIntegrationTests
{
    [ServiceFact, Trait("Category", "Integration")]
    public async Task Elasticsearch_IndexesFuzzySearchesAndUpdatesWithoutDuplicates()
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("DMS_TEST_ELASTICSEARCH") ?? "http://localhost:19200/")
        };
        var indexName = $"dms-test-{Guid.NewGuid():N}";
        var index = new ElasticsearchDocumentIndex(client,
            Options.Create(new ElasticsearchOptions { IndexName = indexName }),
            NullLogger<ElasticsearchDocumentIndex>.Instance);
        var id = Guid.NewGuid();
        try
        {
            await index.IndexAsync(new SearchDocument(id, "scan.pdf", "Cinnamon original", DateTimeOffset.UtcNow));
            using (var refresh = await client.PostAsync($"{indexName}/_refresh", null)) refresh.EnsureSuccessStatusCode();
            Assert.Equal(id, Assert.Single(await index.SearchAsync("Cinnamom")).DocumentId);

            await index.IndexAsync(new SearchDocument(id, "scan.pdf", "Replacement content", DateTimeOffset.UtcNow));
            using (var refresh = await client.PostAsync($"{indexName}/_refresh", null)) refresh.EnsureSuccessStatusCode();
            Assert.Empty(await index.SearchAsync("Cinnamon"));
            Assert.Equal(id, Assert.Single(await index.SearchAsync("Replacement")).DocumentId);
            using var countResponse = await client.GetAsync($"{indexName}/_count");
            countResponse.EnsureSuccessStatusCode();
            using var count = System.Text.Json.JsonDocument.Parse(await countResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, count.RootElement.GetProperty("count").GetInt32());
        }
        finally
        {
            using var cleanup = await client.DeleteAsync(indexName);
        }
    }

    [ServiceFact, Trait("Category", "Integration")]
    public async Task Minio_UploadDownloadAndDeletePreserveOriginalBytes()
    {
        var options = new MinioOptions
        {
            Endpoint = Environment.GetEnvironmentVariable("DMS_TEST_MINIO") ?? "localhost:19000"
        };
        using var client = new MinioClient().WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey).Build();
        var storage = new MinioDocumentStorage(client, Options.Create(options), NullLogger<MinioDocumentStorage>.Instance);
        var id = Guid.NewGuid();
        var bytes = "%PDF-1.4\nservice integration test"u8.ToArray();
        try
        {
            await using var stream = new MemoryStream(bytes);
            await storage.StoreAsync(id, stream, bytes.Length, "application/pdf");
            Assert.Equal(bytes, await storage.GetAsync(id));
        }
        finally
        {
            await storage.DeleteAsync(id);
        }
        await Assert.ThrowsAsync<DocumentNotFoundException>(() => storage.GetAsync(id));
    }

    public sealed class ServiceFactAttribute : FactAttribute
    {
        public ServiceFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("DMS_INTEGRATION_TESTS") != "true")
                Skip = "Start the isolated Compose test system and set DMS_INTEGRATION_TESTS=true.";
        }
    }
}
