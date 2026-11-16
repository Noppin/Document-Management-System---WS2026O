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

    public async Task IndexAsync(
        SearchDocument document,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
                        fields = new[] { "fileName^3", "content" }
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
