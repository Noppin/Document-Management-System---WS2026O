using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Domain.Repositories;

public interface ICollectionRepository
{
    Collection Add(Collection collection);
    IReadOnlyList<Collection> GetAll();
    Collection? GetById(Guid id);
    bool ExistsByName(string name);
    bool AddDocument(Guid collectionId, Guid documentId);
    bool RemoveDocument(Guid collectionId, Guid documentId);
}
