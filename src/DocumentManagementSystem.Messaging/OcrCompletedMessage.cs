namespace DocumentManagementSystem.Messaging;

public sealed record OcrCompletedMessage(
    Guid MessageId,
    Guid DocumentId,
    string FileName,
    string Text,
    DateTimeOffset CompletedAt)
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
