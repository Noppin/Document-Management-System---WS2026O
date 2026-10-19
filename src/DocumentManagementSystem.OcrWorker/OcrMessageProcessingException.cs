namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrMessageProcessingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
