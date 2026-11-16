namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public string Language { get; init; } = "eng";
    public int Dpi { get; init; } = 200;
}
