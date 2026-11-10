namespace DocumentManagementSystem.Persistence.Entities;

public sealed class CollectionDocumentEntity
{
    public Guid CollectionId { get; init; }
    public CollectionEntity Collection { get; init; } = null!;
    public Guid DocumentId { get; init; }
    public DocumentEntity Document { get; init; } = null!;
}
