namespace DocumentManagementSystem.Search;

public sealed record SearchDocument(
    Guid DocumentId,
    string FileName,
    string Content,
    DateTimeOffset IndexedAt);
