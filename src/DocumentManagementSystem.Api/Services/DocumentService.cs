using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;

namespace DocumentManagementSystem.Api.Services;

public sealed class DocumentService(IDocumentRepository documents) : IDocumentService
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    public async Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.File.Length == 0)
            throw new ArgumentException("The uploaded file must not be empty.", nameof(request));

        if (request.File.Length > MaxFileSize)
            throw new ArgumentException("The uploaded file exceeds the 20 MB limit.", nameof(request));

        if (!string.Equals(Path.GetExtension(request.File.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF documents are accepted.", nameof(request));

        if (!string.Equals(request.File.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The uploaded file must have content type application/pdf.", nameof(request));

        await using var stream = request.File.OpenReadStream();
        var header = new byte[5];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        if (bytesRead != header.Length || !header.SequenceEqual("%PDF-"u8.ToArray()))
            throw new ArgumentException("The uploaded file is not a valid PDF.", nameof(request));

        var document = new Document
        {
            FileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType,
            FileSize = request.File.Length
        };

        documents.Add(document);
        return ToResponse(document);
    }

    public IReadOnlyList<DocumentResponse> GetAll() =>
        documents.GetAll().Select(ToResponse).ToList();

    public DocumentResponse? Get(Guid id)
    {
        var document = documents.GetById(id);
        return document is null ? null : ToResponse(document);
    }

    public bool Delete(Guid id) => documents.Delete(id);

    private static DocumentResponse ToResponse(Document document) =>
        new(document.Id, document.FileName, document.ContentType, document.FileSize, document.CreatedAt);
}
