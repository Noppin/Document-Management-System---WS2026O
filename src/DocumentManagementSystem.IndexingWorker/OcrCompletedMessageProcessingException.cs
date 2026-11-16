namespace DocumentManagementSystem.IndexingWorker;

public sealed class OcrCompletedMessageProcessingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
