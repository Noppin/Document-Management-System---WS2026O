using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.OcrWorker.Tests;

public sealed class OcrDocumentProcessorTests
{
    [Fact]
    public async Task ProcessAsync_LoadsPdfFromStorageAndRunsOcr()
    {
        var documentId = Guid.NewGuid();
        var storage = new FakeStorage("%PDF-test"u8.ToArray());
        var engine = new FakeOcrEngine("Hello World");
        var processor = new OcrDocumentProcessor(
            storage,
            engine,
            NullLogger<OcrDocumentProcessor>.Instance);

        var result = await processor.ProcessAsync(new DocumentUploadedMessage(
            Guid.NewGuid(),
            documentId,
            "HelloWorld.pdf",
            "application/pdf",
            9,
            DateTimeOffset.UtcNow));

        Assert.Equal("Hello World", result);
        Assert.Equal(documentId, storage.LastRequestedDocumentId);
        Assert.Equal("%PDF-test"u8.ToArray(), engine.LastPdf);
    }

    private sealed class FakeStorage(byte[] content) : IDocumentStorage
    {
        public Guid LastRequestedDocumentId { get; private set; }

        public Task<byte[]> GetAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            LastRequestedDocumentId = documentId;
            return Task.FromResult(content);
        }

        public Task StoreAsync(Guid documentId, Stream contentStream, long contentLength, string contentType,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeOcrEngine(string text) : IOcrEngine
    {
        public byte[]? LastPdf { get; private set; }

        public Task<string> RecognizeAsync(byte[] pdfContent, CancellationToken cancellationToken = default)
        {
            LastPdf = pdfContent;
            return Task.FromResult(text);
        }
    }
}
