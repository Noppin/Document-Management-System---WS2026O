using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.Search;

public sealed class ElasticsearchDocumentIndex(
    HttpClient client,
    IOptions<ElasticsearchOptions> options,
    ILogger<ElasticsearchDocumentIndex> logger) : IDocumentSearchIndex
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ElasticsearchOptions _options = options.Value;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            var mappings = new
            {
                properties = new
                {
                    documentId = new { type = "keyword" },
                    fileName = new { type = "text" },
                    content = new { type = "text" },
                    indexedAt = new { type = "date" }
                }
            };

            using var create = await client.PutAsJsonAsync(
                _options.IndexName, new { mappings }, JsonOptions, cancellationToken);
            if (!create.IsSuccessStatusCode)
            {
                var body = await create.Content.ReadAsStringAsync(cancellationToken);
                // Another worker or a previous deployment may already have created the index.
                using var error = JsonDocument.Parse(body);
                if (create.StatusCode != System.Net.HttpStatusCode.BadRequest ||
                    !error.RootElement.TryGetProperty("error", out var details) ||
                    !details.TryGetProperty("type", out var type) ||
                    type.GetString() != "resource_already_exists_exception")
                    throw new SearchIndexException($"Could not create search index: {(int)create.StatusCode}");

                using var update = await client.PutAsJsonAsync(
                    // Old dynamically-created indices map documentId as text. Keep that
                    // compatible type: document identity/upserts use _id, not this field.
                    $"{_options.IndexName}/_mapping", new
                    {
                        properties = new
                        {
                            fileName = new { type = "text" },
                            content = new { type = "text" },
                            indexedAt = new { type = "date" }
                        }
                    }, JsonOptions, cancellationToken);
                if (!update.IsSuccessStatusCode)
                    throw new SearchIndexException($"Could not apply search mapping: {(int)update.StatusCode}");
            }

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task IndexAsync(
        SearchDocument document,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureIndexAsync(cancellationToken);
            using var response = await client.PutAsJsonAsync(
                $"{_options.IndexName}/_doc/{document.DocumentId}",
                document,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new SearchIndexException(
                    $"Elasticsearch rejected indexing document {document.DocumentId}: {(int)response.StatusCode} {body}");
            }

            logger.LogInformation(
                "Indexed OCR text for document {DocumentId} in Elasticsearch index {IndexName}",
                document.DocumentId,
                _options.IndexName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SearchIndexException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SearchIndexException(
                $"Could not index document {document.DocumentId} in Elasticsearch.",
                exception);
        }
    }

    public async Task DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.DeleteAsync(
                $"{_options.IndexName}/_doc/{documentId}",
                cancellationToken);

            // 404: the document was never indexed (e.g. OCR still running) or the index does not exist yet.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new SearchIndexException(
                    $"Elasticsearch rejected deleting document {documentId}: {(int)response.StatusCode} {body}");
            }

            logger.LogInformation(
                "Removed document {DocumentId} from Elasticsearch index {IndexName}",
                documentId,
                _options.IndexName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SearchIndexException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SearchIndexException(
                $"Could not delete document {documentId} from Elasticsearch.",
                exception);
        }
    }

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            var payload = new
            {
                size = 50,
                query = new
                {
                    multi_match = new
                    {
                        query = query.Trim(),
                        fields = new[] { "fileName^3", "content" },
                        fuzziness = "AUTO"
                    }
                }
            };

            using var response = await client.PostAsJsonAsync(
                $"{_options.IndexName}/_search",
                payload,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new SearchIndexException(
                    $"Elasticsearch search failed: {(int)response.StatusCode} {body}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty("hits", out var hitsContainer) ||
                !hitsContainer.TryGetProperty("hits", out var hits))
            {
                return [];
            }

            var results = new List<SearchDocument>();
            foreach (var hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("_source", out var source))
                {
                    continue;
                }

                var document = source.Deserialize<SearchDocument>(JsonOptions);
                if (document is not null)
                {
                    results.Add(document);
                }
            }

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SearchIndexException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SearchIndexException("Could not search Elasticsearch.", exception);
        }
    }
}
