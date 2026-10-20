using System.Text.Json;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.OcrWorker;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.OcrWorker.Tests;

public sealed class OcrMessageProcessorTests
{
    [Fact]
    public void Process_ValidMessage_ReturnsDocumentMessage()
    {
        var expected = new DocumentUploadedMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hello.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        var body = JsonSerializer.SerializeToUtf8Bytes(expected);
        var processor = new OcrMessageProcessor(NullLogger<OcrMessageProcessor>.Instance);

        var actual = processor.Process(body);

        Assert.Equal(expected.DocumentId, actual.DocumentId);
        Assert.Equal(expected.FileName, actual.FileName);
    }

    [Fact]
    public void Process_InvalidJson_ThrowsLayerSpecificException()
    {
        var processor = new OcrMessageProcessor(NullLogger<OcrMessageProcessor>.Instance);

        Assert.Throws<OcrMessageProcessingException>(() => processor.Process("not-json"u8.ToArray()));
    }

    [Fact]
    public void Process_UnsupportedSchemaVersion_ThrowsLayerSpecificException()
    {
        var message = new DocumentUploadedMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hello.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow)
        {
            SchemaVersion = DocumentUploadedMessage.CurrentSchemaVersion + 1
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var processor = new OcrMessageProcessor(NullLogger<OcrMessageProcessor>.Instance);

        Assert.Throws<OcrMessageProcessingException>(() => processor.Process(body));
    }

    [Fact]
    public void Process_MissingDocumentId_ThrowsLayerSpecificException()
    {
        var message = new DocumentUploadedMessage(
            Guid.NewGuid(),
            Guid.Empty,
            "hello.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var processor = new OcrMessageProcessor(NullLogger<OcrMessageProcessor>.Instance);

        Assert.Throws<OcrMessageProcessingException>(() => processor.Process(body));
    }
}
