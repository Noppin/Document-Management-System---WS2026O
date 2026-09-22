using DocumentManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public sealed class CollectionRepository(DocumentManagementDbContext dbContext) : ICollectionRepository
{
    public Collection Add(Collection collection)
    {
        dbContext.Collections.Add(collection);
        dbContext.SaveChanges();
        return collection;
    }

    public IReadOnlyList<Collection> GetAll() => dbContext.Collections
        .AsNoTracking()
        .Include(collection => collection.CollectionDocuments)
        .OrderBy(collection => collection.Name)
        .ToList();

    public Collection? GetById(Guid id) => dbContext.Collections
        .AsNoTracking()
        .Include(collection => collection.CollectionDocuments)
        .SingleOrDefault(collection => collection.Id == id);

    public bool AddDocument(Guid collectionId, Guid documentId)
    {
        if (!dbContext.Collections.Any(collection => collection.Id == collectionId) ||
            !dbContext.Documents.Any(document => document.Id == documentId))
            return false;

        if (dbContext.CollectionDocuments.Any(link =>
                link.CollectionId == collectionId && link.DocumentId == documentId))
            return true;

        dbContext.CollectionDocuments.Add(new CollectionDocument
        {
            CollectionId = collectionId,
            DocumentId = documentId
        });
        dbContext.SaveChanges();
        return true;
    }

    public bool RemoveDocument(Guid collectionId, Guid documentId)
    {
        var link = dbContext.CollectionDocuments.SingleOrDefault(item =>
            item.CollectionId == collectionId && item.DocumentId == documentId);
        if (link is null)
            return false;

        dbContext.CollectionDocuments.Remove(link);
        dbContext.SaveChanges();
        return true;
    }
}