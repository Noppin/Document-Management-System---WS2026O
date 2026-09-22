using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;

namespace DocumentManagementSystem.Api.Services;

public sealed class CollectionService(
    ICollectionRepository collectionRepository,
    IDocumentRepository documentRepository) : ICollectionService
{
    public CollectionResponse Create(CreateCollectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("A collection name is required.", nameof(request));

        var collection = collectionRepository.Add(new Collection { Name = request.Name.Trim() });
        return ToResponse(collection);
    }

    public IReadOnlyList<CollectionResponse> GetAll() =>
        collectionRepository.GetAll().Select(ToResponse).ToList();

    public CollectionDetailResponse? GetById(Guid id)
    {
        var collection = collectionRepository.GetById(id);
        if (collection is null)
            return null;

        var documents = collection.CollectionDocuments
            .Select(link => documentRepository.GetById(link.DocumentId))
            .OfType<Document>()
            .Select(ToDocumentResponse)
            .ToList();

        return new CollectionDetailResponse(collection.Id, collection.Name, collection.CreatedAt, documents);
    }

    public bool AddDocument(Guid collectionId, Guid documentId) =>
        documentRepository.GetById(documentId) is not null &&
        collectionRepository.AddDocument(collectionId, documentId);

    public bool RemoveDocument(Guid collectionId, Guid documentId) =>
        collectionRepository.RemoveDocument(collectionId, documentId);

    private static CollectionResponse ToResponse(Collection collection) =>
        new(collection.Id, collection.Name, collection.CreatedAt, collection.CollectionDocuments.Count);

    private static DocumentResponse ToDocumentResponse(Document document) =>
        new(document.Id, document.FileName, document.ContentType, document.FileSize, document.CreatedAt);
}
