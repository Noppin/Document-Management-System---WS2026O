namespace DocumentManagementSystem.Domain.Entities;

public sealed class Collection
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int DocumentCount { get; init; }
    public IReadOnlyList<Document> Documents { get; init; } = Array.Empty<Document>();
}
