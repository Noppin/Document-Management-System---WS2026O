using DocumentManagementSystem.Messaging;

namespace DocumentManagementSystem.OcrWorker;

public interface IOcrDocumentProcessor
{
    Task<string> ProcessAsync(
        DocumentUploadedMessage message,
        CancellationToken cancellationToken = default);
}
