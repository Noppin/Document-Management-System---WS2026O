using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class InMemoryTestDatabase
{
    private readonly DbContextOptions<DocumentManagementDbContext> _options;

    public InMemoryTestDatabase()
    {
        _options = new DbContextOptionsBuilder<DocumentManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public DocumentManagementDbContext CreateContext() => new(_options);
}
