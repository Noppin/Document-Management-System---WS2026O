namespace DocumentManagementSystem.Storage;

public class DocumentStorageException(string message, Exception innerException)
    : Exception(message, innerException);
