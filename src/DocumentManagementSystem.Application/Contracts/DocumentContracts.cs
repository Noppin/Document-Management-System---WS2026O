using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Application.Contracts;

public sealed class UploadDocumentRequest
{
    public required IFormFile File { get; init; }
}

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    DateTimeOffset CreatedAt);
