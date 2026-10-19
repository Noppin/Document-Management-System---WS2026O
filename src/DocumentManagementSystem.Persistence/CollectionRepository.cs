using DocumentManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Persistence;

public sealed class CollectionRepository(
    DocumentManagementDbContext dbContext,
    ILogger<CollectionRepository>? logger = null) : ICollectionRepository
{
    private readonly ILogger<CollectionRepository> _logger =
        logger ?? NullLogger<CollectionRepository>.Instance;

    public Collection Add(Collection collection)
    {
        dbContext.Collections.Add(collection);
        dbContext.SaveChanges();

        _logger.LogInformation(
            "Stored collection {CollectionId} ({CollectionName}) in the database",
            collection.Id,
            collection.Name);
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

        _logger.LogInformation(
            "Linked document {DocumentId} to collection {CollectionId} in the database",
            documentId,
            collectionId);
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

        _logger.LogInformation(
            "Unlinked document {DocumentId} from collection {CollectionId} in the database",
            documentId,
            collectionId);
        return true;
    }
}
