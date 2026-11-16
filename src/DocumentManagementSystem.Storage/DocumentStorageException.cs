namespace DocumentManagementSystem.Storage;

public sealed class DocumentStorageException(string message, Exception innerException)
    : Exception(message, innerException);
