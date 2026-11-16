// Mit AI geschrieben (Testergänzungen).
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
        var publisher = new RecordingOcrResultPublisher();
        var processor = new OcrDocumentProcessor(
            storage,
            engine,
            publisher,
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
        var published = Assert.Single(publisher.Messages);
        Assert.Equal(documentId, published.DocumentId);
        Assert.Equal("HelloWorld.pdf", published.FileName);
        Assert.Equal("Hello World", published.Text);
    }


    [Fact]
    public async Task ProcessAsync_PublishingFails_PropagatesFailureInsteadOfReportingSuccess()
    {
        var processor = new OcrDocumentProcessor(new FakeStorage("%PDF-test"u8.ToArray()),
            new FakeOcrEngine("Hello"), new FailingPublisher(), NullLogger<OcrDocumentProcessor>.Instance);

        await Assert.ThrowsAsync<DocumentQueueException>(() => processor.ProcessAsync(new DocumentUploadedMessage(
            Guid.NewGuid(), Guid.NewGuid(), "a.pdf", "application/pdf", 9, DateTimeOffset.UtcNow)));
    }

    [Fact]
    public async Task ProcessAsync_OcrFails_DoesNotPublishResult()
    {
        var publisher = new RecordingOcrResultPublisher();
        var processor = new OcrDocumentProcessor(new FakeStorage("%PDF-test"u8.ToArray()),
            new FailingEngine(), publisher, NullLogger<OcrDocumentProcessor>.Instance);

        await Assert.ThrowsAsync<OcrExecutionException>(() => processor.ProcessAsync(new DocumentUploadedMessage(
            Guid.NewGuid(), Guid.NewGuid(), "a.pdf", "application/pdf", 9, DateTimeOffset.UtcNow)));
        Assert.Empty(publisher.Messages);
    }

    private sealed class FailingPublisher : IOcrResultQueuePublisher
    {
        public Task PublishAsync(OcrCompletedMessage message, CancellationToken cancellationToken = default) =>
            throw new DocumentQueueException("Publish failed", new IOException());
    }

    private sealed class FailingEngine : IOcrEngine
    {
        public Task<string> RecognizeAsync(byte[] pdfContent, CancellationToken cancellationToken = default) =>
            throw new OcrExecutionException("Invalid PDF");
    }

    private sealed class RecordingOcrResultPublisher : IOcrResultQueuePublisher
    {
        public List<OcrCompletedMessage> Messages { get; } = [];

        public Task PublishAsync(
            OcrCompletedMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
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
