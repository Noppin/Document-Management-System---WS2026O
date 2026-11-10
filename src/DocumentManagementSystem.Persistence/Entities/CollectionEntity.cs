namespace DocumentManagementSystem.Persistence.Entities;

public sealed class CollectionEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<CollectionDocumentEntity> CollectionDocuments { get; init; } = new List<CollectionDocumentEntity>();
}
