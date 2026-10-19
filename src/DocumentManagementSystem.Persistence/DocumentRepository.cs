using DocumentManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentManagementSystem.Persistence;

public sealed class DocumentRepository(
    DocumentManagementDbContext dbContext,
    ILogger<DocumentRepository>? logger = null) : IDocumentRepository
{
    private readonly ILogger<DocumentRepository> _logger =
        logger ?? NullLogger<DocumentRepository>.Instance;

    public void Add(Document document)
    {
        dbContext.Documents.Add(document);
        dbContext.SaveChanges();

        _logger.LogInformation(
            "Stored document {DocumentId} ({FileName}) in the database",
            document.Id,
            document.FileName);
    }

    public IReadOnlyList<Document> GetAll() => dbContext.Documents
        .AsNoTracking()
        .OrderByDescending(document => document.CreatedAt)
        .ToList();

    public Document? GetById(Guid id) => dbContext.Documents
        .AsNoTracking()
        .SingleOrDefault(document => document.Id == id);

    public bool Delete(Guid id)
    {
        var document = dbContext.Documents.Find(id);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} does not exist and cannot be deleted", id);
            return false;
        }

        dbContext.Documents.Remove(document);
        dbContext.SaveChanges();

        _logger.LogInformation("Deleted document {DocumentId} ({FileName}) from the database", id, document.FileName);
        return true;
    }
}
