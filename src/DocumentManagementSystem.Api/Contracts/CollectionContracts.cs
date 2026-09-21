namespace DocumentManagementSystem.Api.Contracts;

public sealed class CreateCollectionRequest
{
    public required string Name { get; init; }
}

public sealed record CollectionResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    int DocumentCount);

public sealed record CollectionDetailResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DocumentResponse> Documents);

public sealed record AssignDocumentRequest(Guid DocumentId);
