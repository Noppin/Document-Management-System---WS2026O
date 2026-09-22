using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class CollectionRepositoryTests
{
    private readonly InMemoryTestDatabase _database = new();

    [Fact]
    public void Add_PersistsCollection_AndGetByIdReturnsIt()
    {
        var collection = CreateCollection("Invoices");

        using (var context = _database.CreateContext())
        {
            new CollectionRepository(context).Add(collection);
        }

        using var readContext = _database.CreateContext();
        var loaded = new CollectionRepository(readContext).GetById(collection.Id);

        Assert.NotNull(loaded);
        Assert.Equal(collection.Id, loaded!.Id);
        Assert.Equal("Invoices", loaded.Name);
    }

    [Fact]
    public void GetAll_ReturnsAllCollections_OrderedByName()
    {
        using (var context = _database.CreateContext())
        {
            var repository = new CollectionRepository(context);
            repository.Add(CreateCollection("Zeta"));
            repository.Add(CreateCollection("Alpha"));
        }

        using var readContext = _database.CreateContext();
        var collections = new CollectionRepository(readContext).GetAll();

        Assert.Equal(new[] { "Alpha", "Zeta" }, collections.Select(collection => collection.Name));
    }

    [Fact]
    public void AddDocument_LinksDocumentToCollection()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument();

        using (var context = _database.CreateContext())
        {
            Assert.True(new CollectionRepository(context).AddDocument(collectionId, documentId));
        }

        using var readContext = _database.CreateContext();
        var collection = new CollectionRepository(readContext).GetById(collectionId);

        Assert.NotNull(collection);
        var link = Assert.Single(collection!.CollectionDocuments);
        Assert.Equal(collectionId, link.CollectionId);
        Assert.Equal(documentId, link.DocumentId);
    }

    [Fact]
    public void AddDocument_UnknownCollection_ReturnsFalse()
    {
        var documentId = SeedDocument();

        using var context = _database.CreateContext();
        Assert.False(new CollectionRepository(context).AddDocument(Guid.NewGuid(), documentId));
    }

    [Fact]
    public void AddDocument_UnknownDocument_ReturnsFalse()
    {
        var collectionId = SeedCollection("Invoices");

        using var context = _database.CreateContext();
        Assert.False(new CollectionRepository(context).AddDocument(collectionId, Guid.NewGuid()));
    }

    [Fact]
    public void AddDocument_Twice_KeepsSingleLink()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument();

        using (var context = _database.CreateContext())
        {
            var repository = new CollectionRepository(context);
            Assert.True(repository.AddDocument(collectionId, documentId));
            Assert.True(repository.AddDocument(collectionId, documentId));
        }

        using var readContext = _database.CreateContext();
        var collection = new CollectionRepository(readContext).GetById(collectionId);

        Assert.NotNull(collection);
        Assert.Single(collection!.CollectionDocuments);
    }

    [Fact]
    public void RemoveDocument_RemovesLink_ReturnsTrue()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument();

        using (var context = _database.CreateContext())
        {
            var repository = new CollectionRepository(context);
            repository.AddDocument(collectionId, documentId);
            Assert.True(repository.RemoveDocument(collectionId, documentId));
        }

        using var readContext = _database.CreateContext();
        var collection = new CollectionRepository(readContext).GetById(collectionId);

        Assert.NotNull(collection);
        Assert.Empty(collection!.CollectionDocuments);
    }

    [Fact]
    public void RemoveDocument_UnknownLink_ReturnsFalse()
    {
        var collectionId = SeedCollection("Invoices");

        using var context = _database.CreateContext();
        Assert.False(new CollectionRepository(context).RemoveDocument(collectionId, Guid.NewGuid()));
    }

    private Guid SeedCollection(string name)
    {
        var collection = CreateCollection(name);
        using var context = _database.CreateContext();
        new CollectionRepository(context).Add(collection);
        return collection.Id;
    }

    private Guid SeedDocument()
    {
        var document = new Document
        {
            FileName = "sample.pdf",
            ContentType = "application/pdf",
            FileSize = 12,
            Content = "%PDF-1.4"u8.ToArray()
        };

        using var context = _database.CreateContext();
        new DocumentRepository(context).Add(document);
        return document.Id;
    }

    private static Collection CreateCollection(string name) => new() { Name = name };
}
