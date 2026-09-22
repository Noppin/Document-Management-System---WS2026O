using DocumentManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public sealed class DocumentRepository(DocumentManagementDbContext dbContext) : IDocumentRepository
{
    public void Add(Document document)
    {
        dbContext.Documents.Add(document);
        dbContext.SaveChanges();
    }

    public IReadOnlyList<Document> GetAll() => dbContext.Documents
        .AsNoTracking()
        .OrderByDescending(document => document.CreatedAt)
        .ToList();

    public Document? GetById(Guid id) => dbContext.Documents
        .AsNoTracking()
        .SingleOrDefault(document => document.Id == id);

    public bool Delete(Guid id)
    {
        var document = dbContext.Documents.Find(id);
        if (document is null)
            return false;

        dbContext.Documents.Remove(document);
        dbContext.SaveChanges();
        return true;
    }
}