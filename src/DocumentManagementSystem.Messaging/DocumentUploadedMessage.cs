namespace DocumentManagementSystem.Messaging;

public sealed record DocumentUploadedMessage(
    Guid MessageId,
    Guid DocumentId,
    string FileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAt)
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
