using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence.Entities;
using DocumentManagementSystem.TestHelpers;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class DocumentRepositoryTests
{
    private readonly InMemoryTestDatabase _database = new();

    [Fact]
    public void Add_PersistsDocument_AndGetByIdReturnsIt()
    {
        var document = CreateDocument();

        using (var context = _database.CreateContext())
        {
            new DocumentRepository(context, TestMapper.Create()).Add(document);
        }

        using var readContext = _database.CreateContext();
        var loaded = new DocumentRepository(readContext, TestMapper.Create()).GetById(document.Id);

        Assert.NotNull(loaded);
        Assert.Equal(document.Id, loaded!.Id);
        Assert.Equal(document.FileName, loaded.FileName);
        Assert.Equal(document.ContentType, loaded.ContentType);
        Assert.Equal(document.FileSize, loaded.FileSize);
        Assert.Equal(document.Content, loaded.Content);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        using var context = _database.CreateContext();

        Assert.Null(new DocumentRepository(context, TestMapper.Create()).GetById(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsAllDocuments_OrderedByCreatedAtDescending()
    {
        var older = CreateDocument(DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = CreateDocument(DateTimeOffset.UtcNow);

        using (var context = _database.CreateContext())
        {
            var repository = new DocumentRepository(context, TestMapper.Create());
            repository.Add(older);
            repository.Add(newer);
        }

        using var readContext = _database.CreateContext();
        var documents = new DocumentRepository(readContext, TestMapper.Create()).GetAll();

        Assert.Equal(new[] { newer.Id, older.Id }, documents.Select(document => document.Id));
    }

    [Fact]
    public void Delete_ExistingDocument_ReturnsTrueAndRemovesIt()
    {
        var document = CreateDocument();

        using (var context = _database.CreateContext())
        {
            new DocumentRepository(context, TestMapper.Create()).Add(document);
        }

        using (var context = _database.CreateContext())
        {
            Assert.True(new DocumentRepository(context, TestMapper.Create()).Delete(document.Id));
        }

        using var readContext = _database.CreateContext();
        Assert.Null(new DocumentRepository(readContext, TestMapper.Create()).GetById(document.Id));
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        using var context = _database.CreateContext();

        Assert.False(new DocumentRepository(context, TestMapper.Create()).Delete(Guid.NewGuid()));
    }

    [Fact]
    public void Add_TracksOnlyPersistenceEntity_NotBusinessModel()
    {
        using var context = _database.CreateContext();
        var document = CreateDocument();

        new DocumentRepository(context, TestMapper.Create()).Add(document);

        var tracked = Assert.Single(context.ChangeTracker.Entries());
        var entity = Assert.IsType<DocumentEntity>(tracked.Entity);
        Assert.Equal(document.Id, entity.Id);
        Assert.NotSame(document, tracked.Entity);
    }

    [Fact]
    public void GetById_ReturnsIndependentBusinessModels_WithoutTrackingEntities()
    {
        var document = CreateDocument();
        using (var context = _database.CreateContext())
        {
            new DocumentRepository(context, TestMapper.Create()).Add(document);
        }
        using var readContext = _database.CreateContext();
        var repository = new DocumentRepository(readContext, TestMapper.Create());

        var first = repository.GetById(document.Id);
        var second = repository.GetById(document.Id);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(first.Id, second.Id);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    private static Document CreateDocument(DateTimeOffset? createdAt = null) => new()
    {
        FileName = "sample.pdf",
        ContentType = "application/pdf",
        FileSize = 12,
        Content = "%PDF-1.4"u8.ToArray(),
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow
    };
}
