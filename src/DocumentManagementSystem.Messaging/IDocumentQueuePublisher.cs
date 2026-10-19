namespace DocumentManagementSystem.Messaging;

public interface IDocumentQueuePublisher
{
    Task PublishAsync(DocumentUploadedMessage message, CancellationToken cancellationToken = default);
}
