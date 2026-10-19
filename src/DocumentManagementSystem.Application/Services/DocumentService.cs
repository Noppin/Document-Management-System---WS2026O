using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Persistence;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services;

public sealed class DocumentService(
    IDocumentRepository documents,
    IMapper mapper,
    IValidator<UploadDocumentRequest> validator,
    IDocumentQueuePublisher queuePublisher,
    ILogger<DocumentService> logger) : IDocumentService
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

        logger.LogInformation(
            "Persisted uploaded document {DocumentId} ({FileName}, {FileSize} bytes) for OCR processing",
            document.Id,
            document.FileName,
            document.FileSize);

        try
        {
            await queuePublisher.PublishAsync(
                new DocumentUploadedMessage(
                    MessageId: Guid.NewGuid(),
                    DocumentId: document.Id,
                    FileName: document.FileName,
                    ContentType: document.ContentType,
                    FileSize: document.FileSize,
                    UploadedAt: document.CreatedAt),
                cancellationToken);
        }
        catch (DocumentQueueException)
        {
            documents.Delete(document.Id);

            logger.LogWarning(
                "Removed document {DocumentId} because the OCR queue publish failed",
                document.Id);

            throw;
        }

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
