// Mit AI geschrieben.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocumentManagementSystem.Persistence.Tests;

public sealed class MinioMigrationSchemaTests
{
    [Fact]
    public void ModelAndSnapshotAgreeAndMigrationPreservesLegacyData()
    {
        using var database = new DocumentManagementDbContext(new DbContextOptionsBuilder<DocumentManagementDbContext>()
            .UseNpgsql("Host=localhost;Database=schema_validation").Options);
        Assert.False(database.Database.HasPendingModelChanges());
        var sql = database.GetService<IMigrator>().GenerateScript();
        Assert.Contains("ALTER TABLE documents ALTER COLUMN \"Content\" DROP NOT NULL", sql);
        Assert.DoesNotContain("DROP COLUMN", sql);
    }
}
