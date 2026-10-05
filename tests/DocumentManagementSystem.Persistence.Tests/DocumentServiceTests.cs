using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class DocumentServiceTests
{
    private readonly InMemoryTestDatabase _database = new();

    [Fact]
    public async Task UploadAsync_ValidPdf_StoresDocumentAndReturnsResponse()
    {
        var service = CreateService();

        var response = await service.UploadAsync(CreateUploadRequest("invoice.pdf"));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("invoice.pdf", response.FileName);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Equal(8, response.FileSize);

        using var context = _database.CreateContext();
        Assert.Single(new DocumentRepository(context).GetAll());
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(CreateUploadRequest("empty.pdf", Array.Empty<byte>())));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task UploadAsync_NonPdfExtension_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(CreateUploadRequest("notes.txt")));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task UploadAsync_WrongContentType_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf", "%PDF-1.4"u8.ToArray(), "text/plain")));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task UploadAsync_MissingPdfHeader_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf", "not-a-pdf"u8.ToArray())));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task GetAll_ReturnsDocuments_OrderedByCreatedAtDescending()
    {
        var older = SeedDocument("older.pdf", DateTimeOffset.UtcNow.AddMinutes(-5));
        var newer = SeedDocument("newer.pdf", DateTimeOffset.UtcNow);

        var documents = CreateService().GetAll();

        Assert.Equal(new[] { newer, older }, documents.Select(document => document.Id));
    }

    [Fact]
    public void Get_ExistingDocument_ReturnsResponse()
    {
        var id = SeedDocument("invoice.pdf");

        var response = CreateService().Get(id);

        Assert.NotNull(response);
        Assert.Equal("invoice.pdf", response!.FileName);
    }

    [Fact]
    public void Get_UnknownDocument_ReturnsNull()
    {
        Assert.Null(CreateService().Get(Guid.NewGuid()));
    }

    [Fact]
    public void Delete_ExistingDocument_ReturnsTrue()
    {
        var id = SeedDocument("invoice.pdf");

        Assert.True(CreateService().Delete(id));
        Assert.Null(CreateService().Get(id));
    }

    [Fact]
    public void Delete_UnknownDocument_ReturnsFalse()
    {
        Assert.False(CreateService().Delete(Guid.NewGuid()));
    }

    private DocumentService CreateService() =>
        new(new DocumentRepository(_database.CreateContext()), TestMapper.Create());

    private Guid SeedDocument(string fileName, DateTimeOffset? createdAt = null)
    {
        var document = new Document
        {
            FileName = fileName,
            ContentType = "application/pdf",
            FileSize = 8,
            Content = "%PDF-1.4"u8.ToArray(),
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };

        using var context = _database.CreateContext();
        new DocumentRepository(context).Add(document);
        return document.Id;
    }

    private static UploadDocumentRequest CreateUploadRequest(
        string fileName,
        byte[]? content = null,
        string contentType = "application/pdf")
    {
        var bytes = content ?? "%PDF-1.4"u8.ToArray();
        return new UploadDocumentRequest
        {
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            }
        };
    }
}