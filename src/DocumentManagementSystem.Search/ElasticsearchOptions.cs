namespace DocumentManagementSystem.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Endpoint { get; init; } = "http://elasticsearch:9200";
    public string IndexName { get; init; } = "documents";
}
