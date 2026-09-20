namespace DocumentManagementSystem.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long FileSize { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
