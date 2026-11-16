using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Storage;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Api;

/// <summary>Resumable backfill: never discard a legacy PDF before verifying the MinIO copy.</summary>
public sealed class LegacyDocumentContentMigration(
    DocumentManagementDbContext database,
    IDocumentStorage storage,
    ILogger<LegacyDocumentContentMigration> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (await database.Documents.FirstOrDefaultAsync(document => document.Content != null,
                   cancellationToken) is { } document)
        {
            var content = document.Content!;
            await using var stream = new MemoryStream(content, writable: false);
            await storage.StoreAsync(document.Id, stream, content.Length, document.ContentType, cancellationToken);
            var stored = await storage.GetAsync(document.Id, cancellationToken);
            if (!content.AsSpan().SequenceEqual(stored))
                throw new DocumentStorageException($"MinIO verification failed for legacy document {document.Id}.",
                    new InvalidDataException("The stored PDF differs from the legacy content."));

            document.Content = null;
            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Migrated legacy PDF {DocumentId} to MinIO", document.Id);
            database.ChangeTracker.Clear();
        }
    }
}
