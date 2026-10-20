using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services;

public sealed class CollectionService(
    ICollectionRepository collectionRepository,
    IDocumentRepository documentRepository,
    IValidator<CreateCollectionRequest> validator,
    IMapper mapper,
    ILogger<CollectionService> logger) : ICollectionService
{
    public CollectionResponse Create(CreateCollectionRequest request)
    {
        validator.ValidateAndThrow(request);

        var name = request.Name.Trim();
        if (collectionRepository.ExistsByName(name))
            throw new ArgumentException($"A collection named '{name}' already exists.");

        var collection = collectionRepository.Add(new Collection { Name = name });

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

        return mapper.Map<CollectionDetailResponse>(collection);
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
