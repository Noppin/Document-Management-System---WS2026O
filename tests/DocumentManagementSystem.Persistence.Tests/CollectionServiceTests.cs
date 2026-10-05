using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class CollectionServiceTests
{
    private readonly InMemoryTestDatabase _database = new();

    [Fact]
    public void Create_TrimsName_AndReturnsResponse()
    {
        var response = CreateService().Create(new CreateCollectionRequest { Name = "  Invoices  " });

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Invoices", response.Name);
        Assert.Equal(0, response.DocumentCount);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateService().Create(new CreateCollectionRequest { Name = "   " }));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void GetAll_ReturnsCollections_OrderedByName()
    {
        CreateService().Create(new CreateCollectionRequest { Name = "Zeta" });
        CreateService().Create(new CreateCollectionRequest { Name = "Alpha" });

        var collections = CreateService().GetAll();

        Assert.Equal(new[] { "Alpha", "Zeta" }, collections.Select(collection => collection.Name));
    }

    [Fact]
    public void GetById_ExistingCollection_ReturnsCollectionWithoutDocuments()
    {
        var id = SeedCollection("Invoices");

        var response = CreateService().GetById(id);

        Assert.NotNull(response);
        Assert.Equal("Invoices", response!.Name);
        Assert.Empty(response.Documents);
    }

    [Fact]
    public void GetById_ExistingCollection_ReturnsMappedDocuments()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument("invoice.pdf");
        CreateService().AddDocument(collectionId, documentId);

        var response = CreateService().GetById(collectionId);

        Assert.NotNull(response);
        var document = Assert.Single(response!.Documents);
        Assert.Equal("invoice.pdf", document.FileName);
    }

    [Fact]
    public void GetById_UnknownCollection_ReturnsNull()
    {
        Assert.Null(CreateService().GetById(Guid.NewGuid()));
    }

    [Fact]
    public void AddDocument_LinksDocument_ReturnsTrue()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument("invoice.pdf");

        Assert.True(CreateService().AddDocument(collectionId, documentId));

        var response = CreateService().GetById(collectionId);
        Assert.NotNull(response);
        Assert.Single(response!.Documents);
    }

    [Fact]
    public void AddDocument_UnknownDocument_ReturnsFalse()
    {
        var collectionId = SeedCollection("Invoices");

        Assert.False(CreateService().AddDocument(collectionId, Guid.NewGuid()));
    }

    [Fact]
    public void RemoveDocument_RemovesLink_ReturnsTrue()
    {
        var collectionId = SeedCollection("Invoices");
        var documentId = SeedDocument("invoice.pdf");
        CreateService().AddDocument(collectionId, documentId);

        Assert.True(CreateService().RemoveDocument(collectionId, documentId));

        var response = CreateService().GetById(collectionId);
        Assert.NotNull(response);
        Assert.Empty(response!.Documents);
    }

    [Fact]
    public void RemoveDocument_UnknownLink_ReturnsFalse()
    {
        var collectionId = SeedCollection("Invoices");

        Assert.False(CreateService().RemoveDocument(collectionId, Guid.NewGuid()));
    }

    private CollectionService CreateService() =>
        new(
            new CollectionRepository(_database.CreateContext()),
            new DocumentRepository(_database.CreateContext()),
            TestMapper.Create());

    private Guid SeedCollection(string name)
    {
        var collection = new Collection { Name = name };
        using var context = _database.CreateContext();
        new CollectionRepository(context).Add(collection);
        return collection.Id;
    }

    private Guid SeedDocument(string fileName)
    {
        var document = new Document
        {
            FileName = fileName,
            ContentType = "application/pdf",
            FileSize = 8,
            Content = "%PDF-1.4"u8.ToArray()
        };

        using var context = _database.CreateContext();
        new DocumentRepository(context).Add(document);
        return document.Id;
    }
}