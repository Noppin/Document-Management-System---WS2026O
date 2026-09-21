namespace DocumentManagementSystem.Domain.Entities;

public sealed class CollectionDocument
{
    public Guid CollectionId { get; init; }
    public Collection Collection { get; init; } = null!;
    public Guid DocumentId { get; init; }
    public Document Document { get; init; } = null!;
}
