namespace DocumentManagementSystem.OcrWorker;

public interface IOcrEngine
{
    Task<string> RecognizeAsync(
        byte[] pdfContent,
        CancellationToken cancellationToken = default);
}
