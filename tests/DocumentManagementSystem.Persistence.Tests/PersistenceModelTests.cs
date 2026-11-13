using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence.Entities;
using DocumentManagementSystem.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_UsesPersistenceEntities_NotDomainModels()
    {
        using var context = new InMemoryTestDatabase().CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(DocumentEntity)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CollectionEntity)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CollectionDocumentEntity)));
        Assert.Null(context.Model.FindEntityType(typeof(Document)));
        Assert.Null(context.Model.FindEntityType(typeof(Collection)));
    }

    [Fact]
    public void Model_MatchesMigrationSnapshot()
    {
        var options = new DbContextOptionsBuilder<DocumentManagementDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new DocumentManagementDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        TestMapper.Create().ConfigurationProvider.AssertConfigurationIsValid();
    }
}
