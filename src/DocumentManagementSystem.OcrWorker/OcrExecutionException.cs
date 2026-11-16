namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrExecutionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
