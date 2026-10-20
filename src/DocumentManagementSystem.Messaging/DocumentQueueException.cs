namespace DocumentManagementSystem.Messaging;

public sealed class DocumentQueueException(string message, Exception? innerException = null)
    : Exception(message, innerException);
