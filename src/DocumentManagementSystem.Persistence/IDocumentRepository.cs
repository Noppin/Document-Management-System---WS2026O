using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public interface IDocumentRepository
{
    void Add(Document document);
    IReadOnlyList<Document> GetAll();
    Document? GetById(Guid id);
    bool Delete(Guid id);
}
