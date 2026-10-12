using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagementSystem.Persistence;

public static class DatabaseMigrationExtensions
{
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementDbContext>();
        dbContext.Database.Migrate();
    }
}
