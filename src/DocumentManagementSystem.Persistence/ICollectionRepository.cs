using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public interface ICollectionRepository
{
    Collection Add(Collection collection);
    IReadOnlyList<Collection> GetAll();
    Collection? GetById(Guid id);
    bool AddDocument(Guid collectionId, Guid documentId);
    bool RemoveDocument(Guid collectionId, Guid documentId);
}
