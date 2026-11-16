// Mit AI geschrieben (Testergänzungen und -anpassungen).
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Validation;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.TestHelpers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using DocumentManagementSystem.Storage;
using DocumentManagementSystem.Search;

namespace DocumentManagementSystem.Application.Tests;

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
        Assert.Single(new DocumentRepository(context, TestMapper.Create()).GetAll());
        Assert.Null(context.Documents.Single().Content);
    }


    [Fact]
    public async Task UploadAsync_ValidPdf_StoresOriginalPdfInMinioAbstraction()
    {
        var storage = new InMemoryDocumentStorage();
        var service = CreateService(storage: storage);

        var response = await service.UploadAsync(CreateUploadRequest("invoice.pdf"));

        Assert.True(storage.Documents.TryGetValue(response.Id, out var stored));
        Assert.Equal("%PDF-1.4"u8.ToArray(), stored);
    }

    [Fact]
    public async Task UploadAsync_ValidPdf_PublishesOcrQueueMessage()
    {
        var queue = new RecordingDocumentQueuePublisher();
        var service = CreateService(queue);

        var response = await service.UploadAsync(CreateUploadRequest("invoice.pdf"));

        var message = Assert.Single(queue.Messages);
        Assert.Equal(response.Id, message.DocumentId);
        Assert.Equal("invoice.pdf", message.FileName);
        Assert.Equal("application/pdf", message.ContentType);
        Assert.Equal(8, message.FileSize);
        Assert.NotEqual(Guid.Empty, message.MessageId);
    }

    [Fact]
    public async Task UploadAsync_QueuePublishFails_ThrowsDocumentQueueException()
    {
        var service = CreateService(new ThrowingDocumentQueuePublisher());

        await Assert.ThrowsAsync<DocumentQueueException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf")));
    }

    [Fact]
    public async Task UploadAsync_QueuePublishFails_DoesNotPersistDocument()
    {
        var service = CreateService(new ThrowingDocumentQueuePublisher());

        await Assert.ThrowsAsync<DocumentQueueException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf")));

        using var context = _database.CreateContext();
        Assert.Empty(new DocumentRepository(context, TestMapper.Create()).GetAll());
    }

    [Fact]
    public async Task UploadAsync_QueuePublishFails_RemovesMinioObject()
    {
        var storage = new InMemoryDocumentStorage();
        var service = CreateService(new ThrowingDocumentQueuePublisher(), storage);

        await Assert.ThrowsAsync<DocumentQueueException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf")));

        Assert.Empty(storage.Documents);
    }

    [Fact]
    public async Task UploadAsync_StorageFails_DoesNotPersistOrPublish()
    {
        var queue = new RecordingDocumentQueuePublisher();
        var storage = new InMemoryDocumentStorage { FailStore = true };
        await Assert.ThrowsAsync<DocumentStorageException>(() =>
            CreateService(queue, storage).UploadAsync(CreateUploadRequest("a.pdf")));
        Assert.Empty(queue.Messages);
        using var context = _database.CreateContext();
        Assert.Empty(context.Documents);
    }

    [Fact]
    public async Task UploadAsync_RollbackStorageFails_PreservesOriginalFailureAndRemovesMetadata()
    {
        var storage = new InMemoryDocumentStorage { FailDelete = true };
        await Assert.ThrowsAsync<DocumentQueueException>(() =>
            CreateService(new ThrowingDocumentQueuePublisher(), storage).UploadAsync(CreateUploadRequest("a.pdf")));
        using var context = _database.CreateContext();
        Assert.Empty(context.Documents);
        // Failed cleanup is logged, not falsely reported as a successful rollback.
        Assert.Single(storage.Documents);
    }

    [Fact]
    public async Task Download_StorageFails_PropagatesStorageException()
    {
        var id = SeedDocument("a.pdf");
        await Assert.ThrowsAsync<DocumentStorageException>(() =>
            CreateService(storage: new InMemoryDocumentStorage { FailRead = true }).GetDownloadAsync(id));
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadAsync(CreateUploadRequest("empty.pdf", Array.Empty<byte>())));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == "The uploaded file must not be empty.");
    }

    [Fact]
    public async Task UploadAsync_NonPdfExtension_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadAsync(CreateUploadRequest("notes.txt")));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == "Only PDF documents are accepted.");
    }

    [Fact]
    public async Task UploadAsync_WrongContentType_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf", "%PDF-1.4"u8.ToArray(), "text/plain")));

        Assert.Contains(exception.Errors,
            error => error.ErrorMessage == "The uploaded file must have content type application/pdf.");
    }

    [Fact]
    public async Task UploadAsync_MissingPdfHeader_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UploadAsync(CreateUploadRequest("invoice.pdf", "not-a-pdf"u8.ToArray())));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == "The uploaded file is not a valid PDF.");
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
    public async Task Delete_ExistingDocument_ReturnsTrue()
    {
        var id = SeedDocument("invoice.pdf");

        var storage = new InMemoryDocumentStorage();
        storage.Documents[id] = "%PDF-1.4"u8.ToArray();
        Assert.True(await CreateService(storage: storage).DeleteAsync(id));
        Assert.Null(CreateService().Get(id));
    }

    [Fact]
    public async Task Delete_ExistingDocument_RemovesItFromSearchIndex()
    {
        var id = SeedDocument("a.pdf");
        var searchIndex = new RecordingSearchIndex();

        Assert.True(await CreateService(searchIndex: searchIndex).DeleteAsync(id));

        Assert.Equal([id], searchIndex.DeletedIds);
    }

    [Fact]
    public async Task Delete_SearchIndexUnavailable_StillDeletesDocument()
    {
        var id = SeedDocument("a.pdf");

        Assert.True(await CreateService(searchIndex: new RecordingSearchIndex(failOnDelete: true)).DeleteAsync(id));
        Assert.Null(CreateService().Get(id));
    }

    [Fact]
    public async Task Delete_UnknownDocument_ReturnsFalse()
    {
        Assert.False(await CreateService().DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDownload_ExistingDocument_ReturnsStoredContent()
    {
        var storage = new InMemoryDocumentStorage();
        var id = SeedDocument("invoice.pdf");
        storage.Documents[id] = "%PDF-1.4"u8.ToArray();
        var service = CreateService(storage: storage);

        var download = await service.GetDownloadAsync(id);

        Assert.NotNull(download);
        Assert.Equal("application/pdf", download!.ContentType);
        Assert.Equal("invoice.pdf", download.FileName);
        Assert.Equal("%PDF-1.4"u8.ToArray(), download.Content);
    }

    [Fact]
    public async Task GetDownload_UnknownDocument_ReturnsNull()
    {
        var service = CreateService();

        Assert.Null(await service.GetDownloadAsync(Guid.NewGuid()));
    }

    private DocumentService CreateService(
        IDocumentQueuePublisher? queuePublisher = null,
        IDocumentStorage? storage = null,
        IDocumentSearchIndex? searchIndex = null) =>
        new(new DocumentRepository(_database.CreateContext(), TestMapper.Create()), TestMapper.Create(),
            new UploadDocumentRequestValidator(), queuePublisher ?? new RecordingDocumentQueuePublisher(),
            storage ?? new InMemoryDocumentStorage(),
            searchIndex ?? new RecordingSearchIndex(),
            NullLogger<DocumentService>.Instance);

    private Guid SeedDocument(string fileName, DateTimeOffset? createdAt = null)
    {
        var document = new Document
        {
            FileName = fileName,
            ContentType = "application/pdf",
            FileSize = 8,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };

        using var context = _database.CreateContext();
        new DocumentRepository(context, TestMapper.Create()).Add(document);
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

    private sealed class InMemoryDocumentStorage : IDocumentStorage
    {
        public Dictionary<Guid, byte[]> Documents { get; } = [];
        public bool FailStore { get; init; }
        public bool FailRead { get; init; }
        public bool FailDelete { get; init; }

        public async Task StoreAsync(
            Guid documentId,
            Stream content,
            long contentLength,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (FailStore) throw new DocumentStorageException("Storage unavailable", new IOException());
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Documents[documentId] = buffer.ToArray();
        }

        public Task<byte[]> GetAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            FailRead ? Task.FromException<byte[]>(new DocumentStorageException("Read failed", new IOException()))
                : Task.FromResult(Documents[documentId]);

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            if (FailDelete) throw new DocumentStorageException("Cleanup failed", new IOException());
            Documents.Remove(documentId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSearchIndex(bool failOnDelete = false) : IDocumentSearchIndex
    {
        public List<Guid> DeletedIds { get; } = [];

        public Task IndexAsync(SearchDocument document, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            if (failOnDelete)
            {
                throw new SearchIndexException("Elasticsearch is unavailable.");
            }

            DeletedIds.Add(documentId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchDocument>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SearchDocument>>([]);
    }

    private sealed class RecordingDocumentQueuePublisher : IDocumentQueuePublisher
    {
        public List<DocumentUploadedMessage> Messages { get; } = [];

        public Task PublishAsync(
            DocumentUploadedMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDocumentQueuePublisher : IDocumentQueuePublisher
    {
        public Task PublishAsync(
            DocumentUploadedMessage message,
            CancellationToken cancellationToken = default) =>
            throw new DocumentQueueException(
                $"Could not enqueue document {message.DocumentId} for OCR processing.",
                new InvalidOperationException("RabbitMQ is unavailable."));
    }
}
