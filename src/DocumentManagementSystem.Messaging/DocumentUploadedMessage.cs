namespace DocumentManagementSystem.Messaging;

public sealed record DocumentUploadedMessage(
    Guid MessageId,
    Guid DocumentId,
    string FileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAt);
