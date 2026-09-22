namespace DocumentManagementSystem.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long FileSize { get; init; }
    public required byte[] Content { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public int Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<CollectionDocument> CollectionDocuments { get; init; } = new List<CollectionDocument>();
}
