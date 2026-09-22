using DocumentManagementSystem.Domain.Entities;

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
            new DocumentRepository(context).Add(document);
        }

        using var readContext = _database.CreateContext();
        var loaded = new DocumentRepository(readContext).GetById(document.Id);

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

        Assert.Null(new DocumentRepository(context).GetById(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsAllDocuments_OrderedByCreatedAtDescending()
    {
        var older = CreateDocument(DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = CreateDocument(DateTimeOffset.UtcNow);

        using (var context = _database.CreateContext())
        {
            var repository = new DocumentRepository(context);
            repository.Add(older);
            repository.Add(newer);
        }

        using var readContext = _database.CreateContext();
        var documents = new DocumentRepository(readContext).GetAll();

        Assert.Equal(new[] { newer.Id, older.Id }, documents.Select(document => document.Id));
    }

    [Fact]
    public void Delete_ExistingDocument_ReturnsTrueAndRemovesIt()
    {
        var document = CreateDocument();

        using (var context = _database.CreateContext())
        {
            new DocumentRepository(context).Add(document);
        }

        using (var context = _database.CreateContext())
        {
            Assert.True(new DocumentRepository(context).Delete(document.Id));
        }

        using var readContext = _database.CreateContext();
        Assert.Null(new DocumentRepository(readContext).GetById(document.Id));
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        using var context = _database.CreateContext();

        Assert.False(new DocumentRepository(context).Delete(Guid.NewGuid()));
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
