using System.Collections.Concurrent;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public void Add(Document document) => _documents[document.Id] = document;

    public Document? GetById(Guid id) => _documents.TryGetValue(id, out var document) ? document : null;
}
