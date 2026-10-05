using DocumentManagementSystem.Application.Contracts;

namespace DocumentManagementSystem.Application.Services;

public interface ICollectionService
{
    CollectionResponse Create(CreateCollectionRequest request);
    IReadOnlyList<CollectionResponse> GetAll();
    CollectionDetailResponse? GetById(Guid id);
    bool AddDocument(Guid collectionId, Guid documentId);
    bool RemoveDocument(Guid collectionId, Guid documentId);
}
