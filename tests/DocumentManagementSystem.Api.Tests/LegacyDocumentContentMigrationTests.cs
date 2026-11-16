// Mit AI geschrieben.
using DocumentManagementSystem.Api;
using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Persistence.Entities;
using DocumentManagementSystem.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Api.Tests;

public sealed class LegacyDocumentContentMigrationTests
{
    [Fact]
    public async Task Migration_VerifiesStoredBytesBeforeClearingLegacyContentAndIsResumable()
    {
        await using var database = CreateDatabase();
        var document = LegacyDocument();
        database.Documents.Add(document);
        await database.SaveChangesAsync();
        var storage = new RecordingStorage();
        var migration = new LegacyDocumentContentMigration(database, storage,
            NullLogger<LegacyDocumentContentMigration>.Instance);

        await migration.RunAsync();
        await migration.RunAsync();

        Assert.Null((await database.Documents.SingleAsync()).Content);
        Assert.Equal("%PDF-legacy"u8.ToArray(), storage.Content);
        Assert.Equal(1, storage.Writes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Migration_FailedStorageOrVerificationPreservesOriginal(bool failWrite)
    {
        await using var database = CreateDatabase();
        database.Documents.Add(LegacyDocument());
        await database.SaveChangesAsync();
        var storage = new RecordingStorage { FailWrite = failWrite, CorruptRead = !failWrite };
        var migration = new LegacyDocumentContentMigration(database, storage,
            NullLogger<LegacyDocumentContentMigration>.Instance);

        await Assert.ThrowsAsync<DocumentStorageException>(() => migration.RunAsync());
        database.ChangeTracker.Clear();
        Assert.Equal("%PDF-legacy"u8.ToArray(), (await database.Documents.SingleAsync()).Content);
    }

    private static DocumentManagementDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<DocumentManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DocumentEntity LegacyDocument() => new()
    {
        FileName = "legacy.pdf", ContentType = "application/pdf", FileSize = 11,
        Content = "%PDF-legacy"u8.ToArray()
    };

    private sealed class RecordingStorage : IDocumentStorage
    {
        public byte[] Content { get; private set; } = [];
        public int Writes { get; private set; }
        public bool FailWrite { get; init; }
        public bool CorruptRead { get; init; }

        public async Task StoreAsync(Guid documentId, Stream content, long contentLength, string contentType,
            CancellationToken cancellationToken = default)
        {
            if (FailWrite) throw new DocumentStorageException("Storage unavailable", new IOException());
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Content = buffer.ToArray();
            Writes++;
        }

        public Task<byte[]> GetAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CorruptRead ? Array.Empty<byte>() : Content);

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
