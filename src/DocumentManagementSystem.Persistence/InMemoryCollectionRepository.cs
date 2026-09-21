using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public sealed class InMemoryCollectionRepository : ICollectionRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Collection> _collections = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _documentIds = new();

    public Collection Add(Collection collection)
    {
        lock (_gate)
        {
            _collections[collection.Id] = collection;
            _documentIds[collection.Id] = new HashSet<Guid>();
            return Snapshot(collection);
        }
    }

    public IReadOnlyList<Collection> GetAll()
    {
        lock (_gate)
        {
            return _collections.Values.Select(Snapshot).ToList();
        }
    }

    public Collection? GetById(Guid id)
    {
        lock (_gate)
        {
            return _collections.TryGetValue(id, out var collection) ? Snapshot(collection) : null;
        }
    }

    public bool AddDocument(Guid collectionId, Guid documentId)
    {
        lock (_gate)
        {
            if (!_collections.ContainsKey(collectionId))
            {
                return false;
            }

            _documentIds[collectionId].Add(documentId);
            return true;
        }
    }

    public bool RemoveDocument(Guid collectionId, Guid documentId)
    {
        lock (_gate)
        {
            return _collections.ContainsKey(collectionId) && _documentIds[collectionId].Remove(documentId);
        }
    }

    private Collection Snapshot(Collection collection)
    {
        var snapshot = new Collection
        {
            Id = collection.Id,
            Name = collection.Name,
            CreatedAt = collection.CreatedAt
        };

        foreach (var documentId in _documentIds[collection.Id])
        {
            snapshot.CollectionDocuments.Add(new CollectionDocument
            {
                CollectionId = collection.Id,
                DocumentId = documentId
            });
        }

        return snapshot;
    }
}
