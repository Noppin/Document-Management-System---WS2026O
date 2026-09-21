using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public interface IDocumentRepository
{
    void Add(Document document);
    Document? GetById(Guid id);
}
