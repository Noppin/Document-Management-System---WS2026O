using AutoMapper;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using DocumentManagementSystem.Storage;
using DocumentManagementSystem.Search;

namespace DocumentManagementSystem.Application.Services;

public sealed class DocumentService(
    IDocumentRepository documents,
    IMapper mapper,
    IValidator<UploadDocumentRequest> validator,
    IDocumentQueuePublisher queuePublisher,
    IDocumentStorage storage,
    IDocumentSearchIndex searchIndex,
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
            Status = DocumentStatus.Queued
        };

        await using var storageStream = new MemoryStream(content, writable: false);
        await storage.StoreAsync(
            document.Id,
            storageStream,
            document.FileSize,
            document.ContentType,
            cancellationToken);

        try
        {
            documents.Add(document);

            logger.LogInformation(
                "Persisted uploaded document {DocumentId} ({FileName}, {FileSize} bytes) and stored its PDF in MinIO",
                document.Id,
                document.FileName,
                document.FileSize);

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
        catch
        {
            // Cleanup must still run when the original request has been cancelled. Keep the
            // original failure, even if one cleanup step fails, and attempt both resources.
            try
            {
                documents.Delete(document.Id);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(cleanupException, "Failed to remove metadata for document {DocumentId} during rollback", document.Id);
            }

            try
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await storage.DeleteAsync(document.Id, cleanupTimeout.Token);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Failed to remove MinIO object for document {DocumentId} during upload rollback",
                    document.Id);
            }

            logger.LogWarning(
                "Attempted rollback for document {DocumentId} after the upload pipeline failed; check cleanup errors",
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

    public async Task<DocumentDownloadResponse?> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = documents.GetById(id);
        if (document is null)
        {
            return null;
        }

        var content = await storage.GetAsync(id, cancellationToken);
        return new DocumentDownloadResponse(content, document.ContentType, document.FileName);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = documents.GetById(id);
        if (document is null)
        {
            return false;
        }

        await storage.DeleteAsync(id, cancellationToken);
        var deleted = documents.Delete(id);

        try
        {
            await searchIndex.DeleteAsync(id, cancellationToken);
        }
        catch (SearchIndexException exception)
        {
            // The document itself is gone; a stale search entry must not fail the delete.
            logger.LogWarning(
                exception,
                "Could not remove document {DocumentId} from the search index",
                id);
        }

        return deleted;
    }
}
