namespace DocumentManagementSystem.Messaging;

public interface IOcrResultQueuePublisher
{
    Task PublishAsync(
        OcrCompletedMessage message,
        CancellationToken cancellationToken = default);
}
