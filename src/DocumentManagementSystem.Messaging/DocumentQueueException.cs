namespace DocumentManagementSystem.Messaging;

public sealed class DocumentQueueException(string message, Exception innerException)
    : Exception(message, innerException);
