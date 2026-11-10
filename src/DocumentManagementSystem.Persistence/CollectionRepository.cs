using AutoMapper;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Persistence;

public sealed class CollectionRepository(
    DocumentManagementDbContext dbContext,
    IMapper mapper,
    ILogger<CollectionRepository>? logger = null) : ICollectionRepository
{
    private readonly ILogger<CollectionRepository> _logger =
        logger ?? NullLogger<CollectionRepository>.Instance;

    public Collection Add(Collection collection)
    {
        var entity = mapper.Map<CollectionEntity>(collection);
        dbContext.Collections.Add(entity);
        dbContext.SaveChanges();

        _logger.LogInformation(
            "Stored collection {CollectionId} ({CollectionName}) in the database",
            collection.Id,
            collection.Name);
        return mapper.Map<Collection>(entity);
    }

    public IReadOnlyList<Collection> GetAll() => mapper.Map<List<Collection>>(dbContext.Collections
        .AsNoTracking()
        .Include(collection => collection.CollectionDocuments)
        .OrderBy(collection => collection.Name)
        .ToList());

    public Collection? GetById(Guid id)
    {
        var entity = dbContext.Collections
            .AsNoTracking()
            .Include(collection => collection.CollectionDocuments)
            .ThenInclude(link => link.Document)
            .SingleOrDefault(collection => collection.Id == id);
        return entity is null ? null : mapper.Map<Collection>(entity);
    }

    public bool ExistsByName(string name) => dbContext.Collections
        .Any(collection => collection.Name == name);

    public bool AddDocument(Guid collectionId, Guid documentId)
    {
        if (!dbContext.Collections.Any(collection => collection.Id == collectionId) ||
            !dbContext.Documents.Any(document => document.Id == documentId))
            return false;

        if (dbContext.CollectionDocuments.Any(link =>
                link.CollectionId == collectionId && link.DocumentId == documentId))
            return true;

        dbContext.CollectionDocuments.Add(new CollectionDocumentEntity
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
