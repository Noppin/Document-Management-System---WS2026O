using System.Collections.Concurrent;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public void Add(Document document) => _documents[document.Id] = document;

    public IReadOnlyList<Document> GetAll() => _documents.Values
        .OrderByDescending(document => document.CreatedAt)
        .ToList();

    public Document? GetById(Guid id) => _documents.TryGetValue(id, out var document) ? document : null;

    public bool Delete(Guid id) => _documents.TryRemove(id, out _);
}
