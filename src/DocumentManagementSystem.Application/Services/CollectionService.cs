using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services;

public sealed class CollectionService(
    ICollectionRepository collectionRepository,
    IDocumentRepository documentRepository,
    IMapper mapper,
    ILogger<CollectionService> logger) : ICollectionService
{
    public CollectionResponse Create(CreateCollectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("A collection name is required.", nameof(request));

        var collection = collectionRepository.Add(new Collection { Name = request.Name.Trim() });

        logger.LogInformation(
            "Created collection {CollectionId} ({CollectionName})",
            collection.Id,
            collection.Name);

        return mapper.Map<CollectionResponse>(collection);
    }

    public IReadOnlyList<CollectionResponse> GetAll() =>
        collectionRepository.GetAll().Select(collection => mapper.Map<CollectionResponse>(collection)).ToList();

    public CollectionDetailResponse? GetById(Guid id)
    {
        var collection = collectionRepository.GetById(id);
        if (collection is null)
        {
            logger.LogWarning("Collection {CollectionId} does not exist", id);
            return null;
        }

        var documents = collection.CollectionDocuments
            .Select(link => documentRepository.GetById(link.DocumentId))
            .OfType<Document>()
            .Select(document => mapper.Map<DocumentResponse>(document))
            .ToList();

        return new CollectionDetailResponse(collection.Id, collection.Name, collection.CreatedAt, documents);
    }

    public bool AddDocument(Guid collectionId, Guid documentId)
    {
        var added = documentRepository.GetById(documentId) is not null &&
            collectionRepository.AddDocument(collectionId, documentId);

        if (!added)
            logger.LogWarning("Could not add document {DocumentId} to collection {CollectionId}", documentId, collectionId);

        return added;
    }

    public bool RemoveDocument(Guid collectionId, Guid documentId)
    {
        var removed = collectionRepository.RemoveDocument(collectionId, documentId);

        if (!removed)
            logger.LogWarning("Could not remove document {DocumentId} from collection {CollectionId}", documentId, collectionId);

        return removed;
    }
}
