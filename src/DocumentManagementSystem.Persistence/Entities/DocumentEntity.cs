namespace DocumentManagementSystem.Persistence.Entities;

public sealed class DocumentEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long FileSize { get; init; }
    // Read only for migration of pre-MinIO uploads. New documents store no binary data here.
    public byte[]? Content { get; set; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public int Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<CollectionDocumentEntity> CollectionDocuments { get; init; } = new List<CollectionDocumentEntity>();
}
