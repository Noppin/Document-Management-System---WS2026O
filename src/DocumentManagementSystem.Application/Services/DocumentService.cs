using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;
using FluentValidation;

namespace DocumentManagementSystem.Application.Services;

public sealed class DocumentService(IDocumentRepository documents, IMapper mapper, IValidator<UploadDocumentRequest> validator) : IDocumentService
{
    public async Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);
        var content = stream.ToArray();

        var document = new Document
        {
            FileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType,
            FileSize = request.File.Length,
            Content = content
        };

        documents.Add(document);
        return mapper.Map<DocumentResponse>(document);
    }

    public IReadOnlyList<DocumentResponse> GetAll() =>
        documents.GetAll().Select(document => mapper.Map<DocumentResponse>(document)).ToList();

    public DocumentResponse? Get(Guid id)
    {
        var document = documents.GetById(id);
        return document is null ? null : mapper.Map<DocumentResponse>(document);
    }

    public DocumentDownloadResponse? GetDownload(Guid id)
    {
        var document = documents.GetById(id);
        return document is null
            ? null
            : new DocumentDownloadResponse(document.Content, document.ContentType, document.FileName);
    }

    public bool Delete(Guid id) => documents.Delete(id);
}
