namespace DocumentManagementSystem.Search;

public sealed class SearchIndexException(string message, Exception? innerException = null)
    : Exception(message, innerException);
