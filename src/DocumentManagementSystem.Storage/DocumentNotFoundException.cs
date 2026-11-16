namespace DocumentManagementSystem.Storage;

/// <summary>
/// The requested document object does not exist in MinIO. Retrying will not help.
/// </summary>
public sealed class DocumentNotFoundException(string message, Exception innerException)
    : DocumentStorageException(message, innerException);
