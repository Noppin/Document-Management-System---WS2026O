using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Storage;

namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrDocumentProcessor(
    IDocumentStorage storage,
    IOcrEngine ocrEngine,
    IOcrResultQueuePublisher resultPublisher,
    ILogger<OcrDocumentProcessor> logger) : IOcrDocumentProcessor
{
    public async Task<string> ProcessAsync(
        DocumentUploadedMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Fetching original PDF for document {DocumentId} from MinIO",
            message.DocumentId);

        var pdf = await storage.GetAsync(message.DocumentId, cancellationToken);

        logger.LogInformation(
            "Starting OCR for document {DocumentId} ({FileName}, {FileSize} bytes)",
            message.DocumentId,
            message.FileName,
            pdf.Length);

        var text = await ocrEngine.RecognizeAsync(pdf, cancellationToken);

        logger.LogInformation(
            "OCR finished for document {DocumentId}; recognized {CharacterCount} characters",
            message.DocumentId,
            text.Length);

        await resultPublisher.PublishAsync(
            new OcrCompletedMessage(
                MessageId: Guid.NewGuid(),
                DocumentId: message.DocumentId,
                FileName: message.FileName,
                Text: text,
                CompletedAt: DateTimeOffset.UtcNow),
            cancellationToken);

        return text;
    }
}
